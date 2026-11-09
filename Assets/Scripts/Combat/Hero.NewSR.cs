using System.Collections;
using System.Linq;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Bo Nguyet Tho Luan (R) + Tho San Hac An (SR) — kit duyet, so CHUA can. Hai hero nay khong can component rieng
    /// (khong tai nguyen / trieu hoi / bien hinh) nen so song thang tren `HeroDefinition` nhu bo N:
    ///   - Ngoc Hoan (don Nguyet Tho, dan vong): dan chung, no bang `impactFrames`.
    ///   - Hoi Luan (ReturnStrike, 1 sao): moi don thu 3 TRUNG dich, vong quay ve gay them ×`passivePower` len toi da
    ///     3 dich tren duong ve (moi dich 1 lan). Dem don trung, doi muc tieu khong xoa dem. Duong ve phu het be sau san.
    ///   - Ngoc Luan Hoi (RingReturn, mana): nem luan lon xuyen duong dai `skillRadius` theo huong khoa luc phong roi
    ///     thu ve cung duong; luot di ×`skillPower`, luot ve 75% luot di; moi dich toi da 1 hit moi luot.
    ///   - No Hac Tien (don Tho San, dan no): ap pha giap nhe `RavenBreak` trong `RavenBreakSeconds` giay.
    ///   - Pha An (ShieldBane, 1 sao): don truc tiep cua chinh Tho San len muc tieu dang co khien +×`passivePower`
    ///     (chi xet khien luc trung).
    ///   - Qua Den Xe Giap (phu, 3 sao): moi `RavenCooldown` giay, uu tien muc tieu con khien trong tam: qua an bay toi,
    ///     gay ×`RavenPower` + pha giap manh `RavenBreakBig` + Khoa Hoi Mau `RavenNoHeal` giay (boss mot nua).
    ///     Khong con muc tieu co khien thi ban muc tieu thuong ×1,5.
    ///   - Phan Quyet Hac An (SigilJudgment, mana): dung 3 coc an tai 3 dich nhieu mau nhat trong vung `skillRadius`
    ///     quanh muc tieu; sau 2s moi coc no ×`skillPower` (muc tieu Xe Giap +50%) + Khoa Hoi Mau 4s (boss 2s).
    /// </summary>
    public partial class Hero
    {
        const float RavenCooldown = 10f;
        const float RavenPower = 2f;
        const float RavenNormalPower = 1.5f;
        const float RavenBreak = 0.1f;
        const float RavenBreakSeconds = 2f;
        const float RavenBreakBig = 0.25f;
        const float RavenBreakBigSeconds = 4f;
        const float RavenNoHeal = 4f;
        const float JudgmentDelay = 2f;
        /// <summary>An la con cua don vi (da x UnitScale) va art ~68 px rong: o 1.0 no to hon ca than dich va che het no.</summary>
        const float JudgmentMarkScale = 0.5f;
        const float JudgmentNoHeal = 4f;

        int _moonCount;
        float _ravenReady;
        /// <summary>Muc tieu Qua Den Xe Giap gan nhat + han hieu luc (Phan Quyet Hac An +50% len no).</summary>
        Unit _rended;
        float _rendedUntil;

        void InitialiseNewSR()
        {
            _moonCount = 0;
            _ravenReady = 0f;
            _rended = null;
            _rendedUntil = 0f;
        }

        /// <summary>Don thuong vua trung (goi tu `OnProjectileHit`, ca chinh lan phu).</summary>
        void OnNewSRHit(Unit enemy, bool primary, float damage, bool shielded)
        {
            if (_definition == null || enemy == null || enemy.IsDying) return;
            if (_gourd != null) _gourd.OnBasicHit(enemy);
            if (_ink != null) _ink.OnBasicHit(enemy);
            if (_hong != null) _hong.OnBasicHit(enemy);
            if (_definition.skillKind == SkillKind.SigilJudgment)
            {
                // No Hac Tien: pha giap nhe ke ca khi chua mo noi tai.
                enemy.ApplyArmorBreak(RavenBreak, RavenBreakSeconds);
            }
            if (!primary) return;
            // Xet khien luc trung (truoc khi dan tru): phat lam VO khien van duoc cong.
            if (HasPassive(PassiveKind.ShieldBane) && shielded)
                enemy.Health.TakeDamage(damage * _definition.passivePower, _health);
            if (HasPassive(PassiveKind.ReturnStrike) && ++_moonCount % 3 == 0)
                MoonReturn(enemy, damage);
        }

        void UpdateNewSR()
        {
            if (_definition == null || _definition.skillKind != SkillKind.SigilJudgment) return;
            if (!MinorUnlocked || !WaveManager.Fighting || _dying || Casting || IsStunned || IsDisarmed) return;
            if (Time.time < _ravenReady || AttackTargetInRange() == null) return;
            _ravenReady = Time.time + RavenCooldown;
            StartCoroutine(CastRavenRend());
        }

        void MoonReturn(Unit target, float damage)
        {
            var from = target.transform.position;
            var to = transform.position;
            // Be day tia phai nhan `UnitScale` nhu moi art khac (art 1 px = 0,02 world): de 0,3 thi tia mong 1/3 art.
            OneShotFx.SpawnBeam(from, to, _definition.skillFrames, SortOrder.Air, 0.3f * Battlefield.UnitScale);
            int hits = 0;
            foreach (var enemy in Unit.Hostiles(Side).ToArray())
            {
                if (enemy == null || enemy.IsDying || hits >= 3) continue;
                if (!Battlefield.InLaneBand(enemy, from, to)) continue;
                enemy.Health.TakeDamage(damage * _definition.passivePower, _health);
                hits++;
            }
            if (hits > 0) OnHitsLanded(hits);
        }

        IEnumerator CastRavenRend()
        {
            var target = AttackTargetInRange();
            if (target == null) yield break;
            Unit marked = null;
            foreach (var enemy in Unit.Hostiles(Side).ToArray())
            {
                if (enemy == null || enemy.IsDying) continue;
                if (Battlefield.GroundDistance(enemy.transform.position, transform.position) - enemy.BodyRadius > _definition.range) continue;
                if (enemy.Health.Shield > 0f) { marked = enemy; break; }
                if (marked == null) marked = enemy;
            }
            if (marked == null) yield break;
            TravellingHeroFx.Spawn(BodyCenter, marked.BodyCenter, _definition.projectileFrames, 0.3f);
            yield return new WaitForSeconds(0.3f);
            if (marked.IsDying || _dying) yield break;
            bool shielded = marked.Health.Shield > 0f;
            marked.Health.TakeDamage(AttackDamage * (shielded ? RavenPower : RavenNormalPower), _health);
            marked.ApplyArmorBreak(RavenBreakBig, RavenBreakBigSeconds);
            _rended = marked;
            _rendedUntil = Time.time + RavenBreakBigSeconds;
            marked.Status.Apply(StatusId.NoHeal, 0f, marked.IsBoss ? RavenNoHeal * 0.5f : RavenNoHeal, _health, Side);
            OneShotFx.Spawn(marked.BodyCenter, _definition.passiveFrames, false, SortOrder.Air, Battlefield.UnitScale * _definition.impactScale);
            OnHitsLanded(1);
        }

        void CastRingReturn()
        {
            AuraMultipliers(out float multiplier, out _);
            var mainTarget = FindAttackTarget();
            if (mainTarget != null) multiplier *= PassiveDamageMultiplier(mainTarget);
            float facing = FacingRight ? 1f : -1f;
            var origin = transform.position;
            var end = origin + new Vector3(facing * _definition.skillRadius, 0f, 0f);
            StartCoroutine(RingTrip(origin, end, BaseDamage * multiplier * _definition.skillPower));
        }

        IEnumerator RingTrip(Vector3 origin, Vector3 end, float damage)
        {
            // Vong ve tam o (pivot center), day art thap hon tam 14 px -> nang 0.28 world cho vong lan TREN mat dat.
            var lift = new Vector3(0f, 0.28f, 0f);
            TravellingHeroFx.Spawn(origin + lift, end + lift, _definition.skillFrames, 0.4f);
            yield return new WaitForSeconds(0.4f);
            SweepLine(origin, end, damage);
            TravellingHeroFx.Spawn(end + lift, origin + lift, _definition.skillFrames, 0.4f);
            yield return new WaitForSeconds(0.4f);
            SweepLine(end, origin, damage * 0.75f);
        }

        void SweepLine(Vector3 from, Vector3 to, float damage)
        {
            int hits = 0;
            foreach (var enemy in Unit.Hostiles(Side).ToArray())
            {
                if (enemy == null || enemy.IsDying) continue;
                // Vong lan tren lan: do X tren doan [from..to], chieu sau phu het be sau san.
                if (!Battlefield.InLaneBand(enemy, from, to)) continue;
                enemy.Health.TakeDamage(damage, _health);
                hits++;
            }
            if (hits > 0) OnHitsLanded(hits);
        }

        void CastSigilJudgment()
        {
            var target = AttackTargetInRange();
            var at = target != null ? target.transform.position
                : transform.position + new Vector3(FacingRight ? 2f : -2f, 0f, 0f);
            var victims = Unit.Hostiles(Side).ToArray()
                .Where(u => u != null && !u.IsDying
                    && Battlefield.GroundDistance(u.transform.position, at) - u.BodyRadius <= _definition.skillRadius)
                .OrderByDescending(u => u.Health.Current).Take(3).ToArray();
            foreach (var victim in victims)
                victim.ApplyMark(this, JudgmentDelay, _definition.skillFrames, JudgmentMarkScale);
            StartCoroutine(DetonateJudgment(victims));
        }

        IEnumerator DetonateJudgment(Unit[] victims)
        {
            yield return new WaitForSeconds(JudgmentDelay);
            if (_dying) yield break;
            AuraMultipliers(out float multiplier, out _);
            foreach (var victim in victims)
            {
                if (victim == null || victim.IsDying) continue;
                bool rend = victim == _rended && Time.time < _rendedUntil;
                victim.Health.TakeDamage(BaseDamage * multiplier * _definition.skillPower * (rend ? 1.5f : 1f), _health);
                victim.Status.Apply(StatusId.NoHeal, 0f,
                    victim.IsBoss ? JudgmentNoHeal * 0.5f : JudgmentNoHeal, _health, Side);
                OneShotFx.Spawn(victim.BodyCenter, _definition.skillIntroFrames, false, SortOrder.Air, Battlefield.UnitScale * _definition.impactScale);
            }
            OnHitsLanded(victims.Length);
        }
    }
}
