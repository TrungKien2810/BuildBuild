using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Noi tai cua hero khong co bo chieu rieng (`HeroDefinition.passiveKind`, chot 2026-09-29; so CHUA can). Mo theo
    /// `passiveStar` nhu moi noi tai:
    ///   - Truy Kich (Ironblade): don thuong vao quai thap mau +sat thuong;
    ///   - Khieu Khich (Cragfist): quai quanh ong bo unit khac vao cho ong — `Enemy.EngageHero` hoi `Taunter`;
    ///   - Nham Yeu Diem (Hawkfletch): cu phat thu N chi mang;
    ///   - Vo Bang (Frostweaver): quai dang bi lam cham chet trong tam -> no manh bang quanh xac;
    ///   - Cuu Nguy (Vespera): dong minh thap mau duoc hoi nhieu hon.
    /// </summary>
    public partial class Hero
    {
        int _shotCount;

        bool HasPassive(PassiveKind kind) => _definition != null && _definition.passiveKind == kind && PassiveUnlocked;

        void InitialisePassive()
        {
            _shotCount = 0;
            Enemy.Killed -= OnEnemyKilled;
            if (HasPassive(PassiveKind.Shatter)) Enemy.Killed += OnEnemyKilled;
        }

        /// <summary>He so nhan cho MOT don thuong vao <paramref name="target"/> (goi dung mot lan moi phat — dem phat Crit).</summary>
        float PassiveDamageMultiplier(Enemy target)
        {
            if (HasPassive(PassiveKind.Execute))
                return target.Health.Fraction < _definition.passiveThreshold ? 1f + _definition.passivePower : 1f;
            if (HasPassive(PassiveKind.Crit))
                return ++_shotCount % _definition.passiveEvery == 0 ? _definition.passivePower : 1f;
            return 1f;
        }

        /// <summary>Cuu Nguy: he so hoi mau cho <paramref name="ally"/>.</summary>
        float RescueMultiplier(Hero ally) =>
            HasPassive(PassiveKind.Rescue) && ally.Health.Fraction < _definition.passiveThreshold
                ? 1f + _definition.passivePower : 1f;

        /// <summary>
        /// Khieu Khich: cho cua hero gan nhat co noi tai nay ma <paramref name="enemy"/> dung trong `passiveRadius` va con
        /// cho trong (quai ban xa khong can cho). null = khong ai khieu khich.
        /// </summary>
        public static Blocker Taunter(Enemy enemy)
        {
            Blocker best = null;
            float bestSqr = float.PositiveInfinity;
            foreach (var hero in Alive)
            {
                if (hero == null || hero._dying || hero._blocker == null || !hero.HasPassive(PassiveKind.Taunt)) continue;
                float radius = hero._definition.passiveRadius;
                float sqr = (hero.transform.position - enemy.transform.position).sqrMagnitude;
                if (sqr > radius * radius || sqr >= bestSqr) continue;
                if (enemy.CanBeHeld && !hero._blocker.Holds(enemy) && !hero._blocker.HasFreeSlot(enemy)) continue;
                bestSqr = sqr;
                best = hero._blocker;
            }
            return best;
        }

        /// <summary>Vo Bang: xac dang bi lam cham trong tam danh no, quai quanh xac an don thuong x `passivePower`.</summary>
        void OnEnemyKilled(Enemy dead)
        {
            if (_dying || dead == null || !dead.IsSlowed) return;
            if ((dead.transform.position - transform.position).sqrMagnitude > AttackRange * AttackRange) return;

            var at = dead.transform.position;
            float damage = BaseDamage * _definition.passivePower;
            // Chep ra mang: quai chet vi vu no goi lai ham nay (no day chuyen) va lam doi danh sach.
            foreach (var enemy in Enemy.Alive.ToArray())
            {
                if (enemy == null || enemy == dead || enemy.IsDying) continue;
                if (Battlefield.GroundDistance(enemy.transform.position, at) > _definition.passiveRadius + enemy.BodyRadius) continue;
                enemy.Health.TakeDamage(damage, _health);
            }
            OneShotFx.Spawn(dead.BodyCenter, _definition.impactFrames, false, SortOrder.Air,
                Battlefield.UnitScale * _definition.impactScale);
        }
    }
}
