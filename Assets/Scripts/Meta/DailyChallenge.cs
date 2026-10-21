using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuildBuild.Meta
{
    /// <summary>
    /// Thu thach NGAY (GDD 10.4 "Daily Quests" — nhung day la NGU CANH CHOI, khac track nhiem vu dem hoat dong):
    /// moi ngay mot wave duy nhat, dung lai bang quai co san, them mot bien the nhe. Khac Hanh trinh (moc vinh vien)
    /// va khac Chinh Phat (tran hai thanh).
    ///
    /// Seed lay tu NGAY chu khong tu `string.GetHashCode()`: hash cua chuoi trong .NET doi theo tien trinh, cung mot
    /// ngay mo game hai lan se ra hai de bai khac nhau.
    ///
    /// Thua thi KHONG tinh la da lam — nguoi choi duoc thu lai trong ngay (moc "da xong" chi ghi khi THANG).
    /// </summary>
    public static class DailyChallenge
    {
        /// <summary>`yyyy-MM-dd` — khoa luu "hom nay da xong chua".</summary>
        public static string DayKey => DateTime.Now.ToString("yyyy-MM-dd");

        static int DayNumber => int.Parse(DateTime.Now.ToString("yyyyMMdd"));

        /// <summary>Wave cua hom nay: 31..41, tuc luon la vung sau bang boss cuoi.</summary>
        public static int Wave => 31 + DayNumber % 11;

        /// <summary>Bien the ngay chan / ngay le — de bai khac nhau nhung van doc duoc bang mot cau.</summary>
        public static bool Enraged => DayNumber % 2 == 0;

        public static float DamageScale => Enraged ? 1.35f : 1f;

        /// <summary>Ngay khong "no gian" thi de bai khac di: them quan thuong tren san.</summary>
        public static int ExtraEnemies => Enraged ? 0 : 3;

        public static string Title => Enraged ? $"Enraged Wave {Wave}" : $"Heavy Wave {Wave}";

        public static string Rule => Enraged
            ? "Enemies hit 35% harder. Same line, same castle."
            : "Extra enemies on the field, normal damage.";

        public static int GoldReward => 600 + 40 * Wave;
        public static int Shards => 2;

        /// <summary>Hom nay da THANG thu thach chua (thua thi chua tinh).</summary>
        public static bool Cleared => SaveSystem.Data.dailyDay == DayKey;

        public static void MarkCleared()
        {
            SaveSystem.Data.dailyDay = DayKey;
            SaveSystem.Save();
        }

        /// <summary>Cong thuong: vang qua `addGold`, manh cho mot unit dang so huu (nhu moc wave / ruong AFK).</summary>
        public static string Grant(Action<int> addGold)
        {
            int gold = GoldReward;
            addGold?.Invoke(gold);

            var data = SaveSystem.Data;
            int shards = 0;
            if (data.owned.Count > 0)
            {
                shards = Shards;
                for (int i = 0; i < shards; i++) data.owned[Random.Range(0, data.owned.Count)].shards++;
            }
            MarkCleared();
            return shards > 0
                ? $"Daily Challenge cleared: +{gold} Gold  ·  +{shards} Shards"
                : $"Daily Challenge cleared: +{gold} Gold";
        }
    }
}
