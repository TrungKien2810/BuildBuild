using UnityEngine;

namespace BuildBuild.Data
{
    /// <summary>
    /// So cua bo chieu Chainwarden (Cai nguc moc xich, SR — GAME_DESIGN.md muc 3, chot 2026-09-24). Noi tai Cai Nguc
    /// (giu 4 quai + Xich) nam o `HeroDefinition.maxHeld` / `heldDamageBonus`; lop nay giu hai chieu con lai.
    /// Moi so la gia tri KHOI DIEM Claude de xuat, CHUA can bang — chinh trong Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "BuildBuild/Warden Definition", fileName = "Warden_")]
    public class WardenDefinition : ScriptableObject
    {
        [Header("Chieu thuong — Quat Xich (cooldown an)")]
        public float sweepCooldown = 10f;
        [Tooltip("Ban kinh tren MAT DAT tinh tu chan Chainwarden toi mep than quai.")]
        public float sweepRadius = 1f;
        [Tooltip("Sat thuong THAP — tac dung chinh la troi.")]
        public float sweepDamage = 25f;
        [Tooltip("Troi: quai dung im nhung van danh. Tinh anh/boss bi troi mot nua.")]
        public float rootDuration = 1.5f;
        [Tooltip("FX quay xich quanh minh. Trong = khong co FX (art chua co).")]
        public Sprite[] sweepFrames;
        [Tooltip("Clip than Quat Xich dang thuong (quay xich tren tay), chay cung luc voi FX vong. Trong = chi co FX.")]
        public SpriteAnimationClip sweepBody = new SpriteAnimationClip { name = "skill", loop = false };
        [Tooltip("Vong xich troi quanh chan quai trong luc bi troi (Quat Xich / Loc Xich).")]
        public Sprite[] rootFrames;
        [Tooltip("O khoa tren dau quai dang mang Xich (bi Chainwarden giu hoac trung Nem Moc).")]
        public Sprite[] chainIconFrames;

        [Header("Chieu lon — Nem Moc mo dau Cuong No (thanh mana cua Hero, SkillKind.HookThrow)")]
        [Tooltip("Tam nem (tren man, tinh tu chan Chainwarden).")]
        public float hookRange = 6f;
        [Tooltip("Giay keo quai tu cho no ve sat Chainwarden.")]
        public float hookPullTime = 0.3f;
        [Tooltip("Choang sau khi bi keo. Boss bi choang mot nua.")]
        public float hookStun = 0.8f;
        [Tooltip("Xich tu moc: quai bi keo nhan them sat thuong nay trong `hookChainDuration` giay, ke ca khi khong bi giu.")]
        public float hookChainBonus = 0.2f;
        public float hookChainDuration = 4f;
        [Tooltip("Dau moc BAY tu tay toi quai (art `hook-fx`, quay trai). Trong thi khong co hinh moc/xich.")]
        public Sprite[] hookFrames;
        [Tooltip("Dau moc NGOAM, bam tren quai trong luc keo. Trong thi dung frame bay cuoi.")]
        public Sprite hookBite;
        [Tooltip("Mot doan xich lap, rai tu tay toi moc.")]
        public Sprite chainLink;
        [Tooltip("Tay cam cuoi xich trong clip than Nem Moc: toa do LOCAL cua sprite (art quay trai, chua nhan UnitScale). " +
            "`chain_end` o 3-5 cua `skill` ~ (23, 57) trong o 128, pivot (64, day 90), 100 PPU.")]
        public Vector2 hookHand = new Vector2(-0.41f, 0.33f);
        [Tooltip("Sao choang lap tren dau quai bi keo (dung chung Enemy/stunned-status).")]
        public Sprite[] stunFrames;

        [Header("Cuong No — bien hinh VINH VIEN (toi khi chet) ngay sau Nem Moc (2 xich dai hai ben)")]
        [Tooltip("He so sat thuong don thuong + Loc Xich.")]
        public float rageDamageMultiplier = 1.5f;
        [Tooltip("He so toc danh.")]
        public float rageRateMultiplier = 1.3f;
        [Tooltip("Tam danh can chien khi Cuong No (thay `HeroDefinition.range`), vua hai xich dai. Moi don danh trung " +
                 "them con gan nhat o BEN KIA trong tam nay.")]
        public float rageRange = 2f;
        [Tooltip("Giam sat thuong nhan vao (0,2 = -20%), dat vao `Health.Armor` cua hero trong luc Cuong No.")]
        [Range(0f, 0.9f)] public float rageDamageReduction = 0.2f;
        [Tooltip("Hut mau: hoi lai phan nay cua mau THAT da tru o quai (0,25 = 25%).")]
        [Range(0f, 1f)] public float rageLifesteal = 0.25f;
        [Tooltip("Bo animation dang Cuong No (rage-idle/walk/attack/hurt; death = death thuong; skill = Loc Xich). " +
                 "Trong = phong to + nhuom sprite thuong (`rageScale`, `rageTint`).")]
        public SpriteAnimationSet rageAnimations;
        [Tooltip("Clip bien hinh phat ngay luc vao Cuong No. Ten phai la \"skill\" de `Hero` mo khoa khi het clip.")]
        public SpriteAnimationClip rageTransform = new SpriteAnimationClip { name = "skill", loop = false };
        [Tooltip("Chi dung khi `rageAnimations` trong: phong to sprite thuong.")]
        public float rageScale = 1.15f;
        [Tooltip("Chi dung khi `rageAnimations` trong: nhuom sprite thuong.")]
        public Color rageTint = new Color(1f, 0.6f, 0.55f);

        [Header("Loc Xich — chieu thuong thay Quat Xich trong luc Cuong No")]
        public float stormCooldown = 4f;
        public float stormRadius = 2f;
        [Tooltip("He so x sat thuong don thuong (da nhan Cuong No).")]
        public float stormPower = 2f;
        public float stormRootDuration = 1f;
        [Tooltip("FX quay hai xich 360 do. Trong = dung `sweepFrames` (bo qua khi than da co clip `rageAnimations.skill`).")]
        public Sprite[] stormFrames;

        [Header("Chieu phu 2 — Giat Xich (cooldown an, mo theo `HeroDefinition.secondMinorStar`, chot 2026-09-29)")]
        public float yankCooldown = 8f;
        [Tooltip("Chi giat khi dang giu it nhat bay nhieu quai (Cai Nguc).")]
        [Min(1)] public int yankMinHeld = 2;
        [Tooltip("Sat thuong moi quai = don thuong (da nhan Cuong No) x (yankPower + yankPowerPerHeld x so quai bi giat).")]
        public float yankPower = 1f;
        public float yankPowerPerHeld = 0.5f;
        [Tooltip("Choang sau cu giat. Tinh anh/boss bi choang mot nua.")]
        public float yankStun = 1f;
        [Tooltip("Cuong No: keo them moi quai CHUA bi giu trong `rageRange` ve sat minh truoc khi dap.")]
        public float yankPullTime = 0.2f;
        [Tooltip("Clip than dang Cuong No (ten \"skill\" de `Hero` mo khoa; hitFrame = o giat). Dang thuong / trong = chi FX.")]
        public SpriteAnimationClip yankBody = new SpriteAnimationClip { name = "skill", loop = false };
        [Tooltip("FX va dap dat tren TUNG quai bi giat.")]
        public Sprite[] yankFrames;
    }
}
