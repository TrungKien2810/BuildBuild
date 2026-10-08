using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Noi tai cua hero khong co bo chieu rieng (`HeroDefinition.passiveKind`, chot 2026-09-29; so CHUA can). Mo theo
    /// `passiveStar` nhu moi noi tai:
    ///   - Truy Kich (Ironblade): don thuong vao quai thap mau +sat thuong;
    ///   - Khieu Khich (Cragfist): quai quanh ong bo unit khac vao cho ong — `Enemy.EngageHero` hoi `Taunter` (ca khieu
    ///     khich co han `Hero.Taunt`);
    ///   - Nham Yeu Diem (Hawkfletch): cu phat thu N chi mang;
    ///   - Vo Bang (Frostweaver): quai dang bi lam cham chet trong tam -> no manh bang quanh xac;
    ///   - Cuu Nguy (Vespera): dong minh thap mau duoc hoi nhieu hon;
    ///   - Khac Che (Warhorn): don thuong + huc vao tinh anh / boss +sat thuong;
    ///   - Ngam Doc (Phu Thuy Dam Lay): don thuong cong tang Doc (`Venom`);
    ///   - Bao Tu (Hiep Si Nam): bi danh can chien thi nha bao tu quanh minh;
    ///   - Thoc Hang (Ngu Nhan): don thuong xuyen sang quai dung ngay sau muc tieu;
    ///   - Ben Lua (Phap Su Lua): don thuong gay Chay; quai dang Chay chet thi lua lan (`Burn`);
    ///   - Chan Dan (Nguoi Lun): dan quai ban xa trung ong thi vo (`BreaksShot`, goi tu `EnemyExtras`);
    ///   - Day Roi (Tien Det To): quai an don thuong bi truot don (`Unit.ApplyTangle`).
    /// </summary>
    public partial class Hero
    {
        int _shotCount;
        float _sporeReadyAt;

        /// <summary>Bao Tu nha toi da 2 lan/giay — bi vay boi 3 quai thi khong thanh may xay sat thuong.</summary>
        const float SporeInterval = 0.5f;

        bool HasPassive(PassiveKind kind) => _definition != null && _definition.passiveKind == kind && PassiveUnlocked;

        void InitialisePassive()
        {
            _shotCount = 0;
            Unit.Killed -= OnEnemyKilled;
            if (HasPassive(PassiveKind.Shatter)) Unit.Killed += OnEnemyKilled;
            _sporeReadyAt = 0f;
            InitialiseLowTrio();
            Health.AnyDamaged -= OnAnyDamaged;
            if (HasPassive(PassiveKind.Spores)) Health.AnyDamaged += OnAnyDamaged;
        }

        /// <summary>
        /// `Projectile` bao moi quai vua an don thuong cua hero nay (<paramref name="primary"/> = con bi nham).
        /// Ngam Doc: +1 tang Doc. Thoc Hang: con dung ngay sau muc tieu an them x `passivePower`.
        /// </summary>
        public void OnProjectileHit(Unit enemy, bool primary, float damage)
        {
            if (_definition == null || enemy == null) return;
            if (HasPassive(PassiveKind.SeedStacks)) SeedMark.Apply(enemy, this);
            if (HasPassive(PassiveKind.Venom)) AddVenom(enemy, 1);
            if (primary && HasPassive(PassiveKind.Pierce)) PierceBehind(enemy, damage * _definition.passivePower);
            if (_definition.passiveKind == PassiveKind.Ignite) AddBurn(enemy);
            if (HasPassive(PassiveKind.Tangle)) enemy.ApplyTangle(_definition.passivePower, _definition.passiveDuration);
            if (primary) OnLowTrioHit(enemy);    // Chan Dong / Lien Hoan / Vung Bao Tu / Kep Pha Giap — Hero.LowTrio.cs
        }

        /// <summary>Chay theo so cua noi tai Ignite — don thuong va Mua Tan Lua luon dot, noi tai mo moi lan lua khi quai chet.</summary>
        void AddBurn(Unit enemy) =>
            enemy.Status.Apply(StatusId.Burn, _definition.passivePower, _definition.passiveDuration, _health, Side,
                spread: HasPassive(PassiveKind.Ignite) ? _definition.passiveRadius : 0f, fx: _definition.passiveFrames);

        /// <summary>Chan Dan: dan quai cham hero nay tai <paramref name="at"/> — vo (FX) va tra true thi bo sat thuong.</summary>
        public bool BreaksShot(Vector3 at)
        {
            if (_dying || !HasPassive(PassiveKind.ShotBreak)) return false;
            OneShotFx.Spawn(at, _definition.passiveFrames, false, SortOrder.Air, Battlefield.UnitScale);
            return true;
        }

        /// <summary>Cong tang Doc theo so cua noi tai Venom — chieu Vac Suong Doc dung chung, ke ca khi noi tai chua mo.</summary>
        void AddVenom(Unit enemy, int stacks) =>
            enemy.Status.Apply(StatusId.Poison, _definition.passivePower, _definition.passiveDuration, _health, Side,
                stacks, _definition.passiveEvery, fx: _definition.passiveFrames);

        /// <summary>Thoc Hang: quai gan nhat dung SAU <paramref name="target"/> theo huong dang nhin, trong `passiveRadius`.</summary>
        void PierceBehind(Unit target, float damage)
        {
            float facing = FacingRight ? 1f : -1f;
            var at = target.transform.position;
            Unit best = null;
            float bestDistance = float.PositiveInfinity;
            foreach (var enemy in Unit.Hostiles(Side))
            {
                if (enemy == null || enemy == target || enemy.IsDying || !enemy.TargetableBy(this)) continue;
                if ((enemy.transform.position.x - at.x) * facing <= 0f) continue;
                float distance = Battlefield.GroundDistance(enemy.transform.position, at) - enemy.BodyRadius;
                if (distance > _definition.passiveRadius || distance >= bestDistance) continue;
                bestDistance = distance;
                best = enemy;
            }
            if (best != null) best.Health.TakeDamage(damage, _health);
        }

        /// <summary>Bao Tu: hero nay bi don CAN CHIEN (quai/hero khong ban xa) -> quai trong `passiveRadius` an `passivePower`.</summary>
        void OnAnyDamaged(Health victim, Health source, float amount, float shieldAbsorbed, Health shieldCaster)
        {
            if (victim != _health || source == null || _dying || Time.time < _sporeReadyAt) return;
            var attacker = source.GetComponent<Unit>();
            bool melee = attacker is Enemy enemy ? !enemy.Definition.IsRanged : attacker is Hero hero && !hero.IsRangedUnit;
            if (!melee) return;
            _sporeReadyAt = Time.time + SporeInterval;

            var at = transform.position;
            foreach (var target in Unit.Hostiles(Side).ToArray())
            {
                if (target == null || target.IsDying) continue;
                if (Battlefield.GroundDistance(target.AimPoint(at), at) > _definition.passiveRadius + target.BodyRadius) continue;
                target.Health.TakeDamage(_definition.passivePower, _health, kind: DamageKind.Reflected);
            }
            OneShotFx.Spawn(at, _definition.passiveFrames, false, SortOrder.ForUnit(this) + 1, Battlefield.UnitScale);
        }

        /// <summary>He so nhan cho MOT don thuong vao <paramref name="target"/> (goi dung mot lan moi phat — dem phat Crit).</summary>
        float PassiveDamageMultiplier(Unit target)
        {
            if (HasPassive(PassiveKind.Execute))
                return target.Health.Fraction < _definition.passiveThreshold ? 1f + _definition.passivePower : 1f;
            if (HasPassive(PassiveKind.Crit))
                return ++_shotCount % _definition.passiveEvery == 0 ? _definition.passivePower : 1f;
            if (HasPassive(PassiveKind.Combo)) return ComboMultiplier(target);
            return BaneMultiplier(target);
        }

        /// <summary>Khac Che (Warhorn): he so sat thuong vao <paramref name="target"/> — tinh anh / boss +`passivePower`.</summary>
        float BaneMultiplier(Unit target) =>
            HasPassive(PassiveKind.BossBane) && (target.IsBoss || target.IsElite) ? 1f + _definition.passivePower : 1f;

        /// <summary>Cuu Nguy: he so hoi mau cho <paramref name="ally"/>.</summary>
        float RescueMultiplier(Hero ally) =>
            HasPassive(PassiveKind.Rescue) && ally.Health.Fraction < _definition.passiveThreshold
                ? 1f + _definition.passivePower : 1f;

        /// <summary>
        /// Khieu Khich: cho cua hero gan nhat co noi tai nay ma <paramref name="enemy"/> dung trong `passiveRadius` va con
        /// cho trong (quai ban xa khong can cho). null = khong ai khieu khich.
        /// </summary>
        public static Blocker Taunter(Unit enemy)
        {
            Blocker best = null;
            float bestSqr = float.PositiveInfinity;
            foreach (var hero in Alive)
            {
                if (hero == null || hero._dying || hero._blocker == null || !hero.IsHostileTo(enemy.Side)) continue;
                bool passive = hero.HasPassive(PassiveKind.Taunt);
                if (!passive && !hero.Taunting) continue;
                // Khieu khich co han (Truong Phi Duong Duong Kieu Hong) — Hero.DaoVien.cs.
                float radius = Mathf.Max(passive ? hero._definition.passiveRadius : 0f, hero.Taunting ? hero._tauntRadius : 0f);
                float sqr = (hero.transform.position - enemy.transform.position).sqrMagnitude;
                if (sqr > radius * radius || sqr >= bestSqr) continue;
                if (enemy.CanBeHeld && !hero._blocker.Holds(enemy) && !hero._blocker.HasFreeSlot(enemy)) continue;
                bestSqr = sqr;
                best = hero._blocker;
            }
            return best;
        }

        /// <summary>Vo Bang: xac dang bi lam cham trong tam danh no, quai quanh xac an don thuong x `passivePower`.</summary>
        void OnEnemyKilled(Unit dead)
        {
            if (_dying || dead == null || !dead.IsHostileTo(Side) || !dead.IsSlowed) return;
            if ((dead.transform.position - transform.position).sqrMagnitude > AttackRange * AttackRange) return;

            var at = dead.transform.position;
            float damage = BaseDamage * _definition.passivePower;
            // Chep ra mang: quai chet vi vu no goi lai ham nay (no day chuyen) va lam doi danh sach.
            foreach (var enemy in Unit.Hostiles(Side).ToArray())
            {
                if (enemy == null || enemy == dead || enemy.IsDying) continue;
                if (Battlefield.GroundDistance(enemy.AimPoint(at), at) > _definition.passiveRadius + enemy.BodyRadius) continue;
                enemy.Health.TakeDamage(damage, _health);
            }
            OneShotFx.Spawn(dead.BodyCenter, _definition.impactFrames, false, SortOrder.Air,
                Battlefield.UnitScale * _definition.impactScale);
        }
    }
}
