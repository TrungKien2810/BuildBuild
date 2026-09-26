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
    }

    /// <summary>
    /// Chi so chien dau cua mot hero dat xuong san. So goc lay tu GAME_DESIGN.md muc 3 — la gia tri
    /// KHOI DIEM de can bang, nen de o asset chu khong hard-code trong script.
    /// </summary>
    [CreateAssetMenu(menuName = "BuildBuild/Hero Definition", fileName = "Hero_")]
    public class HeroDefinition : ScriptableObject
    {
        public string displayName = "Frostweaver";

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
        [Tooltip("Don thuong day lui moi quai trung, ke ca quai dang vung attack (ky si tu than).")]
        public bool knockbackOnHit;

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
        [Tooltip("FrostZone: ban kinh vung. ConeSweep: chieu dai nhat chem.")]
        public float skillRadius = 1.6f;
        [Tooltip("FrostZone: vung ton tai bao lau. Petrify: bat dong bao lau. RapidFire: buff bao lau.")]
        public float skillDuration = 3f;
        [Tooltip("ConeSweep: he so x damage. RapidFire: he so x toc danh. Petrify: ti le sat thuong phan lai (0,2 = 20%).")]
        public float skillPower = 1f;
        [Tooltip("FrostZone: frame vung bang (trong = placeholder). ConeSweep: frame vet chem. Petrify: vo da LAP trong luc hoa da.")]
        public Sprite[] skillFrames;
        [Tooltip("Petrify: vo da moc len, chay MOT lan truoc khi lap skillFrames.")]
        public Sprite[] skillIntroFrames;
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

        [Header("Animation")]
        public SpriteAnimationSet animations;

        [Header("Chan dung")]
        [Tooltip(
            "Avatar 128x128 o Assets/Art/Generated/UI/avatars/ — dung cho nut chon hero va cho moi " +
            "cho UI can chi ra ĐAY LA AI. Khong dung sprite idle cua tran dau: idle ve 3/4 quay trai " +
            "va can theo chan, thu nho vao o vuong thi mat quay di cho khac va chan bi cat.")]
        public Sprite avatar;
    }
}
