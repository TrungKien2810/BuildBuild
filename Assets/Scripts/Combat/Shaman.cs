using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Ba chieu cua boss Phap su Orc (GAME_DESIGN.md muc 5), gan qua `EnemyExtras` khi `EnemyDefinition.shaman` co.
    /// Chi dung API public cua `Enemy` (cung cach `Boss`):
    ///   - Mieng Ho Lua: moi `pitInterval` giay, CHI khi co hero / linh trong `pitRange` — danh dau cum dong nhat, sau
    ///                   `pitDelay` giay no: `pitDamage` moi unit phe ta con trong vong. Chua co ai thi giu chieu.
    ///   - Khien Phep  : moi `shieldInterval` giay boc khien `shieldAmount` len boss khac (Minotaur), khong co thi quai
    ///                   gan nhat trong `shieldRange`. Het `shieldDuration` giay thi khien tan.
    ///   - Dich Chuyen : hero CAN CHIEN ap sat trong `teleportTrigger` -> bien mat, hien lai xa hon `teleportDistance`
    ///                   ve phia phai, sat mep phai thi re len / xuong lane (hoi `teleportCooldown`).
    /// Ho Lua / Khien chay clip `castBody`, chieu ra o hitFrame. Chieu chi bat dau khi shaman khong dang vung don.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public class Shaman : MonoBehaviour
    {
        const float MeleeRange = 1.5f;

        Enemy _enemy;
        ShamanDefinition _definition;
        SpriteAnimator _animator;
        float _damageScale = 1f;
        float _pitTimer;
        float _shieldTimer;
        float _teleportTimer;
        System.Action _pendingCast;

        // Mot vong danh dau dang cho no (interval 12 > delay 1,5 nen khong bao gio chong hai vong).
        Vector3 _pitAt;
        float _pitBlowsAt;

        // Mot khien dang song (interval 18 > duration 6).
        Health _shielded;
        float _shieldUntil;
        GameObject _shieldFx;

        /// <param name="damageScale">He so sat thuong boss cua wave — nhan vao Ho Lua va luong khien.</param>
        public void Initialise(Enemy enemy, ShamanDefinition definition, float damageScale)
        {
            _enemy = enemy;
            _definition = definition;
            _damageScale = damageScale;
            _animator = GetComponent<SpriteAnimator>();
            if (_animator != null)
            {
                _animator.HitFrame -= OnHitFrame;
                _animator.HitFrame += OnHitFrame;
                _animator.ClipFinished -= OnClipFinished;
                _animator.ClipFinished += OnClipFinished;
            }
            if (transform.Find("HealthBar") == null) StatBar.AttachHealth(enemy);
            _pitTimer = definition.pitInterval;
            _shieldTimer = definition.shieldInterval;
            _teleportTimer = 0f;
            _pitBlowsAt = 0f;
            _pendingCast = null;
        }

        void OnDestroy()
        {
            if (_animator != null)
            {
                _animator.HitFrame -= OnHitFrame;
                _animator.ClipFinished -= OnClipFinished;
            }
            EndShield();
        }

        void Update()
        {
            if (_enemy == null || _definition == null) return;
            if (_shielded != null && Time.time >= _shieldUntil) EndShield();
            // Vong da danh dau van no ke ca khi shaman vua chet — nguoi choi da thay canh bao.
            if (_pitBlowsAt > 0f && Time.time >= _pitBlowsAt) BlowPit();
            if (_enemy.IsDying || _enemy.IsFrozen) return;

            _pitTimer -= Time.deltaTime;
            _shieldTimer -= Time.deltaTime;
            _teleportTimer -= Time.deltaTime;
            if (_enemy.IsBusy) return;

            if (_teleportTimer <= 0f && MeleeThreat())
            {
                _teleportTimer = _definition.teleportCooldown;
                _enemy.PlayCast(_definition.vanishBody);
                if (!_definition.vanishBody.IsValid) Reappear();
            }
            else if (_shieldTimer <= 0f && ShieldTarget() != null)
            {
                _shieldTimer = _definition.shieldInterval;
                Cast(CastShield);
            }
            else if (_pitTimer <= 0f && PitCentre().HasValue)
            {
                _pitTimer = _definition.pitInterval;
                Cast(MarkPit);
            }
        }

        void Cast(System.Action effect)
        {
            var body = _definition.castBody;
            if (body == null || !body.IsValid || _animator == null || body.hitFrame <= 0)
            {
                _enemy.PlayCast(body);
                effect();
                return;
            }
            _pendingCast = effect;
            _enemy.PlayCast(body);
        }

        void OnHitFrame(string clip)
        {
            if (_enemy == null || _enemy.IsDying || !isActiveAndEnabled) return;
            if (clip != _definition.castBody.name || _pendingCast == null) return;
            var effect = _pendingCast;
            _pendingCast = null;
            effect();
        }

        void OnClipFinished(string clip)
        {
            if (_enemy == null || _enemy.IsDying) return;
            if (clip == _definition.castBody.name) _pendingCast = null;   // bi cat ngang thi bo chieu
            else if (clip == _definition.vanishBody.name) Reappear();
        }

        // ---------------------------------------------------------------- Dich Chuyen

        bool MeleeThreat()
        {
            foreach (var hero in Hero.Alive)
            {
                if (hero == null || hero.IsDying || !hero.IsHostileTo(_enemy.Side) || hero.CurrentAttackRange > MeleeRange) continue;
                if (Battlefield.GroundDistance(hero.transform.position, transform.position) - hero.BodyRadius
                    <= _definition.teleportTrigger + _enemy.BodyRadius) return true;
            }
            return false;
        }

        /// <summary>Huong thu dich chuyen tren MAT DAT, uu tien truoc -> sau (ngang diem thi lay huong truoc).</summary>
        static readonly Vector2[] TeleportDirections =
        {
            new Vector2(1f, 0f), new Vector2(0.7f, 0.7f), new Vector2(0.7f, -0.7f), new Vector2(0f, 1f), new Vector2(0f, -1f),
        };

        /// <summary>
        /// Hien lai `teleportDistance` (do tren mat dat) theo huong xa hero can chien gan nhat nhat. Uu tien ve phai; sat
        /// mep phai (hero can chien duoi toi day) thi Clamp an het buoc ngang -> re len / xuong lane thay vi dung tai cho.
        /// </summary>
        void Reappear()
        {
            var from = transform.position;
            var threat = NearestMelee();
            var best = from;
            float bestScore = -1f;
            foreach (var direction in TeleportDirections)
            {
                var step = new Vector3(direction.x, direction.y * Battlefield.DepthSquash, 0f) * _definition.teleportDistance;
                var to = Battlefield.Clamp(from + step, _enemy.BodyRadius, clampRight: true);
                float score = Battlefield.GroundDistance(to, threat ?? from);
                if (score <= bestScore + 0.01f) continue;
                bestScore = score;
                best = to;
            }
            transform.position = best;
            _enemy.PlayCast(_definition.appearBody);
        }

        Vector3? NearestMelee()
        {
            Vector3? nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (var hero in Hero.Alive)
            {
                if (hero == null || hero.IsDying || !hero.IsHostileTo(_enemy.Side) || hero.CurrentAttackRange > MeleeRange) continue;
                float distance = Battlefield.GroundDistance(hero.transform.position, transform.position);
                if (distance >= nearestDistance) continue;
                nearestDistance = distance;
                nearest = hero.transform.position;
            }
            return nearest;
        }

        // ---------------------------------------------------------------- Mieng Ho Lua

        /// <summary>Hero / linh trong `pitRange` co nhieu dong doi trong `pitRadius` quanh no nhat; null = khong ai.</summary>
        Vector3? PitCentre()
        {
            Vector3? best = null;
            int bestCount = 0;
            foreach (var candidate in Hero.Alive)
            {
                if (candidate == null || candidate.IsDying || !candidate.IsHostileTo(_enemy.Side)) continue;
                if (Battlefield.GroundDistance(candidate.transform.position, transform.position) > _definition.pitRange) continue;
                int count = 0;
                foreach (var other in Hero.Alive)
                    if (other != null && !other.IsDying && other.IsHostileTo(_enemy.Side) &&
                        Battlefield.GroundDistance(other.transform.position, candidate.transform.position) <= _definition.pitRadius)
                        count++;
                if (count <= bestCount) continue;
                bestCount = count;
                best = candidate.transform.position;
            }
            return best;
        }

        void MarkPit()
        {
            var centre = PitCentre();
            if (!centre.HasValue) return;
            _pitAt = centre.Value;
            _pitBlowsAt = Time.time + _definition.pitDelay;
            var mark = new GameObject("PitMark");
            mark.transform.position = _pitAt;
            OneShotFx.SpawnLoop(mark.transform, Vector3.zero, _definition.pitMarkFrames, _definition.pitDelay,
                SortOrder.GroundDecal + 1, scale: Battlefield.UnitScale);
            Destroy(mark, _definition.pitDelay);
        }

        void BlowPit()
        {
            _pitBlowsAt = 0f;
            foreach (var hero in Hero.Alive.ToArray())
            {
                if (hero == null || hero.IsDying || !hero.IsHostileTo(_enemy.Side)) continue;
                if (Battlefield.GroundDistance(hero.transform.position, _pitAt) - hero.BodyRadius > _definition.pitRadius) continue;
                hero.Health.TakeDamage(_definition.pitDamage * _damageScale, _enemy != null ? _enemy.Health : null, kind: DamageKind.Periodic);
            }
            OneShotFx.Spawn(_pitAt, _definition.pitBurstFrames, false, SortOrder.Air, Battlefield.UnitScale);
        }

        // ---------------------------------------------------------------- Khien Phep

        /// <summary>Boss khac con song (Minotaur) truoc, khong co thi quai gan nhat trong `shieldRange`.</summary>
        Enemy ShieldTarget()
        {
            Enemy nearest = null;
            float nearestSqr = _definition.shieldRange * _definition.shieldRange;
            foreach (var enemy in Enemy.Alive)
            {
                if (enemy == null || enemy == _enemy || enemy.IsDying) continue;
                if (enemy.IsBoss) return enemy;
                float sqr = (enemy.AimPoint(transform.position) - transform.position).sqrMagnitude;
                if (sqr > nearestSqr) continue;
                nearestSqr = sqr;
                nearest = enemy;
            }
            return nearest;
        }

        void CastShield()
        {
            var target = ShieldTarget();
            if (target == null) return;
            EndShield();
            _shielded = target.Health;
            _shieldUntil = Time.time + _definition.shieldDuration;
            _shielded.AddShield(_definition.shieldAmount * _damageScale);
            _shielded.ShieldBroken += OnShieldBroken;
            // Art ve theo co shaman (o 256, vo cao ~219 px) — cha da nhan UnitScale nen scale local 1 = dung co art.
            // Pivot FX o TAM o: dat vao giua than (toa do local cua cha = world / UnitScale), khong vao chan.
            var bodyCentre = new Vector3(0f, target.BodyHeight * 0.5f / Battlefield.UnitScale, 0f);
            _shieldFx = OneShotFx.SpawnLoop(target.transform, bodyCentre, _definition.shieldFrames,
                _definition.shieldDuration, SortOrder.Air);
        }

        void OnShieldBroken() => EndShield();

        void EndShield()
        {
            if (_shielded != null)
            {
                _shielded.ShieldBroken -= OnShieldBroken;
                _shielded.ClearShield();
            }
            _shielded = null;
            if (_shieldFx != null) Destroy(_shieldFx);
            _shieldFx = null;
        }
    }
}
