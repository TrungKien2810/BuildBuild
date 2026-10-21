using System;
using System.Collections.Generic;
using System.IO;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Meta
{
    [Serializable]
    public class OwnedUnit
    {
        public string id;
        public int level = 1;
        public int stars;
        public int shards;
    }

    /// <summary>Mot mon trang bi (xem `Armory`). `heroId` = unitId hero dang mac, rong = nam trong tui.</summary>
    [Serializable]
    public class OwnedEquipment
    {
        public int uid;
        public EquipmentSlot slot;
        public Rarity rarity;
        public int level = 1;
        public string heroId = "";
    }

    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int gold;
        public int rollsSinceSR;
        public int rollsSinceSSR;
        /// <summary>Cap nguoi choi (GDD muc 10.0) + XP trong cap hien tai — xem `Meta/PlayerLevel`.</summary>
        public int playerLevel = 1;
        public int playerXp;
        /// <summary>Luot quay mien phi duoc tang moi 5 cap; man Gacha tieu truoc khi tru vang.</summary>
        public int freeRolls;
        /// <summary>Man Cai dat: am luong nhac 0..1 (mac dinh 0,4 = hang so cu trong `Sfx`) + tat tieng SFX.</summary>
        public float musicVolume = 0.4f;
        public bool sfxMuted;
        public List<OwnedUnit> owned = new List<OwnedUnit>();

        /// <summary>Wave se danh o lan bam tiep theo — song qua viec doi sang man gacha va tat game.</summary>
        public int nextWave = 1;
        /// <summary>Ky luc wave cao nhat da thang (`Meta/WaveMilestones`) — hien khi lap ky luc moi sau wave 30.</summary>
        public int highestWave;
        /// <summary>Id cac goi y ngu canh da hien (`Meta/ContextTips`) — moi y chi hien MOT lan trong doi acc.</summary>
        public List<string> seenTips = new List<string>();
        /// <summary>`yyyy-MM-dd` cua lan THANG thu thach ngay gan nhat (`Meta/DailyChallenge`); "" = chua tung thang.</summary>
        public string dailyDay = "";
        public int bossRushBest;
        public int bossRushClears;
        /// <summary>Lo hang + cac o da mua cua tung loai cua hang (`Meta/Shop`) — moi loai mot ky lam moi.</summary>
        public ShopSave shop = new ShopSave();
        /// <summary>Cap tung nhanh cua bang ky nang toan tai khoan (`Meta/SkillBoard`).</summary>
        public int boardAttack;
        public int boardHealth;
        public int boardHaste;
        /// <summary>Uu tien muc tieu cua don danh thuong (`Combat/Targeting`): 0 = gan nhat (mac dinh).</summary>
        public int targetMode;
        /// <summary>Doi hinh 9 o, theo thu tu o; moi phan tu la ten asset HeroDefinition, "" = o trong.</summary>
        public List<string> lineup = new List<string>();
        /// <summary>Cap thanh tri 1..3 (GAME_DESIGN.md muc 2): doi sprite, mau thanh va so be phao.</summary>
        public int castleTier = 1;
        /// <summary>Phao tren cac be phao dai, tu tren xuong; moi phan tu la ten asset CannonDefinition, "" = be trong.</summary>
        public List<string> cannons = new List<string>();
        /// <summary>Linh o cac cho dung bai sau thanh, tu tren xuong; ten asset SoldierDefinition, "" = cho trong.</summary>
        public List<string> soldiers = new List<string>();
        /// <summary>Acc test: coi nhu so huu MOI unit, ke ca unit chua vao gacha (Chainwarden, Vespera).</summary>
        public bool ownsAll;
        /// <summary>Kho trang bi (ca do dang mac lan do trong tui) + bo dem cap uid cho mon moi.</summary>
        public List<OwnedEquipment> equipment = new List<OwnedEquipment>();
        public int nextEquipmentUid;
        /// <summary>Moc bat dau tich ruong AFK (giay Unix, UTC); 0 = chua co, `AfkChest` dat khi doc lan dau.</summary>
        public long afkSince;
        /// <summary>Ban do Chinh Phat: 3 thanh da do tham, chuoi thang, luot trong ngay (`ConquestMap`).</summary>
        public ConquestSave conquest = new ConquestSave();
        /// <summary>Hanh trinh tan thu (`QuestLog`): id da nhan, ve chon tuong, bo dem quay / Chinh Phat / ruong AFK.</summary>
        public QuestSave quests = new QuestSave();

        /// <summary>
        /// Unit da so huu, null neu chua. Acc `ownsAll` tu tao ban ghi khi hoi lan dau — level/sao/manh van luu nhu
        /// unit quay ra that.
        /// </summary>
        public OwnedUnit Find(string id)
        {
            var unit = owned.Find(o => o.id == id);
            if (unit == null && ownsAll && !string.IsNullOrEmpty(id))
            {
                unit = new OwnedUnit { id = id };
                owned.Add(unit);
            }
            return unit;
        }
    }

    /// <summary>
    /// Save JSON o `Application.persistentDataPath` (GDD muc 9, chot 2026-09-11), MOT file moi tai khoan:
    /// acc chinh `save.json`, acc khac `save-<ten>.json`; acc dang choi nho trong PlayerPrefs.
    /// Acc moi duoc tang hero khoi dau (`StarterUnitId`); acc test so huu tat ca (`SaveData.ownsAll`).
    /// Thua KHONG xoa gi o day: vang, so huu, level, pity deu song qua lan thua (GDD muc 1).
    /// </summary>
    public static class SaveSystem
    {
        public const string MainAccount = "main";
        public const string TestAccount = "test";
        /// <summary>Acc de xem lai luc moi choi: moi lan chon la xoa save, bat dau lai tu dau.</summary>
        public const string FreshAccount = "fresh";
        /// <summary>Hero tang acc moi: Frostweaver — hero dau tien cua game, tam xa danh lan, bac N.</summary>
        public const string StarterUnitId = "mage_elemental";
        const string AccountKey = "BuildBuild.Account";

        static SaveData _data;

        public static string Account => PlayerPrefs.GetString(AccountKey, MainAccount);
        public static string FilePath => Path.Combine(Application.persistentDataPath,
            Account == MainAccount ? "save.json" : $"save-{Account}.json");
        public static SaveData Data => _data ??= Load();

        /// <summary>Doi acc; nguoi goi tu nap lai scene de moi he thong doc save moi.</summary>
        public static void SwitchAccount(string account)
        {
            PlayerPrefs.SetString(AccountKey, account);
            PlayerPrefs.Save();
            _data = null;
            if (account == FreshAccount && File.Exists(FilePath)) File.Delete(FilePath);
        }

        public static void Save()
        {            // Ghi ra file tam roi moi thay: tat game giua luc ghi thi van con ban cu nguyen ven.
            string tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(Data, true));
            if (File.Exists(FilePath)) File.Delete(FilePath);
            File.Move(tmp, FilePath);
        }

        /// <summary>
        /// Xoa tien trinh cua acc dang choi (man Cai dat). KHONG doi acc — tao lai du lieu goc theo dung luat
        /// `NewAccount` (acc test van so huu tat ca, acc thuong duoc tang hero khoi dau) roi ghi de.
        /// </summary>
        public static void ResetProgress()
        {
            _data = NewAccount();
            Save();
        }

        static SaveData Load()
        {
            if (!File.Exists(FilePath)) return NewAccount();
            try
            {
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath)) ?? NewAccount();
            }
            catch (Exception e)
            {
                string backup = FilePath + ".corrupt";
                File.Copy(FilePath, backup, true);
                Debug.LogError($"[SaveSystem] Doc save that bai ({e.Message}). Ban hong giu o {backup}, bat dau save moi.");
                return NewAccount();
            }
        }

        static SaveData NewAccount()
        {
            var data = new SaveData { ownsAll = Account == TestAccount };
            if (!data.ownsAll) data.owned.Add(new OwnedUnit { id = StarterUnitId });
            return data;
        }

        // Tat Domain Reload khi vao Play Mode thi bien static song qua cac lan Play — doc lai tu file.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _data = null;
    }
}
