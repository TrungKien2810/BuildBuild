using System.Collections;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Bo chieu Vo Tang Gio (GAME_DESIGN.md muc 3, `GalemonkDefinition`), gan vao `Hero` khi `HeroDefinition.galemonk` co:
    ///   - Chuong Phong (don thuong): dan gio tu long ban tay toi quai, day lui `boltPush`.
    ///   - Thuan Phong (passiveStar): quai bi gio day/hut cham 20% trong 2s (ca boss).
    ///   - Phong Tao (minorStar): cooldown an, day chuong (dung lai clip attack) — buc tuong gio bay ve phia muc tieu dang
    ///     danh (cheo theo hang cua no), moi quai tren duong an mot lan + day lui `wavePush`.
    ///   - Long Quyen (chieu xin, mana): loc xoay o cum quai dong nhat, hut quai trong `skillRadius` ve tam, giu
    ///     `skillDuration` giay, sat thuong moi giay.
    /// Tinh anh bi hut nhung KHONG bi day; boss khong bi day/hut, chi an cham. Day lui di qua `Unit.PullTo` nen quai dang bi
    /// vay giu cung bi day ra roi tu di lai (luat chan khong bi bo qua).
    /// </summary>
    [RequireComponent(typeof(Hero))]
    public class Galemonk : MonoBehaviour
    {
        const float FramesPerSecond = 9f;   // cung nhip 110 ms/frame voi SpriteAnimationSet
        /// <summary>Long ban tay o o phong gio (attack-5), tinh tu chan, art 1x quay trai: (-34, +41) art-px.</summary>
        static readonly Vector2 PalmOffset = new Vector2(0.34f, 0.41f);
        /// <summary>Thoi gian bi day (PullTo) — ngan nhu mot cu hat.</summary>
        const float PushTime = 0.15f;

        Hero _hero;
        // Hieu ung khong gan vao hero (de khong chay theo) — hero bi huy giua chieu thi coroutine dung, tu don o OnDestroy.
        readonly System.Collections.Generic.List<GameObject> _fx = new System.Collections.Generic.List<GameObject>();
        GalemonkDefinition _definition;
        float _waveCooldown;
        float _waveAt = -1f;

        public void Initialise(Hero hero, GalemonkDefinition definition)
        {
            _hero = hero;
            _definition = definition;
            _waveCooldown = definition.waveCooldown;
            _waveAt = -1f;
        }

        void Update()
        {
            if (_definition == null || _hero == null || _hero.IsDying) return;
            if (_waveAt > 0f && Time.time >= _waveAt)
            {
                _waveAt = -1f;
                StartCoroutine(Wave());
            }
            if (!_hero.MinorUnlocked) return;
            _waveCooldown -= Time.deltaTime;
            if (_waveCooldown > 0f || _waveAt > 0f || _hero.AttackTargetInRange() == null) return;
            var clip = _hero.Definition.animations != null ? _hero.Definition.animations.attack : null;
            if (!_hero.PlayAction(clip)) return;   // dang ban clip khac: thu lai frame sau
            _waveCooldown = _definition.waveCooldown;
            _waveAt = Time.time + Mathf.Max(0, clip.hitFrame - 1) / FramesPerSecond;
        }

        // ---------------------------------------------------------------- Chuong Phong

        /// <summary>Don thuong, goi tu `Hero.FinishCast`: dan gio tu long ban tay toi <paramref name="target"/>.</summary>
        public void Strike(Unit target, float damage)
        {
            float side = _hero.FacesRight ? 1f : -1f;
            var palm = transform.position + new Vector3(side * PalmOffset.x, PalmOffset.y, 0f) * Battlefield.UnitScale;
            StartCoroutine(Bolt(palm, target, damage));
        }

        IEnumerator Bolt(Vector3 from, Unit target, float damage)
        {
            var renderer = Sprite("WindBolt", _definition.boltFrames, from);
            if (renderer == null) yield break;
            float t = 0f;
            var to = target.BodyCenter;
            while (true)
            {
                if (target != null && !target.IsDying) to = target.BodyCenter;
                var step = to - renderer.transform.position;
                float move = _definition.boltSpeed * Time.deltaTime;
                // Art ve quay trai: bay sang phai thi lat.
                renderer.flipX = step.x > 0f;
                Animate(renderer, _definition.boltFrames, t += Time.deltaTime);
                if (step.magnitude <= move) break;
                renderer.transform.position += step.normalized * move;
                yield return null;
            }
            Destroy(renderer.gameObject);
            if (target == null || target.IsDying || _hero == null) yield break;
            OneShotFx.Spawn(target.BodyCenter, _definition.hitFrames, false, SortOrder.Air, Battlefield.UnitScale);
            if (!target.IsElite) Push(target, target.transform.position.x >= transform.position.x ? 1f : -1f,
                _definition.boltPush);
            target.Health.TakeDamage(damage, _hero.Health);
            _hero.OnHitsLanded(1);
        }

        // ---------------------------------------------------------------- Phong Tao

        /// <summary>
        /// Buc tuong gio bay ve phia MUC TIEU dang danh (cheo theo hang cua no), khong con la hang hero dang dung:
        /// ban truoc quai khac hang khong bao gio bi trung, te hon la song KHONG BAN khi chi co quai khac hang
        /// (nguoi dung 2026-10-21). Moi quai tren duong an mot lan.
        /// </summary>
        IEnumerator Wave()
        {
            var direction = new Vector3(_hero.FacesRight ? 1f : -1f, 0f, 0f);
            var target = _hero.AttackTargetInRange();
            if (target != null && !target.IsDying)
            {
                var delta = target.transform.position - transform.position;
                delta.z = 0f;
                if (delta.sqrMagnitude > 0.0001f) direction = delta.normalized;
            }
            var renderer = Sprite("GaleWave", _definition.waveFrames, transform.position);
            if (renderer == null) yield break;
            // Art ve bay sang TRAI: lat + xoay theo huong bay (cheo thi nghien theo goc).
            renderer.flipX = direction.x >= 0f;
            renderer.transform.rotation = Quaternion.Euler(0f, 0f, Tilt(direction));
            var hit = new System.Collections.Generic.HashSet<Unit>();
            float damage = _hero.AttackDamage * _definition.wavePower;
            float travelled = 0f, t = 0f;
            while (travelled < _definition.waveDistance && _hero != null)
            {
                float move = _definition.waveSpeed * Time.deltaTime;
                renderer.transform.position += direction * move;
                renderer.sortingOrder = SortOrder.ForUnit(renderer.transform);
                travelled += move;
                Animate(renderer, _definition.waveFrames, t += Time.deltaTime);
                var at = renderer.transform.position;
                foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
                {
                    if (enemy == null || enemy.IsDying || enemy.FliesHigh || hit.Contains(enemy)) continue;
                    var d = enemy.transform.position - at;
                    // Do trong he truc cua song: doc duong bay = be day buc tuong, ngang duong bay = chieu rong.
                    float along = Mathf.Abs(d.x * direction.x + d.y * direction.y);
                    float across = Mathf.Abs(d.x * direction.y - d.y * direction.x);
                    if (along > enemy.BodyRadius + 0.2f || across > _definition.waveHalfDepth) continue;
                    hit.Add(enemy);
                    OneShotFx.Spawn(enemy.BodyCenter, _definition.hitFrames, false, SortOrder.Air, Battlefield.UnitScale);
                    if (!enemy.IsElite) Push(enemy, direction.x >= 0f ? 1f : -1f, _definition.wavePush);
                    enemy.Health.TakeDamage(damage, _hero.Health);
                }
                yield return null;
            }
            Destroy(renderer.gameObject);
        }

        /// <summary>Goc nghieng cua art ve NGANG khi bay cheo: lat theo dau x nhu `Projectile` (art mac dinh quay trai).</summary>
        static float Tilt(Vector3 direction) =>
            Mathf.Atan2(direction.y, Mathf.Abs(direction.x)) * Mathf.Rad2Deg * (direction.x >= 0f ? 1f : -1f);

        // ---------------------------------------------------------------- Long Quyen

        /// <summary>Chieu xin, goi tu `Hero.FinishCast` (o ha chuong): loc xoay o cum quai dong nhat.</summary>
        public void CastUltimate()
        {
            if (_hero == null || _hero.IsDying) return;
            var point = _hero.DensestCluster(_definition.tornadoSearchRange, _hero.Definition.skillRadius);
            if (!point.HasValue) return;
            StartCoroutine(Tornado(point.Value));
        }

        IEnumerator Tornado(Vector3 centre)
        {
            var def = _hero.Definition;
            float hold = def.skillDuration;
            var root = new GameObject("Tornado");
            _fx.Add(root);
            root.transform.position = centre;
            OneShotFx.SpawnLoop(root.transform, Vector3.zero, _definition.tornadoFrames, hold + _definition.tornadoPullTime,
                SortOrder.ForUnit(root.transform), scale: Battlefield.UnitScale);

            // Hut: moi quai trong ban kinh keo ve quanh tam (giu lech nho de khong chong khit), roi troi den het loc.
            foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
            {
                if (enemy == null || enemy.IsDying || enemy.FliesHigh) continue;
                if (Battlefield.GroundDistance(enemy.transform.position, centre) - enemy.BodyRadius > def.skillRadius) continue;
                Slow(enemy, hold);
                if (enemy.IsBoss) continue;
                var offset = (enemy.transform.position - centre) * 0.15f;
                enemy.PullTo(centre + offset, _definition.tornadoPullTime);
                enemy.ApplyRoot(_definition.tornadoPullTime + hold);
            }

            float until = Time.time + _definition.tornadoPullTime + hold;
            float tick = _definition.tornadoTickInterval;
            while (Time.time < until && _hero != null && !_hero.IsDying)
            {
                yield return new WaitForSeconds(tick);
                float damage = _hero.AttackDamage * def.skillPower * tick;
                foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
                {
                    if (enemy == null || enemy.IsDying || enemy.FliesHigh) continue;
                    if (Battlefield.GroundDistance(enemy.transform.position, centre) - enemy.BodyRadius > def.skillRadius) continue;
                    enemy.Health.TakeDamage(damage, _hero.Health);
                }
            }
            Destroy(root);
        }

        // ---------------------------------------------------------------- chung

        /// <summary>Day <paramref name="enemy"/> lui <paramref name="distance"/> theo x (boss khong bi day) + Thuan Phong.</summary>
        void Push(Unit enemy, float side, float distance)
        {
            Slow(enemy, 0f);
            if (enemy.IsBoss) return;
            var to = Battlefield.Clamp(enemy.transform.position + new Vector3(side * distance, 0f, 0f), enemy.BodyRadius,
                clampRight: true);
            enemy.PullTo(to, PushTime);
        }

        /// <summary>Thuan Phong: cham `slowPercent` trong `slowDuration` (+ <paramref name="extra"/> giay bi giu).</summary>
        void Slow(Unit enemy, float extra)
        {
            if (_hero.PassiveUnlocked) enemy.ApplySlow(_definition.slowPercent, _definition.slowDuration + extra);
        }

        SpriteRenderer Sprite(string name, Sprite[] frames, Vector3 position)
        {
            if (frames == null || frames.Length == 0) return null;
            var go = new GameObject(name);
            _fx.RemoveAll(fx => fx == null);
            _fx.Add(go);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * Battlefield.UnitScale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.sortingOrder = SortOrder.Air;
            return renderer;
        }

        void OnDestroy()
        {
            foreach (var fx in _fx)
                if (fx != null) Destroy(fx);
        }

        static void Animate(SpriteRenderer renderer, Sprite[] frames, float t) =>
            renderer.sprite = frames[(int)(t * FramesPerSecond) % frames.Length];
    }
}
