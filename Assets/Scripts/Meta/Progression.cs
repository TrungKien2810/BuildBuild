using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Meta
{
    /// <summary>
    /// Level + sao cua mot unit da so huu (GAME_DESIGN.md muc 6-7, luat mo skill chot 2026-09-28):
    ///   - level n -> n+1 ton `floor(50 x n^1,5)` vang, moi level +8% mau va sat thuong goc; tran level theo sao
    ///     (`MaxLevel`, nguoi dung chot 2026-09-29): 0-1 sao lv 20, moi sao sau +20, du sao thi khong gioi han;
    ///   - sao la NUT BAM ton mang (`GachaConfig.shardsPerStar`), khong tu len khi quay; moi sao +15% mau va sat thuong
    ///     goc; toi da `MaxStars` (GDD chua co tran — Claude dat tam);
    ///   - so huu la dung duoc danh thuong + chieu cuoi; noi tai / chieu phu mo theo sao (`HeroDefinition.passiveStar`...).
    /// Level va sao CONG tuyen tinh voi nhau tren chi so goc, khong nhan chong.
    /// Unit khong co id (linh xuong, ky si, dia long) khong co tien trien: he so 1, moi thu mo san.
    /// Phao giu gia/level rieng trong `CannonDefinition` (co tu truoc) — o day chi cong them sao; tran level dung chung.
    /// </summary>
    public static class Progression
    {
        public const int LevelsPerStar = 20;
        public const float LevelBonus = 0.08f;
        public const int MaxStars = 5;
        public const float StarBonus = 0.15f;

        /// <summary>Level toi da o so sao nay (hero, linh, phao): 0-1 sao 20, 2 sao 40 … du sao `int.MaxValue`.</summary>
        public static int MaxLevel(int stars) =>
            stars >= MaxStars ? int.MaxValue : LevelsPerStar * Mathf.Max(1, stars);

        public static OwnedUnit Owned(string unitId) =>
            string.IsNullOrEmpty(unitId) ? null : SaveSystem.Data.Find(unitId);

        /// <summary>Sao dung de mo skill. Unit khong co id coi nhu du sao.</summary>
        public static int Stars(string unitId) =>
            string.IsNullOrEmpty(unitId) ? MaxStars : Owned(unitId)?.stars ?? 0;

        /// <summary>He so nhan mau va sat thuong goc cua hero / linh tu level + sao.</summary>
        public static float StatMultiplier(string unitId)
        {
            var owned = Owned(unitId);
            if (owned == null) return 1f;
            return 1f + LevelBonus * (Mathf.Max(1, owned.level) - 1) + StarBonus * owned.stars;
        }

        /// <summary>
        /// He so cuoi cung tren chi so goc: level + sao + trang bi + bang ky nang, CONG voi nhau (khong nhan chong).
        /// `equipment` null = bo qua trang bi (linh, unit trieu hoi).
        /// </summary>
        public static void Multipliers(string unitId, EquipmentConfig equipment, out float health, out float damage, out float attackRate)
        {
            float progress = StatMultiplier(unitId);
            Armory.Bonuses(unitId, equipment, out float gearHealth, out float gearDamage, out float gearRate);
            // Bang ky nang la % TOAN TAI KHOAN — cong cung cho voi level/sao/trang bi, khong nhan rieng.
            health = progress + gearHealth + SkillBoard.Bonus(BoardNode.Health);
            damage = progress + gearDamage + SkillBoard.Bonus(BoardNode.Attack);
            attackRate = 1f + gearRate + SkillBoard.Bonus(BoardNode.Haste);
        }

        /// <summary>Sat thuong phao: level theo `CannonDefinition`, cong them sao va bang ky nang.</summary>
        public static float CannonDamage(CannonDefinition cannon, int level)
        {
            var owned = Owned(cannon.unitId);
            float baseDamage = cannon.DamageAt(level) + cannon.damage * StarBonus * (owned?.stars ?? 0);
            return baseDamage * (1f + SkillBoard.Bonus(BoardNode.Attack));
        }

        /// <summary>Gia vang nang hero / linh tu `level` len `level + 1` (GDD muc 7). Phao dung `CannonDefinition.UpgradeCost`.</summary>
        public static int LevelUpCost(int level) => Mathf.FloorToInt(50f * Mathf.Pow(Mathf.Max(1, level), 1.5f));

        public static bool CanStarUp(OwnedUnit owned, int shardsPerStar) =>
            owned != null && owned.stars < MaxStars && owned.shards >= shardsPerStar;

        /// <summary>Tru mang, +1 sao, ghi save. Khong du mang / da toi da thi tra false, khong doi gi.</summary>
        public static bool TryStarUp(OwnedUnit owned, int shardsPerStar)
        {
            if (!CanStarUp(owned, shardsPerStar)) return false;
            owned.shards -= shardsPerStar;
            owned.stars++;
            SaveSystem.Save();
            return true;
        }
    }
}
