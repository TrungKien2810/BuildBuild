using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Phan cua nhanh Tru gan THEM vao quai, tach khoi `Enemy` (A giu file — plan 2 thanh vien, muc 1). `WaveManager`
    /// goi `Attach` ngay sau `Enemy.Initialise`:
    ///   - Giap (`EnemyDefinition.damageReduction`) qua `Health.Armor`;
    ///   - Quai BAN XA (Goblin Na): LUAT danh van la cua `Enemy` — `attackRange` 4 nen dung cach muc tieu 4 ma danh, va
    ///     `heroAggroRange` &gt;= tam ban nen hero / linh trong tam duoc chon truoc tuong. Component nay chi tha VIEN DA
    ///     o frame roi tay de nguoi choi thay quai dang ban;
    ///   - Boss: gan `Boss` (ba co che GDD muc 5);
    ///   - Phu mau xanh nhat khi dang bi phao Bang lam cham.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public class EnemyExtras : MonoBehaviour
    {
        /// <summary>Xanh nhat hon dong bang de phan biet "cham" voi "dung han".</summary>
        static readonly Color SlowedTint = new Color(0.75f, 0.9f, 1f);

        /// <summary>Diem da roi tay: truoc nguc, tinh tu chan o co art 1x (nhan scale than khi dung).</summary>
        static readonly Vector3 ShotOffset = new Vector3(-0.12f, 0.22f, 0f);

        Enemy _enemy;
        SpriteRenderer _renderer;
        SpriteAnimator _animator;
        Castle _castle;

        public static void Attach(Enemy enemy, float damageMultiplier)
        {
            var extras = enemy.GetComponent<EnemyExtras>();
            if (extras == null) extras = enemy.gameObject.AddComponent<EnemyExtras>();
            extras.Initialise(enemy, damageMultiplier);
        }

        void Initialise(Enemy enemy, float damageMultiplier)
        {
            _enemy = enemy;
            _renderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<SpriteAnimator>();
            var definition = enemy.Definition;
            enemy.Health.Armor = definition.damageReduction;

            if (_animator != null)
            {
                _animator.HitFrame -= OnHitFrame;
                if (definition.IsRanged)
                {
                    _castle = FindAnyObjectByType<Castle>();
                    _animator.HitFrame += OnHitFrame;
                }
            }

            var boss = GetComponent<Boss>();
            if (definition.boss != null)
            {
                if (boss == null) boss = gameObject.AddComponent<Boss>();
                boss.Initialise(enemy, definition.boss, damageMultiplier);
            }
            else if (boss != null) Destroy(boss);
        }

        void OnDestroy()
        {
            if (_animator != null) _animator.HitFrame -= OnHitFrame;
        }

        // LateUpdate: `Enemy` dat lai mau (dong bang / choang / trang) trong Update — to SAU no.
        void LateUpdate()
        {
            if (_enemy == null || _renderer == null || _enemy.IsDying || _enemy.IsFrozen) return;
            if (_enemy.IsSlowed) _renderer.color = SlowedTint;
        }

        /// <summary>Frame roi tay cua clip attack: tha vien da ve phia hero dang bi ban, khong co thi ve tuong.</summary>
        void OnHitFrame(string clip)
        {
            if (clip != "attack" || _enemy == null || _enemy.IsDying) return;
            var definition = _enemy.Definition;
            var offset = ShotOffset * Battlefield.UnitScale;
            if (_renderer != null && _renderer.flipX) offset.x = -offset.x;   // art quay trai: flipX = nhin phai
            var from = transform.position + offset;

            var hero = _enemy.EngagedHero;
            if (hero != null && !hero.IsDying)
                EnemyShot.Launch(from, hero.transform, hero.BodyHeight * 0.5f, definition.projectileSpeed,
                    definition.projectileFrames, definition.impactFrames, definition.projectileScale);
            else if (_castle != null)
                EnemyShot.Launch(from, _castle.transform, null, definition.projectileSpeed,
                    definition.projectileFrames, definition.impactFrames, definition.projectileScale);
        }
    }
}
