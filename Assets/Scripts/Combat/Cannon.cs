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

        CannonDefinition _definition;
        SpriteRenderer _renderer;
        SpriteAnimator _animator;
        float _damage;
        float _cooldown;
        Unit _pending;
        bool _firing;
        Faction _side;

        public CannonDefinition Definition => _definition;
        public int Level { get; private set; }

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<SpriteAnimator>();
        }

        /// <summary><paramref name="side"/> = Npc: phao tren thanh NPC (Chinh Phat) — quay trai, ban phe ta.</summary>
        public void Initialise(CannonDefinition definition, int level, Faction side = Faction.Player)
        {
            _definition = definition;
            _side = side;
            SetLevel(level);
            _cooldown = 0f;
            _pending = null;
            _firing = false;

            transform.localScale = Vector3.one * definition.displayScale;
            _renderer.flipX = side == Faction.Player;
            _renderer.color = definition.tint;
            if (definition.slowIconFrames != null && definition.slowIconFrames.Length > 0)
                EnemyExtras.SlowIconFrames = definition.slowIconFrames;

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

        /// <summary>Doi thu con song trong tam, gan thanh minh nhat (phe ta: x nho nhat; thanh NPC ben phai: x lon nhat).</summary>
        Unit FindTarget()
        {
            Unit best = null;
            float bestX = float.MaxValue;
            float rangeSqr = _definition.range * _definition.range;
            foreach (var enemy in Unit.Hostiles(_side))
            {
                if (enemy == null || enemy.IsDying) continue;
                if ((enemy.AimPoint(transform.position) - transform.position).sqrMagnitude > rangeSqr) continue;
                float x = _side == Faction.Player ? enemy.transform.position.x : -enemy.transform.position.x;
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

            var muzzle = (Vector3)_definition.muzzle * _definition.displayScale;
            if (_renderer.flipX) muzzle.x = -muzzle.x;

            var go = new GameObject("CannonShot");
            go.transform.position = transform.position + muzzle;
            go.transform.localScale = Vector3.one * (_definition.projectileScale * Battlefield.UnitScale);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = SortOrder.Air;
            renderer.color = _definition.tint;
            go.AddComponent<Projectile>().LaunchCannon(target, _damage, _definition, _side);
            Sfx.Play(_definition.fireSfx);
        }
    }
}
