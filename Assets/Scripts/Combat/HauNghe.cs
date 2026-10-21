using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Bo Hau Nghe (`HauNgheKit`), gan vao `Hero` khi `HeroDefinition.hauNghe` co:
    ///   - Nhat Tien (don thuong): dan ten do `HeroDefinition.projectileFrames` ban — khong nam o day.
    ///   - Thien Nhan Xa Nhat (passiveStar): ten trung MUC TIEU DANG NHAM tich Nhat An (toi da `sealMax`; het
    ///     `sealSeconds` khong trung lai, doi muc tieu hoac muc tieu chet thi mat het). Moi an dang co cong
    ///     `sealDamagePerStack` sat thuong len chinh muc tieu do — tru danh thuong (cong thang vao don vua trung) lan
    ///     chieu phu. Du `sealMax` an thi chieu phu KE TIEP chay dang Thien Cung (bien doi rieng tung chieu) roi tieu an.
    ///   - Nhat Luan Tran Tuyen (minorStar): ten ghim xuong dat canh muc tieu, bao truoc `luanTelegraph` giay roi mo vung
    ///     `luanRadius` trong `luanDuration` giay: dich trong vung bi cham, o lai het nhp bao truoc thi bi giu chan MOT lan.
    ///     Dang Thien Cung: vung thanh dai `luanLineLength` chan nhip tien quan, KHONG tang thoi gian giu chan.
    ///   - Cuu Tien Quy Nhat (secondMinorStar): `cuuTienVolleys` nhp x `cuuTienArrowsPerVolley` ten hoi tu vao MOT muc tieu;
    ///     bi ngat (choang / khoa don / chet / mat muc tieu) thi mat cac nhp con lai, khong giu tang sang nguoi khac.
    ///     Dang Thien Cung: nhp cuoi hop thanh mot mui nang xuyen muc tieu sang MOT dich phia sau.
    ///   - Nhat Boc Lien Hoan (thirdMinorStar): ten no tai muc tieu thanh `bocTines` tia ngan cham toi da `bocTines` dich
    ///     gan do (khong ban lai cung mot nguoi). Dang Thien Cung: mot dich bi tia ha thi them MOT vu no phu.
    ///   - Xa Nhat (chieu cuoi, mana): khoa huong luc thao tay VE PHIA MUC TIEU dang nham (cheo theo hang cua no, khong
    ///     phai hang hero dang dung), giuong cung bao truoc (frame cham cua clip cast + `aimHoldSeconds`) roi ban ten lon
    ///     xuyen `shotLength` theo huong do; moi dich tren duong an `shotPower` ATK mot lan, muc tieu da nham an them
    ///     `sealBonus` ATK moi Nhat An roi xoa het an. Bi ngat trong luc giuong thi khong co sat thuong.
    /// Ghi chu ky thuat: `CastUltimate` KHONG tu `PlayAction` clip chieu cuoi — `Hero.FinishCast` da goi no o frame cham
    /// cua chinh clip `skill` (goi lai la clip tua ve frame 1 — bai hoc Nguoi Soi 2026-10-08). Chieu phu tu hen gio bang
    /// `HitDelay(clip)` va tu `PlayAction` clip cua minh. So CHUA can.
    /// </summary>
    [RequireComponent(typeof(Hero))]
    public class HauNghe : MonoBehaviour
    {
        /// <summary>Van toc bay cua ten lon chieu cuoi (giay) — nhanh de doc ra "xuyen", khong phai nhip nham.</summary>
        const float ShotFlightSeconds = 0.35f;

        /// <summary>Do cao bay cua ten lon so voi chan (= `MuzzleOffset.y` cua ten thuong).</summary>
        const float ShotFlightHeight = 0.6f;

        /// <summary>Nua chieu dai art vach ngam (148 art px = 2,96 world) — duoi vach nam o chan hero.</summary>
        const float AimHalfLength = 1.48f;

        /// <summary>Be day vach nham chieu cuoi khi ban vao khoang khong (khong khoa duoc theo dich cua art).</summary>
        const float ShotLinePadding = 0.6f;

        /// <summary>FX vung nhat luan: 3 frame bao truoc / 3 frame hoat dong / 3 frame tan (theo art-contract).</summary>
        const int LuanTelegraphFrames = 3;
        const int LuanActiveFrames = 3;

        Hero _hero;
        HauNgheKit _kit;
        Unit _sealed;
        int _seals;
        float _sealUntil;
        float _luanReady, _cuuTienReady, _bocReady;
        GameObject _skyBowFx;

        bool SkyBowReady => _seals >= _kit.sealMax;

        public void Initialise(Hero hero, HauNgheKit kit)
        {
            _hero = hero;
            _kit = kit;
            _sealed = null;
            _seals = 0;
            _sealUntil = _luanReady = _cuuTienReady = _bocReady = 0f;
            ClearSeals();
        }

        void OnDisable() => ClearSeals();

        void Update()
        {
            if (_kit == null || _hero == null || _hero.IsDying || !WaveManager.Fighting) return;
            DropExpiredSeals();
            if (_hero.Casting || _hero.IsStunned || _hero.IsDisarmed) return;
            var target = _hero.AttackTargetInRange();
            if (target == null) return;
            // Ba chieu phu dung chung mot nhip chon: chieu nao san sang thi chay, uu tien chieu hoi lau nhat.
            if (_hero.ThirdMinorUnlocked && Time.time >= _bocReady && HasCrowdNear(target)) { Boc(target); return; }
            if (_hero.SecondMinorUnlocked && Time.time >= _cuuTienReady) { StartCoroutine(CuuTien(target)); return; }
            if (_hero.MinorUnlocked && Time.time >= _luanReady) { StartCoroutine(Luan(target)); }
        }

        /// <summary>Ten thuong vua trung dich (goi tu `Hero.OnProjectileHit`): cong sat thuong cua an dang co roi tich them an.</summary>
        public void OnBasicHit(Unit enemy, bool primary)
        {
            if (_kit == null || !_hero.PassiveUnlocked || !primary || enemy == null || enemy.IsDying) return;
            if (_sealed != enemy)
            {
                ClearSeals();
                _sealed = enemy;
            }
            // An dang co cong sat thuong cho don VUA trung (tinh truoc khi cong them an moi).
            if (_seals > 0) enemy.Health.TakeDamage(_hero.AttackDamage * _seals * _kit.sealDamagePerStack, _hero.Health);
            _seals = Mathf.Min(_kit.sealMax, _seals + 1);
            _sealUntil = Time.time + _kit.sealSeconds;
            _sealed.ApplyMark(this, _kit.sealSeconds, _kit.seal);
            if (SkyBowReady) ShowSkyBow(0.8f);
        }

        // ---------------------------------------------------------------- phu 1: Nhat Luan Tran Tuyen

        IEnumerator Luan(Unit target)
        {
            if (!_hero.PlayAction(_kit.luanCast)) yield break;
            _luanReady = Time.time + _kit.luanCooldown;
            bool sky = ConsumeSeals();
            yield return new WaitForSeconds(_hero.HitDelay(_kit.luanCast));
            if (_hero.IsDying) yield break;

            var at = target != null && !target.IsDying ? target.transform.position : FrontOfHero();
            var art = sky && _kit.luanEmpowered.Length > 0 ? _kit.luanEmpowered : _kit.luanGround;
            var intro = Head(art, LuanTelegraphFrames);
            var active = Middle(art, LuanTelegraphFrames, LuanActiveFrames);
            var outro = Tail(art, LuanTelegraphFrames + LuanActiveFrames);
            // Dang Thien Cung: vung cham CUNG la dai phu het be sau (khop `InZone`), khong thi quai hang khac bi giu
            // chan ma khong bi cham.
            SkillZone.Spawn(at, sky ? _kit.luanLineLength * 0.5f : _kit.luanRadius, _kit.luanDuration, _hero.Side,
                active, Battlefield.UnitScale,
                everyFrame: enemy => { if (!enemy.IsBoss) enemy.ApplySlow(_kit.luanSlow, 0.25f); },
                onExpire: () => OneShotFx.Spawn(at, outro, false, SortOrder.GroundDecal + 1, Battlefield.UnitScale),
                intro: intro, fullLane: sky);

            // Het nhp bao truoc: giu chan MOT lan (boss chi bi cham) — xem `SproutRoot`.
            yield return new WaitForSeconds(_kit.luanTelegraph);
            if (_hero.IsDying) yield break;
            SproutRoot(at, sky);
        }

        void SproutRoot(Vector3 at, bool sky)
        {
            foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
            {
                if (enemy == null || enemy.IsDying || enemy.IsBoss || !InZone(enemy, at, sky)) continue;
                enemy.ApplyRoot(enemy.IsElite ? _kit.luanRoot * 0.5f : _kit.luanRoot);
            }
        }

        /// <summary>
        /// Trong vung (do tren mat dat): dang thuong la hinh tron, dang Thien Cung la dai chan quan `luanLineLength`
        /// phu het be sau san. Ban truoc lay GIAO cua dai voi hinh tron — nua dai 1,7 > ban kinh 1,3 nen giao
        /// luon bang hinh tron va Thien Cung khong doi gi (review 2026-10-10).
        /// </summary>
        bool InZone(Unit enemy, Vector3 at, bool sky)
        {
            if (sky) return Battlefield.InLaneColumn(enemy, at.x, _kit.luanLineLength * 0.5f);
            return Battlefield.GroundDistance(enemy.AimPoint(at), at) <= _kit.luanRadius + enemy.BodyRadius;
        }

        // ---------------------------------------------------------------- phu 2: Cuu Tien Quy Nhat

        IEnumerator CuuTien(Unit target)
        {
            if (!_hero.PlayAction(_kit.cuuTienCast)) yield break;
            _cuuTienReady = Time.time + _kit.cuuTienCooldown;
            bool sky = ConsumeSeals();
            yield return new WaitForSeconds(_hero.HitDelay(_kit.cuuTienCast));
            if (_hero.IsDying || target == null || target.IsDying) yield break;

            for (int volley = 0; volley < _kit.cuuTienVolleys; volley++)
            {
                if (_hero.IsDying || target == null || target.IsDying) yield break;
                if (_hero.IsStunned || _hero.IsDisarmed) yield break;    // bi ngat: mat cac nhp con lai
                bool last = volley == _kit.cuuTienVolleys - 1;
                float power = last && sky ? _kit.cuuTienEmpoweredPower : _kit.cuuTienVolleyPower;
                var frames = last && sky ? _kit.volleyEmpowered : _kit.volley;
                // Ba ten cua mot nhp: mot hit duy nhat cho MOI DICH (mot chieu nhieu ten khong tu lap cường hoá).
                OneShotFx.Spawn(target.BodyCenter, frames, _hero.FacesRight, SortOrder.Air, Battlefield.UnitScale);
                target.Health.TakeDamage(_hero.AttackDamage * power, _hero.Health);
                _hero.OnHitsLanded(1);
                if (last && sky) PierceBehind(target, _hero.AttackDamage * _kit.cuuTienPiercePower);
                if (!last) yield return new WaitForSeconds(_kit.cuuTienVolleyGap);
            }
        }

        /// <summary>Dang Thien Cung: mui nang xuyen muc tieu sang MOT dich ngay sau theo huong ban.</summary>
        void PierceBehind(Unit target, float damage)
        {
            float facing = _hero.FacesRight ? 1f : -1f;
            Unit behind = null;
            float best = float.PositiveInfinity;
            foreach (var enemy in Unit.Hostiles(_hero.Side))
            {
                if (enemy == null || enemy.IsDying || enemy == target) continue;
                float ahead = (enemy.transform.position.x - target.transform.position.x) * facing;
                if (ahead <= 0f || ahead >= best) continue;
                best = ahead;
                behind = enemy;
            }
            if (behind == null) return;
            OneShotFx.Spawn(behind.BodyCenter, _kit.sunArrow, _hero.FacesRight, SortOrder.Air, Battlefield.UnitScale);
            behind.Health.TakeDamage(damage, _hero.Health);
        }

        // ---------------------------------------------------------------- phu 3: Nhat Boc Lien Hoan

        void Boc(Unit target)
        {
            if (!_hero.PlayAction(_kit.bocCast)) return;
            _bocReady = Time.time + _kit.bocCooldown;
            bool sky = ConsumeSeals();
            StartCoroutine(CastBoc(target, sky));
        }

        IEnumerator CastBoc(Unit target, bool sky)
        {
            yield return new WaitForSeconds(_hero.HitDelay(_kit.bocCast));
            if (_hero.IsDying || target == null || target.IsDying) yield break;
            OneShotFx.Spawn(target.BodyCenter, _kit.burst, !_hero.FacesRight, SortOrder.Air, Battlefield.UnitScale);
            target.Health.TakeDamage(_hero.AttackDamage * _kit.bocImpactPower, _hero.Health);
            _hero.OnHitsLanded(1);

            bool echoed = false;
            foreach (var enemy in Nearby(target.transform.position, _kit.bocTines, target))
            {
                if (enemy == null || enemy.IsDying) continue;
                OneShotFx.Spawn(enemy.BodyCenter, _kit.chain, _hero.FacesRight, SortOrder.Air, Battlefield.UnitScale);
                enemy.Health.TakeDamage(_hero.AttackDamage * _kit.bocTinePower, _hero.Health);
                _hero.OnHitsLanded(1);
                if (!sky || echoed || !enemy.IsDying) continue;
                echoed = true;   // toi da MOT vu no phu moi lan dung
                OneShotFx.Spawn(enemy.BodyCenter, _kit.burst, !_hero.FacesRight, SortOrder.Air, Battlefield.UnitScale);
                foreach (var splash in Unit.Hostiles(_hero.Side).ToArray())
                {
                    if (splash == null || splash.IsDying || splash == enemy) continue;
                    if (Battlefield.GroundDistance(splash.AimPoint(enemy.transform.position), enemy.transform.position)
                        > _kit.bocEchoRadius + splash.BodyRadius) continue;
                    splash.Health.TakeDamage(_hero.AttackDamage * _kit.bocEchoPower, _hero.Health);
                }
            }
        }

        bool HasCrowdNear(Unit target) => Nearby(target.transform.position, 1, target).Any();

        IEnumerable<Unit> Nearby(Vector3 at, int count, Unit exclude) =>
            Unit.Hostiles(_hero.Side).ToArray()
                .Where(u => u != null && !u.IsDying && u != exclude
                    && Battlefield.GroundDistance(u.AimPoint(at), at) <= _kit.bocRadius + u.BodyRadius)
                .OrderBy(u => Battlefield.GroundDistance(u.AimPoint(at), at))
                .Take(count);

        // ---------------------------------------------------------------- chieu cuoi: Xa Nhat

        /// <summary>Goi o frame cham cua clip `skill` cua chinh chieu cuoi (xem ghi chu dau file).</summary>
        public void CastUltimate()
        {
            int seals = Mathf.Clamp(_seals, 0, _kit.sealMax);
            var aimed = _sealed;
            // Huong ban khoa ngay luc thao tay: ve phia MUC TIEU dang nham (cheo theo hang cua no), khong phai hang
            // hero dang dung — quai khac hang truoc day bi hut du hero dang ban chieu vao no (nguoi dung 2026-10-21).
            var aim = AimDirection(aimed);
            ClearSeals();
            StartCoroutine(CastXaNhat(aimed, aim, seals));
        }

        /// <summary>Huong ban tren mat dat (z = 0): uu tien Nhat An da nham con song, khong co thi muc tieu dang danh.</summary>
        Vector3 AimDirection(Unit aimed)
        {
            var target = aimed != null && !aimed.IsDying ? aimed : _hero.AttackTargetInRange();
            if (target != null && !target.IsDying)
            {
                var delta = target.transform.position - _hero.transform.position;
                delta.z = 0f;
                if (delta.sqrMagnitude > 0.0001f) return delta.normalized;
            }
            return new Vector3(_hero.FacesRight ? 1f : -1f, 0f, 0f);
        }

        /// <summary>Goc nghieng cho art ve NGANG khi ban cheo: lat theo dau x nhu `Projectile` (art mac dinh quay trai).</summary>
        static float TiltOf(Vector3 aim) =>
            Mathf.Atan2(aim.y, Mathf.Abs(aim.x)) * Mathf.Rad2Deg * (aim.x >= 0f ? 1f : -1f);

        IEnumerator CastXaNhat(Unit aimed, Vector3 aim, int seals)
        {
            var origin = _hero.transform.position;
            var aimArt = _kit.aim;
            var aimLoop = Head(aimArt, aimArt.Length - 2);          // 4 frame bao truoc (art-contract: telegraph 1-4)
            var aimFade = Tail(aimArt, aimArt.Length - 2);
            // Vach ngam: art tam o, mui chi TRAI -> dat nua vach ve phia ban, lat + xoay theo huong ban cheo.
            float tilt = TiltOf(aim);
            var aimAt = origin + aim * AimHalfLength;
            var aimFx = OneShotFx.SpawnLoop(null, aimAt, aimLoop, _kit.aimHoldSeconds, SortOrder.GroundDecal + 1,
                scale: Battlefield.UnitScale);
            if (aimFx != null)
            {
                aimFx.GetComponent<SpriteRenderer>().flipX = aim.x >= 0f;
                aimFx.transform.rotation = Quaternion.Euler(0f, 0f, tilt);
            }
            ShowSkyBow(_kit.aimHoldSeconds);

            float until = Time.time + _kit.aimHoldSeconds;
            while (Time.time < until)
            {
                if (_hero.IsDying || _hero.IsStunned || _hero.IsDisarmed) yield break;   // ngat: khong co sat thuong
                yield return null;
            }
            OneShotFx.Spawn(aimAt, aimFade, aim.x >= 0f, SortOrder.GroundDecal + 1, Battlefield.UnitScale, degrees: tilt);

            // Mat muc tieu van ban theo huong da khoa, nhung bonus Nhat An chi danh cho dich da nham.
            var from = _hero.transform.position;
            var to = from + aim * _kit.shotLength;
            // Hitline do o chan; mui ten bay o tam cao tay cung (= MuzzleOffset.y cua ten thuong), nghieng theo duong bay.
            var flight = new Vector3(0f, ShotFlightHeight, 0f);
            TravellingHeroFx.Spawn(from + flight, to + flight, _kit.sunArrow, ShotFlightSeconds, tiltToPath: true);
            yield return new WaitForSeconds(ShotFlightSeconds);
            if (_hero.IsDying) yield break;

            foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
            {
                if (enemy == null || enemy.IsDying || !OnShotLine(enemy, from, to)) continue;
                float bonus = enemy == aimed && !aimed.IsDying ? 1f + seals * _kit.sealBonus : 1f;
                enemy.Health.TakeDamage(_hero.AttackDamage * _kit.shotPower * bonus, _hero.Health);
                OneShotFx.Spawn(enemy.BodyCenter, _kit.hit, !_hero.FacesRight, SortOrder.Air, Battlefield.UnitScale);
            }
            _hero.OnHitsLanded(1);
        }

        bool OnShotLine(Unit enemy, Vector3 from, Vector3 to)
        {
            var p = enemy.transform.position;
            float along = Mathf.Clamp01(Vector3.Dot(p - from, to - from) / Mathf.Max(0.01f, (to - from).sqrMagnitude));
            var onLine = from + (to - from) * along;
            return Battlefield.GroundDistance(enemy.AimPoint(onLine), onLine)
                <= _kit.shotHalfWidth + ShotLinePadding + enemy.BodyRadius;
        }

        // ---------------------------------------------------------------- Nhat An (song han + FX)

        /// <summary>Tieu het an khi dang du (chieu phu chay dang Thien Cung); tra true = lan nay la Thien Cung.</summary>
        bool ConsumeSeals()
        {
            if (!SkyBowReady) return false;
            ClearSeals();
            return true;
        }

        void DropExpiredSeals()
        {
            if (_seals <= 0) return;
            if (Time.time < _sealUntil && _sealed != null && !_sealed.IsDying) return;
            ClearSeals();
        }

        void ClearSeals()
        {
            _seals = 0;
            _sealed = null;
            ClearSkyBow();
        }

        /// <summary>Cung nhat mo canh khi an da day — dau hieu doc duoc rang chieu phu ke tiep la dang Thien Cung.</summary>
        void ShowSkyBow(float seconds)
        {
            if (_kit.skyBow.Length == 0) return;
            ClearSkyBow();
            // Cha (hero) da nhan `Battlefield.UnitScale`, nen offset tinh theo than phai chia lai scale cua cha.
            var offset = new Vector3(0f, _hero.BodyHeight * 0.5f / Battlefield.UnitScale, 0f);
            _skyBowFx = OneShotFx.SpawnLoop(_hero.transform, offset, _kit.skyBow, seconds, SortOrder.Air);
        }

        void ClearSkyBow()
        {
            if (_skyBowFx == null) return;
            Destroy(_skyBowFx);
            _skyBowFx = null;
        }

        Vector3 FrontOfHero() => _hero.transform.position + new Vector3(_hero.FacesRight ? 2f : -2f, 0f, 0f);

        /// <summary>Frame dau / giua / cuoi cua mot FX co chia pha trong art-contract (rong = mang rong).</summary>
        static Sprite[] Head(Sprite[] art, int count) => Slice(art, 0, Mathf.Clamp(count, 0, art.Length));
        static Sprite[] Middle(Sprite[] art, int skip, int count) =>
            Slice(art, Mathf.Clamp(skip, 0, art.Length), Mathf.Clamp(count, 0, Mathf.Max(0, art.Length - skip)));
        static Sprite[] Tail(Sprite[] art, int skip) =>
            Slice(art, Mathf.Clamp(skip, 0, art.Length), Mathf.Max(0, art.Length - skip));

        static Sprite[] Slice(Sprite[] art, int start, int count)
        {
            if (art == null || art.Length == 0 || count <= 0) return System.Array.Empty<Sprite>();
            var slice = new Sprite[count];
            for (int i = 0; i < count; i++) slice[i] = art[Mathf.Min(art.Length - 1, start + i)];
            return slice;
        }
    }
}
