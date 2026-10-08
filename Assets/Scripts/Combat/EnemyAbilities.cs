using System.Linq;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Chieu rieng cua quai thuong (IDEAS "Quai dich gay trang thai", so Claude de xuat, CHUA can). `EnemyExtras` gan khi
    /// `EnemyDefinition` co `breathCooldown` hoac `ringCooldown`:
    ///   - Phun lua (Ky Nhong Than Hong): hero / linh trong tam phia truoc -> chay clip skill, dai lua bao huong luc gong,
    ///     frame phun moi chay vung Chay; bi choang / chet truoc frame phun thi bo vung bao, vung da phun thi giu;
    ///   - Rung chuong (Goblin Rung Chuong): moi `ringCooldown` giay chay clip skill, quai quanh no tang toc chay + toc danh.
    /// Vi tri FX lay tu ban ghep lop da duyet (`tempart/EnemyIdentities/poison-burn/process_actions.py`): o dai lua dat
    /// lech o than (-112, -52) art-px, o luong lua tu mieng lech (-72, -42).
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public class EnemyAbilities : MonoBehaviour
    {
        /// <summary>Tam o dai lua so voi chan Ky Nhong (art quay trai, 1 art-px = 0,01 local).</summary>
        static readonly Vector2 StripOffset = new Vector2(-1.12f, 0.52f);
        static readonly Vector2 MouthOffset = new Vector2(-0.72f, 0.42f);
        /// <summary>Icon tang toc tren dau quai duoc buff (art 10-12 px, o 128).</summary>
        const float HasteIconGap = 0.1f;

        Enemy _enemy;
        SpriteAnimator _animator;
        SpriteRenderer _renderer;
        EnemyDefinition _definition;
        float _damageMultiplier;
        float _breathReady, _ringReady;
        bool _breathing, _ringing, _facingRight;
        GameObject _telegraph;

        public void Initialise(Enemy enemy, float damageMultiplier)
        {
            _enemy = enemy;
            _definition = enemy.Definition;
            _damageMultiplier = damageMultiplier;
            _renderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<SpriteAnimator>();
            if (_animator != null)
            {
                _animator.HitFrame -= OnHitFrame;
                _animator.HitFrame += OnHitFrame;
            }
            // Lan dau o giua chu ky: ca dan khong cung phun / rung ngay khi vua ra san.
            _breathReady = Time.time + _definition.breathCooldown * 0.5f;
            _ringReady = Time.time + _definition.ringCooldown * 0.5f;
            _breathing = _ringing = false;
            CancelTelegraph();
        }

        void OnDestroy() => CancelTelegraph();

        void Update()
        {
            if (_enemy == null || _definition == null) return;
            if (_enemy.IsDying || _enemy.IsStunned || _enemy.IsFrozen || _enemy.IsLaunched || _enemy.IsPuppet)
            {
                // Ngat truoc frame phun: bo vung bao (chua co lua that).
                if (_breathing) CancelTelegraph();
                _breathing = _ringing = false;
                return;
            }
            if (!WaveManager.Fighting || _enemy.IsBusy) return;

            if (_definition.ringCooldown > 0f && Time.time >= _ringReady) BeginRing();
            else if (_definition.breathCooldown > 0f && Time.time >= _breathReady) TryBreath();
        }

        void OnHitFrame(string clipName)
        {
            if (clipName != "skill" || _enemy == null || _enemy.IsDying) return;
            if (_ringing) Ring();
            else if (_breathing) Breathe();
        }

        // ---------------------------------------------------------------- rung chuong

        void BeginRing()
        {
            _ringReady = Time.time + _definition.ringCooldown;
            var clip = _definition.animations != null ? _definition.animations.skill : null;
            if (clip == null || !clip.IsValid || clip.hitFrame <= 0)
            {
                Ring();
                return;
            }
            _ringing = true;
            _enemy.PlayCast(clip);
        }

        void Ring()
        {
            _ringing = false;
            var at = transform.position;
            OneShotFx.Spawn(at, _definition.ringWaveFrames, false, SortOrder.GroundDecal + 1, Battlefield.UnitScale);
            foreach (var ally in Enemy.Alive.ToArray())
            {
                if (ally == null || ally.IsDying || ally.Side != _enemy.Side) continue;
                if (Battlefield.GroundDistance(ally.transform.position, at) > _definition.ringRadius + ally.BodyRadius) continue;
                ally.ApplyHaste(_definition.ringHaste, _definition.ringSeconds);
                var offset = new Vector3(0f, ally.BodyHeight / Battlefield.UnitScale + HasteIconGap, 0f);
                OneShotFx.SpawnLoop(ally.transform, offset, _definition.hasteFrames, _definition.ringSeconds,
                    SortOrder.ForUnit(ally) + 1);
            }
        }

        // ---------------------------------------------------------------- phun lua

        void TryBreath()
        {
            var target = BreathTarget();
            if (target == null) return;
            var clip = _definition.animations != null ? _definition.animations.skill : null;
            _breathReady = Time.time + _definition.breathCooldown;
            _facingRight = target.transform.position.x > transform.position.x;
            if (_renderer != null) _renderer.flipX = _facingRight;   // art quay trai: flipX = nhin phai

            // Bao huong ngay luc gong: dai lua mo tren dat cho toi frame phun.
            CancelTelegraph();
            _telegraph = new GameObject("BreathTelegraph");
            _telegraph.transform.position = StripCentre();
            var loop = OneShotFx.SpawnLoop(_telegraph.transform, Vector3.zero, _definition.breathTelegraphFrames, 10f,
                SortOrder.GroundDecal + 1, scale: Battlefield.UnitScale);
            Flip(loop);

            if (clip == null || !clip.IsValid || clip.hitFrame <= 0)
            {
                Breathe();
                return;
            }
            _breathing = true;
            _enemy.PlayCast(clip);
        }

        /// <summary>Hero / linh dich gan nhat dung truoc mat trong `breathRange` (theo x), lech hang khong qua 1.</summary>
        Unit BreathTarget()
        {
            Unit best = null;
            float bestDistance = float.PositiveInfinity;
            var at = transform.position;
            foreach (var hero in Hero.Alive)
            {
                if (hero == null || hero.IsDying || !hero.IsHostileTo(_enemy.Side)) continue;
                var delta = hero.transform.position - at;
                float distance = Mathf.Abs(delta.x);
                if (distance > _definition.breathRange || Mathf.Abs(delta.y) > 1f || distance >= bestDistance) continue;
                bestDistance = distance;
                best = hero;
            }
            return best;
        }

        Vector3 StripCentre() => Offset(StripOffset);

        Vector3 Offset(Vector2 local)
        {
            float side = _facingRight ? -1f : 1f;   // offset do tren art quay trai
            return transform.position + new Vector3(local.x * side, local.y, 0f) * Battlefield.UnitScale;
        }

        void Flip(GameObject fx)
        {
            if (fx == null) return;
            var renderer = fx.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.flipX = _facingRight;
        }

        void Breathe()
        {
            _breathing = false;
            CancelTelegraph();
            var centre = StripCentre();
            OneShotFx.Spawn(Offset(MouthOffset), _definition.breathMouthFrames, _facingRight, SortOrder.Air, Battlefield.UnitScale);

            var strip = new GameObject("BreathFire");
            strip.transform.position = centre;
            float seconds = _definition.breathActiveSeconds;
            Flip(OneShotFx.SpawnLoop(strip.transform, Vector3.zero, _definition.breathActiveFrames, seconds,
                SortOrder.GroundDecal + 1, scale: Battlefield.UnitScale));
            Destroy(strip, seconds);

            // Chay ap moi giay (khong moi frame): ngay luc phun + moi giay tiep theo trong vung. Vung song tiep du Ky Nhong chet.
            float dps = _definition.breathBurnPerSecond * _damageMultiplier;
            float burnSeconds = _definition.breathBurnSeconds;
            var source = _enemy.Health;
            var side = _enemy.Side;
            System.Action<Unit> burn = unit =>
            {
                if (unit is Hero) unit.Status.Apply(StatusId.Burn, dps, burnSeconds, source, side);
            };
            foreach (var unit in Unit.Hostiles(side).ToArray())
                if (unit != null && !unit.IsDying
                    && Battlefield.GroundDistance(unit.transform.position, centre) <= _definition.breathRadius + unit.BodyRadius)
                    burn(unit);
            bool flip = _facingRight;
            var fade = _definition.breathFadeFrames;
            SkillZone.Spawn(centre, _definition.breathRadius, seconds, side, null, Battlefield.UnitScale, everySecond: burn,
                onExpire: () => OneShotFx.Spawn(centre, fade, flip, SortOrder.GroundDecal + 1, Battlefield.UnitScale));
        }

        void CancelTelegraph()
        {
            if (_telegraph != null) Destroy(_telegraph);
            _telegraph = null;
        }
    }
}
