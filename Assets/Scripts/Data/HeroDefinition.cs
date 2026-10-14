using UnityEngine;

namespace BuildBuild.Data
{
    /// <summary>
    /// Hai tang ky nang. Khac nhau o CHO NGUOI CHOI THAY: chieu LON co thanh mana tren dau nen doan
    /// truoc duoc luc no len; chieu NHO chay ngam theo cooldown, khong bao gi ca.
    /// </summary>
    public enum SkillTier
    {
        /// <summary>Chieu lon: day thanh mana thi tu cast, mana ve 0.</summary>
        Ultimate,
        /// <summary>Chieu nho: cooldown AN, khong hien thanh, khong an mana.</summary>
        Minor,
    }

    /// <summary>Ky nang LAM GI khi cast. Y nghia cua skillRadius / skillDuration / skillPower doi theo loai.</summary>
    public enum SkillKind
    {
        None,
        /// <summary>Frostweaver: vung bang ban kinh `skillRadius`, ton tai `skillDuration`.</summary>
        FrostZone,
        /// <summary>Cragfist Hoa Da: bat dong + mien sat thuong `skillDuration`, phan `skillPower` sat thuong.</summary>
        Petrify,
        /// <summary>Ironblade: chem quat dai `skillRadius`, sat thuong damage x `skillPower`.</summary>
        ConeSweep,
        /// <summary>Hawkfletch Ban Nhanh Lien Hoan: `skillDuration` giay toc danh x `skillPower`.</summary>
        RapidFire,
        /// <summary>Bonecaller: goi ky si tu than (con song thi hoi day mau). So nam o `summoner`.</summary>
        SummonKnight,
        /// <summary>Ky si tu than: phi dai `skillRadius`, `skillPower` sat thuong, choang `skillDuration`.</summary>
        Charge,
        /// <summary>Chainwarden Nem Moc: keo quai tam xa ve sat minh + choang + Xich. So nam o `warden`.</summary>
        HookThrow,
        /// <summary>Wyrmspear: lan dau Long Hoa (bien hinh + goi dia long), sau do Thien Dia Long Giang. So nam o `dragonRider`.</summary>
        DragonRider,
        /// <summary>
        /// Vespera Den Canh: vong ban kinh `skillRadius` dat quanh dong minh thap mau nhat trong tam; moi hero trong vong
        /// hoi damage x `skillPower` mau + khien `shieldAmount` trong `skillDuration` giay.
        /// </summary>
        Lantern,
        /// <summary>Banneret Hieu Lenh Xung Phong: ca doi +sat thuong. So nam o `banner`.</summary>
        Rally,
        /// <summary>
        /// Wukong Nhu Y Thien Tru: gay khong lo dap vet dai `skillRadius` vao cum quai, damage x `skillPower`, choang
        /// `skillDuration`. Phan con lai cua bo chieu nam o `wukong`.
        /// </summary>
        GiantStaff,
        /// <summary>
        /// Stormcaller Loi Bao: cot set giang xuong quai trong tam, damage x `skillPower` moi cot, choang `skillDuration`.
        /// Phan con lai cua bo chieu nam o `stormcaller`.
        /// </summary>
        ThunderStorm,
        /// <summary>
        /// Galemonk Long Quyen: loc xoay o cum quai dong nhat, hut quai trong `skillRadius`, giu `skillDuration` giay, moi
        /// giay damage x `skillPower`. Phan con lai cua bo chieu nam o `galemonk`.
        /// </summary>
        Whirlwind,
        /// <summary>
        /// Bo Dao Vien: Luu Bi Dao Vien Ket Nghia / Quan Vu Thanh Long Hien / Truong Phi Duong Duong Kieu Hong — nghia cua
        /// `skillPower` / `skillRadius` / `skillDuration` doi theo hero, xem `DaoVienDefinition`.
        /// </summary>
        DaoVien,
        /// <summary>
        /// Phu Thuy Dam Lay Vac Suong Doc: suong ban kinh `skillRadius` o cum quai dong nhat, ton tai `skillDuration` giay;
        /// quai trong suong +1 tang Doc moi giay va bi pha giap `skillPower` (0,2 = nhan them 20%). So Doc lay o noi tai Venom.
        /// </summary>
        ToxicMist,
        /// <summary>
        /// Hiep Si Nam No Nam: 3 cay nam moc quanh chan cach `skillRadius`, `skillDuration` giay sau no bao tu — moi cay
        /// damage x `skillPower` cho quai trong 0,6.
        /// </summary>
        MushroomBloom,
        /// <summary>
        /// Ngu Nhan Xoay Nuoc: xoay ban kinh `skillRadius` quanh chan `skillDuration` giay — quai trong xoay khong danh duoc
        /// (tinh anh -50% sat thuong, boss khong bi khoa), moi giay an damage x `skillPower`.
        /// </summary>
        Whirlpool,
        /// <summary>
        /// Phap Su Lua Mua Tan Lua: 3 cau lua roi lech nhau quanh cum quai dong nhat, moi qua no ban kinh `skillRadius` gay
        /// damage x `skillPower` + Chay, de lai vung lua `skillDuration` giay. FX: `skillIntroFrames` cau roi,
        /// `skillOutroFrames` vu no, `skillFrames` vung lua lap. So Chay lay o noi tai Ignite.
        /// </summary>
        EmberRain,
        /// <summary>
        /// Nguoi Lun Tuong Rune: tuong doc dai `skillRadius` moi phia truoc mat, `skillDuration` giay — chan quai + dan quai,
        /// hero dung yen sau khien toi het gio. FX: `skillIntroFrames` moc, `skillFrames` lap, `skillOutroFrames` tan.
        /// </summary>
        RuneWall,
        /// <summary>
        /// Tien Det To Mua Roi: quai nhieu mau nhat trong tam (tru boss) sang phe minh `skillDuration` giay (tinh anh mot
        /// nua). FX: `skillFrames` day roi lap, `skillOutroFrames` dut to.
        /// </summary>
        Puppet,
        BloodShatter, SoulPact, SeedRain, TrapDetonation, SigilBombs,
        /// <summary>
        /// Hiep Si Bo Hung Huc Sung: xung quanh chan ban kinh `skillRadius`, damage x `skillPower`, choang `skillDuration`
        /// (tinh anh / boss mot nua). FX: `skillFrames` xung dat, `stunFrames` dau choang.
        /// </summary>
        HornSlam,
        /// <summary>
        /// Hiep Khach Song Dao Bao Dao: khoa muc tieu dang danh, 6 nhat trong 3s, moi nhat damage x `skillPower` va huong
        /// tang Lien Hoan luc bat dau; het chieu xoa tang. FX: `skillFrames` vet dao.
        /// </summary>
        BladeStorm,
        /// <summary>
        /// Nu Tu Chuong Bac Hoi Chuong: go Doc/Chay/Cham/Troi/Khoa Don/Choang khoi moi dong minh; doi thu cham `skillPower`
        /// trong `skillDuration` (tinh anh mot nua, boss khong). FX: `skillFrames` song chuong.
        /// </summary>
        BellToll,
        /// <summary>
        /// Nguyet Tho Luan Ngoc Luan Hoi: nem luan lon xuyen duong dai `skillRadius` theo huong khoa luc phong roi thu ve
        /// cung duong; luot di damage x `skillPower`, luot ve = 75% luot di. Moi dich toi da 1 hit moi luot.
        /// </summary>
        RingReturn,
        /// <summary>
        /// Tho San Hac An Phan Quyet Hac An: dung 3 coc an trong vung `skillRadius` quanh muc tieu, sau `skillDuration`
        /// giay no moi coc damage x `skillPower` (muc tieu Xe Giap +50%); gan Khoa Hoi Mau.
        /// </summary>
        SigilJudgment,
        /// <summary>
        /// Dao Si Ho Lo Can Khon Khai Ho: hut sach Doc/Chay/Cham quanh dong minh roi tieu toan bo Khi phun non
        /// `skillDuration` giay, moi nhip damage x (0,5 + 0,1 x Khi). So nam o `gourdTaoist`.
        /// </summary>
        GourdUnleash,
        /// <summary>
        /// Ngu But Su Hoa Long Diem Tinh: mac long quet dai `skillRadius` roi no cuoi duong. So nam o `inkScribe`.
        /// </summary>
        InkDragon,
        /// <summary>
        /// Vu Nu Hong Lien Hong Lien Dai Vu: tung nu sen lon roi no toan bo nu. So nam o `hongLien`.
        /// </summary>
        LotusBloom,
        /// <summary>
        /// Sa Ve Sa Thanh Tran Tuyen: sat thuong quanh minh + vung cat giam toc/dan xa. So nam o `saVe`.
        /// </summary>
        SandFort,
        /// <summary>
        /// Khoi Loi Su Khoi Vuong Giang Tran: roi bung khop thanh dai khoi, nen song chan dong + chien dau. So nam o `khoiLoiSu`.
        /// </summary>
        GrandPuppet,
        /// <summary>
        /// Nguoi Soi Nguyet Bien: MOT LAN moi tran, hoa soi VINH VIEN trong tran. So nam o `werewolf`.
        /// </summary>
        MoonTransform,
        /// <summary>
        /// Hau Nghe Xa Nhat: giuong cung bao truoc `aimSeconds` giay roi ban ten lon xuyen doan lane `shotLength`,
        /// moi dich an `shotPower` ATK mot lan, muc tieu da nham an them `sealBonus` ATK moi Nhat An roi xoa an.
        /// So nam o `hauNghe`.
        /// </summary>
        SolarShot,
        /// <summary>
        /// Bach Loc Tien Bach Loc Hoi Xuan: bao truoc roi bong loc buoc doc lane, de lai dai hoa hoi `laneHealPerSecond`
        /// ATK moi giay cho hero/linh dang song trong dai. So nam o `bachLocTien`.
        /// </summary>
        DeerRevival,
    }

    /// <summary>
    /// Noi tai cua hero KHONG co bo chieu rieng (chot 2026-09-29). Hero co component rieng (Bonecaller, Chainwarden,
    /// Wyrmspear, Banneret) giu noi tai trong do, o nay de None. Y nghia cua `passive*` doi theo loai.
    /// </summary>
    public enum PassiveKind
    {
        None,
        /// <summary>Ironblade Truy Kich: don thuong vao quai duoi `passiveThreshold` mau +`passivePower` sat thuong.</summary>
        Execute,
        /// <summary>Cragfist Khieu Khich: quai trong `passiveRadius` bo unit khac, vao cho cua hero nay (con cho trong).</summary>
        Taunt,
        /// <summary>Hawkfletch Nham Yeu Diem: cu phat thu `passiveEvery` sat thuong x `passivePower`.</summary>
        Crit,
        /// <summary>
        /// Frostweaver Vo Bang: quai dang bi lam cham chet trong tam danh -> no, quai trong `passiveRadius` an
        /// don thuong x `passivePower`.
        /// </summary>
        Shatter,
        /// <summary>Vespera Cuu Nguy: dong minh duoi `passiveThreshold` mau duoc hoi +`passivePower`.</summary>
        Rescue,
        /// <summary>Warhorn Khac Che: moi don (ke ca huc) vao tinh anh / boss +`passivePower` sat thuong.</summary>
        BossBane,
        /// <summary>
        /// Phu Thuy Dam Lay Ngam Doc: moi quai an don thuong +1 tang Doc (toi da `passiveEvery`), moi tang `passivePower`
        /// sat thuong/giay, trung lai lam moi `passiveDuration` giay.
        /// </summary>
        Venom,
        /// <summary>Hiep Si Nam Bao Tu: bi danh can chien thi quai trong `passiveRadius` an `passivePower` (toi da 2 lan/giay).</summary>
        Spores,
        /// <summary>Ngu Nhan Thoc Hang: don thuong xuyen qua — quai dung ngay sau muc tieu (trong `passiveRadius`) an x `passivePower`.</summary>
        Pierce,
        /// <summary>
        /// Phap Su Lua Ben Lua: don thuong (luon) gay Chay `passivePower` sat thuong/giay trong `passiveDuration` giay, lam moi
        /// khong cong don; noi tai mo thi quai dang Chay chet -> lua nhay sang quai gan nhat trong `passiveRadius`.
        /// </summary>
        Ignite,
        /// <summary>Nguoi Lun Chan Dan: dan quai ban xa trung ong thi vo, khong sat thuong. `passiveFrames` = tia dan vo.</summary>
        ShotBreak,
        /// <summary>Tien Det To Day Roi: quai an don thuong bi `passivePower` (0,25 = 25%) don truot trong `passiveDuration` giay.</summary>
        Tangle,
        SeedStacks, Traps, SigilWeakness,
        /// <summary>Bo Hung Chan Dong: moi don thuong thu `passiveEvery` trung muc tieu hat no lui `passiveRadius` (tinh anh mot nua, boss khong).</summary>
        Quake,
        /// <summary>
        /// Song Dao Lien Hoan: moi don trung CUNG muc tieu +`passivePower` sat thuong don ke tiep, toi da `passiveEvery` tang;
        /// doi muc tieu hoac `passiveDuration` giay khong trung thi mat tang. `passiveFrames` = dau tang (frame i = i+1 tang).
        /// </summary>
        Combo,
        /// <summary>
        /// Chuong Bac Thanh Tay: moi `passiveDuration` giay go 1 hieu ung Doc -> Chay -> Cham khoi dong minh gan nhat dang bi
        /// trong `passiveRadius`; khong ai can thi giu luot. `passiveFrames` = FX thanh tay tren nguoi duoc go.
        /// </summary>
        Purify,
        /// <summary>Linh Nam: don trung de vung bao tu ban kinh `passiveRadius` `passiveDuration` giay, quai trong vung cham `passivePower`.</summary>
        SporeField,
        /// <summary>Cua Kim Sat: moi don thu `passiveEvery` pha giap muc tieu `passivePower` trong `passiveDuration` giay (chi lam moi, khong cong).</summary>
        ClampBreak,
        /// <summary>
        /// Nguyet Tho Luan Hoi Luan: moi don thu 3 TRUNG dich, vong quay ve gay them `passivePower` x damage len toi da
        /// 3 dich tren duong ve (moi dich 1 lan).
        /// </summary>
        ReturnStrike,
        /// <summary>
        /// Tho San Hac An Pha An: don truc tiep cua chinh Tho San len muc tieu dang co khien +`passivePower` sat thuong
        /// (chi xet khien luc trung, khong tang phan vao mau).
        /// </summary>
        ShieldBane,
    }

    /// <summary>
    /// Chi so chien dau cua mot hero dat xuong san. So goc lay tu GAME_DESIGN.md muc 3 — la gia tri
    /// KHOI DIEM de can bang, nen de o asset chu khong hard-code trong script.
    /// </summary>
    [CreateAssetMenu(menuName = "BuildBuild/Hero Definition", fileName = "Hero_")]
    public class HeroDefinition : ScriptableObject
    {
        /// <summary>
        /// Ten hien thi. De TRONG chu khong lay ten mot hero lam mac dinh: `SceneBuilder` bake ten nay vao bang chon
        /// hero luc build scene, nen mot asset tao ma quen dat ten se hien sai ten cua hero khac (dot 2026-10-09:
        /// dong "Hau Nghe" hien chu "Frostweaver" — ten mac dinh cu cua lop).
        /// </summary>
        public string displayName = "";
        [Tooltip(
            "Id `UnitDefinition` trong gacha (vd. knight, soldier_warwolf). Chi dat/trieu hoi duoc khi save da so huu id " +
            "nay. Trong = khong bao gio so huu (linh xuong/ky si cua Bonecaller khong can — khong qua bang chon).")]
        public string unitId;
        [Tooltip("Vai tro hien tren the + bo loc man Doi quan (ten/icon o `HeroRoleCatalog`). Khong doi hanh vi tran.")]
        public HeroRole role;

        [Header("Chi so goc (GAME_DESIGN.md muc 3)")]
        [Tooltip("Mau cua hero. Hero AN DON nhu moi thu khac tren san — het mau la chet, het wave moi song lai.")]
        public float maxHealth = 260f;
        public float damage = 35f;
        [Tooltip("So don moi giay.")]
        public float attackRate = 0.6f;
        [Tooltip("Tam danh (don vi the gioi), do tu chan hero. <= 1,5 la can chien.")]
        public float range = 5f;

        [Header("Di chuyen")]
        [Tooltip("Don vi the gioi moi giay. Goblin di 1.5 de so sanh.")]
        public float moveSpeed = 1.2f;
        [Tooltip(
            "CHI tuong TAM XA (range > 1,5): hero di xa o toi da bao nhieu unit de dua quai vao tam. Quai " +
            "khong vao tam duoc trong khoang do thi hero ve o. Can chien bo qua o nay — duoi quai khap san.")]
        public float leashRange = 3f;

        [Header("Chan quai (GAME_DESIGN.md muc 2)")]
        [Tooltip("So quai unit nay GIU cung luc; quai thu N+1 tro di di tiep ve lau dai. Moi unit 3, Chainwarden 4.")]
        [Min(1)] public int maxHeld = 3;
        [Tooltip(
            "Quai DANG bi unit nay giu mang trang thai Xich: nhan them bay nhieu phan sat thuong tu MOI nguon " +
            "(0,2 = +20%). 0 = khong Xich. Chainwarden noi tai Cai Nguc.")]
        [Min(0f)] public float heldDamageBonus;
        [Tooltip("Chi Chainwarden: chieu thuong Quat Xich + so cua chieu lon Nem Moc. Trong = khong co.")]
        public WardenDefinition warden;
        [Tooltip("Chi Wyrmspear: noi tai Nghich Lan, chieu thuong, bien hinh Long Nhan + Bach Dia Long. Trong = khong co.")]
        public DragonRiderDefinition dragonRider;
        [Tooltip("Chi Banneret: don thuong buff ngau nhien 1 dong minh, noi tai Quan Ky, Cam Co, Hieu Lenh. Trong = khong co.")]
        public BannerDefinition banner;
        [Tooltip("Chi Wukong: The Than, Nho Long Phan Than, Toan Phong Con, so phu cua Nhu Y Thien Tru. Trong = khong co.")]
        public WukongDefinition wukong;
        [Tooltip("Chi Stormcaller: Xich Loi nay chuoi, Qua Tai, Cot Thu Loi, Loi Bo, so phu cua Loi Bao. Trong = khong co.")]
        public StormcallerDefinition stormcaller;
        [Tooltip("Chi Galemonk: Chuong Phong day lui, Thuan Phong, Phong Tao, so phu cua Long Quyen. Trong = khong co.")]
        public GalemonkDefinition galemonk;
        [Tooltip("Chi bo Dao Vien (Luu Bi / Quan Vu / Truong Phi): noi tai Ket Nghia chung + bo chieu tung nguoi. Trong = khong co.")]
        public DaoVienDefinition daoVien;
        public BloodswordsmanKit bloodswordsman;
        public SoulbinderKit soulbinder;
        [Tooltip("Chi Dao Si Ho Lo: tich Khi, Thu Ta, Luyen Khi Pha, Can Khon Khai Ho. Trong = khong co.")]
        public GourdTaoistKit gourdTaoist;
        [Tooltip("Chi Ngu But Su: Tam But, Hoanh But, Tran Son An, Hoa Long Diem Tinh. Trong = khong co.")]
        public InkScribeKit inkScribe;
        [Tooltip("Chi Vu Nu Hong Lien: Gieo Sen, Song Phien Vu, Lien Tam Dan, Hong Lien Dai Vu. Trong = khong co.")]
        public HongLienKit hongLien;
        [Tooltip("Chi Sa Ve: Tu Sa, Tram Sa Quyen, Boi Thach, Sa Thanh Tran Tuyen. Trong = khong co.")]
        public SaVeKit saVe;
        [Tooltip("Chi Khoi Loi Su: roi MocKhoi, Ho Chu, Khoi Vuong Giang Tran. Trong = khong co.")]
        public KhoiLoiSuKit khoiLoiSu;
        [Tooltip("Chi Nguoi Soi: Huyet Thuc, Tru Nguyet, Ngoam Ha, Nguyet Bien hoa soi. Trong = khong co.")]
        public WerewolfKit werewolf;
        [Tooltip("Chi Hau Nghe: Nhat An, Nhat Luan Tran Tuyen, Cuu Tien Quy Nhat, Nhat Boc Lien Hoan, Xa Nhat. Trong = khong co.")]
        public HauNgheKit hauNghe;
        [Tooltip("Chi Bach Loc Tien: Sinh Khi, Tam Mam Hoi Xuan, Hoan Diep Tuc Sinh, Loc Anh Tuan Lam, Bach Loc Hoi Xuan. Trong = khong co.")]
        public BachLocTienKit bachLocTien;
        [Tooltip("Hero nao trong bo Dao Vien — quyet dinh phan nao cua `daoVien` chay.")]
        public SwornBrother swornBrother;

        [Header("Dan")]
        public float projectileSpeed = 8f;
        [Tooltip("Ban kinh no quanh diem trung (don vi the gioi) — vai tro sat thuong dien rong.")]
        public float splashRadius = 0.8f;
        public Sprite[] projectileFrames;
        [Tooltip("Scale sprite dan LUC BAY (luc no tra ve 1). Frost-bolt ve rong 80 px, to hon ca Frostweaver 31 px -> 0,5.")]
        public float projectileScale = 1f;
        public Sprite[] impactFrames;
        [Tooltip("Scale frame NO (nhan them UnitScale). Frost-impact ve to -> 0,6.")]
        public float impactScale = 1f;
        [Tooltip("Don thuong day lui moi quai trung, ke ca quai dang vung attack (ky si tu than). Charge: quai bi huc cung bi day lui.")]
        public bool knockbackOnHit;

        [Header("Am thanh (ten file trong Resources/Audio/SFX, rong = im)")]
        [Tooltip("Luc ra don thuong (nha day cung, tung phep). Don can chien da co tieng trung chung, de rong.")]
        public string fireSfx;
        [Tooltip("Luc dan trung dich (chi hero co dan bay).")]
        public string impactSfx;
        [Tooltip("Luc tung chieu lon.")]
        public string skillSfx;

        [Header("Ky nang")]
        [Tooltip("None = khong co ky nang. Loai quyet dinh 3 o Radius / Duration / Power ben duoi nghia la gi.")]
        public SkillKind skillKind = SkillKind.None;

        [Tooltip("Chieu LON chay bang thanh mana; chieu NHO chay bang cooldown an, khong co thanh.")]
        public SkillTier skillTier = SkillTier.Ultimate;

        [Tooltip("CHI cho chieu NHO: giay giua hai lan cast. 0 = khong co chieu nho. Chieu lon bo qua o nay.")]
        public float skillCooldown;

        [Tooltip("CHI cho chieu LON: mana day thi TU DONG cast. 0 = khong co chieu lon. GDD muc 3, cot Mana.")]
        public float manaMax;
        [Tooltip("Mana hoi moi giay, chay ca khi khong danh duoc con nao.")]
        public float manaPerSecond = 0.35f;
        [Tooltip("Mana cong them cho MOI con quai an don thuong — danh dong thi day thanh nhanh hon.")]
        public float manaPerHit = 0.25f;
        [Tooltip("FrostZone: ban kinh vung. ConeSweep: chieu dai nhat chem. Lantern: ban kinh vong Den Canh.")]
        public float skillRadius = 1.6f;
        [Tooltip("FrostZone: vung ton tai bao lau. Petrify: bat dong bao lau. RapidFire: buff bao lau. Lantern: khien ton tai bao lau.")]
        public float skillDuration = 3f;
        [Tooltip("ConeSweep: he so x damage. RapidFire: he so x toc danh. Petrify: ti le sat thuong phan lai (0,2 = 20%). Lantern: mau hoi = damage x he so.")]
        public float skillPower = 1f;
        [Tooltip("FrostZone: frame vung bang (trong = placeholder). ConeSweep: frame vet chem. Petrify: vo da LAP trong luc hoa da. Lantern: vong tren dat. Charge: bui tung o chan luc bat dau phi.")]
        public Sprite[] skillFrames;
        [Tooltip("Petrify: vo da moc len, chay MOT lan truoc khi lap skillFrames. MushroomBloom: nam moc len (giu frame cuoi toi khi no).")]
        public Sprite[] skillIntroFrames;
        [Tooltip("Whirlpool: xoay tan, chay MOT lan khi het chieu. SporeField (Linh Nam): vung bao tu tan.")]
        public Sprite[] skillOutroFrames;
        [Tooltip("Charge (Ky si tu than): sao choang LAP tren dau quai trong luc bi choang. Trong = khong co FX.")]
        public Sprite[] stunFrames;

        /// <summary>
        /// Chi hero nao duoc bat ro rang trong asset moi co ky nang — loai va CA HAI tang deu mac dinh
        /// "khong co". Doi tang ma quen dien so cua tang do thi hero mat ky nang han, khong am tham chay tang cu.
        /// </summary>
        public bool HasSkill => skillKind != SkillKind.None
            && (skillTier == SkillTier.Ultimate ? manaMax > 0f : skillCooldown > 0f);

        [Tooltip("Chi hero trieu hoi (Bonecaller): noi tai + chieu thuong + linh. Trong = khong trieu hoi.")]
        public SummonerDefinition summoner;

        [Header("Ho tro")]
        [Tooltip(
            "Don thuong la HOI MAU: `damage` mau cho dong minh thap mau nhat (theo ti le) trong `range`, khong ban quai " +
            "(Vespera). Hero di toi dong minh bi thuong trong day xich, khong co ai bi thuong thi ve o.")]
        public bool healsAllies;
        [Tooltip("FX hoi mau chay mot lan tren nguoi duoc hoi.")]
        public Sprite[] healFrames;
        [Tooltip("RapidFire: FX lap tren dau trong luc buff. Trong = to mau cam nhu cu.")]
        public Sprite[] buffFrames;
        [Tooltip("Lantern: luong sat thuong khien ao chan duoc.")]
        public float shieldAmount;
        [Tooltip("Lantern: vo khien LAP quanh than nguoi duoc che.")]
        public Sprite[] shieldFrames;
        [Tooltip("Lantern: khien vo (chay mot lan khi bi danh het).")]
        public Sprite[] shieldBreakFrames;

        [Header("Mo khoa theo sao (chot 2026-09-28)")]
        [Tooltip(
            "So sao can de mo NOI TAI (Chainwarden Cai Nguc, Bonecaller dung xac, Wyrmspear Nghich Lan). Danh thuong + " +
            "chieu cuoi luon mo tu 0 sao. Hero khong co noi tai bo qua o nay.")]
        [Min(0)] public int passiveStar = 1;
        [Tooltip("So sao can de mo CHIEU PHU (Quat Xich, vong an ky, Xuyen Long + Lien Thuong).")]
        [Min(0)] public int minorStar = 3;
        [Tooltip("Chieu phu thu hai: Wyrmspear Dia Long Chan (cua rong), Chainwarden Giat Xich.")]
        [Min(0)] public int secondMinorStar = 3;
        [Tooltip(
            "Chieu phu thu BA (bo SSR 3 chieu phu: Hau Nghe, Bach Loc Tien). Hero chi co hai chieu phu bo qua o nay. " +
            "Mo cung luc voi hai chieu phu kia theo khung sao hien hanh.")]
        [Min(0)] public int thirdMinorStar = 3;

        [Header("Noi tai hero khong co bo chieu rieng (mo theo passiveStar, chot 2026-09-29; so CHUA can)")]
        [Tooltip("Loai noi tai — y nghia tung o ben duoi doi theo loai, xem `PassiveKind`.")]
        public PassiveKind passiveKind;
        [Tooltip("Execute/Rescue/BossBane: +ti le (0,5 = +50%). Crit: he so nhan. Shatter: he so don thuong cua vu no.")]
        public float passivePower;
        [Tooltip("Execute/Rescue: nguong mau muc tieu (0,3 = duoi 30%).")]
        public float passiveThreshold;
        [Tooltip("Taunt: ban kinh keo quai. Shatter: ban kinh vu no.")]
        public float passiveRadius;
        [Tooltip("Crit: cu phat thu may. Venom: so tang Doc toi da.")]
        [Min(1)] public int passiveEvery = 1;
        [Tooltip("Venom: tang Doc ton tai bao lau (trung lai thi dem lai).")]
        public float passiveDuration;
        [Tooltip("Spores: bui bao tu quanh chan khi nha. Venom / Ignite: FX Doc / Chay bam than quai (trong = FX chung Resources/StatusLibrary).")]
        public Sprite[] passiveFrames;

        [Header("Mo ta ky nang (man Doi quan)")]
        [Tooltip("\"Ten: mo ta\" cua chieu cuoi. Trong = hero khong co chieu cuoi.")]
        [TextArea(2, 4)] public string ultimateText;
        [Tooltip("Noi tai — moi hero deu co (hero N/R chi thieu chieu phu).")]
        [TextArea(2, 4)] public string passiveText;
        [TextArea(2, 4)] public string minorText;
        [Tooltip("Chieu phu thu hai (Wyrmspear, Chainwarden).")]
        [TextArea(2, 4)] public string secondMinorText;
        [Tooltip("Chieu phu thu ba — chi bo SSR 3 chieu phu (Hau Nghe, Bach Loc Tien).")]
        [TextArea(2, 4)] public string thirdMinorText;
        [Tooltip("Icon canh dong ky nang (UI/icons/icon-skill-*). Trong = de trong o icon.")]
        public Sprite ultimateIcon;
        public Sprite minorIcon;
        [Tooltip("Icon dong noi tai (UI/icons/icon-passive-*).")]
        public Sprite passiveIcon;
        public Sprite secondMinorIcon;
        [Tooltip("Icon chieu phu thu ba (bo SSR 3 chieu phu).")]
        public Sprite thirdMinorIcon;

        [Header("Animation")]
        public SpriteAnimationSet animations;
        [Tooltip("Art ve QUAY PHAI (3 linh trieu hoi). Mac dinh du an la quay trai — sai o nay la unit danh quay lung.")]
        public bool artFacesRight;

        [Header("Chan dung")]
        [Tooltip(
            "Avatar 128x128 o Assets/Art/Generated/UI/avatars/ — dung cho nut chon hero va cho moi " +
            "UI that needs to identify WHO THIS IS. Do not use battle idle sprites: idle faces left in 3/4 view " +
            "va can theo chan, thu nho vao o vuong thi mat quay di cho khac va chan bi cat.")]
        public Sprite avatar;
    }
}
