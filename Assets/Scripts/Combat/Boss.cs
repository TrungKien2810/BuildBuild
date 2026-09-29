using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Ba co che boss (GAME_DESIGN.md muc 5), gan vao quai boss qua `EnemyExtras`. Chi dung API public cua `Enemy`
    /// (A giu file — plan 2 thanh vien, muc 1):
    ///   - Don thuong quet vung: frame cham cua clip attack, moi unit phe ta khac trong `cleaveRadius` cung an don
    ///                 (`Enemy` da danh muc tieu chinh).
    ///   - Chan Dia  : moi `slamInterval` giay, CHI khi co hero / linh trong `slamRadius` quanh dau bua (`SlamPoint`) —
    ///                 `slamDamage` sat thuong + choang `slamStun` giay moi unit phe ta trong vung. Chua co ai thi giu chieu.
    ///   - Tieng Thet: moi `roarInterval` giay, moi quai tren san +`roarHaste` toc chay trong `roarDuration` giay.
    ///   - Noi Gian  : duoi `enrageBelow` mau — +`enrageAttackRateBonus` toc danh va mien lam cham, tu do toi het tran.
    /// Giap -30% nam o `EnemyDefinition.damageReduction` (Health.Armor, do `EnemyExtras` dat).
    ///
    /// Chan Dia / Tieng Thet chay clip THAN (`slamBody`, `roarBody`) qua `Enemy.PlayCast`; hieu ung ra o `hitFrame`
    /// cua clip. Chieu chi bat dau khi boss khong dang vung don (`Enemy.IsBusy`). Clip trong thi ra don ngay.
    /// Thanh mau tren dau: `StatBar.AttachHealth(Enemy)`.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public class Boss : MonoBehaviour
    {
        /// <summary>Mau phu luc noi gian — do, doc ra "nguy hiem hon" ma khong can UI.</summary>
        static readonly Color EnrageTint = new Color(1f, 0.6f, 0.55f);

        /// <summary>FX buff Tieng Thet tren dau quai: toa do LOCAL (than da nhan UnitScale), art 96 px -> scale 0,5.</summary>
        static readonly Vector3 BuffOffset = new Vector3(0f, 0.65f, 0f);
        const float BuffFxScale = 0.5f;

        Enemy _enemy;
        BossDefinition _definition;
        SpriteRenderer _renderer;
        SpriteAnimator _animator;
        float _damageScale = 1f;
        float _slamTimer;
        float _roarTimer;
        bool _enraged;

        public bool IsEnraged => _enraged;

        /// <param name="damageScale">He so sat thuong cua con boss nay (wave 20 x2, wave 30 x3,5) — nhan vao Chan Dia.</param>
        public void Initialise(Enemy enemy, BossDefinition definition, float damageScale)
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
            _slamTimer = definition.slamInterval;
            _roarTimer = definition.roarInterval;
            _enraged = false;
        }

        void OnDestroy()
        {
            if (_animator != null) _animator.HitFrame -= OnHitFrame;
        }

        void Update()
        {
            if (_enemy == null || _definition == null || _enemy.IsDying) return;

            if (!_enraged && _enemy.Health.Fraction < _definition.enrageBelow) Enrage();

            // Dang dong bang thi dong ho dung — ra don ngay khi tan bang la bat cong.
            if (_enemy.IsFrozen) return;

            _slamTimer -= Time.deltaTime;
            _roarTimer -= Time.deltaTime;

            // Dang vung don / dang cast thi giu chieu: cat ngang la nuot mat don.
            if (_enemy.IsBusy) return;

            if (_roarTimer <= 0f)
            {
                _roarTimer = _definition.roarInterval;
                Cast(_definition.roarBody, Roar);
            }
            else if (_slamTimer <= 0f && AnyAllyInSlam())
            {
                _slamTimer = _definition.slamInterval;
                Cast(_definition.slamBody, Slam);
            }
        }

        // LateUpdate: `Enemy` dat lai mau trang moi frame trong Update — to do SAU no.
        void LateUpdate()
        {
            if (_enraged && _renderer != null && _enemy != null && !_enemy.IsDying && !_enemy.IsFrozen)
                _renderer.color = EnrageTint;
        }

        void Enrage()
        {
            _enraged = true;
            _enemy.AttackRateMultiplier = 1f + _definition.enrageAttackRateBonus;
            _enemy.SlowImmune = true;
        }

        /// <summary>Chay clip than; hieu ung ra o hitFrame (`OnHitFrame`). Khong co clip thi ra ngay.</summary>
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
            if (_enemy == null || _definition == null || _enemy.IsDying) return;
            // Don chinh vua danh sap lau dai -> wave thua, quai bi tat, doi hinh da dung lai: phan lan khong duoc trung hero moi.
            if (!isActiveAndEnabled) return;
            if (clip == "attack") Cleave();
            else if (clip == _definition.slamBody.name) Slam();
            else if (clip == _definition.roarBody.name) Roar();
        }

        /// <summary>Muc tieu chinh da an don tu `Enemy.LandHit`; day danh them moi unit khac trong `cleaveRadius`.</summary>
        void Cleave()
        {
            var primary = _enemy.EngagedHero;
            foreach (var hero in Hero.Alive.ToArray())
            {
                if (hero == null || hero.IsDying || hero == primary) continue;
                if (Battlefield.GroundDistance(hero.transform.position, transform.position) - hero.BodyRadius > _definition.cleaveRadius)
                    continue;
                hero.Health.TakeDamage(_enemy.Damage, _enemy.Health);
            }
        }

        bool AnyAllyInSlam()
        {
            foreach (var hero in Hero.Alive)
                if (InSlam(hero)) return true;
            return false;
        }

        /// <summary>Cho dau bua cham dat: `slamReach` truoc chan, phia boss dang nhin (art quay trai, flipX = nhin phai).</summary>
        Vector3 SlamPoint =>
            transform.position + new Vector3(_renderer != null && _renderer.flipX ? _definition.slamReach : -_definition.slamReach, 0f, 0f);

        bool InSlam(Hero hero) =>
            hero != null && !hero.IsDying &&
            Battlefield.GroundDistance(hero.transform.position, SlamPoint) - hero.BodyRadius <= _definition.slamRadius;

        void Slam()
        {
            foreach (var hero in Hero.Alive.ToArray())
            {
                if (!InSlam(hero)) continue;
                hero.Health.TakeDamage(_definition.slamDamage * _damageScale, _enemy.Health);
                hero.ApplyStun(_definition.slamStun);
            }
            OneShotFx.Spawn(SlamPoint, _definition.slamFrames, false, SortOrder.GroundDecal + 1,
                Battlefield.UnitScale);
        }

        void Roar()
        {
            foreach (var enemy in Enemy.Alive.ToArray())
            {
                if (enemy == null || enemy.IsDying) continue;
                enemy.ApplyHaste(_definition.roarHaste, _definition.roarDuration);
                OneShotFx.SpawnLoop(enemy.transform, BuffOffset, _definition.roarBuffFrames, _definition.roarDuration,
                    SortOrder.Air, scale: BuffFxScale);
            }
            OneShotFx.Spawn(_enemy.BodyCenter, _definition.roarFrames, false, SortOrder.Air, Battlefield.UnitScale);
        }
    }
}
