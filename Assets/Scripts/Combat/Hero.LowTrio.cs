using System.Collections;
using System.Linq;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Bo hero bac thap Bo Hung / Song Dao / Chuong Bac (GAME_DESIGN.md muc 3, kit duyet 2026-10-03, so CHUA can) va noi
    /// tai cua hai linh moi (Linh Nam, Cua Kim Sat):
    ///   - Chan Dong + Huc Sung (Bo Hung): don thu 3 hat lui; huc dat quanh chan choang;
    ///   - Lien Hoan + Bao Dao (Song Dao): tang sat thuong khi danh cung mot con; 6 nhat don vao con do;
    ///   - Thanh Tay + Hoi Chuong (Chuong Bac): go mot hieu ung moi 10s; go het khong che ca doi + cham doi thu;
    ///   - Vung Bao Tu (Linh Nam): don trung de vung lam cham;
    ///   - Kep Pha Giap (Cua Kim Sat): don thu N pha giap muc tieu.
    /// </summary>
    public partial class Hero
    {
        /// <summary>Thoi gian bi hat lui (PullTo) — ngan nhu mot cu hat, cung nhip Galemonk.</summary>
        const float QuakePushTime = 0.15f;
        /// <summary>Bao Dao: 6 nhat trong 3 giay.</summary>
        const int BladeStormSlashes = 6;
        const float BladeStormInterval = 0.5f;
        /// <summary>O vung bao tu: diem dat (64,76) cua art nam duoi tam o 12 art-px.</summary>
        const float SporeFieldLift = 0.12f;

        int _hitCount;
        Unit _comboTarget;
        int _comboStacks;
        float _comboLastHit;
        float _purifyReady;
        bool _bladeStorming;

        void InitialiseLowTrio()
        {
            _hitCount = 0;
            _comboTarget = null;
            _comboStacks = 0;
            _purifyReady = 0f;
            _bladeStorming = false;
        }

        /// <summary>Don thuong vua trung (goi tu `OnProjectileHit`, chi con bi nham).</summary>
        void OnLowTrioHit(Unit enemy)
        {
            if (HasPassive(PassiveKind.Combo)) AddCombo(enemy);
            if (HasPassive(PassiveKind.SporeField)) SpawnSporeField(enemy.transform.position);
            if (HasPassive(PassiveKind.Quake) && ++_hitCount % _definition.passiveEvery == 0) Quake(enemy);
            if (HasPassive(PassiveKind.ClampBreak) && ++_hitCount % _definition.passiveEvery == 0)
            {
                enemy.ApplyArmorBreak(_definition.passivePower, _definition.passiveDuration);
                OneShotFx.Spawn(enemy.BodyCenter, _definition.passiveFrames, false, SortOrder.Air, Battlefield.UnitScale);
            }
        }

        // ---------------------------------------------------------------- Bo Hung

        /// <summary>Chan Dong: hat <paramref name="enemy"/> lui `passiveRadius` theo huong dang nhin (tinh anh mot nua, boss khong).</summary>
        void Quake(Unit enemy)
        {
            float facing = FacingRight ? 1f : -1f;
            OneShotFx.Spawn(transform.position + new Vector3(facing * 0.3f, MuzzleOffset.y, 0f), _definition.passiveFrames,
                FacingRight, SortOrder.Air, Battlefield.UnitScale);
            if (enemy.IsBoss) return;
            float distance = enemy.IsElite ? _definition.passiveRadius * 0.5f : _definition.passiveRadius;
            var to = Battlefield.Clamp(enemy.transform.position + new Vector3(facing * distance, 0f, 0f), enemy.BodyRadius,
                clampRight: true);
            enemy.PullTo(to, QuakePushTime);
        }

        void CastHornSlam()
        {
            AuraMultipliers(out float multiplier, out _);
            float damage = BaseDamage * multiplier * _definition.skillPower;
            var at = transform.position;
            foreach (var enemy in Unit.Hostiles(Side).ToArray())
            {
                if (enemy == null || enemy.IsDying) continue;
                if (Battlefield.GroundDistance(enemy.AimPoint(at), at) > _definition.skillRadius + enemy.BodyRadius) continue;
                enemy.Health.TakeDamage(damage, _health);
                float stun = enemy.IsBoss || enemy.IsElite ? _definition.skillDuration * 0.5f : _definition.skillDuration;
                enemy.ApplyStun(stun, _definition.stunFrames);
            }
            OneShotFx.Spawn(at, _definition.skillFrames, FacingRight, SortOrder.GroundDecal + 1,
                ZoneFxScale(_definition.skillFrames, _definition.skillRadius));
        }

        // ---------------------------------------------------------------- Song Dao

        void AddCombo(Unit enemy)
        {
            bool keep = enemy == _comboTarget && Time.time - _comboLastHit <= _definition.passiveDuration;
            _comboStacks = keep ? Mathf.Min(_definition.passiveEvery, _comboStacks + 1) : 1;
            _comboTarget = enemy;
            _comboLastHit = Time.time;
            var marks = _definition.passiveFrames;
            if (marks != null && marks.Length > 0)
                enemy.ApplyMark(this, _definition.passiveDuration, new[] { marks[Mathf.Min(_comboStacks, marks.Length) - 1] });
        }

        /// <summary>So tang Lien Hoan con hieu luc tren <paramref name="target"/> (0 = khong).</summary>
        int ComboStacksOn(Unit target) =>
            target != null && target == _comboTarget && Time.time - _comboLastHit <= _definition.passiveDuration ? _comboStacks : 0;

        float ComboMultiplier(Unit target) => 1f + _definition.passivePower * ComboStacksOn(target);

        void CastBladeStorm()
        {
            var target = FindAttackTarget();
            if (target == null) return;
            StartCoroutine(BladeStorm(target, HasPassive(PassiveKind.Combo) ? ComboMultiplier(target) : 1f));
        }

        IEnumerator BladeStorm(Unit target, float comboBonus)
        {
            _bladeStorming = true;
            for (int i = 0; i < BladeStormSlashes; i++)
            {
                if (_dying || target == null || target.IsDying || !InRange(target)) break;
                if (i > 0 && animator != null) animator.Play("attack", restart: true);
                Face(target.AimPoint(transform.position).x);
                AuraMultipliers(out float multiplier, out _);
                target.Health.TakeDamage(BaseDamage * multiplier * _definition.skillPower * comboBonus, _health);
                OneShotFx.Spawn(target.BodyCenter, _definition.skillFrames, FacingRight, SortOrder.Air, Battlefield.UnitScale);
                OneShotFx.Spawn(target.BodyCenter, _definition.impactFrames, FacingRight, SortOrder.Air, Battlefield.UnitScale);
                yield return new WaitForSeconds(BladeStormInterval);
            }
            _bladeStorming = false;
            _comboTarget = null;    // het chieu xoa tang Lien Hoan
            _comboStacks = 0;
            if (_dying) yield break;
            _casting = false;
            if (animator != null) animator.Play("idle");
        }

        // ---------------------------------------------------------------- Chuong Bac

        /// <summary>Thanh Tay: het hoi thi go 1 hieu ung (Doc -> Chay -> Cham) khoi dong minh gan nhat dang bi; khong ai thi giu luot.</summary>
        void UpdatePurify()
        {
            if (!HasPassive(PassiveKind.Purify) || !WaveManager.Fighting || _dying || Time.time < _purifyReady) return;
            Hero best = null;
            float bestDistance = float.PositiveInfinity;
            foreach (var ally in Alive)
            {
                if (ally == null || ally._dying || ally.Side != Side || !Afflicted(ally)) continue;
                float distance = Battlefield.GroundDistance(ally.transform.position, transform.position);
                if (distance > _definition.passiveRadius || distance >= bestDistance) continue;
                bestDistance = distance;
                best = ally;
            }
            if (best == null) return;
            _purifyReady = Time.time + _definition.passiveDuration;
            if (best.Status.Has(StatusId.Poison)) best.Status.Remove(StatusId.Poison);
            else if (best.Status.Has(StatusId.Burn)) best.Status.Remove(StatusId.Burn);
            else best.Status.Remove(StatusId.Slow);
            OneShotFx.Spawn(best.BodyCenter, _definition.passiveFrames, false, SortOrder.Air, Battlefield.UnitScale);
        }

        static bool Afflicted(Hero ally) =>
            ally.Status.Has(StatusId.Poison) || ally.Status.Has(StatusId.Burn) || ally.Status.Has(StatusId.Slow);

        void CastBellToll()
        {
            OneShotFx.Spawn(transform.position, _definition.skillFrames, false, SortOrder.GroundDecal + 1,
                Battlefield.UnitScale * 2f);
            foreach (var ally in Alive.ToArray())
            {
                if (ally == null || ally._dying || ally.Side != Side) continue;
                bool any = Afflicted(ally) || ally.IsStunned || ally.IsRooted || ally.IsDisarmed;
                ally.Status.Remove(StatusId.Poison);
                ally.Status.Remove(StatusId.Burn);
                ally.Status.Remove(StatusId.Slow);
                ally.Status.Remove(StatusId.Stun);
                ally.ClearControl();
                if (any) OneShotFx.Spawn(ally.BodyCenter, _definition.passiveFrames, false, SortOrder.Air, Battlefield.UnitScale);
            }
            foreach (var enemy in Unit.Hostiles(Side).ToArray())
            {
                if (enemy == null || enemy.IsDying || enemy.IsBoss) continue;
                enemy.ApplySlow(enemy.IsElite ? _definition.skillPower * 0.5f : _definition.skillPower, _definition.skillDuration);
            }
        }

        // ---------------------------------------------------------------- Linh Nam

        /// <summary>Vung bao tu tai chan quai trung don: quai trong vung cham `passivePower`, het vung chay `skillOutroFrames` (tan).</summary>
        void SpawnSporeField(Vector3 at)
        {
            float slow = _definition.passivePower;
            SkillZone.Spawn(at + new Vector3(0f, SporeFieldLift * Battlefield.UnitScale, 0f), _definition.passiveRadius,
                _definition.passiveDuration, Side, _definition.passiveFrames, Battlefield.UnitScale,
                everyFrame: enemy => enemy.ApplySlow(slow, 0.3f), outro: _definition.skillOutroFrames);
        }
    }
}
