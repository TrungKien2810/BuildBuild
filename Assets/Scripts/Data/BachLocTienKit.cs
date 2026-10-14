using UnityEngine;

namespace BuildBuild.Data
{
    /// <summary>
    /// Bo Bach Loc Tien (SSR Hoi mau, kit V2 duyet 2026-10-06 — so CHUA can):
    ///   - Loc Tuc (hanh dong thuong, `HeroDefinition.healsAllies`): hoi `basicHealPower` ATK cho dong minh bi thuong
    ///     thap ti le mau nhat trong tam (gom chinh minh); khong co ai bi thuong thi khong tung.
    ///   - Linh Mach Rung Thieng (noi tai, 1 sao): luong mau THUC hoi duoc nap Sinh Khi, co tran; day Sinh Khi thi
    ///     chieu phu KE TIEP co mot nhp Linh Loc tro giup rieng roi tieu thanh. Nhp tro giup khong tu nap lai Sinh Khi.
    ///   - Tam Mam Hoi Xuan (phu 1): trong toi da `mamMax` mam duoi chan cac dong minh bi thuong thap nhat trong tam;
    ///     mam hoi theo nhp, truong thanh sau `mamMaturePulses` nhp; mam moi thay mam cu nhat.
    ///   - Hoan Diep Tuc Sinh (phu 2): co mam TRUONG THANH va co dong minh nguy cap trong tam -> thu hoach mam do de
    ///     hoi manh tuc thoi (mam nguon bien mat).
    ///   - Loc Anh Tuan Lam (phu 3): co it nhat 2 mam -> bong loc chay tu mam cu nhat toi mam moi nhat, hoi mot nhp cho
    ///     dong minh tren duong va go MOT Doc/Chay moi nguoi (uu tien Doc).
    ///   - Bach Loc Hoi Xuan (chieu cuoi, mana): bao truoc roi bong loc buoc doc lane, de lai dai hoa co dinh hoi
    ///     `laneHealPerSecond` ATK moi giay cho hero/linh dang song trong dai.
    /// So nam o day; moi thong so deu la so TAM cua Claude (nguoi dung se can lai sau khi thu Play).
    /// </summary>
    public class BachLocTienKit : ScriptableObject
    {
        [Header("Loc Tuc (hanh dong thuong)")]
        [Tooltip("Luong hoi mot lan = he so nay x ATK (0,7 = 70%).")]
        public float basicHealPower = 0.7f;

        [Header("Linh Mach Rung Thieng (noi tai, 1 sao)")]
        [Tooltip("Tran Sinh Khi (don vi = mau THUC hoi duoc, chua nhan he so nao).")]
        public float spiritMax = 30f;

        [Header("Tam Mam Hoi Xuan (phu 1, 3 sao)")]
        public float mamCooldown = 11f;
        [Tooltip("So mam toi da tren san; mam moi thay mam cu nhat.")]
        public int mamMax = 3;
        [Tooltip("Nua be ngang vung hoi cua mot mam (world X). Mam hoi theo COT phu HET be sau san (11 don " +
            "vi mat dat — moi hang hero di qua deu an, 2026-10-10: 0,9 -> 1,1), nen so nay chi con la be ngang.")]
        public float mamRadius = 1.1f;
        [Tooltip("Mam song bao lau ke tu khi trong.")]
        public float mamSeconds = 8f;
        [Tooltip("Nhip hoi cua mam (giay).")]
        public float mamPulseSeconds = 1f;
        [Tooltip("Hoi moi nhp = he so nay x ATK.")]
        public float mamPulsePower = 0.25f;
        [Tooltip("So nhp de mam non truong thanh (dung duoc cho Hoan Diep Tuc Sinh / Loc Anh).")]
        public int mamMaturePulses = 3;

        [Header("Hoan Diep Tuc Sinh (phu 2, 3 sao)")]
        public float diepCooldown = 9f;
        [Tooltip("Dong minh duoi ti le mau nay trong tam la 'nguy cap'.")]
        public float urgentFraction = 0.4f;
        [Tooltip("Hoi cuu nguy = he so nay x ATK.")]
        public float diepHealPower = 1.6f;

        [Header("Loc Anh Tuan Lam (phu 3, 3 sao)")]
        public float locAnhCooldown = 13f;
        [Tooltip("So mam toi thieu de dung duoc chieu (kit: it nhat 2 mam).")]
        public int locAnhMinSprouts = 2;
        [Tooltip("Hoi cho moi dong minh tren duong = he so nay x ATK.")]
        public float locAnhHealPower = 0.3f;
        [Tooltip("Nua be ngang duong loc chay (world X) — duong phu HET be sau san nen so nay chi con be ngang.")]
        public float locAnhHalfWidth = 0.5f;

        [Header("Bach Loc Hoi Xuan (chieu cuoi, mana)")]
        [Tooltip(
            "Thoi gian GIU TRUONG sau frame cham cua clip cast. Tong bao truoc = (hitFrame-1)/fps + so nay " +
            "(kit ghi 0,7 giay -> clip 4 frame @130 ms = 0,39 + 0,2).")]
        public float laneHoldSeconds = 0.2f;
        [Tooltip("Chieu dai dai hoa (world X) — dai phu HET be sau san (moi hang hero), nen day chi con chieu " +
            "dai: 2026-10-10 nguoi dung chot 'mo rong ca chieu doc lan chieu ngang' -> 6 -> 8.")]
        public float laneLength = 8f;
        [Tooltip("Dai hoa ton tai bao lau (kit: 5 giay).")]
        public float laneSeconds = 5f;
        [Tooltip("Hoi moi giay cho nguoi trong dai = he so nay x ATK.")]
        public float laneHealPerSecond = 0.7f;
        [Tooltip("Dang co nhp Linh Loc tro giup: them MOT nhp hoi nua khi dai bat dau.")]
        public float laneSupportPower = 0.7f;

        [Header("Clip than")]
        public SpriteAnimationClip mamCast = new SpriteAnimationClip { name = "skill", loop = false };
        public SpriteAnimationClip diepCast = new SpriteAnimationClip { name = "skill", loop = false };
        public SpriteAnimationClip locAnhCast = new SpriteAnimationClip { name = "skill", loop = false };
        public SpriteAnimationClip hoiXuanCast = new SpriteAnimationClip { name = "skill", loop = false };

        [Header("FX")]
        [Tooltip("Hat sang xanh bay tu truong toi nguoi duoc hoi (don thuong).")]
        public Sprite[] bolt;
        [Tooltip("Nhip hoi no tren nguoi nhan (kiem luon khi giai duoc Doc/Chay).")]
        public Sprite[] bloom;
        [Tooltip("Mam: 2 frame dau mam non, 2 frame sau truong thanh (dung lam intro), roi lap frame truong thanh.")]
        public Sprite[] mamGround;
        [Tooltip("Bong loc mang mam non ve cho nguoi duoc cuu (Hoan Diep).")]
        public Sprite[] transfer;
        [Tooltip("Duong loc chay (bao truoc + dang chay).")]
        public Sprite[] path;
        [Tooltip("Bong bach loc buoc (lap).")]
        public Sprite[] deer;
        [Tooltip("Nhanh goc quanh chan truong khi Sinh Khi day.")]
        public Sprite[] spirit;
        [Tooltip("Dai hoa cua chieu cuoi.")]
        public Sprite[] lane;
    }
}
