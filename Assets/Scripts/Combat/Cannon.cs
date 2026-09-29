using BuildBuild.Core;
using BuildBuild.Data;
using BuildBuild.Meta;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Mot khau phao tu ban tren be phao dai (GAME_DESIGN.md muc 2 + 4). Khong co mau, khong bi danh, khong di chuyen.
    ///
    /// Nham quai TRONG TAM gan lau dai nhat (x nho nhat) — con sap cham tuong la con nguy hiem nhat, khong phai con
    /// dung gan nong phao nhat. Moi phat: chay clip "attack", dan ra o `hitFrame` (luc loe nong), chon lai muc tieu
    /// tai thoi diem do neu con cu da chet. Dan dung chung `Projectile` voi hero (`LaunchCannon`).
    ///
    /// Sprite ve QUAY TRAI nhu moi unit; quai den tu ben phai nen luon lat flipX.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(SpriteAnimator))]
    public class Cannon : MonoBehaviour
    {
        /// <summary>Nong phao tinh tu chan (pivot) o co art 1x, phia NONG (ben trai art). Nhan scale than khi dung.</summary>
        static readonly Vector3 MuzzleOffset = new Vector3(-0.3f, 0.3f, 0f);

        CannonDefinition _definition;
        SpriteRenderer _renderer;
        SpriteAnimator _animator;
        float _damage;
        float _cooldown;
        Enemy _pending;
        bool _firing;

        public CannonDefinition Definition => _definition;
        public int Level { get; private set; }

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<SpriteAnimator>();
        }

        public void Initialise(CannonDefinition definition, int level)
        {
            _definition = definition;
            SetLevel(level);
            _cooldown = 0f;
            _pending = null;
            _firing = false;

            transform.localScale = Vector3.one * definition.displayScale;
            _renderer.flipX = true;
            _renderer.color = definition.tint;

            _animator.Bind(definition.animations);
            _animator.HitFrame -= OnHitFrame;
            _animator.HitFrame += OnHitFrame;
            _animator.ClipFinished -= OnClipFinished;
            _animator.ClipFinished += OnClipFinished;
        }

        /// <summary>Nang level giua wave thi phao dang lap an so moi ngay, khong can go ra lap lai.</summary>
        public void SetLevel(int level)
        {
            Level = _definition.ClampLevel(level);
            _damage = Progression.CannonDamage(_definition, Level);    // level + sao
        }

        void Update()
        {
            if (_definition == null || _definition.fireRate <= 0f) return;
            _cooldown -= Time.deltaTime;
            if (_firing || _cooldown > 0f) return;

            var target = FindTarget();
            if (target == null) return;

            _cooldown = 1f / _definition.fireRate;
            _pending = target;
            _firing = true;
            var clip = _definition.animations != null ? _definition.animations.attack : null;
            _animator.Play("attack", restart: true);
            if (clip == null || !clip.IsValid || clip.hitFrame <= 0) Release();
        }

        /// <summary>Quai con song trong tam, gan lau dai nhat (x nho nhat).</summary>
        Enemy FindTarget()
        {
            Enemy best = null;
            float bestX = float.MaxValue;
            float rangeSqr = _definition.range * _definition.range;
            foreach (var enemy in Enemy.Alive)
            {
                if (enemy == null || enemy.IsDying) continue;
                if ((enemy.transform.position - transform.position).sqrMagnitude > rangeSqr) continue;
                float x = enemy.transform.position.x;
                if (x >= bestX) continue;
                bestX = x;
                best = enemy;
            }
            return best;
        }

        void OnHitFrame(string clip)
        {
            if (clip == "attack") Release();
        }

        void OnClipFinished(string clip)
        {
            if (clip != "attack") return;
            if (_pending != null) Release();    // clip ngan hon hitFrame da khai: van phai ra dan
            _firing = false;
            _animator.Play("idle");
        }

        void Release()
        {
            var target = _pending != null && !_pending.IsDying ? _pending : FindTarget();
            _pending = null;
            if (_definition.animations == null || !_definition.animations.attack.IsValid) _firing = false;
            if (target == null) return;

            var muzzle = MuzzleOffset * _definition.displayScale;
            if (_renderer.flipX) muzzle.x = -muzzle.x;

            var go = new GameObject("CannonShot");
            go.transform.position = transform.position + muzzle;
            go.transform.localScale = Vector3.one * (_definition.projectileScale * Battlefield.UnitScale);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = SortOrder.Air;
            renderer.color = _definition.tint;
            go.AddComponent<Projectile>().LaunchCannon(target, _damage, _definition);
        }
    }
}
