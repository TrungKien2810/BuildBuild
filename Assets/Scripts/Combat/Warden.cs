using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Bo chieu rieng cua Chainwarden (Cai nguc moc xich, SR — GAME_DESIGN.md muc 3, chot 2026-09-24) — `Hero` tu gan
    /// component nay khi `HeroDefinition.warden` co gia tri, giong `Necromancer` cua Bonecaller.
    ///   - NOI TAI Cai Nguc: KHONG o day — `Blocker` giu 4 quai (`maxHeld`) va quai dang bi giu mang Xich
    ///     (`heldDamageBonus`, nhan them sat thuong tu MOI nguon qua `Health.Vulnerability`);
    ///   - CHIEU THUONG Quat Xich (cooldown AN): co quai trong `sweepRadius` thi quay xich — sat thuong thap + TROI
    ///     (dung im, van danh). Boss troi mot nua. Khong co quai thi giu chieu san sang, khong phi;
    ///   - CHIEU LON Nem Moc (thanh mana cua `Hero`, `SkillKind.HookThrow`): keo quai ban xa (uu tien), khong co thi
    ///     con xa nhat chua bi minh giu, trong `hookRange`, ve sat minh + choang ngan + Xich `hookChainDuration` giay.
    /// Art Nem Moc (`hook-throw-body`, `hook-fx`) dang cho duyet: than dung clip attack, FX de trong.
    /// </summary>
    [RequireComponent(typeof(Hero))]
    public class Warden : MonoBehaviour
    {
        Hero _hero;
        WardenDefinition _definition;
        float _sweepCooldown;

        public void Initialise(Hero hero, WardenDefinition definition)
        {
            _hero = hero;
            _definition = definition;
            _sweepCooldown = definition.sweepCooldown;
        }

        void Update()
        {
            if (_definition == null || _hero == null || _hero.IsDying) return;

            _sweepCooldown -= Time.deltaTime;
            if (_sweepCooldown > 0f || !AnyInSweep()) return;
            _sweepCooldown = _definition.sweepCooldown;
            Sweep();
        }

        bool InSweep(Enemy enemy) =>
            Battlefield.GroundDistance(enemy.transform.position, transform.position) - enemy.BodyRadius <= _definition.sweepRadius;

        bool AnyInSweep()
        {
            foreach (var enemy in Enemy.Alive)
                if (enemy != null && !enemy.IsDying && InSweep(enemy)) return true;
            return false;
        }

        /// <summary>Troi TRUOC khi tru mau: con chet vi don nay khong can troi, con song thi dung im ngay frame sau.</summary>
        void Sweep()
        {
            foreach (var enemy in Enemy.Alive.ToArray())
            {
                if (enemy == null || enemy.IsDying || !InSweep(enemy)) continue;
                enemy.ApplyRoot(enemy.IsBoss ? _definition.rootDuration * 0.5f : _definition.rootDuration);
                enemy.Health.TakeDamage(_definition.sweepDamage, _hero.Health);
            }
            OneShotFx.Spawn(transform.position, _definition.sweepFrames, false, SortOrder.Air, Battlefield.UnitScale);
        }

        /// <summary>Goi tu `Hero.FinishCast` khi thanh mana day. Tra ve false neu khong co quai nao de moc.</summary>
        public bool ThrowHook()
        {
            if (_definition == null || _hero == null || _hero.IsDying) return false;
            var target = PickHookTarget();
            if (target == null) return false;

            // Dich: sat mep than Chainwarden, phia quai dang dung, cung hang y — quai can chien toi noi la vao cho giu.
            float side = target.transform.position.x >= transform.position.x ? 1f : -1f;
            var destination = transform.position +
                new Vector3(side * (_hero.BodyRadius + target.BodyRadius + 0.05f), 0f, 0f);
            destination = Battlefield.Clamp(destination, target.BodyRadius, clampRight: false);

            float stun = target.IsBoss ? _definition.hookStun * 0.5f : _definition.hookStun;
            target.PullTo(destination, _definition.hookPullTime);
            target.ApplyStun(_definition.hookPullTime + stun, _definition.stunFrames);
            target.ApplyChain(_definition.hookChainBonus, _definition.hookChainDuration);
            if (_definition.hookFrames != null && _definition.hookFrames.Length > 0)
                OneShotFx.SpawnLoop(target.transform, Vector3.zero, _definition.hookFrames, _definition.hookPullTime,
                    SortOrder.Air);
            return true;
        }

        /// <summary>
        /// Quai BAN XA gan nhat trong tam (GDD: uu tien Goblin Na); khong co thi con XA nhat chua bi Chainwarden giu —
        /// keo con dung sat roi thi phi chieu.
        /// </summary>
        Enemy PickHookTarget()
        {
            float rangeSqr = _definition.hookRange * _definition.hookRange;
            Enemy ranged = null, far = null;
            float rangedSqr = float.PositiveInfinity, farSqr = -1f;
            var own = _hero.Blocker;

            foreach (var enemy in Enemy.Alive)
            {
                if (enemy == null || enemy.IsDying) continue;
                float sqr = (enemy.transform.position - transform.position).sqrMagnitude;
                if (sqr > rangeSqr) continue;

                if (!enemy.CanBeHeld)
                {
                    if (sqr < rangedSqr) { rangedSqr = sqr; ranged = enemy; }
                    continue;
                }
                if (own != null && own.Holds(enemy) && enemy.Holder == own) continue;
                if (sqr > farSqr) { farSqr = sqr; far = enemy; }
            }
            return ranged != null ? ranged : far;
        }
    }
}
