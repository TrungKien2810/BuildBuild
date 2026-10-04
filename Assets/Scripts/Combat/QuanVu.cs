using System.Collections;
using System.Collections.Generic;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Quan Vu — sat thuong can chien (bo Dao Vien, `DaoVienDefinition`):
    ///   - Vo Thanh (passiveStar): don thuong chem quat trung toi da `cleaveTargets` quai truoc mat; moi quai Quan Vu ha
    ///     +1 tang Uy (+`awePerStack` cong, +`lifestealPerStack` hut mau, toi da `maxStacks`) — mat khi het wave vi hero
    ///     duoc dung lai.
    ///   - Thanh Long Yen Nguyet (minorStar): cooldown an, vung dao phong song dao trang khuyet doc lane, xuyen quai,
    ///     don thuong x `wavePower`.
    ///   - Thac Dao Ke (secondMinorStar): mau tut duoi `feintThreshold` (1 lan/wave) thi lui `feintRetreat` gia thua roi
    ///     quay lai chem quai truoc mat x `feintPower`.
    ///   - Thanh Long Hien (chieu xin): hon rong luc tu luoi dao lao doc lane xuyen moi quai (ca quai bay), don thuong x
    ///     `skillPower` x (1 + mau da mat x `dragonMissingHealthBonus`); moi quai bi ha hoan `dragonKillMana` thanh mana.
    /// </summary>
    public class QuanVu : DaoVienBrother
    {
        int _stacks;
        float _waveCooldown;
        float _waveAt = -1f;
        bool _feintUsed;

        public override void CollectEffects(List<UnitEffect> into)
        {
            if (_stacks > 0) into.Add(new UnitEffect(EffectKind.Passive, Hero.SkillName(_hero.Definition.passiveText),
                stacks: _stacks, icon: _hero.Definition.passiveIcon));
        }

        public override void Initialise(Hero hero, DaoVienDefinition definition)
        {
            base.Initialise(hero, definition);
            _stacks = 0;
            _waveCooldown = definition.waveCooldown;
            _waveAt = -1f;
            _feintUsed = false;
            hero.Health.DamageDealt -= OnDamageDealt;
            hero.Health.DamageDealt += OnDamageDealt;
            hero.Health.Damaged -= OnDamaged;
            hero.Health.Damaged += OnDamaged;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_hero == null || _hero.Health == null) return;
            _hero.Health.DamageDealt -= OnDamageDealt;
            _hero.Health.Damaged -= OnDamaged;
        }

        protected override void Update()
        {
            base.Update();
            if (_definition == null || _hero == null || _hero.IsDying) return;
            if (_waveAt > 0f && Time.time >= _waveAt)
            {
                _waveAt = -1f;
                StartCoroutine(Wave(Side));
            }
            if (!_hero.MinorUnlocked) return;
            _waveCooldown -= Time.deltaTime;
            if (_waveCooldown > 0f || _waveAt > 0f || !EnemyAhead(_definition.waveDistance, _definition.waveHalfDepth)) return;
            float at = Play(_definition.slashBody);
            if (at < 0f) return;
            _waveCooldown = _definition.waveCooldown;
            _waveAt = at;
        }

        // ---------------------------------------------------------------- Vo Thanh

        public override void Multipliers(ref float damage)
        {
            base.Multipliers(ref damage);
            if (_hero.PassiveUnlocked) damage *= 1f + _stacks * _definition.awePerStack;
        }

        void OnDamageDealt(float amount)
        {
            if (_stacks <= 0 || amount <= 0f || _hero.IsDying || !_hero.PassiveUnlocked) return;
            _hero.Health.Heal(amount * _stacks * _definition.lifestealPerStack, _hero.Health);
        }

        /// <summary>Gay sat thuong; ha quai thi +1 Uy. Tra ve true neu ha.</summary>
        bool Slay(Unit enemy, float damage)
        {
            if (!Hit(enemy, damage)) return false;
            if (_hero.PassiveUnlocked) _stacks = Mathf.Min(_definition.maxStacks, _stacks + 1);
            return true;
        }

        public override bool Strike(Unit target, float damage)
        {
            Slay(target, damage);
            int hits = 1;
            if (_hero.PassiveUnlocked)
            {
                var others = new List<Unit>();
                float reach = _hero.CurrentAttackRange + 0.3f;
                foreach (var enemy in Unit.Hostiles(_hero.Side))
                {
                    if (enemy == null || enemy == target || enemy.IsDying || !enemy.TargetableBy(_hero)) continue;
                    var d = enemy.transform.position - transform.position;
                    if (d.x * Side < -enemy.BodyRadius || Mathf.Abs(d.y) > _definition.cleaveHalfDepth) continue;
                    if (Battlefield.GroundDistance(enemy.AimPoint(transform.position), transform.position) - enemy.BodyRadius > reach)
                        continue;
                    others.Add(enemy);
                }
                others.Sort((a, b) => (a.transform.position - transform.position).sqrMagnitude
                    .CompareTo((b.transform.position - transform.position).sqrMagnitude));
                for (int i = 0; i < others.Count && hits < _definition.cleaveTargets; i++, hits++) Slay(others[i], damage);
            }
            _hero.OnHitsLanded(hits);
            return true;
        }

        // ---------------------------------------------------------------- Thanh Long Yen Nguyet

        /// <summary>Song dao chay thang theo <paramref name="side"/>; moi quai tren duong an mot lan.</summary>
        IEnumerator Wave(float side)
        {
            var renderer = Sprite("CrescentWave", _definition.crescentFrames, transform.position,
                _definition.quanVuFxScale);
            if (renderer == null) yield break;
            renderer.flipX = side > 0f;   // art bay sang trai
            var hit = new HashSet<Unit>();
            float damage = _hero.AttackDamage * _definition.wavePower;
            float travelled = 0f, t = 0f;
            while (travelled < _definition.waveDistance && _hero != null)
            {
                float move = _definition.waveSpeed * Time.deltaTime;
                renderer.transform.position += new Vector3(side * move, 0f, 0f);
                renderer.sortingOrder = SortOrder.ForUnit(renderer.transform);
                travelled += move;
                Animate(renderer, _definition.crescentFrames, t += Time.deltaTime);
                var at = renderer.transform.position;
                foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
                {
                    if (enemy == null || enemy.IsDying || enemy.FliesHigh || hit.Contains(enemy)) continue;
                    var d = enemy.transform.position - at;
                    if (Mathf.Abs(d.x) > enemy.BodyRadius + 0.2f || Mathf.Abs(d.y) > _definition.waveHalfDepth) continue;
                    hit.Add(enemy);
                    Slay(enemy, damage);
                }
                yield return null;
            }
            Destroy(renderer.gameObject);
        }

        // ---------------------------------------------------------------- Thac Dao Ke

        void OnDamaged(float _)
        {
            if (_feintUsed || _hero.IsDying || !_hero.SecondMinorUnlocked) return;
            if (_hero.Health.IsDead || _hero.Health.Fraction >= _definition.feintThreshold) return;
            _feintUsed = true;
            StartCoroutine(Feint(Side));
        }

        IEnumerator Feint(float side)
        {
            _hero.Shove(-side * _definition.feintRetreat, _definition.feintRetreatTime);
            yield return new WaitForSeconds(_definition.feintRetreatTime + 0.05f);
            if (_hero == null || _hero.IsDying) yield break;
            var clip = _hero.Definition.animations != null ? _hero.Definition.animations.attack : null;
            _hero.PlayAction(clip);
            var fxAt = transform.position + new Vector3(side * 0.6f, _hero.BodyHeight * 0.5f, 0f);
            OneShotFx.Spawn(fxAt, _definition.slashTrailFrames, side > 0f, SortOrder.Air,
                Battlefield.UnitScale * _definition.quanVuFxScale);
            float damage = _hero.AttackDamage * _definition.feintPower;
            foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
            {
                if (enemy == null || enemy.IsDying || enemy.FliesHigh) continue;
                var d = enemy.transform.position - transform.position;
                if (d.x * side < -enemy.BodyRadius || Mathf.Abs(d.y) > _definition.cleaveHalfDepth) continue;
                if (Battlefield.GroundDistance(enemy.AimPoint(transform.position), transform.position) - enemy.BodyRadius >
                    _definition.feintReach) continue;
                Slay(enemy, damage);
            }
        }

        // ---------------------------------------------------------------- Thanh Long Hien

        public override void CastUltimate()
        {
            if (_hero == null || _hero.IsDying) return;
            StartCoroutine(Dragon(Side));
        }

        IEnumerator Dragon(float side)
        {
            var definition = _hero.Definition;
            var start = transform.position + new Vector3(side * 0.5f, _hero.BodyHeight * 0.5f, 0f);
            var renderer = Sprite("SpiritDragon", _definition.dragonFrames, start, _definition.quanVuFxScale);
            if (renderer == null) yield break;
            renderer.flipX = side > 0f;   // art bay sang trai
            float lane = transform.position.y;
            float missing = 1f - _hero.Health.Fraction;
            float damage = _hero.AttackDamage * definition.skillPower * (1f + missing * _definition.dragonMissingHealthBonus);
            var hit = new HashSet<Unit>();
            float travelled = 0f, t = 0f;
            while (travelled < _definition.dragonDistance && _hero != null)
            {
                float move = _definition.dragonSpeed * Time.deltaTime;
                renderer.transform.position += new Vector3(side * move, 0f, 0f);
                travelled += move;
                Animate(renderer, _definition.dragonFrames, t += Time.deltaTime);
                float x = renderer.transform.position.x;
                foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
                {
                    if (enemy == null || enemy.IsDying || hit.Contains(enemy)) continue;
                    if (Mathf.Abs(enemy.transform.position.x - x) > enemy.BodyRadius + 0.4f ||
                        Mathf.Abs(enemy.transform.position.y - lane) > _definition.dragonHalfDepth) continue;
                    hit.Add(enemy);
                    if (Slay(enemy, damage)) _hero.AddMana(definition.manaMax * _definition.dragonKillMana);
                }
                yield return null;
            }
            Destroy(renderer.gameObject);
        }
    }
}
