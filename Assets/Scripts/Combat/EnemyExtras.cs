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
    ///     `heroAggroRange` &gt;= tam ban nen hero / linh trong tam duoc chon truoc tuong. Component nay nhan `Enemy.FireShot`:
    ///     tha VIEN DA o frame roi tay, sat thuong tru luc vien da cham muc tieu;
    ///   - Boss: gan `Boss` (ba co che GDD muc 5); Phap su Orc gan `Shaman`; Rong Ma Hoa gan `Dreadwyrm`;
    ///   - Phu mau xanh nhat + bong tuyet tren dau khi dang bi phao Bang lam cham.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public class EnemyExtras : MonoBehaviour
    {
        /// <summary>Xanh nhat hon dong bang de phan biet "cham" voi "dung han".</summary>
        static readonly Color SlowedTint = new Color(0.75f, 0.9f, 1f);

        /// <summary>Diem da roi tay: truoc nguc, tinh tu chan o co art 1x (nhan scale than khi dung).</summary>
        static readonly Vector3 ShotOffset = new Vector3(-0.12f, 0.22f, 0f);

        /// <summary>Bong tuyet tren dau quai dang bi lam cham — `Cannon` gan khi Phao Bang ra be; null = khong ve.</summary>
        public static Sprite[] SlowIconFrames;
        /// <summary>Khoang tu dinh dau quai toi tam bong tuyet (toa do local cua quai; art 128 px, icon ~84 px).</summary>
        const float SlowIconGap = 0.15f;
        const float SlowIconScale = 0.3f;

        Enemy _enemy;
        SpriteRenderer _renderer;
        GameObject _slowIcon;
        SpriteRenderer _slowIconRenderer;
        float _damageMultiplier = 1f;

        public static void Attach(Enemy enemy, float damageMultiplier)
        {
            var extras = enemy.GetComponent<EnemyExtras>();
            if (extras == null) extras = enemy.gameObject.AddComponent<EnemyExtras>();
            extras.Initialise(enemy, damageMultiplier);
        }

        void Initialise(Enemy enemy, float damageMultiplier)
        {
            _enemy = enemy;
            _damageMultiplier = damageMultiplier;
            _renderer = GetComponent<SpriteRenderer>();
            var definition = enemy.Definition;
            enemy.Health.Armor = definition.damageReduction;

            enemy.FireShot = definition.IsRanged ? Fire : (System.Action<Health, float>)null;

            var boss = GetComponent<Boss>();
            if (definition.boss != null)
            {
                if (boss == null) boss = gameObject.AddComponent<Boss>();
                boss.Initialise(enemy, definition.boss, damageMultiplier);
            }
            else if (boss != null) Destroy(boss);

            var shaman = GetComponent<Shaman>();
            if (definition.shaman != null)
            {
                if (shaman == null) shaman = gameObject.AddComponent<Shaman>();
                shaman.Initialise(enemy, definition.shaman, damageMultiplier);
            }
            else if (shaman != null) Destroy(shaman);

            var dragon = GetComponent<Dreadwyrm>();
            if (definition.dragon != null)
            {
                if (dragon == null) dragon = gameObject.AddComponent<Dreadwyrm>();
                dragon.Initialise(enemy, definition.dragon, damageMultiplier);
            }
            else if (dragon != null) Destroy(dragon);
        }

        // LateUpdate: `Enemy` dat lai mau (dong bang / choang / trang) trong Update — to SAU no.
        void LateUpdate()
        {
            UpdateSlowIcon();
            if (_enemy == null || _renderer == null || _enemy.IsDying || _enemy.IsFrozen) return;
            if (_enemy.IsSlowed) _renderer.color = SlowedTint;
        }

        /// <summary>Bong tuyet hien/tat theo `IsSlowed` (thoi han slow doi moi lan trung nen soat moi frame).</summary>
        void UpdateSlowIcon()
        {
            bool show = _enemy != null && !_enemy.IsDying && _enemy.IsSlowed && SlowIconFrames != null && SlowIconFrames.Length > 0;
            if (show != (_slowIcon != null))
            {
                if (show)
                {
                    // Neo theo dinh dau that cua quai (BodyHeight la world, quai scale UnitScale), can giua than —
                    // offset co dinh lam bong tuyet lo lung lech o quai thap (soi) va chim vao than quai cao.
                    var offset = new Vector3(0f, _enemy.BodyHeight / Battlefield.UnitScale + SlowIconGap, 0f);
                    _slowIcon = OneShotFx.SpawnLoop(transform, offset, SlowIconFrames, float.PositiveInfinity,
                        SortOrder.ForUnit(_enemy) + 1, scale: SlowIconScale);
                    _slowIconRenderer = _slowIcon != null ? _slowIcon.GetComponent<SpriteRenderer>() : null;
                }
                else
                {
                    Destroy(_slowIcon);
                    _slowIcon = null;
                    _slowIconRenderer = null;
                }
            }
            if (_slowIconRenderer != null && _renderer != null) _slowIconRenderer.sortingOrder = _renderer.sortingOrder + 1;
        }

        /// <summary>
        /// Frame roi tay cua clip attack (`Enemy.FireShot`): tha vien da ve muc tieu dang danh (hero / linh / tuong), sat
        /// thuong tru luc vien da cham. Muc tieu bi don di giua duong thi vien vo tai cho cu, khong tru ai.
        /// </summary>
        void Fire(Health target, float damage)
        {
            if (_enemy == null || target == null) return;
            var definition = _enemy.Definition;
            var offset = ShotOffset * Battlefield.UnitScale;
            if (_renderer != null && _renderer.flipX) offset.x = -offset.x;   // art quay trai: flipX = nhin phai
            var from = transform.position + offset;
            var source = _enemy.Health;

            var unit = target.GetComponent<Unit>();
            bool wall = unit == null || unit is Castle;
            // Nem vao tuong: khong de vung doc (khong co hero nao dung o do).
            bool pool = !wall && definition.poolFrames != null && definition.poolFrames.Length > 0;
            float dps = definition.poolDamagePerSecond * _damageMultiplier;
            System.Action<Vector3> landed = at =>
            {
                // Chan Dan (Nguoi Lun Khien Rune): dan vo tren khien, khong sat thuong, khong vung doc.
                if (unit is Hero hero && hero.BreaksShot(at)) return;
                if (target != null) target.TakeDamage(damage, source);
                if (pool) PoisonPool.Spawn(at, definition.poolFrames, definition.poolDuration, definition.poolRadius, dps);
            };

            EnemyShot.Launch(from, target.transform, wall ? (float?)null : unit.BodyHeight * 0.5f, definition.projectileSpeed,
                definition.projectileFrames, definition.impactFrames, definition.projectileScale,
                definition.projectileArc, landed, _enemy.Side);
        }
    }
}
