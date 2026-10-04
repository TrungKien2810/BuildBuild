using System.Collections;
using System.Collections.Generic;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Bo chieu Phap su sam set (GAME_DESIGN.md muc 3, `StormcallerDefinition`), gan vao `Hero` khi `HeroDefinition.stormcaller` co:
    ///   - Xich Loi (don thuong): tia set trung 1 quai roi nay toi da `chainBounces` quai gan nhat (ban kinh `chainRadius`),
    ///     moi lan nay -30%. Nay qua Cot Thu Loi: +2 lan nay, lan nay do khong giam.
    ///   - Qua Tai (passiveStar): moi tia set trung quai +1 tang (icon tren dau), du 3 -> no: don do +60% + choang 0,3s (boss/
    ///     tinh anh khong choang). Te Liet: quai dang choang an +35% set, set nay uu tien quai dang choang. Set vao khien x2.
    ///   - Cot Thu Loi (minorStar): cam cot dong giua cum quai (dung lai clip attack), song 6s, moi 1s giat quai ke cot.
    ///   - Loi Bo (secondMinorStar): quai ap sat -> hoa set lui 2 o, cho cu no choang 0,5s.
    ///   - Loi Bao (chieu xin, mana): 5 cot set giang xuong cho quai dung trong tam, moi cot sat thuong lon + choang.
    /// Moi tia set (ke ca cot, Loi Bao, vu no Loi Bo) di qua `Zap` nen deu an tang Qua Tai.
    /// </summary>
    [RequireComponent(typeof(Hero))]
    public class Stormcaller : MonoBehaviour
    {
        const float FramesPerSecond = 9f;   // cung nhip 110 ms/frame voi SpriteAnimationSet
        /// <summary>Cot set tu troi cham dat o frame 2 cua strike-fx.</summary>
        const int StrikeImpactFrame = 2;
        /// <summary>Qua cau dau truong o o phong set (attack-5), tinh tu chan, art 1x quay trai: (-30, +38) art-px.</summary>
        static readonly Vector2 OrbOffset = new Vector2(0.30f, 0.38f);
        /// <summary>Dau cot dong tinh tu chan cot (rod: dat o hang 68, dinh qua cau ~hang 10, art 1x).</summary>
        const float RodTopHeight = 0.55f;
        /// <summary>Be day tia set (art ve dung co unit).</summary>
        const float BoltThickness = Battlefield.UnitScale * 0.75f;

        class Rod
        {
            public GameObject Root;
            public float Until;
            public float NextTick;
            public Vector3 Top => Root.transform.position + new Vector3(0f, RodTopHeight * Battlefield.UnitScale, 0f);
        }

        Hero _hero;
        StormcallerDefinition _definition;
        float _rodCooldown;
        float _rodAt = -1f;
        Vector3 _rodPoint;
        float _blinkCooldown;
        readonly List<Rod> _rods = new List<Rod>();
        /// <summary>Tang Qua Tai tung quai + icon tren dau no (con cua quai, quai chet thi mat theo).</summary>
        readonly Dictionary<Unit, (int count, SpriteRenderer pip)> _charges = new Dictionary<Unit, (int, SpriteRenderer)>();

        public void Initialise(Hero hero, StormcallerDefinition definition)
        {
            _hero = hero;
            _definition = definition;
            _rodCooldown = definition.rodCooldown;
            _blinkCooldown = definition.blinkCooldown;
            _rodAt = -1f;
            ClearRods();
        }

        void OnDestroy() => ClearRods();

        void Update()
        {
            if (_definition == null || _hero == null || _hero.IsDying) return;
            UpdateRods();

            if (_hero.SecondMinorUnlocked)
            {
                _blinkCooldown -= Time.deltaTime;
                if (_blinkCooldown <= 0f && !_hero.Casting && !_hero.IsStunned)
                {
                    var threat = NearestThreat();
                    if (threat != null) Blink(threat);
                }
            }

            if (_rodAt > 0f && Time.time >= _rodAt)
            {
                _rodAt = -1f;
                SpawnRod(_rodPoint);
            }
            if (!_hero.MinorUnlocked) return;
            _rodCooldown -= Time.deltaTime;
            if (_rodCooldown > 0f || _rodAt > 0f) return;
            var point = _hero.DensestCluster(_hero.CurrentAttackRange, _definition.rodRadius);
            if (!point.HasValue) return;
            var clip = _hero.Definition.animations != null ? _hero.Definition.animations.attack : null;
            if (!_hero.PlayAction(clip)) return;   // dang ban clip khac: thu lai frame sau
            _rodCooldown = _definition.rodCooldown;
            _rodPoint = point.Value;
            _rodAt = Time.time + Mathf.Max(0, clip.hitFrame - 1) / FramesPerSecond;
        }

        // ---------------------------------------------------------------- Xich Loi

        /// <summary>Don thuong, goi tu `Hero.FinishCast`: tia set tu qua cau dau truong toi <paramref name="target"/> roi nay.</summary>
        public void Strike(Unit target, float damage)
        {
            var side = _hero.FacesRight ? 1f : -1f;
            var orb = transform.position + new Vector3(side * OrbOffset.x, OrbOffset.y, 0f) * Battlefield.UnitScale;
            _hero.OnHitsLanded(Chain(target, damage, orb));
        }

        int Chain(Unit first, float damage, Vector3 from)
        {
            var hit = new HashSet<Unit>();
            var usedRods = new HashSet<Rod>();
            int bounces = _definition.chainBounces;
            bool keepDamage = false;
            var point = from;
            var current = first;
            while (true)
            {
                var at = BodyCentre(current);
                OneShotFx.SpawnBeam(point, at, _definition.boltFrames, SortOrder.Air, BoltThickness);
                Zap(current, damage);
                hit.Add(current);
                point = at;

                // Nay qua cot: dong set chay vao dau cot roi ra tiep, them lan nay va lan sau khong giam.
                var rod = NearestRod(point, usedRods);
                if (rod != null)
                {
                    usedRods.Add(rod);
                    OneShotFx.SpawnBeam(point, rod.Top, _definition.boltFrames, SortOrder.Air, BoltThickness);
                    point = rod.Top;
                    bounces += _definition.rodBonusBounces;
                    keepDamage = true;
                }

                if (bounces <= 0) break;
                var next = NextTarget(point, hit);
                if (next == null) break;
                bounces--;
                if (!keepDamage) damage *= 1f - _definition.chainFalloff;
                keepDamage = false;
                current = next;
            }
            return hit.Count;
        }

        /// <summary>Quai gan nhat chua trung trong `chainRadius`; Te Liet (noi tai) uu tien quai dang choang.</summary>
        Unit NextTarget(Vector3 point, HashSet<Unit> hit)
        {
            Unit best = null;
            bool bestStunned = false;
            float bestSqr = float.PositiveInfinity;
            bool preferStunned = _hero.PassiveUnlocked;
            foreach (var enemy in Unit.Hostiles(_hero.Side))
            {
                if (enemy == null || enemy.IsDying || hit.Contains(enemy) || !enemy.TargetableBy(_hero)) continue;
                float reach = _definition.chainRadius + enemy.BodyRadius;
                float sqr = (BodyCentre(enemy) - point).sqrMagnitude;
                if (sqr > reach * reach) continue;
                bool stunned = preferStunned && enemy.IsStunned;
                if (best != null && (bestStunned && !stunned || bestStunned == stunned && sqr >= bestSqr)) continue;
                best = enemy;
                bestStunned = stunned;
                bestSqr = sqr;
            }
            return best;
        }

        Rod NearestRod(Vector3 point, HashSet<Rod> used)
        {
            Rod best = null;
            float bestSqr = _definition.chainRadius * _definition.chainRadius;
            foreach (var rod in _rods)
            {
                if (used.Contains(rod)) continue;
                float sqr = (rod.Top - point).sqrMagnitude;
                if (sqr > bestSqr) continue;
                bestSqr = sqr;
                best = rod;
            }
            return best;
        }

        static Vector3 BodyCentre(Unit enemy) => enemy.transform.position + new Vector3(0f, enemy.BodyHeight * 0.5f, 0f);

        // ---------------------------------------------------------------- Qua Tai

        /// <summary>
        /// Mot tia set trung <paramref name="enemy"/>: Te Liet, tang Qua Tai (du thi no), set vao khien x2, choang
        /// <paramref name="stun"/> giay (boss mot nua) roi gay sat thuong.
        /// </summary>
        void Zap(Unit enemy, float damage, float stun = 0f)
        {
            if (enemy == null || enemy.IsDying) return;
            float stunSeconds = stun > 0f ? (enemy.IsBoss ? stun * 0.5f : stun) : 0f;
            if (_hero.PassiveUnlocked)
            {
                if (enemy.IsStunned) damage *= 1f + _definition.paralyzeBonus;
                if (AddCharge(enemy))
                {
                    damage *= 1f + _definition.overloadBonus;
                    if (!enemy.IsBoss && !enemy.IsElite) stunSeconds = Mathf.Max(stunSeconds, _definition.overloadStun);
                    OneShotFx.Spawn(enemy.transform.position, _definition.burstFrames, false, SortOrder.Air,
                        Battlefield.UnitScale * 0.6f);
                }
                // Phan sat thuong roi vao khien tinh x2: x mau that an het khien khi 2x = khien.
                float shield = enemy.Health.Shield;
                if (shield > 0f && _definition.shieldMultiplier > 1f)
                    damage += Mathf.Min(damage, shield / _definition.shieldMultiplier) * (_definition.shieldMultiplier - 1f);
            }
            if (stunSeconds > 0f) enemy.ApplyStun(stunSeconds, _hero.Definition.stunFrames);
            OneShotFx.Spawn(BodyCentre(enemy), _definition.hitFrames, false, SortOrder.Air, Battlefield.UnitScale * 0.5f);
            enemy.Health.TakeDamage(damage, _hero.Health);
        }

        /// <summary>+1 tang; du `maxCharges` thi xoa tang va tra ve true (no).</summary>
        bool AddCharge(Unit enemy)
        {
            _charges.TryGetValue(enemy, out var mark);
            int count = mark.count + 1;
            if (count >= _definition.maxCharges)
            {
                if (mark.pip != null) Destroy(mark.pip.gameObject);
                _charges.Remove(enemy);
                return true;
            }
            var pip = mark.pip;
            var frames = _definition.chargePipFrames;
            if (pip == null && frames != null && frames.Length > 0)
            {
                var go = new GameObject("ChargePips");
                go.transform.SetParent(enemy.transform, false);
                // Tren dinh dau quai (local cua quai = world / scale cua no).
                go.transform.localPosition = enemy.transform.InverseTransformVector(
                    new Vector3(0f, enemy.BodyHeight + 0.15f, 0f));
                pip = go.AddComponent<SpriteRenderer>();
                pip.sortingOrder = SortOrder.Air;
            }
            if (pip != null) pip.sprite = frames[Mathf.Min(count, frames.Length) - 1];
            _charges[enemy] = (count, pip);
            if (_charges.Count > 64) CleanCharges();
            return false;
        }

        void CleanCharges()
        {
            var dead = new List<Unit>();
            foreach (var enemy in _charges.Keys)
                if (enemy == null || enemy.IsDying) dead.Add(enemy);
            foreach (var enemy in dead) _charges.Remove(enemy);
        }

        // ---------------------------------------------------------------- Cot Thu Loi

        void SpawnRod(Vector3 point)
        {
            if (_hero.IsDying) return;
            var root = new GameObject("LightningRod");
            root.transform.position = point;
            OneShotFx.SpawnLoop(root.transform, Vector3.zero, _definition.rodFrames, _definition.rodLifetime,
                SortOrder.ForUnit(root.transform), scale: Battlefield.UnitScale);
            _rods.Add(new Rod
            {
                Root = root,
                Until = Time.time + _definition.rodLifetime,
                NextTick = Time.time + _definition.rodTickInterval,
            });
        }

        void UpdateRods()
        {
            for (int i = _rods.Count - 1; i >= 0; i--)
            {
                var rod = _rods[i];
                if (rod.Root == null || Time.time >= rod.Until)
                {
                    if (rod.Root != null) Destroy(rod.Root);
                    _rods.RemoveAt(i);
                    continue;
                }
                if (Time.time < rod.NextTick) continue;
                rod.NextTick += _definition.rodTickInterval;
                float damage = _hero.AttackDamage * _definition.rodPower;
                foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
                {
                    if (enemy == null || enemy.IsDying) continue;
                    if (Battlefield.GroundDistance(enemy.AimPoint(rod.Root.transform.position), rod.Root.transform.position) - enemy.BodyRadius >
                        _definition.rodRadius) continue;
                    OneShotFx.SpawnBeam(rod.Top, BodyCentre(enemy), _definition.boltFrames, SortOrder.Air, BoltThickness);
                    Zap(enemy, damage);
                }
            }
        }

        void ClearRods()
        {
            foreach (var rod in _rods)
                if (rod.Root != null) Destroy(rod.Root);
            _rods.Clear();
        }

        // ---------------------------------------------------------------- Loi Bo

        Unit NearestThreat()
        {
            Unit best = null;
            float bestGap = _definition.blinkTrigger;
            foreach (var enemy in Unit.Hostiles(_hero.Side))
            {
                if (enemy == null || enemy.IsDying || enemy.FliesHigh) continue;
                float gap = Battlefield.GroundDistance(enemy.AimPoint(transform.position), transform.position) - enemy.BodyRadius;
                if (gap > bestGap) continue;
                bestGap = gap;
                best = enemy;
            }
            return best;
        }

        /// <summary>Hoa set lui `blinkDistance` theo truc x, ra xa quai ap sat; cho cu no choang.</summary>
        void Blink(Unit threat)
        {
            _blinkCooldown = _definition.blinkCooldown;
            var from = transform.position;
            float away = threat.transform.position.x >= from.x ? -1f : 1f;
            var to = Battlefield.Clamp(from + new Vector3(away * _definition.blinkDistance, 0f, 0f), _hero.BodyRadius,
                clampRight: true);

            OneShotFx.Spawn(from, _definition.blinkFrames, false, SortOrder.Air, Battlefield.UnitScale);
            transform.position = to;
            OneShotFx.Spawn(to, _definition.blinkFrames, false, SortOrder.Air, Battlefield.UnitScale);
            OneShotFx.Spawn(from, _definition.burstFrames, false, SortOrder.Air, Battlefield.UnitScale);

            float damage = _hero.AttackDamage * _definition.blinkPower;
            foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
            {
                if (enemy == null || enemy.IsDying) continue;
                if (Battlefield.GroundDistance(enemy.AimPoint(from), from) - enemy.BodyRadius > _definition.blinkBurstRadius)
                    continue;
                Zap(enemy, damage, _definition.blinkStun);
            }
        }

        // ---------------------------------------------------------------- Loi Bao

        /// <summary>Chieu xin, goi tu `Hero.FinishCast` (o dam truong cham dat): cot set giang lan luot xuong quai trong tam.</summary>
        public void CastUltimate()
        {
            if (_hero == null || _hero.IsDying) return;
            var targets = new List<Unit>();
            float range = _hero.CurrentAttackRange;
            foreach (var enemy in Unit.Hostiles(_hero.Side))
                if (enemy != null && !enemy.IsDying && enemy.TargetableBy(_hero) &&
                    (enemy.AimPoint(transform.position) - transform.position).sqrMagnitude <= range * range)
                    targets.Add(enemy);
            if (targets.Count == 0) return;
            targets.Sort((a, b) => (a.transform.position - transform.position).sqrMagnitude
                .CompareTo((b.transform.position - transform.position).sqrMagnitude));

            float damage = _hero.AttackDamage * _hero.Definition.skillPower;
            // It quai hon so cot: cot thua giang vong lai tu con gan nhat.
            for (int i = 0; i < _definition.stormBolts; i++)
                StartCoroutine(Bolt(targets[i % targets.Count], damage, i * _definition.stormInterval));
        }

        IEnumerator Bolt(Unit target, float damage, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (target == null || target.IsDying) yield break;
            OneShotFx.Spawn(target.transform.position, _definition.strikeFrames, false, SortOrder.Air, Battlefield.UnitScale);
            yield return new WaitForSeconds((StrikeImpactFrame - 1) / FramesPerSecond);
            if (target == null || target.IsDying || _hero == null) yield break;
            Zap(target, damage, _hero.Definition.skillDuration);
        }
    }
}
