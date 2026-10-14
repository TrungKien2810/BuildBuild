using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Bo Bach Loc Tien (`BachLocTienKit`), gan vao `Hero` khi `HeroDefinition.bachLocTien` co. Hero nay la HERO HOI MAU
    /// (`HeroDefinition.healsAllies`): `Hero.Support` lo phan di chuyen / nham nguoi thap mau nhat, con luong hoi + FX
    /// cua don thuong do `DeliverBasicHeal` o day quyet dinh (hat sang xanh bay tu truong toi nguoi nhan).
    ///   - Loc Tuc (don thuong): hoi `basicHealPower` ATK cho dong minh bi thuong thap ti le mau nhat trong tam.
    ///   - Linh Mach Rung Thieng (passiveStar): luong mau THUC hoi duoc nap Sinh Khi (tran `spiritMax`); day thi chieu
    ///     phu KE TIEP co mot nhp Linh Loc tro giup rieng roi tieu thanh. Nhp tro giup khong tu nap lai Sinh Khi.
    ///   - Tam Mam Hoi Xuan (minorStar): trong toi da `mamMax` mam (mam moi thay mam cu nhat) duoi chan cac dong minh bi
    ///     thuong thap nhat trong tam; mam hoi theo nhp `mamPulseSeconds`, truong thanh sau `mamMaturePulses` nhp; vung
    ///     chong nhau chi hoi MOT nhp manh nhat.
    ///   - Hoan Diep Tuc Sinh (secondMinorStar): co mam TRUONG THANH + dong minh nguy cap trong tam -> thu hoach mam do
    ///     hoi manh tuc thoi (`diepHealPower`), mam nguon bien mat.
    ///   - Loc Anh Tuan Lam (thirdMinorStar): co it nhat `locAnhMinSprouts` mam -> bong loc chay tu mam CU NHAT toi mam
    ///     MOI NHAT, hoi mot nhp cho dong minh tren duong va go MOT Doc/Chay moi nguoi (uu tien Doc).
    ///   - Bach Loc Hoi Xuan (chieu cuoi, mana): bao truoc (frame cham cua clip cast + `laneHoldSeconds`), bong loc buoc
    ///     doc lane roi de lai dai hoa
    ///     co dinh `laneSeconds` giay, hoi `laneHealPerSecond` ATK moi giay cho hero/linh dang song trong dai; mot nguoi
    ///     nhan toi da MOT nhp moi giay. Nhp Linh Loc tro giup: them MOT nhp hoi nua luc dai bat dau.
    /// Ghi chu ky thuat: `CastUltimate` KHONG tu `PlayAction` clip chieu cuoi — `Hero.FinishCast` da goi o frame cham cua
    /// chinh clip `skill`. So CHUA can.
    /// </summary>
    [RequireComponent(typeof(Hero))]
    public class BachLocTien : MonoBehaviour
    {
        /// <summary>Van toc hat hoi bay tu truong toi nguoi nhan (giay) — hoi ap sau khi hat bay xong.</summary>
        const float BoltFlightSeconds = 0.25f;

        /// <summary>FX `tam-mam-ground` chia pha trong art-contract: mam non [0..1], non [2..3], truong thanh [4..5], thu hoach [6..8].</summary>
        const int MamSeedFrames = 2;
        const int MamYoungFrames = 2;
        const int MamMatureFrames = 2;
        const int MamHarvestStart = 6;

        /// <summary>FX `hoi-xuan-flower-zone`: bao truoc 2 / no 2 / hoat dong 3 / tan 2 (theo art-contract).</summary>
        const int LaneIntroFrames = 4;
        const int LaneActiveFrames = 3;

        class Sprout
        {
            public Vector3 at;
            public float dueAt;
            public float nextPulse;
            public int pulses;
            public bool mature;
            public GameObject[] loop;
        }

        Hero _hero;
        BachLocTienKit _kit;
        float _spirit;
        bool _supportHealing;     // dang hoi bang nhp Linh Loc tro giup: luong nay khong nap Sinh Khi
        float _mamReady, _diepReady, _locAnhReady;
        GameObject _spiritFx;
        readonly List<Sprout> _sprouts = new List<Sprout>();
        /// <summary>Nguoi da an nhp hoi cua mam trong frame nay — hai mam chong vung khong hoi hai lan.</summary>
        readonly HashSet<Hero> _pulsedThisFrame = new HashSet<Hero>();

        /// <summary>He so hoi cua don thuong cho `Hero.HealAlly` (kit: 70% ATK).</summary>
        public float BasicHealPower => _kit != null ? _kit.basicHealPower : 1f;

        public void Initialise(Hero hero, BachLocTienKit kit)
        {
            _hero = hero;
            _kit = kit;
            _spirit = 0f;
            _supportHealing = false;
            _mamReady = _diepReady = _locAnhReady = 0f;
            ClearSprouts();
            Health.AnyHealed -= OnAnyHealed;
            if (kit != null) Health.AnyHealed += OnAnyHealed;
        }

        void OnDisable()
        {
            Health.AnyHealed -= OnAnyHealed;
            ClearSprouts();
        }

        /// <summary>Hoi mau THUC te (ai hoi cung tinh, mien la chinh minh la nguon) nap vao Sinh Khi — co tran, hoi du khong nap.</summary>
        void OnAnyHealed(Health victim, Health healer, float amount)
        {
            if (_kit == null || _hero == null || healer != _hero.Health || amount <= 0f) return;
            if (_supportHealing) return;      // nhp tro giup khong tu nap lai Sinh Khi
            _spirit = Mathf.Min(_kit.spiritMax, _spirit + amount);
        }

        void Update()
        {
            if (_kit == null || _hero == null || _hero.IsDying || !WaveManager.Fighting) return;
            TickSprouts();
            RefreshSpiritFx();
            if (_hero.Casting || _hero.IsStunned || _hero.IsDisarmed) return;
            if (_hero.ThirdMinorUnlocked && Time.time >= _locAnhReady
                && _sprouts.Count >= _kit.locAnhMinSprouts && _hero.PlayAction(_kit.locAnhCast))
            {
                _locAnhReady = Time.time + _kit.locAnhCooldown;
                StartCoroutine(RunDeerPath(ConsumeSpirit()));
                return;
            }
            if (_hero.SecondMinorUnlocked && Time.time >= _diepReady && UrgentAlly() != null && HasMatureSprout()
                && _hero.PlayAction(_kit.diepCast))
            {
                _diepReady = Time.time + _kit.diepCooldown;
                StartCoroutine(RescueSprout(ConsumeSpirit()));
                return;
            }
            if (_hero.MinorUnlocked && Time.time >= _mamReady && WoundedAllies(_kit.mamMax).Count > 0
                && _hero.PlayAction(_kit.mamCast))
            {
                _mamReady = Time.time + _kit.mamCooldown;
                StartCoroutine(PlantSprouts(ConsumeSpirit()));
            }
        }

        // ---------------------------------------------------------------- don thuong: Loc Tuc

        /// <summary>Giao luong hoi cua don thuong (goi tu `Hero.HealAlly`): hat sang bay tu truong toi nguoi nhan roi moi hoi.</summary>
        public void DeliverBasicHeal(Hero ally, float amount)
        {
            if (ally == null || _kit == null) return;
            if (_kit.bolt.Length > 0)
                TravellingHeroFx.Spawn(_hero.BodyCenter, ally.BodyCenter, _kit.bolt, BoltFlightSeconds);
            StartCoroutine(HealAfterBolt(ally, amount));
        }

        IEnumerator HealAfterBolt(Hero ally, float amount)
        {
            yield return new WaitForSeconds(_kit.bolt.Length > 0 ? BoltFlightSeconds : 0f);
            if (ally == null || ally.IsDying) yield break;
            ally.Health.Heal(amount, _hero.Health);
            OneShotFx.Spawn(ally.transform.position, _kit.bloom, false, SortOrder.Air, Battlefield.UnitScale);
        }

        // ---------------------------------------------------------------- phu 1: Tam Mam Hoi Xuan

        IEnumerator PlantSprouts(bool support)
        {
            yield return new WaitForSeconds(_hero.HitDelay(_kit.mamCast));
            if (_hero.IsDying) yield break;
            foreach (var ally in WoundedAllies(_kit.mamMax))
            {
                if (ally == null || ally.IsDying) continue;
                Plant(ally.transform.position, support);
            }
            _hero.OnHitsLanded(1);
        }

        /// <summary>Trong mot mam: mam moi thay mam cu nhat khi da du `mamMax`; tro giup thi mam da san sang thu hoach.</summary>
        void Plant(Vector3 at, bool support, bool young = false)
        {
            while (_sprouts.Count >= _kit.mamMax)
            {
                var oldest = _sprouts[0];
                _sprouts.RemoveAt(0);
                PlayHarvest(oldest);
            }
            var sprout = new Sprout
            {
                at = at,
                dueAt = Time.time + _kit.mamSeconds,
                nextPulse = Time.time + _kit.mamPulseSeconds,
                pulses = support ? _kit.mamMaturePulses : 0,
                mature = support,
            };
            _sprouts.Add(sprout);
            PlayStage(sprout, young);
        }

        void TickSprouts()
        {
            _pulsedThisFrame.Clear();
            for (int i = _sprouts.Count - 1; i >= 0; i--)
            {
                var sprout = _sprouts[i];
                if (Time.time >= sprout.dueAt)
                {
                    _sprouts.RemoveAt(i);
                    PlayHarvest(sprout);
                    continue;
                }
                if (Time.time < sprout.nextPulse) continue;
                sprout.nextPulse += _kit.mamPulseSeconds;
                sprout.pulses++;
                PulseSprout(sprout);
                if (!sprout.mature && sprout.pulses >= _kit.mamMaturePulses)
                {
                    sprout.mature = true;
                    PlayStage(sprout, young: false);
                }
            }
        }

        /// <summary>Nhip hoi cua mot mam: moi dong minh trong vung an MOT nhp (vung chong nhau thi lay nhp manh nhat).</summary>
        void PulseSprout(Sprout sprout)
        {
            float heal = _hero.AttackDamage * _kit.mamPulsePower;
            // Mam hoi theo COT phu het be sau (khong phai vong tron): san sau 11 don vi mat dat va quai/linh
            // dung o hang nao cung phai an — vong tron ban kinh 0,9 chi cham duoc ~1/6 (do 2026-10-10).
            foreach (var ally in Allies(ally => Battlefield.InLaneColumn(ally, sprout.at.x, _kit.mamRadius)))
            {
                // Hai mam chong vung cung nhp: nguoi trong vung chi an mot nhp.
                if (!_pulsedThisFrame.Add(ally)) continue;
                ally.Health.Heal(heal, _hero.Health);
            }
        }

        bool HasMatureSprout() => _sprouts.Any(s => s.mature);

        // ---------------------------------------------------------------- phu 2: Hoan Diep Tuc Sinh

        IEnumerator RescueSprout(bool support)
        {
            yield return new WaitForSeconds(_hero.HitDelay(_kit.diepCast));
            if (_hero.IsDying) yield break;
            var ally = UrgentAlly();
            if (ally == null) yield break;
            var sprout = _sprouts.Where(s => s.mature).OrderBy(s => s.dueAt).FirstOrDefault();
            if (sprout == null) yield break;
            _sprouts.Remove(sprout);
            PlayHarvest(sprout);

            if (_kit.transfer.Length > 0)
                TravellingHeroFx.Spawn(sprout.at, ally.BodyCenter, _kit.transfer, 0.3f);
            yield return new WaitForSeconds(0.3f);
            if (_hero.IsDying || ally.IsDying) yield break;
            ally.Health.Heal(_hero.AttackDamage * _kit.diepHealPower, _hero.Health);
            OneShotFx.Spawn(ally.transform.position, _kit.bloom, false, SortOrder.Air, Battlefield.UnitScale);
            _hero.OnHitsLanded(1);
            // Nhp tro giup: bong loc mang mot mam non moi ve chan nguoi duoc cuu.
            if (support) Plant(ally.transform.position, support: false, young: true);
        }

        // ---------------------------------------------------------------- phu 3: Loc Anh Tuan Lam

        IEnumerator RunDeerPath(bool support)
        {
            yield return new WaitForSeconds(_hero.HitDelay(_kit.locAnhCast));
            if (_hero.IsDying || _sprouts.Count < _kit.locAnhMinSprouts) yield break;
            var ordered = _sprouts.OrderBy(s => s.dueAt).ToArray();
            var from = ordered[0];
            var to = ordered[ordered.Length - 1];
            var start = from.at;
            var end = to.at;

            var pathMid = (start + end) * 0.5f;
            var pathArt = _kit.path;
            if (pathArt.Length > 0)
                OneShotFx.Spawn(pathMid, pathArt, false, SortOrder.GroundDecal + 1, Battlefield.UnitScale);

            float run = 0.6f;
            if (_kit.deer.Length > 0)
                TravellingHeroFx.Spawn(start, end, _kit.deer, run);
            yield return new WaitForSeconds(run);
            if (_hero.IsDying) yield break;

            var healed = new HashSet<Hero>();
            foreach (var ally in Allies(ally => healed.Add(ally)
                && Battlefield.InLaneBand(ally, start, end, _kit.locAnhHalfWidth)))
            {
                ally.Health.Heal(_hero.AttackDamage * _kit.locAnhHealPower, _hero.Health);
                OneShotFx.Spawn(ally.transform.position, _kit.bloom, false, SortOrder.Air, Battlefield.UnitScale);
                // Go MOT hieu ung moi nguoi, uu tien Doc (Chay chi go khi khong co Doc).
                if (ally.Status.Has(StatusId.Poison)) ally.Status.Remove(StatusId.Poison);
                else if (ally.Status.Has(StatusId.Burn)) ally.Status.Remove(StatusId.Burn);
            }
            _hero.OnHitsLanded(1);
            // Nhp tro giup: mam non tren duong duoc danh thuc thanh mam truong thanh.
            if (!support) yield break;
            foreach (var sprout in _sprouts)
            {
                if (sprout.mature || !OnSegment(sprout.at, start, end, _kit.locAnhHalfWidth)) continue;
                sprout.pulses = _kit.mamMaturePulses;
                sprout.mature = true;
                PlayStage(sprout, young: false);
            }
        }

        // ---------------------------------------------------------------- chieu cuoi: Bach Loc Hoi Xuan

        /// <summary>Goi o frame cham cua clip `skill` cua chinh chieu cuoi (xem ghi chu dau file).</summary>
        public void CastUltimate() => StartCoroutine(CastHoiXuan(ConsumeSpirit()));

        IEnumerator CastHoiXuan(bool support)
        {
            var origin = _hero.transform.position;
            float facing = _hero.FacesRight ? 1f : -1f;
            var from = origin;
            var to = origin + new Vector3(facing * _kit.laneLength, 0f, 0f);
            var mid = (from + to) * 0.5f;
            var art = _kit.lane;
            var intro = Slice(art, 0, LaneIntroFrames);
            var active = Slice(art, LaneIntroFrames, LaneActiveFrames);
            var outro = Slice(art, LaneIntroFrames + LaneActiveFrames, art.Length - LaneIntroFrames - LaneActiveFrames);

            if (intro.Length > 0)
                LaneFx.SpawnRows(null, mid.x, intro, _kit.laneHoldSeconds, SortOrder.GroundDecal + 1,
                    scale: Battlefield.UnitScale);
            yield return new WaitForSeconds(_kit.laneHoldSeconds);
            if (_hero.IsDying) yield break;

            if (_kit.deer.Length > 0) TravellingHeroFx.Spawn(from, to, _kit.deer, 0.6f);
            LaneFx.SpawnRows(null, mid.x, active, _kit.laneSeconds, SortOrder.GroundDecal + 1,
                scale: Battlefield.UnitScale);
            if (support)
            {
                _supportHealing = true;
                HealLane(from, to, _hero.AttackDamage * _kit.laneSupportPower);   // nhp Linh Loc tro giup
                _supportHealing = false;
            }

            float until = Time.time + _kit.laneSeconds;
            float nextPulse = Time.time + 1f;
            while (Time.time < until && !_hero.IsDying)
            {
                if (Time.time >= nextPulse)
                {
                    nextPulse += 1f;
                    HealLane(from, to, _hero.AttackDamage * _kit.laneHealPerSecond);
                }
                yield return null;
            }
            if (outro.Length > 0)
                LaneFx.SpawnRowsOnce(mid.x, outro, false, SortOrder.GroundDecal + 1, Battlefield.UnitScale);
            _hero.OnHitsLanded(1);
        }

        /// <summary>Hoi mot nhip cho MOI hero/linh dang song trong dai hoa (khong hoi cong trinh).</summary>
        void HealLane(Vector3 from, Vector3 to, float amount)
        {
            foreach (var ally in Allies(ally => Battlefield.InLaneBand(ally, from, to)))
                ally.Health.Heal(amount, _hero.Health);
        }

        // ---------------------------------------------------------------- dung chung

        /// <summary>Doi minh cua Bach Loc Tien dang song, loc theo <paramref name="filter"/>.</summary>
        IEnumerable<Hero> Allies(System.Func<Hero, bool> filter)
        {
            foreach (var ally in Hero.Alive.ToArray())
            {
                if (ally == null || ally.IsDying || ally.Definition == null || ally.Side != _hero.Side) continue;
                if (filter(ally)) yield return ally;
            }
        }

        /// <summary>Dong minh trong tam danh (healer nham theo `range` nhu Vespera).</summary>
        bool InRange(Hero ally) =>
            ally == _hero || (ally.transform.position - _hero.transform.position).sqrMagnitude
                <= _hero.CurrentAttackRange * _hero.CurrentAttackRange;

        List<Hero> WoundedAllies(int max) => Allies(a => InRange(a) && a.Health.Fraction < 1f)
            .OrderBy(a => a.Health.Fraction)
            .Take(max)
            .ToList();

        Hero UrgentAlly() => Allies(a => InRange(a) && a.Health.Fraction < _kit.urgentFraction)
            .OrderBy(a => a.Health.Fraction)
            .FirstOrDefault();

        /// <summary>Khoang cach tren MAT DAT tu unit toi doan thang — dung de biet ai nam tren duong loc chay.</summary>
        static bool OnSegment(Unit unit, Vector3 from, Vector3 to, float halfWidth)
        {
            var p = unit.transform.position;
            float along = Mathf.Clamp01(Vector3.Dot(p - from, to - from) / Mathf.Max(0.01f, (to - from).sqrMagnitude));
            var onLine = from + (to - from) * along;
            return Battlefield.GroundDistance(unit.AimPoint(onLine), onLine) <= halfWidth + unit.BodyRadius;
        }

        static bool OnSegment(Vector3 at, Vector3 from, Vector3 to, float halfWidth)
        {
            float along = Mathf.Clamp01(Vector3.Dot(at - from, to - from) / Mathf.Max(0.01f, (to - from).sqrMagnitude));
            return Vector3.Distance(at, from + (to - from) * along) <= halfWidth;
        }

        /// <summary>Tieu Sinh Khi khi day (chieu phu ke tiep co nhp Linh Loc tro giup); tra true = lan nay co tro giup.</summary>
        bool ConsumeSpirit()
        {
            if (_kit.spiritMax <= 0f || _spirit < _kit.spiritMax) return false;
            _spirit = 0f;
            return true;
        }

        /// <summary>Nhanh goc quanh chan truong khi Sinh Khi day — dau hieu doc duoc rang chieu phu ke tiep duoc tro giup.</summary>
        void RefreshSpiritFx()
        {
            bool full = _kit.spiritMax > 0f && _spirit >= _kit.spiritMax;
            if (full && _spiritFx == null && _kit.spirit.Length > 0)
                _spiritFx = OneShotFx.SpawnLoop(_hero.transform, Vector3.zero, _kit.spirit, float.PositiveInfinity,
                    SortOrder.GroundDecal + 2);
            else if (!full && _spiritFx != null)
            {
                Destroy(_spiritFx);
                _spiritFx = null;
            }
        }

        /// <summary>Ve pha cua mam theo nhp (seed -> young -> mature): khop hinh voi tien do gameplay, khong doan theo frame.</summary>
        void PlayStage(Sprout sprout, bool young)
        {
            KillStage(sprout);
            var art = _kit.mamGround;
            if (art.Length == 0) return;
            var stage = sprout.mature
                ? Slice(art, MamSeedFrames + MamYoungFrames, MamMatureFrames)
                : Slice(art, young ? MamSeedFrames : 0, MamYoungFrames);
            float seconds = Mathf.Max(0.1f, sprout.dueAt - Time.time);
            // Mam hoi phu het be sau (xem `PulseSprout`) nen hinh cung ve o ca ba hang cho khop vung hoi that.
            sprout.loop = LaneFx.SpawnRows(null, sprout.at.x, stage, seconds, SortOrder.GroundDecal + 1,
                scale: Battlefield.UnitScale);
        }

        /// <summary>Mam bi thu hoach / het han: chay pha thu hoach roi tan.</summary>
        void PlayHarvest(Sprout sprout)
        {
            KillStage(sprout);
            var art = _kit.mamGround;
            if (art.Length <= MamHarvestStart) return;
            LaneFx.SpawnRowsOnce(sprout.at.x, Slice(art, MamHarvestStart, art.Length - MamHarvestStart),
                false, SortOrder.GroundDecal + 1, Battlefield.UnitScale);
        }

        static void KillStage(Sprout sprout)
        {
            if (sprout.loop == null) return;
            foreach (var fx in sprout.loop) if (fx != null) Destroy(fx);
            sprout.loop = null;
        }

        void ClearSprouts()
        {
            foreach (var sprout in _sprouts) KillStage(sprout);
            _sprouts.Clear();
            if (_spiritFx != null) Destroy(_spiritFx);
            _spiritFx = null;
        }

        static Sprite[] Slice(Sprite[] art, int start, int count)
        {
            if (art == null || art.Length == 0 || count <= 0) return System.Array.Empty<Sprite>();
            var slice = new Sprite[count];
            for (int i = 0; i < count; i++) slice[i] = art[Mathf.Min(art.Length - 1, start + i)];
            return slice;
        }
    }
}
