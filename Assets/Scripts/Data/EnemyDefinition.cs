using UnityEngine;

namespace BuildBuild.Data
{
    public enum EnemyKind { Thuong, Nhanh, Tank, Boss }

    /// <summary>
    /// Chi so goc cua mot loai quai. So lay tu GAME_DESIGN.md muc 5 — day la gia tri KHOI DIEM de
    /// can bang, nen de o asset chu khong hard-code trong script.
    ///
    /// Thuong  Goblin   100 / 15 / 1.5 / 5
    /// Nhanh   Ashfang   60 / 10 / 3.0 / 4     (cho soi 4 chan, thay Elf — xem AD-6)
    /// Tank    Orc      400 / 40 / 0.8 / 12
    /// Boss             5000 /120 / 0.6 / 200
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

        [Header("Animation")]
        public SpriteAnimationSet animations;
    }
}
