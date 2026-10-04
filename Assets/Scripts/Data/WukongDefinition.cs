using UnityEngine;

namespace BuildBuild.Data
{
    /// <summary>
    /// Bo chieu Ton Ngo Khong (`Wukong`, SSR, GAME_DESIGN.md muc 3, chot 2026-09-30). Chieu xin Nhu Y Thien Tru chay
    /// bang thanh mana cua `HeroDefinition` (SkillKind.GiantStaff: `skillRadius` = dai vet gay, `skillPower` = he so
    /// sat thuong, `skillDuration` = choang). Moi so la gia tri KHOI DIEM Claude de xuat, CHUA can bang.
    /// </summary>
    [CreateAssetMenu(menuName = "BuildBuild/Wukong Definition", fileName = "Wukong_")]
    public class WukongDefinition : ScriptableObject
    {
        [Header("Noi tai — The Than + Long Ve Than (mo theo passiveStar)")]
        [Tooltip("Phan than len lam ban the khi ban the het mau (moi wave mot lan): mau = ti le nay x mau toi da.")]
        [Range(0f, 1f)] public float substituteHealth = 0.4f;
        [Tooltip("Moi phan than tan (het gio / bi ha) -> ban the hoi ti le nay x mau toi da.")]
        [Range(0f, 1f)] public float cloneReturnHeal = 0.08f;
        [Tooltip("Hut mau tren sat thuong gay ra.")]
        [Range(0f, 1f)] public float lifesteal = 0.1f;

        [Header("Nho Long Phan Than (chieu phu, minorStar)")]
        public float cloneCooldown = 10f;
        [Min(1)] public int cloneCount = 2;
        public float cloneLifetime = 8f;
        [Tooltip("Hero cua phan than (40% cong/mau ban the, khong co ky nang). Nam o Summons/ de khong vao bang chon.")]
        public HeroDefinition cloneDefinition;
        [Tooltip("Prefab Hero chung (nhu linh xuong).")]
        public Combat.Hero unitPrefab;
        [Tooltip("Mau phu len phan than — nhat + hoi trong de phan biet ban the.")]
        public Color cloneTint = new Color(1f, 0.85f, 0.7f, 0.7f);
        [Tooltip("Clip than nho long thoi (ten \"skill\" de `Hero` mo khoa). hitFrame = o thoi ra phan than.")]
        public SpriteAnimationClip cloneBody = new SpriteAnimationClip { name = "skill", loop = false };
        [Tooltip("Khoi long: phan than xuat hien / tan, The Than.")]
        public Sprite[] poofFrames;

        [Header("Toan Phong Con (chieu phu 2, secondMinorStar)")]
        public float spinCooldown = 7f;
        public float spinDuration = 3f;
        public float spinRadius = 1.2f;
        [Tooltip("Sat thuong moi nhip 0,5s = don thuong x he so nay.")]
        public float spinPower = 0.5f;
        [Tooltip("Moi quai trung moi nhip (va moi dan bat trung quai) hoi ti le nay x mau toi da.")]
        public float spinHealPerHit = 0.03f;
        [Tooltip("Tran hoi moi lan xoay (ti le mau toi da).")]
        public float spinHealCap = 0.25f;
        [Tooltip("Clip than xoay gay (ten \"skill\"). O 3-6 lap trong luc xoay.")]
        public SpriteAnimationClip spinBody = new SpriteAnimationClip { name = "skill", loop = false };
        public Sprite[] spinRingFrames;
        [Tooltip("Tia loe khi chan va bat dan.")]
        public Sprite[] deflectFrames;

        [Header("Nhu Y Thien Tru (chieu xin)")]
        [Tooltip("Tam tim cum quai dong nhat de dap gay.")]
        public float ultimateRange = 6f;
        [Tooltip("Nua be rong vet gay (theo truc y, tren mat dat).")]
        public float staffHalfWidth = 0.5f;
        [Tooltip("Vet nut lam cham: ti le / so giay.")]
        [Range(0f, 1f)] public float crackSlow = 0.4f;
        public float crackDuration = 3f;
        [Tooltip("Moi phan than dang song dap gay nho: sat thuong = don cua ban the x he so nay.")]
        public float cloneSmashPower = 0.3f;
        [Tooltip("Gay khong lo roi -> cham dat -> vet nut (o cuoi GIU trong luc lam cham).")]
        public Sprite[] giantStaffFrames;
        [Tooltip("FX hoi mau (dung lai cua Vespera).")]
        public Sprite[] healFrames;
    }
}
