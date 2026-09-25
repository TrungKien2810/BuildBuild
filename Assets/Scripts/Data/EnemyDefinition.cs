using UnityEngine;

namespace BuildBuild.Data
{
    // Them vao CUOI: kind luu trong asset bang so, chen giua la doi loai cua asset cu.
    public enum EnemyKind { Thuong, Nhanh, Tank, Boss, TinhAnh, BanXa }

    /// <summary>
    /// Chi so goc cua mot loai quai. So lay tu GAME_DESIGN.md muc 5 — day la gia tri KHOI DIEM de
    /// can bang, nen de o asset chu khong hard-code trong script.
    ///
    /// Thuong  Goblin   100 / 15 / 1.5 / 5
    /// Nhanh   Ashfang   60 / 10 / 3.0 / 4     (cho soi 4 chan, thay Elf — xem AD-6)
    /// Tank    Orc      400 / 40 / 0.8 / 12
    /// Boss             5000 /120 / 0.6 / 200
    /// TinhAnh Gatebreaker 1200 / 60 / 0.7 / 40   giap -15%
    /// BanXa   GoblinSlinger 80 / 12 / 1.3 / 6    tam 4 (so tam, GDD chua chot)
    /// </summary>
    [CreateAssetMenu(menuName = "BuildBuild/Enemy Definition", fileName = "Enemy_")]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Dinh danh")]
        public string displayName = "Goblin";
        public EnemyKind kind = EnemyKind.Thuong;

        [Header("Chi so goc (wave 1)")]
        public float baseHealth = 100f;
        public float baseDamage = 15f;
        [Tooltip("Don vi the gioi moi giay. Goblin 1.5, Ashfang 3.0, Orc 0.8.")]
        public float moveSpeed = 1.5f;
        [Tooltip("Vang roi ra khi bi diet.")]
        public int goldReward = 5;

        [Header("Tam va nhip danh")]
        [Tooltip("Khoang cach toi muc tieu du de bat dau danh (don vi the gioi).")]
        public float attackRange = 0.6f;
        [Tooltip("So don moi giay.")]
        public float attackRate = 1f;
        [Tooltip(
            "Ban kinh quai NHIN THAY hero va re sang danh hero thay vi di tiep ve lau dai. " +
            "Lon hon attackRange mot chut: quai phai chiu di chech duong de tom hero dung gan duong " +
            "di, nhung khong duoc keo ca doi hinh ve mot hero o cuoi san.")]
        public float heroAggroRange = 2f;

        [Header("Phong thu")]
        [Tooltip("0..1 — phan sat thuong bi giap an mat. Gatebreaker 0,15, boss 0,3.")]
        [Range(0f, 1f)] public float damageReduction;

        [Header("Ban xa (bo trong = can chien)")]
        [Tooltip(
            "Vien da CHI de nhin (EnemyExtras tha o frame roi tay). Luat ban xa nam o so: attackRange lon (Goblin Na 4) " +
            "de dung xa ma danh, heroAggroRange >= attackRange de hero / linh trong tam duoc chon truoc tuong.")]
        public Sprite[] projectileFrames;
        public Sprite[] impactFrames;
        public float projectileSpeed = 7f;
        [Tooltip("Scale sprite dan (nhan them UnitScale).")]
        public float projectileScale = 0.5f;

        public bool IsRanged => projectileFrames != null && projectileFrames.Length > 0;

        [Header("Boss (bo trong = khong co co che boss)")]
        public BossDefinition boss;

        [Header("Animation")]
        public SpriteAnimationSet animations;
    }
}
