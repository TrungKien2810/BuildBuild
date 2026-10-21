using System;
using UnityEngine;

namespace BuildBuild.Meta
{
    /// <summary>Ba nhanh cua bang ky nang toan tai khoan.</summary>
    public enum BoardNode { Attack, Health, Haste }

    /// <summary>
    /// Bang ky nang toan tai khoan (GDD 10.4 "Skill Board" trong Town; IDEAS §Rà y 20).
    ///
    /// Vi sao can: vang cuoi game chi co MOT duong tieu la nang level unit + nang cap thanh, nen no cu chay mai
    /// khong co cho do. Bang nay la cho do thu hai, va khac cua hang o cho no cong % cho MOI unit chu khong ban
    /// manh cho mot con.
    ///
    /// Cong thuc: moi cap cong THEM phan tram vao he so goc, dung cho `Progression.Multipliers` cong (khong nhan
    /// chong) — cung luat voi level / sao / trang bi.
    /// </summary>
    public static class SkillBoard
    {
        public const int MaxRank = 10;
        public const float AttackPerRank = 0.02f;    // +2% sat thuong moi cap
        public const float HealthPerRank = 0.02f;    // +2% mau moi cap
        public const float HastePerRank = 0.015f;    // +1,5% toc danh moi cap

        public static int Rank(BoardNode node)
        {
            var save = SaveSystem.Data;
            switch (node)
            {
                case BoardNode.Attack: return save.boardAttack;
                case BoardNode.Health: return save.boardHealth;
                default: return save.boardHaste;
            }
        }

        public static float PerRank(BoardNode node)
        {
            switch (node)
            {
                case BoardNode.Attack: return AttackPerRank;
                case BoardNode.Health: return HealthPerRank;
                default: return HastePerRank;
            }
        }

        public static string Name(BoardNode node)
        {
            switch (node)
            {
                case BoardNode.Attack: return "Sharpened Blades";
                case BoardNode.Health: return "Battle-Hardened Army";
                default: return "Drilled Ranks";
            }
        }

        /// <summary>Cong them vao he so goc (0,02 = +2%) — `Progression.Multipliers` cong thang so nay.</summary>
        public static float Bonus(BoardNode node) => Rank(node) * PerRank(node);

        /// <summary>Gia cap ke tiep — leo theo cap de khong mua het trong mot wave.</summary>
        public static int Cost(BoardNode node) => 300 + 500 * Rank(node);

        public static bool Maxed(BoardNode node) => Rank(node) >= MaxRank;

        /// <summary>Mua mot cap. `spend` tra false khi thieu vang — cung luat voi `Shop`/`GachaSystem`.</summary>
        public static bool TryUpgrade(BoardNode node, Func<int, bool> spend)
        {
            if (Maxed(node)) return false;
            if (spend == null || !spend(Cost(node))) return false;

            var save = SaveSystem.Data;
            switch (node)
            {
                case BoardNode.Attack: save.boardAttack++; break;
                case BoardNode.Health: save.boardHealth++; break;
                default: save.boardHaste++; break;
            }
            SaveSystem.Save();
            return true;
        }

        /// <summary>Tong chi so cong them — hien o dau bang cho nguoi choi thay minh dang o dau.</summary>
        public static string Summary() =>
            $"+{Mathf.RoundToInt(Bonus(BoardNode.Attack) * 100f)}% DMG  ·  " +
            $"+{Mathf.RoundToInt(Bonus(BoardNode.Health) * 100f)}% HP  ·  " +
            $"+{Bonus(BoardNode.Haste) * 100f:0.#}% ATK SPEED";
    }
}
