using System;
using UnityEngine;

namespace BuildBuild.Meta
{
    /// <summary>
    /// Cap nguoi choi (GDD muc 10.0: chan dung + `Lv` + thanh kinh nghiem CO SO).
    ///
    /// XP chi den tu viec danh that: thang wave thuong, thang wave boss (moi 10 wave) va thang mot tran
    /// Chinh Phat. Ruong AFK KHONG cho XP — no la "cho", khong phai "danh", nen khong mo duong farm AFK
    /// (cung ly do GDD muc 7 chot "chi THANG wave moi co vang").
    ///
    /// Lop nay KHONG tu cong vang: `Meta` khong duoc phep biet `Economy` (vong namespace da ghi o
    /// `IDEAS.md` "No kien truc"), nen no chi ban su kien <see cref="LeveledUp"/> va noi nghe — `BattleHud`,
    /// noi da co san vi tien — tra thuong. Nho vay mo hinh du lieu van test duoc ma khong can scene.
    /// </summary>
    public static class PlayerLevel
    {
        /// <summary>XP can de len cap 2.</summary>
        public const int XpBase = 100;
        /// <summary>Moi cap cong them ngan nay: cap 2 can 150, cap 3 can 200, ...</summary>
        public const int XpPerLevel = 50;
        /// <summary>Moi 5 cap tang 1 luot quay mien phi (tieu o man Gacha, `GachaScreen.Roll`).</summary>
        public const int FreeRollEvery = 5;
        /// <summary>Thang mot tran Chinh Phat — mot tran rieng, khong chia theo wave.</summary>
        public const int ConquestXp = 60;

        public static int Level => Mathf.Max(1, SaveSystem.Data.playerLevel);
        public static int Xp => Mathf.Max(0, SaveSystem.Data.playerXp);
        public static int FreeRolls => Mathf.Max(0, SaveSystem.Data.freeRolls);

        /// <summary>XP hoac cap vua doi — HUD cap nhat thanh + so.</summary>
        public static event Action Changed;
        /// <summary>Vua len cap (tham so = cap moi) — noi nghe tra vang va bao nguoi choi.</summary>
        public static event Action<int> LeveledUp;

        /// <summary>XP de di tu `level` len `level + 1`.</summary>
        public static int XpFor(int level) => XpBase + XpPerLevel * Mathf.Max(0, level - 1);

        /// <summary>Thang wave: deu theo so wave, wave boss (chia het cho 10) them mot cuc.</summary>
        public static int WaveXp(int wave) => 20 + 4 * Mathf.Max(1, wave) + (wave % 10 == 0 ? 50 : 0);

        /// <summary>Vang thuong khi len cap — so TAM (Claude), chua can.</summary>
        public static int GoldFor(int level) => 50 + 20 * Mathf.Max(1, level);

        /// <summary>Cap nay co tang luot quay mien phi khong.</summary>
        public static bool GrantsFreeRoll(int level) => level % FreeRollEvery == 0;

        /// <summary>
        /// Cong XP va len cap (nhieu cap mot luc neu du). Tu luu MOT lan sau khi xu ly xong thay vi moi cap
        /// mot lan — noi goi dang o trong mot luot da co `SaveSystem.Save()`, ghi file lap lai la vo ich.
        /// </summary>
        public static void AddXp(int amount)
        {
            if (amount <= 0) return;

            var save = SaveSystem.Data;
            save.playerLevel = Level;
            save.playerXp = Xp + amount;

            int gained = 0;
            int need = XpFor(save.playerLevel);
            while (save.playerXp >= need)
            {
                save.playerXp -= need;
                save.playerLevel++;
                if (GrantsFreeRoll(save.playerLevel)) save.freeRolls++;
                gained++;
                need = XpFor(save.playerLevel);
            }

            SaveSystem.Save();
            Changed?.Invoke();
            // Ban SAU khi luu: noi nghe len cap con cong vang, ma `Economy.Add` lai tu luu — de sau thi
            // ban ghi cuoi cung la ban day du nhat.
            for (int i = 0; i < gained; i++) LeveledUp?.Invoke(save.playerLevel - gained + 1 + i);
        }
    }
}
