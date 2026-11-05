using System;
using UnityEngine;

namespace BuildBuild.Data
{
    /// <summary>
    /// Dieu kien cua mot nhiem vu. Moi kieu doc THANG tu trang thai save dang co (wave cao nhat, so hero so huu,
    /// level hero cao nhat) hoac tu bo dem trong `QuestSave` — KHONG luu tien do rieng cua tung nhiem vu.
    /// Nho vay tien do luon la HOI TO: lam viec do truoc khi nhiem vu mo khoa thi luc mo khoa no da xong san
    /// (bai hoc tu RAID Progress Missions: mission khong ghi tien do truoc khi thanh mission hien tai).
    /// </summary>
    public enum QuestGoal
    {
        /// <summary>Thang wave `target` (doc `SaveData.nextWave`).</summary>
        ClearWave,
        /// <summary>So huu `target` hero (khong tinh phao / linh).</summary>
        OwnHeroes,
        /// <summary>Mot hero dat level `target`.</summary>
        MaxHeroLevel,
        /// <summary>Quay gacha `target` luot (quay 10 tinh 10).</summary>
        GachaRolls,
        /// <summary>Thang `target` tran Chinh Phat.</summary>
        ConquestWins,
        /// <summary>Nhan ruong AFK `target` lan.</summary>
        AfkClaims,
        /// <summary>Nhan het thuong cua moc nay — dung cho o thuong CUOI MOC (o nay khong tinh vao chinh no).</summary>
        Chapter,
    }

    /// <summary>Thuong cua mot nhiem vu. Moi truong deu tuy chon; khong co truong nao thi o trong.</summary>
    [Serializable]
    public struct QuestReward
    {
        public int gold;
        [Tooltip("unit id duoc tang NGUYEN CON (rong = khong tang tuong). Da so huu thi quy ra manh.")]
        public string hero;
        [Tooltip("Manh cong cho hero vua duoc Hanh trinh tang (0 = khong).")]
        public int shards;
        [Tooltip("Ve CHON tuong: rong | \"sr\" | \"ssr\". Nguoi choi bam chon sau, xem `QuestScreen`.")]
        public string voucher;
    }

    /// <summary>
    /// Mot dong nhiem vu. Noi dung nam trong code (`QuestCatalog`) chu khong o ScriptableObject: bo so nay con TAM va
    /// dang duoc can lai — cung cho voi `AfkChest` va `ConquestMap`, hai he cung giu so bang hang so trong code.
    /// </summary>
    [Serializable]
    public class Quest
    {
        /// <summary>Khoa trong save (`QuestSave.claimed`). Doi id = nhiem vu da nhan hien lai tu dau.</summary>
        public string id;
        /// <summary>Moc 1..7. Moc chi mo khi da nhan HET thuong cua moc truoc.</summary>
        public int stage;
        public string title;
        public QuestGoal goal;
        public int target;
        public QuestReward reward;

        public bool IsChapterReward => goal == QuestGoal.Chapter;
    }

    /// <summary>
    /// Noi dung Hanh trinh tan thu: 7 moc x (3 nhiem vu + 1 o thuong cuoi moc). Hai pattern lay tu game da khao sat:
    /// King God Castle (xong 100% chuong -> tang hero nguyen con) va FEH Heroes' Path (xong het moc truoc moi mo moc sau).
    ///
    /// Vi sao co he nay (do trong code, khong phai cam tinh): acc moi co boi canh tan thu (`SaveSystem.StarterUnits`:
    /// 2 hero + 1 phao + 1 linh) nhung san co 9 o hero, va 10 wave dau chi ~2.000 vang ~ 4 luot quay. Moc 2/3/4 tang
    /// hero CO DINH, moc 5/7 tang VE CHON tuong — theo khao sat 11 tua thi selector that luon nam o moc MUON.
    /// </summary>
    public static class QuestCatalog
    {
        public const int StageCount = 7;
        /// <summary>So o cua mot moc: 3 nhiem vu + o thuong cuoi moc.</summary>
        public const int RowsPerStage = 4;

        /// <summary>Ve "sr" — 4 vai khac nhau de nguoi choi chon theo doi hinh dang thieu.</summary>
        static readonly string[] SrVoucher = { "save", "honglien", "blacksigilhunter", "banneret" };
        /// <summary>Ve "ssr" — ca 8 SSR dang co.</summary>
        static readonly string[] SsrVoucher =
            { "quanvu", "wyrmspear", "stormcaller", "wukong", "bachloctien", "truongphi", "luubi", "haunghe" };

        public static string[] VoucherPool(string voucher) =>
            voucher == "sr" ? SrVoucher : voucher == "ssr" ? SsrVoucher : Array.Empty<string>();

        /// <summary>Manh quy doi khi da so huu hero duoc tang — theo bac, de khong bao gio la "phan thuong vo dung".</summary>
        public static int ShardsFor(Rarity rarity) =>
            rarity == Rarity.SSR ? 100 : rarity == Rarity.SR ? 60 : 20;

        public static readonly Quest[] All =
        {
            Q("j1-1", 1, "Win your first wave", QuestGoal.ClearWave, 1, gold: 300),
            Q("j1-2", 1, "Level a hero to Lv 2", QuestGoal.MaxHeroLevel, 2, gold: 200),
            Q("j1-3", 1, "Summon once at the Gate", QuestGoal.GachaRolls, 1, gold: 200),
            Q("j1-c", 1, "Chapter 1 reward", QuestGoal.Chapter, 3, gold: 300),

            Q("j2-1", 2, "Win wave 3", QuestGoal.ClearWave, 3, gold: 300),
            Q("j2-2", 2, "Level a hero to Lv 5", QuestGoal.MaxHeroLevel, 5, gold: 300),
            Q("j2-3", 2, "Claim the AFK Chest once", QuestGoal.AfkClaims, 1, gold: 300),
            Q("j2-c", 2, "Chapter 2 reward", QuestGoal.Chapter, 3, hero: "archer"),

            Q("j3-1", 3, "Win wave 5", QuestGoal.ClearWave, 5, gold: 400),
            Q("j3-2", 3, "Defeat the wave 10 boss", QuestGoal.ClearWave, 10, gold: 500),
            Q("j3-3", 3, "Level a hero to Lv 8", QuestGoal.MaxHeroLevel, 8, gold: 400),
            Q("j3-c", 3, "Chapter 3 reward", QuestGoal.Chapter, 3, hero: "knight"),

            Q("j4-1", 4, "Win a Conquest battle", QuestGoal.ConquestWins, 1, gold: 500),
            Q("j4-2", 4, "Summon 5 times", QuestGoal.GachaRolls, 5, gold: 500),
            Q("j4-3", 4, "Level a hero to Lv 10", QuestGoal.MaxHeroLevel, 10, gold: 500),
            Q("j4-c", 4, "Chapter 4 reward", QuestGoal.Chapter, 3, hero: "cragfist"),

            Q("j5-1", 5, "Defeat the wave 20 boss", QuestGoal.ClearWave, 20, gold: 600),
            Q("j5-2", 5, "Own 4 heroes", QuestGoal.OwnHeroes, 4, gold: 500),
            Q("j5-3", 5, "Level a hero to Lv 15", QuestGoal.MaxHeroLevel, 15, gold: 500),
            Q("j5-c", 5, "Chapter 5 reward", QuestGoal.Chapter, 3, gold: 600, voucher: "sr"),

            Q("j6-1", 6, "Win wave 25", QuestGoal.ClearWave, 25, gold: 700),
            Q("j6-2", 6, "Win 3 Conquest battles", QuestGoal.ConquestWins, 3, gold: 700),
            Q("j6-3", 6, "Level a hero to Lv 20", QuestGoal.MaxHeroLevel, 20, gold: 600),
            Q("j6-c", 6, "Chapter 6 reward", QuestGoal.Chapter, 3, gold: 800, shards: 60),

            Q("j7-1", 7, "Defeat the wave 30 boss", QuestGoal.ClearWave, 30, gold: 1000),
            Q("j7-2", 7, "Summon 20 times", QuestGoal.GachaRolls, 20, gold: 800),
            Q("j7-3", 7, "Own 6 heroes", QuestGoal.OwnHeroes, 6, gold: 800),
            Q("j7-c", 7, "Chapter 7 reward", QuestGoal.Chapter, 3, gold: 1000, voucher: "ssr"),
        };

        static Quest Q(string id, int stage, string title, QuestGoal goal, int target,
            int gold = 0, string hero = "", int shards = 0, string voucher = "") =>
            new Quest
            {
                id = id, stage = stage, title = title, goal = goal, target = target,
                reward = new QuestReward { gold = gold, hero = hero, shards = shards, voucher = voucher },
            };
    }
}
