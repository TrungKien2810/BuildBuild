using System.Collections.Generic;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Kit boss wave 30 Rong Ma Hoa (GAME_DESIGN.md muc 5), gan qua `EnemyExtras` khi `EnemyDefinition.dragon` co. Rong bay
    /// la la nhung tinh nhu QUAI MAT DAT: di/chan/danh la luat cua `Enemy` (walk = clip idle vo canh). Chi dung API public
    /// cua `Enemy` (cung cach `Boss`, `Shaman`):
    ///   - Don thuong: frame cham cua attack, them toi da `cleaveTargets` unit quanh mom (tinh ca muc tieu chinh).
    ///   - Hoi Tho Ma Hoa: moi `breathInterval` giay, CHI khi co hero trong dai lua — ngoc dau (frame 1-3 cua breath) roi
    ///     phun: sat thuong tuc thi + dat chay `burnDuration` giay (Thieu Hon: khong hoi mau / mana).
    ///   - Vo Canh Cuong Phong: moi `galeInterval` giay, CHI khi co unit trong `galeRadius` — hat lui ~1 o.
    ///   - Goi Hon Lua: moi `soulInterval` giay — quai ho tong boc lua ma (+sat thuong, chet thi no) + sinh 2-3 Ma Hoa.
    ///   - Noi Gian: duoi `enrageBelow` mau — phu mau cam-ruc, +toc danh, mien lam cham, Hoi Tho hoi nhanh gap doi.
    /// Chieu chi bat dau khi rong khong dang vung don (`Enemy.IsBusy`); hieu ung ra o hitFrame cua clip than.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public class Dreadwyrm : MonoBehaviour
    {
        Enemy _enemy;
        DreadwyrmDefinition _definition;
        SpriteRenderer _renderer;
        SpriteAnimator _animator;
        float _damageScale = 1f;
        float _breathTimer;
        float _galeTimer;
        float _soulTimer;
        bool _enraged;
        readonly HashSet<Enemy> _souled = new HashSet<Enemy>();

        /// <param name="damageScale">He so chi so boss cua wave (wave 30 x3,5) — nhan vao sat thuong chieu + chi so Ma Hoa.</param>
        public void Initialise(Enemy enemy, DreadwyrmDefinition definition, float damageScale)
        {
            _enemy = enemy;
            _definition = definition;
            _damageScale = damageScale;
            _renderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<SpriteAnimator>();
            if (_animator != null)
            {
                _animator.HitFrame -= OnHitFrame;
                _animator.HitFrame += OnHitFrame;
            }
            if (transform.Find("HealthBar") == null) StatBar.AttachHealth(enemy);
            _breathTimer = definition.breathInterval * 0.5f;   // phun som: chieu dac trung, nguoi choi can thay som
            _galeTimer = definition.galeInterval;
            _soulTimer = definition.soulInterval;
            _enraged = false;
            _souled.Clear();
        }

        void OnDestroy()
        {
            if (_animator != null) _animator.HitFrame -= OnHitFrame;
        }

        void Update()
        {
            if (_enemy == null || _definition == null || _enemy.IsDying) return;

            if (!_enraged && _enemy.Health.Fraction < _definition.enrageBelow) Enrage();
            if (_enemy.IsFrozen) return;

            _breathTimer -= Time.deltaTime * (_enraged ? _definition.enrageBreathSpeedup : 1f);
            _galeTimer -= Time.deltaTime;
            _soulTimer -= Time.deltaTime;
            if (_enemy.IsBusy) return;

            if (_soulTimer <= 0f)
            {
                _soulTimer = _definition.soulInterval;
                Cast(_definition.roarBody, CallSouls);
            }
            else if (_breathTimer <= 0f && AnyHeroInBreath())
            {
                _breathTimer = _definition.breathInterval;
                Cast(_definition.breathBody, Breathe);
            }
            else if (_galeTimer <= 0f && AnyAllyInGale())
            {
                _galeTimer = _definition.galeInterval;
                Cast(_definition.galeBody, Gale);
            }
        }

        // LateUpdate: `Enemy` dat lai mau trang moi frame trong Update — to do SAU no.
        void LateUpdate()
        {
            if (_enraged && _renderer != null && _enemy != null && !_enemy.IsDying && !_enemy.IsFrozen)
                _renderer.color = _definition.enrageTint;
        }

        void Enrage()
        {
            _enraged = true;
            _enemy.AttackRateMultiplier = 1f + _definition.enrageAttackRateBonus;
            _enemy.SlowImmune = true;
        }

        void Cast(SpriteAnimationClip body, System.Action effect)
        {
            if (body == null || !body.IsValid || _animator == null)
            {
                effect();
                return;
            }
            _enemy.PlayCast(body);
            if (body.hitFrame <= 0) effect();
        }

        void OnHitFrame(string clip)
        {
            if (_enemy == null || _definition == null || _enemy.IsDying || !isActiveAndEnabled) return;
            if (clip == "attack") Cleave();
            else if (clip == _definition.breathBody.name) Breathe();
            else if (clip == _definition.galeBody.name) Gale();
            else if (clip == _definition.roarBody.name) CallSouls();
        }

        // ---------------------------------------------------------------- huong + diem neo

        /// <summary>+1 nhin phai, -1 nhin trai (art quay trai: flipX = nhin phai).</summary>
        float Front => _renderer != null && _renderer.flipX ? 1f : -1f;

        bool FacingRight => Front > 0f;

        Vector3 Anchor(Vector2 art) => transform.position + new Vector3(art.x * -Front, art.y, 0f);

        /// <summary>Hinh chieu cua diem neo xuong mat dat (cung y voi chan rong).</summary>
        Vector3 Ground(Vector2 art) => transform.position + new Vector3(art.x * -Front, 0f, 0f);

        // ---------------------------------------------------------------- don thuong

        /// <summary>Muc tieu chinh da an don tu `Enemy.LandHit`; them unit gan mom nhat cho du `cleaveTargets`.</summary>
        void Cleave()
        {
            var at = Ground(_definition.bite);
            var primary = _enemy.EngagedHero;
            var others = new List<Hero>();
            foreach (var hero in Hero.Alive)
            {
                if (hero == null || hero.IsDying || !hero.IsHostileTo(_enemy.Side) || hero == primary) continue;
                if (Battlefield.GroundDistance(hero.transform.position, at) - hero.BodyRadius > _definition.cleaveRadius) continue;
                others.Add(hero);
            }
            others.Sort((a, b) => Battlefield.GroundDistance(a.transform.position, at)
                .CompareTo(Battlefield.GroundDistance(b.transform.position, at)));
            int extra = Mathf.Max(0, _definition.cleaveTargets - (primary != null ? 1 : 0));
            for (int i = 0; i < others.Count && i < extra; i++)
                others[i].Health.TakeDamage(_enemy.Damage, _enemy.Health);
            OneShotFx.Spawn(Anchor(_definition.bite), _definition.clawFrames, FacingRight, SortOrder.Air, Battlefield.UnitScale);
        }

        // ---------------------------------------------------------------- Hoi Tho Ma Hoa

        bool InBreath(Hero hero)
        {
            if (hero == null || hero.IsDying || !hero.IsHostileTo(_enemy.Side)) return false;
            var mouth = Ground(_definition.mouth);
            float along = (hero.transform.position.x - mouth.x) * Front;
            if (along < -hero.BodyRadius || along > _definition.breathLength + hero.BodyRadius) return false;
            float depth = Mathf.Abs(hero.transform.position.y - mouth.y) / Battlefield.DepthSquash;
            return depth - hero.BodyRadius <= _definition.breathHalfWidth;
        }

        bool AnyHeroInBreath()
        {
            foreach (var hero in Hero.Alive)
                if (InBreath(hero)) return true;
            return false;
        }

        void Breathe()
        {
            float damage = _definition.breathDamage * _damageScale;
            foreach (var hero in Hero.Alive.ToArray())
                if (InBreath(hero)) hero.Health.TakeDamage(damage, _enemy.Health);

            // Sat thuong + vung lua o MAT DAT lane cua rong, mieng thi cao ~1,8: luong lua CHUI tu mieng xuong cham dat o
            // cuoi tam (`breathLength`) de hinh khop cho bi dot. Art dai 186 px, mom o dau NHON ben phai, loe ve trai
            // (lat khi nhin phai) -> keo cho dai bang doan mieng -> diem cham dat roi xoay theo doan do.
            float length = _definition.breathLength;
            var mouth = Anchor(_definition.mouth);
            var ground = Ground(_definition.mouth);
            var stream = ground + new Vector3(Front * length, 0f, 0f) - mouth;
            var artDirection = new Vector3(Front, 0f, 0f);
            float degrees = Vector3.SignedAngle(artDirection, stream, Vector3.forward);
            OneShotFx.Spawn(mouth + stream * 0.5f, _definition.breathFrames, FacingRight, SortOrder.Air,
                stream.magnitude / 1.86f, degrees: degrees);

            int patches = Mathf.Max(1, _definition.burnPatches);
            for (int i = 0; i < patches; i++)
            {
                var at = ground + new Vector3(Front * length * (i + 0.5f) / patches, 0f, 0f);
                at = Battlefield.Clamp(at, 0f, clampRight: false);
                PoisonPool.Spawn(at, _definition.burnFrames, _definition.burnDuration, _definition.burnRadius,
                    _definition.burnDamagePerSecond * _damageScale, _definition.scorchSeconds);
            }
        }

        // ---------------------------------------------------------------- Vo Canh Cuong Phong

        bool InGale(Hero hero) =>
            hero != null && !hero.IsDying && hero.IsHostileTo(_enemy.Side) &&
            Battlefield.GroundDistance(hero.transform.position, transform.position) - hero.BodyRadius
                <= _definition.galeRadius + _enemy.BodyRadius * 0.5f;

        bool AnyAllyInGale()
        {
            foreach (var hero in Hero.Alive)
                if (InGale(hero)) return true;
            return false;
        }

        void Gale()
        {
            foreach (var hero in Hero.Alive.ToArray())
            {
                if (!InGale(hero)) continue;
                float side = hero.transform.position.x - transform.position.x;
                float dir = Mathf.Abs(side) < 0.01f ? Front : Mathf.Sign(side);
                hero.Shove(dir * _definition.galeShove, _definition.galeShoveSeconds);
            }
            OneShotFx.Spawn(Anchor(_definition.bite), _definition.galeFrames, FacingRight, SortOrder.Air,
                Battlefield.UnitScale * 1.5f);
        }

        // ---------------------------------------------------------------- Goi Hon Lua

        void CallSouls()
        {
            foreach (var escort in Enemy.Alive.ToArray())
            {
                if (escort == null || escort == _enemy || escort.IsDying || _souled.Contains(escort)) continue;
                if (_definition.wisp != null && escort.Definition == _definition.wisp) continue;
                Ignite(escort);
            }
            SpawnWisps();
        }

        void Ignite(Enemy escort)
        {
            _souled.Add(escort);
            escort.DamageBonus += _definition.soulDamageBonus;
            // Cha da nhan UnitScale nen scale local 1 = dung co art.
            OneShotFx.SpawnLoop(escort.transform, Vector3.zero, _definition.soulAuraFrames, float.PositiveInfinity,
                SortOrder.ForUnit(escort) + 1);

            // Chup so luc gan: rong chet truoc thi quai van no dung so.
            float damage = _definition.soulBurstDamage * _damageScale;
            float radius = _definition.soulBurstRadius;
            var frames = _definition.soulBurstFrames;
            var health = escort.Health;
            System.Action burst = null;
            burst = () =>
            {
                health.Died -= burst;
                if (escort == null) return;
                var at = escort.transform.position;
                foreach (var hero in Hero.Alive.ToArray())
                {
                    if (hero == null || hero.IsDying || !hero.IsHostileTo(_enemy.Side)) continue;
                    if (Battlefield.GroundDistance(hero.transform.position, at) - hero.BodyRadius > radius) continue;
                    hero.Health.TakeDamage(damage);
                }
                OneShotFx.Spawn(escort.BodyCenter, frames, false, SortOrder.Air, Battlefield.UnitScale);
            };
            health.Died += burst;
        }

        void SpawnWisps()
        {
            var waves = WaveManager.Current;
            if (_definition.wisp == null || waves == null) return;
            int count = Random.Range(_definition.wispMin, _definition.wispMax + 1);
            for (int i = 0; i < count; i++)
            {
                var at = Ground(_definition.bite) + new Vector3(Random.Range(-0.5f, 0.5f),
                    (i - (count - 1) * 0.5f) * 0.6f, 0f);
                at = Battlefield.Clamp(at, 0f, clampRight: false);
                var wisp = waves.SpawnExtra(_definition.wisp, at, _damageScale, _damageScale);
                if (wisp != null) OneShotFx.Spawn(wisp.BodyCenter, _definition.soulBurstFrames, false, SortOrder.Air,
                    Battlefield.UnitScale);
            }
        }
    }
}
