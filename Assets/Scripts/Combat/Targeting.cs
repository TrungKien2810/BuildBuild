using System;
using BuildBuild.Meta;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>Cach hero chon muc tieu cho don danh THUONG.</summary>
    public enum TargetMode { Nearest, Furthest, Weakest, Strongest }

    /// <summary>
    /// "Uu tien muc tieu" (GDD 10.1, nut Auto tren HUD tran). Tran chay tu dong, nen thu nguoi choi thao tac
    /// duoc khong phai la bam don — ma la CHON CACH danh: don dep quai yếu truoc, hay tap trung con khoẻ.
    ///
    /// Mac dinh `Nearest` = dung y nguyen hanh vi cu (khong doi gi cho ai khong dung nut nay).
    /// Chi ap cho don danh thuong (`Hero.FindAttackTarget`); cac chieu tu chon muc tieu theo kich ban rieng.
    /// </summary>
    public static class Targeting
    {
        public static readonly TargetMode[] Modes =
        {
            TargetMode.Nearest, TargetMode.Furthest, TargetMode.Weakest, TargetMode.Strongest,
        };

        /// <summary>Trang thai doi (bam nut) — nhan tren HUD cap nhat chu.</summary>
        public static event Action Changed;

        public static TargetMode Current
        {
            get => Modes[Mathf.Clamp(SaveSystem.Data.targetMode, 0, Modes.Length - 1)];
            set
            {
                SaveSystem.Data.targetMode = Array.IndexOf(Modes, value);
                SaveSystem.Save();
                Changed?.Invoke();
            }
        }

        public static void Cycle()
        {
            int next = (Array.IndexOf(Modes, Current) + 1) % Modes.Length;
            Current = Modes[next];
        }

        /// <summary>Nhan ngan tren nut (o pill duoi nut chi rong ~60 px).</summary>
        public static string ShortLabel
        {
            get
            {
                switch (Current)
                {
                    case TargetMode.Furthest: return "Far";
                    case TargetMode.Weakest: return "Weak";
                    case TargetMode.Strongest: return "Strong";
                    default: return "Near";
                }
            }
        }

        public static string Description
        {
            get
            {
                switch (Current)
                {
                    case TargetMode.Furthest: return "Furthest first";
                    case TargetMode.Weakest: return "Lowest HP first";
                    case TargetMode.Strongest: return "Highest HP first";
                    default: return "Nearest first";
                }
            }
        }

        /// <summary>Diem cua mot muc tieu: CAO hon = duoc chon. `sqr` = binh phuong khoang cach toi hero.</summary>
        public static float Score(Unit enemy, float sqr)
        {
            switch (Current)
            {
                case TargetMode.Furthest: return sqr;
                case TargetMode.Weakest: return -Health(enemy);
                case TargetMode.Strongest: return Health(enemy);
                default: return -sqr;
            }
        }

        static float Health(Unit enemy)
        {
            var health = enemy != null ? enemy.Health : null;
            return health != null ? health.Current : 0f;
        }
    }
}
