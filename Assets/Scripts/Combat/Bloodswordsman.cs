using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Bo chieu Huyet Kiem Khach (`BloodswordsmanKit`), gan vao `Hero` khi `HeroDefinition.bloodswordsman` co:
    ///   - Huyet Y (passiveStar): mau cang mat cang dau; tu ha dich hoi 6% mau, hoi chieu 2s.
    ///   - Hien Huyet (minorStar): tieu mau hien tai, `offerDuration` giay +cong, chem them 1 dich, hoi mau moi don trung.
    ///   - Phan Huyet (secondMinorStar): duoi `parryThreshold` mau, do don truc tiep dau tien roi phan chem 2x.
    ///   - Toai Kiem Quy Huyet (chieu xin, mana): gam manh kiem vao toi da 3 dich, sau `shardDelay` thu ve gay 3x va hoi mau.
    /// </summary>
    [RequireComponent(typeof(Hero))]
    public class Bloodswordsman : MonoBehaviour
    {
        Hero _hero;
        BloodswordsmanKit _kit;
        float _offerUntil, _offerReady, _parryUntil, _parryReady, _killHealReady, _returnAt, _offerHitAt = -1f;
        Health _parrySource;

        /// <summary>Manh kiem dang cam trong dich: hero dung yen giu frame cuoi clip cast toi khi thu ve.</summary>
        public bool Busy => _returnAt > Time.time;

        public void Initialise(Hero hero, BloodswordsmanKit kit)
        {
            _hero = hero;
            _kit = kit;
            _offerUntil = _offerReady = _parryUntil = _parryReady = _killHealReady = _returnAt = 0f;
            _offerHitAt = -1f;
            _parrySource = null;
            _hero.Health.AdjustDirectHit = AdjustParry;
        }

        void OnEnable() { Health.KilledBy += OnKilled; Health.DirectDamage += OnParryHit; }

        void OnDisable()
        {
            Health.KilledBy -= OnKilled;
            Health.DirectDamage -= OnParryHit;
            if (_hero != null && _hero.Health != null) _hero.Health.AdjustDirectHit = null;
        }

        void Update()
        {
            if (_kit == null || _hero == null || _hero.IsDying || !WaveManager.Fighting) return;
            if (_offerHitAt > 0f && Time.time >= _offerHitAt)
            {
                _offerHitAt = -1f;
                _hero.Health.SpendBlood(_kit.offerCost);
                _offerUntil = Time.time + _kit.offerDuration;
                OneShotFx.SpawnLoop(transform, new Vector3(0, _hero.BodyHeight * 0.5f / Battlefield.UnitScale), _kit.aura,
                    _kit.offerDuration, SortOrder.ForUnit(_hero) + 1);
            }
            if (_hero.Casting || Busy || _hero.IsStunned || _hero.IsDisarmed || _hero.AttackTargetInRange() == null) return;
            if (_hero.SecondMinorUnlocked && Time.time >= _parryReady && _hero.Health.Fraction < _kit.parryThreshold
                && _hero.PlayAction(_kit.parry))
            {
                _parryReady = Time.time + _kit.parryCooldown;
                _parryUntil = Time.time + _kit.parryDuration;
                return;
            }
            if (_hero.MinorUnlocked && Time.time >= _offerReady && _hero.PlayAction(_kit.offer))
            {
                _offerReady = Time.time + _kit.offerCooldown;
                _offerHitAt = Time.time + _hero.HitDelay(_kit.offer);
            }
        }

        /// <summary>He so cong: Huyet Y theo mau da mat + Hien Huyet dang chay.</summary>
        public void Multipliers(ref float damage)
        {
            damage *= 1f + (_hero.PassiveUnlocked ? Mathf.Min(0.35f, (1f - _hero.Health.Fraction) * 0.5f) : 0f)
                + (Time.time < _offerUntil ? _kit.offerPower : 0f);
        }

        /// <summary>Don thuong, goi tu `Hero.FinishCast`: chem muc tieu (Hien Huyet: them dich gan nhat trong tam).</summary>
        public void Strike(Unit target, float damage)
        {
            var victims = new List<Unit> { target };
            if (Time.time < _offerUntil)
            {
                var second = Unit.Hostiles(_hero.Side).ToArray()
                    .Where(u => u != null && u != target && !u.IsDying && _hero.InAttackRange(u))
                    .OrderBy(u => Battlefield.GroundDistance(u.transform.position, target.transform.position)).FirstOrDefault();
                if (second != null) victims.Add(second);
            }
            int hits = 0;
            foreach (var victim in victims)
            {
                float before = victim.Health.Current;
                victim.Health.TakeDamage(damage, _hero.Health);
                OneShotFx.Spawn(victim.BodyCenter, _hero.Definition.impactFrames, !_hero.FacesRight, SortOrder.Air, Battlefield.UnitScale);
                if (victim.Health.Current < before) hits++;
            }
            if (hits > 0 && Time.time < _offerUntil) _hero.Health.Heal(_hero.Health.Max * 0.04f, _hero.Health);
            _hero.OnHitsLanded(hits);
        }

        float AdjustParry(float amount, Health source)
        {
            if (Time.time >= _parryUntil || source == null || source == _hero.Health) return amount;
            _parryUntil = 0f;
            _parrySource = source;
            return amount * 0.5f;
        }

        void OnParryHit(Health victim, Health source, float amount, Faction? side)
        {
            if (_hero == null || victim != _hero.Health || _parrySource == null || source != _parrySource) return;
            _parrySource = null;
            if (_hero.Health.IsDead || _hero.IsDying) return;
            source.TakeDamage(_hero.AttackDamage * 2f, _hero.Health, kind: DamageKind.Reflected);
            _hero.Health.Heal(_hero.Health.Max * 0.08f, _hero.Health);
            OneShotFx.Spawn(source.transform.position, _kit.parryHit, false, SortOrder.Air, Battlefield.UnitScale);
        }

        void OnKilled(Health victim, Health source, DamageKind kind)
        {
            if (_hero == null || source != _hero.Health || kind == DamageKind.Linked || _hero.IsDying
                || !_hero.PassiveUnlocked || Time.time < _killHealReady) return;
            var unit = victim.GetComponent<Unit>();
            if (unit == null || !unit.IsHostileTo(_hero.Side)) return;
            _killHealReady = Time.time + 2f;
            _hero.Health.Heal(_hero.Health.Max * 0.06f, _hero.Health);
        }

        /// <summary>Toai Kiem Quy Huyet, goi o frame cham cua clip cast.</summary>
        public void CastUltimate()
        {
            var origin = transform.position;
            float facing = _hero.FacesRight ? 1f : -1f;
            var targets = Unit.Hostiles(_hero.Side).ToArray().Where(u => u != null && !u.IsDying && u.TargetableBy(_hero)
                && (u.transform.position.x - origin.x) * facing >= 0f
                && Battlefield.GroundDistance(u.AimPoint(origin), origin) <= _kit.shardRange + u.BodyRadius
                && Mathf.Abs(u.transform.position.y - origin.y) <= Mathf.Max(0.6f, Mathf.Abs(u.transform.position.x - origin.x)))
                .OrderBy(u => Battlefield.GroundDistance(u.transform.position, origin)).Take(3).ToArray();
            if (targets.Length == 0) return;
            _hero.Health.SpendBlood(_kit.shatterCost);
            float damage = _hero.AttackDamage;
            _returnAt = Time.time + _kit.shardDelay;
            var positions = targets.Select(u => u.BodyCenter).ToArray();
            foreach (var target in targets)
            {
                TravellingHeroFx.Spawn(_hero.BodyCenter, target.BodyCenter, _kit.shardOut, 0.18f);
                target.Health.TakeDamage(damage * 2f, _hero.Health);
                if (!target.IsDying) OneShotFx.SpawnLoop(target.transform,
                    new Vector3(0, target.BodyHeight * 0.5f / Battlefield.UnitScale), _kit.shardStuck, _kit.shardDelay, SortOrder.Air);
            }
            StartCoroutine(ReturnShards(targets, positions, damage));
        }

        IEnumerator ReturnShards(Unit[] targets, Vector3[] positions, float damage)
        {
            yield return new WaitForSeconds(_kit.shardDelay);
            _returnAt = 0f;
            if (_hero.IsDying || _hero.Health.IsDead) yield break;
            for (int i = 0; i < targets.Length; i++)
            {
                var target = targets[i];
                bool alive = target != null && !target.IsDying;
                TravellingHeroFx.Spawn(alive ? target.BodyCenter : positions[i], _hero.BodyCenter, _kit.shardReturn, 0.18f);
                if (alive && target.IsHostileTo(_hero.Side)) target.Health.TakeDamage(damage * 3f, _hero.Health);
            }
            _hero.Health.Heal(_hero.Health.Max * 0.08f * targets.Length, _hero.Health);
            OneShotFx.Spawn(_hero.BodyCenter, _kit.reform, !_hero.FacesRight, SortOrder.Air, Battlefield.UnitScale);
            _hero.ReleaseHeldClip();
        }
    }
}
