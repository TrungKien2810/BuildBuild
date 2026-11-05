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
        /// <summary>Phien ban dinh dang save. Nang len khi them field ma save cu phai duoc xu ly khac di —
        /// xem `SaveSystem.Migrate`.</summary>
        public int version = 2;
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
        /// <summary>
        /// Buoc huong dan tan thu dang cho (`Meta/Tutorial`) — chi so trong `Tutorial.Steps`, bang
        /// `Tutorial.Count` la da xong (hoac da Skip). Save cu duoc dat ve "xong" khi nap, xem `Migrate`.
        /// </summary>
        public int tutorialStep;
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
    /// file cua acc dang dang nhap la `save-&lt;ten acc&gt;.json`, acc "main" (acc cu truoc khi co dang nhap, va
    /// la acc ma luc CHUA dang nhap ghi vao) giu ten `save.json`.
    /// Acc dang dung lay tu `Accounts` — dang nhap o man Login la moi thu sau do doc/ghi dung file cua acc do.
    /// Acc moi duoc tang boi canh tan thu (`StarterUnits`); acc `test` / `admin` so huu tat ca (`SaveData.ownsAll`).
    /// Thua KHONG xoa gi o day: vang, so huu, level, pity deu song qua lan thua (GDD muc 1).
    /// </summary>
    public static class SaveSystem
    {
        public const string MainAccount = "main";
        public const string TestAccount = "test";
        /// <summary>Acc de xem lai luc moi choi: moi lan chon la xoa save, bat dau lai tu dau.</summary>
        public const string FreshAccount = "fresh";
        /// <summary>
        /// Acc seed cho dev/QA: so huu MOI unit (tuong, linh, phao), vang va luot quay du de thu bat cu man nao.
        /// Chi duoc tao trong Editor / Development Build (`Accounts.EnsureSeedAccount`), va ten nay bi khoa
        /// khong cho nguoi choi dang ky trung.
        /// </summary>
        public const string AdminAccount = "admin";
        /// <summary>Mat khau cua acc seed — chi co nghia o build dev, xem `AdminAccount`.</summary>
        public const string AdminPassword = "admin1234";

        /// <summary>
        /// Boi canh tan thu cua acc moi. Vi sao khong con dung mot hero: do bang Play that (2026-10-28) thi
        /// Frostweaver mot minh THANG wave 1-2 nhung THUA wave 3 (hero chet, lau dai con 1.405/2.000) — tuc
        /// nguoi choi bi ket o wave 3 ma khong co duong nao go ra ngoai vong lap wave.
        ///   - `mage_elemental` Frostweaver  : tam xa, sat thuong chinh (N)
        ///   - `beetleknight`  Beetle Knight : can chien 1.050 mau, giu 3 quai (N) — thay cho mot hero duy nhat
        ///   - `cannon_rapid`  Rapid Cannon  : be phao dau tien de be trong tren thanh khong vo nghia (N)
        ///   - `soldier_warwolf` War Wolf    : mot to linh de bai sau thanh co thu de keo ra (N)
        /// Khong dung `knight`/`archer`/`cragfist`: ba hero do la thuong cua Hanh trinh moc 2-4.
        /// </summary>
        public static readonly string[] StarterUnits =
        {
            "mage_elemental", "beetleknight", "cannon_rapid", "soldier_warwolf",
        };

        /// <summary>Thuoc mot acc dev/seed — phien cua chung duoc phep song qua lan mo game.</summary>
        public static bool IsDevAccount(string account) =>
            account == MainAccount || account == TestAccount || account == FreshAccount || account == AdminAccount;

        static SaveData _data;

        /// <summary>
        /// Acc dang dung: acc da dang nhap (`Accounts`); chua dang nhap (bam Play thang vao scene tu Editor)
        /// thi roi ve `MainAccount` nhu truoc khi co dang nhap — khong ghi ra file ten rong.
        /// </summary>
        public static string Account
        {
            get
            {
                string account = Accounts.Current;
                return string.IsNullOrEmpty(account) ? MainAccount : account;
            }
        }

        /// <summary>File save cua mot acc bat ky (acc chua dang nhap / acc dev deu di qua day).</summary>
        public static string FileFor(string account) => Path.Combine(Application.persistentDataPath,
            account == MainAccount ? "save.json" : $"save-{account}.json");

        public static string FilePath => FileFor(Account);
        public static SaveData Data => _data ??= Load();

        /// <summary>Xoa file save cua mot acc (acc dev "fresh": moi lan chon la bat dau lai tu dau).</summary>
        public static void DeleteSave(string account)
        {
            string path = FileFor(account);
            if (File.Exists(path)) File.Delete(path);
        }

        /// <summary>
        /// Bo ban dang nho trong RAM de lan doc sau lay file cua acc moi — goi khi dang nhap / dang xuat.
        /// Khong ghi gi xuong dia: du lieu da duoc `Save()` tai tung thoi diem thay doi roi.
        /// </summary>
        public static void Invalidate()
        {
            _data = null;
            _readFailed = false;
        }

        // Doc file loi IO (file dang bi antivirus / app dong bo khoa) chu KHONG phai JSON hong: du lieu trong bo nho
        // la acc trang tam thoi, nen khong duoc ghi de len save that o lan `Save()` ke tiep.
        static bool _readFailed;

        /// <summary>
        /// Chep mot file save sang file khac neu dich chua co — dung MOT lan luc tao acc dau tien tren may
        /// (xem `Accounts.Register`), de tien do cu trong `save.json` khong bi bo lai phia sau man dang nhap.
        /// </summary>
        public static void AdoptSave(string source, string target)
        {
            if (source == target || !File.Exists(source) || File.Exists(target)) return;
            File.Copy(source, target);
        }

        public static void Save()
        {
            if (Data != null && _readFailed)
            {
                Debug.LogWarning("[SaveSystem] Bo qua ghi save: lan doc truoc loi IO, ghi luc nay se de mat tien do that.");
                return;
            }
            // Ghi ra file tam roi moi thay. `File.Replace` thay NGUYEN TU (nhu `Accounts.WriteBook`): xoa roi move
            // thi tat game dung giua hai buoc la khong con `save.json` nao, lan mo sau thanh acc moi.
            string tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(Data, true));
            if (File.Exists(FilePath)) File.Replace(tmp, FilePath, null);
            else File.Move(tmp, FilePath);
        }

        /// <summary>
        /// Xoa tien trinh cua acc dang choi (man Cai dat). KHONG doi acc — tao lai du lieu goc theo dung luat
        /// `NewAccount` (acc dev van so huu tat ca, acc thuong duoc tang lai boi canh tan thu) roi ghi de.
        /// </summary>
        public static void ResetProgress()
        {
            _data = NewAccount();
            Save();
        }

        static SaveData Load()
        {
            if (!File.Exists(FilePath)) return NewAccount();
            string json;
            try
            {
                json = File.ReadAllText(FilePath);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                _readFailed = true;
                Debug.LogError($"[SaveSystem] Khong doc duoc {FilePath} ({e.Message}). Choi tam acc trang, KHONG ghi de save.");
                return NewAccount();
            }
            try
            {
                return Migrate(JsonUtility.FromJson<SaveData>(json) ?? NewAccount());
            }
            catch (Exception e)
            {
                string backup = FilePath + ".corrupt";
                try { File.Copy(FilePath, backup, true); }
                catch (IOException copyError) { Debug.LogError($"[SaveSystem] Khong chep duoc ban hong: {copyError.Message}"); }
                Debug.LogError($"[SaveSystem] Doc save that bai ({e.Message}). Ban hong giu o {backup}, bat dau save moi.");
                return NewAccount();
            }
        }

        /// <summary>
        /// Nang save cu len dinh dang hien tai. Chay trong bo nho va KHONG ghi dia — lan `Save()` ke tiep moi
        /// ghi, nen mo game chi de xem cung khong tao file.
        ///
        /// Man 2 (2026-10-29) — them day huong dan tan thu (`Meta/Tutorial`): save da co tu truoc duoc danh
        /// dau XONG ca day. Nguoi choi cu khong phai tan thu nua, va biet "1 / 8" tu mot acc da qua 20 wave
        /// chi lam phien ho. Muon xem lai: nut "Replay Guide" o man Cai dat.
        /// </summary>
        static SaveData Migrate(SaveData data)
        {
            if (data.version < 2)
            {
                data.tutorialStep = Tutorial.Count;
                data.version = 2;
            }
            return data;
        }

        static SaveData NewAccount()
        {
            var data = new SaveData();
            if (Account == AdminAccount)
            {
                // Acc seed: so huu moi unit. Khong liet ke id nao o day — `SaveData.Find` tu tao ban ghi cho
                // bat ky unit nao duoc hoi khi `ownsAll`, nen ca 4 loai (hero, linh, phao) deu co san.
                data.ownsAll = true;
                data.gold = 250000;
                data.freeRolls = 50;      // du de thu banner ma khong phai cong vang
                data.playerLevel = 10;    // mo gacha theo cap, mo bang ky nang, hien du so lieu HUD
                return data;
            }
            if (Account == TestAccount)
            {
                data.ownsAll = true;
                return data;
            }
            foreach (var id in StarterUnits) data.owned.Add(new OwnedUnit { id = id });
            return data;
        }

        /// <summary>
        /// Admin (F1): cap boi canh tan thu cho acc DANG choi — de go ket mot save cu (tao truoc 2026-10-28 chi co
        /// Frostweaver) ma khong phai xoa va choi lai. Tra ve so unit vua duoc them.
        /// </summary>
        public static int GrantStarterKit()
        {
            int added = 0;
            foreach (var id in StarterUnits)
            {
                if (Data.Find(id) != null) continue;
                Data.owned.Add(new OwnedUnit { id = id });
                added++;
            }
            if (added > 0) Save();
            return added;
        }

        // Tat Domain Reload khi vao Play Mode thi bien static song qua cac lan Play — doc lai tu file.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _data = null;
    }
}
