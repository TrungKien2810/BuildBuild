using UnityEngine;

namespace BuildBuild.Data
{
    /// <summary>
    /// So cua bo chieu Banneret (Ky Thu cam co, SR buff — nguoi dung chot kit 2026-09-29). `Hero` doc qua component
    /// `Banneret`. So la de xuat, CHUA can bang.
    /// </summary>
    [CreateAssetMenu(menuName = "BuildBuild/Banner Definition", fileName = "Banner_")]
    public class BannerDefinition : ScriptableObject
    {
        [Header("Danh thuong: buff NGAU NHIEN mot dong minh (khong gay sat thuong)")]
        [Tooltip("Moi buff tu don thuong ton tai bao lau (giay). Trung loai thi lam moi, khong cong don.")]
        public float strikeDuration = 4f;
        [Tooltip("Buff +sat thuong (0,25 = +25%).")]
        public float strikeDamageBonus = 0.25f;
        [Tooltip("Buff +toc danh (0,3 = +30%).")]
        public float strikeRateBonus = 0.3f;
        [Tooltip("Buff khien ao: luong chan = sat thuong don thuong cua Banneret x he so.")]
        public float strikeShieldPower = 1.5f;
        [Tooltip("Vo khien lap quanh nguoi duoc che. Chua co art rieng — SceneBuilder muon tam cua Vespera.")]
        public Sprite[] shieldFrames;
        public Sprite[] shieldBreakFrames;
        [Tooltip("Dau buff tren dau dong minh (rally-fx) — dung cho ca don thuong lan chieu cuoi.")]
        public Sprite[] rallyFrames;

        [Header("Noi tai (mo theo passiveStar) Quan Ky")]
        [Tooltip("Dong minh dung trong ban kinh nay quanh Banneret duoc cong toc danh.")]
        public float auraRadius = 2f;
        public float auraRateBonus = 0.15f;

        [Header("Chieu phu (mo theo minorStar) Cam Co")]
        [Tooltip("Cooldown AN (giay). Chi cam khi co dong minh dang danh trong vung.")]
        public float plantCooldown = 10f;
        [Tooltip("Ban kinh vung co quanh cho Banneret dung luc cam.")]
        public float plantRadius = 1.8f;
        public float plantDuration = 5f;
        [Tooltip("Dong minh trong vung nhan it sat thuong hon (0,25 = -25%), tinh sau giap.")]
        public float plantDamageReduction = 0.25f;
        [Tooltip("Clip than cam co. Ten phai la \"skill\" de `Hero` mo khoa khi het clip.")]
        public SpriteAnimationClip plantBody;
        [Tooltip("Vung co tren mat dat (zone-fx).")]
        public Sprite[] zoneFrames;

        [Header("Chieu cuoi Hieu Lenh Xung Phong (thanh mana cua Hero)")]
        [Tooltip("Ca doi tren san +sat thuong (0,3 = +30%).")]
        public float rallyDamageBonus = 0.3f;
        public float rallyDuration = 6f;
    }
}
