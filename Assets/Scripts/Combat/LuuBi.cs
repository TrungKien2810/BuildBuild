using System.Collections.Generic;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Luu Bi — ho tro (bo Dao Vien, `DaoVienDefinition`):
    ///   - Nhan Duc (passiveStar): hero cung phe trong `virtueRadius` +toc hoi mana, anh em ket nghia nhan doi (`ManaRate`).
    ///   - An Duc (minorStar): cooldown an, gio kiem ban phuc — dong minh mat mau nhieu nhat (theo ti le) hoi `graceHeal`
    ///     mau toi da + giai choang / troi / cham / Thieu Hon.
    ///   - Dong Sinh Cong Tu (secondMinorStar): anh em ket nghia nga lan dau moi wave nam `reviveDelay` giay roi duoc dai
    ///     lua keo day voi `reviveHealth` mau + bat tu `reviveInvulnerable` giay (moi nguoi 1 lan/wave). Hero dung lai moi
    ///     wave (`HeroBoard`) nen danh sach da keo tu xoa.
    ///   - Dao Vien Ket Nghia (chieu xin): hoa dao roi khap san, moi dong minh hoi `skillPower` mau toi da + `oathDamage`
    ///     cong trong `skillDuration` giay; anh em ket nghia +`oathBrotherMana` mana.
    /// </summary>
    public class LuuBi : DaoVienBrother
    {
        /// <summary>Moi Luu Bi dang tren san — Nhan Duc / Dong Sinh Cong Tu tra qua day.</summary>
        public static readonly List<LuuBi> Active = new List<LuuBi>();

        float _graceCooldown;
        float _graceAt = -1f;
        readonly HashSet<Hero> _revived = new HashSet<Hero>();

        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);

        public override void Initialise(Hero hero, DaoVienDefinition definition)
        {
            base.Initialise(hero, definition);
            _graceCooldown = definition.graceCooldown;
            _graceAt = -1f;
            _revived.Clear();
        }

        protected override void Update()
        {
            base.Update();
            if (_definition == null || _hero == null || _hero.IsDying) return;
            if (_graceAt > 0f && Time.time >= _graceAt)
            {
                _graceAt = -1f;
                Grace();
            }
            if (!_hero.MinorUnlocked || !WaveManager.Fighting) return;
            _graceCooldown -= Time.deltaTime;
            if (_graceCooldown > 0f || _graceAt > 0f || MostWounded() == null) return;
            float at = Play(_definition.blessBody);
            if (at < 0f) return;    // dang ban clip khac: thu lai frame sau
            _graceCooldown = _definition.graceCooldown;
            _graceAt = at;
        }

        // ---------------------------------------------------------------- Nhan Duc

        /// <summary>Phan toc hoi mana Luu Bi nay cong cho <paramref name="hero"/> (0 neu ngoai tam / chua mo noi tai).</summary>
        public float VirtueFor(Hero hero, bool brother)
        {
            if (_definition == null || _hero == null || _hero.IsDying || !_hero.PassiveUnlocked || hero.Side != _hero.Side) return 0f;
            if (Battlefield.GroundDistance(hero.transform.position, transform.position) > _definition.virtueRadius) return 0f;
            return _definition.virtueManaRate * (brother ? 2f : 1f);
        }

        // ---------------------------------------------------------------- An Duc

        /// <summary>Dong minh ti le mau thap nhat (hoac dang choang), khong tinh nguoi dang nam cho keo day.</summary>
        Hero MostWounded()
        {
            Hero best = null;
            float bestFraction = float.PositiveInfinity;
            foreach (var ally in Hero.Alive)
            {
                if (ally == null || ally.IsDying || ally.Side != _hero.Side || ally.Health.IsDead) continue;
                float fraction = ally.IsStunned ? 0f : ally.Health.Fraction;
                if (fraction >= 1f || fraction >= bestFraction) continue;
                bestFraction = fraction;
                best = ally;
            }
            return best;
        }

        void Grace()
        {
            var ally = MostWounded();
            if (ally == null) return;
            ally.Health.Heal(ally.Health.Max * _definition.graceHeal, _hero.Health);
            ally.Cleanse();
            OneShotFx.Spawn(ally.transform.position, _definition.healRingFrames, false, SortOrder.ForUnit(ally) + 1,
                Battlefield.UnitScale);
        }

        // ---------------------------------------------------------------- Dong Sinh Cong Tu

        public bool TryRevive(Hero dying)
        {
            if (_definition == null || _hero == null || _hero.IsDying || dying == _hero || !_hero.SecondMinorUnlocked) return false;
            var definition = dying.Definition;
            if (dying.Side != _hero.Side || definition == null || definition.daoVien == null ||
                definition.swornBrother == SwornBrother.None) return false;
            if (!_revived.Add(dying)) return false;
            dying.BeginRevive(_definition.reviveDelay, _definition.reviveHealth, _definition.reviveInvulnerable,
                _definition.silkFrames);
            return true;
        }

        // ---------------------------------------------------------------- Dao Vien Ket Nghia

        public override void CastUltimate()
        {
            if (_hero == null || _hero.IsDying) return;
            var definition = _hero.Definition;
            float seconds = definition.skillDuration;

            var camera = Camera.main;
            if (camera != null)
                for (int i = 0; i < _definition.petalClusters; i++)
                {
                    var at = camera.ViewportToWorldPoint(new Vector3(Random.Range(0.05f, 0.95f), Random.Range(0.25f, 0.85f), 0f));
                    at.z = 0f;
                    OneShotFx.SpawnLoop(null, at, _definition.petalFrames, seconds, SortOrder.Air, scale: Battlefield.UnitScale);
                }

            foreach (var ally in Hero.Alive.ToArray())
            {
                if (ally == null || ally.IsDying || ally.Side != _hero.Side || ally.Health.IsDead) continue;
                ally.Health.Heal(ally.Health.Max * definition.skillPower, _hero.Health);
                ally.ApplyBuff("dao-vien", _definition.oathDamage, 0f, seconds);
                OneShotFx.Spawn(ally.transform.position, _definition.healRingFrames, false, SortOrder.ForUnit(ally) + 1,
                    Battlefield.UnitScale);
                var brother = ally.Definition != null ? ally.Definition.swornBrother : SwornBrother.None;
                if (ally != _hero && brother != SwornBrother.None && ally.Definition.daoVien != null)
                    ally.AddMana(_definition.oathBrotherMana);
            }
        }
    }
}
