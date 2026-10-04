using UnityEngine;

namespace BuildBuild.Data
{
    /// <summary>
    /// Chi so mot loai phao lap tren be phao dai (GAME_DESIGN.md muc 4). So la gia tri KHOI DIEM de can bang,
    /// de o asset chu khong hard-code trong script.
    ///
    ///   Lien Thanh  12 / 2,0/s / tam 8   don muc tieu
    ///   No          30 / 0,5/s / tam 7   AOE ban kinh 2 (moi quai trong vung no an don — chot 2026-09-24)
    ///   Bang         8 / 1,0/s / tam 7   lam cham 40% trong 3s
    ///   No Xuyen    22 / 0,6/s / tam 9   lao xuyen toi da 3 quai (so Claude de xuat 2026-10-01, CHUA can)
    ///
    /// Phao khong co mau, khong bi danh. Level lay tu save (`OwnedUnit.level` cua `unitId`); nang level theo
    /// GDD muc 7: gia floor(50 x n^1,5), +8% sat thuong goc moi level (cong tuyen tinh), toi da level 20.
    /// </summary>
    [CreateAssetMenu(menuName = "BuildBuild/Cannon Definition", fileName = "Cannon_")]
    public class CannonDefinition : ScriptableObject
    {
        [Header("Dinh danh")]
        public string displayName = "Pháo Liên Thanh";
        [Tooltip("Trung `UnitDefinition.id` trong gacha — so huu va level doc tu save theo id nay.")]
        public string unitId = "cannon_rapid";
        public Sprite icon;

        [Header("Chi so goc (GAME_DESIGN.md muc 4, level 1)")]
        public float damage = 12f;
        [Tooltip("So phat moi giay.")]
        public float fireRate = 2f;
        [Tooltip("Tam ban (don vi the gioi), do thang tu nong phao toi chan quai.")]
        public float range = 8f;

        [Header("Dac diem")]
        [Tooltip("0 = don muc tieu. > 0 = no: moi quai co than cham vong nay quanh chan muc tieu (do tren mat dat) deu an don.")]
        public float aoeRadius;
        [Tooltip("0..1 — 0,4 = cham 40%. 0 = khong lam cham.")]
        [Range(0f, 1f)] public float slowPercent;
        [Tooltip("Giay lam cham moi lan trung.")]
        public float slowDuration;
        [Tooltip("Icon LAP tren dau quai dang bi lam cham (bong tuyet). Trong = chi phu mau xanh.")]
        public Sprite[] slowIconFrames;
        [Tooltip("0 = dan dung lai o muc tieu. > 0 = lao XUYEN: bay thang qua muc tieu, moi quai tren duong an don mot lan, " +
            "toi da chung nay con roi tan (No Xuyen).")]
        public int pierceCount;

        [Header("Nang level (GAME_DESIGN.md muc 7)")]
        [Tooltip("Sat thuong cong them moi level, tinh tren sat thuong goc (0,08 = +8%).")]
        public float damagePerLevel = 0.08f;
        [Tooltip("Gia nang level n -> n+1 = floor(upgradeCostBase x n^upgradeCostExponent).")]
        public float upgradeCostBase = 50f;
        public float upgradeCostExponent = 1.5f;

        [Header("Dan")]
        public float projectileSpeed = 14f;
        public Sprite[] projectileFrames;
        [Tooltip("Scale sprite dan LUC BAY (nhan them UnitScale).")]
        public float projectileScale = 0.5f;
        public Sprite[] impactFrames;
        [Tooltip("Scale frame NO (nhan them UnitScale). Phao No to hon de doc ra vung AOE.")]
        public float impactScale = 1f;

        [Header("Am thanh (ten file trong Resources/Audio/SFX, rong = im)")]
        public string fireSfx = "cannon-fire";
        public string impactSfx;

        [Header("Hien thi")]
        public SpriteAnimationSet animations;
        [Tooltip("Scale sprite phao tren be. Art 128 px, be phao dai ~80 px sau khi thu nho phao dai.")]
        public float displayScale = 1.4f;
        [Tooltip("Cho dan xuat hien, tinh tu chan (pivot) o co art 1x (art-px / 100), phia nong (ben trai art). " +
            "Nhan displayScale khi dung.")]
        public Vector2 muzzle = new Vector2(-0.3f, 0.3f);
        [Tooltip(
            "Mau phu len than phao + dan + no. Trang = art that (ca 3 phao da co art rieng).")]
        public Color tint = Color.white;

        /// <summary>Tran tren theo sao nam o `Progression.MaxLevel`; o day chi chan duoi.</summary>
        public int ClampLevel(int level) => Mathf.Max(1, level);

        /// <summary>Sat thuong o level <paramref name="level"/>: goc x (1 + damagePerLevel x (level - 1)).</summary>
        public float DamageAt(int level) => damage * (1f + damagePerLevel * (ClampLevel(level) - 1));

        /// <summary>Gia nang tu <paramref name="level"/> len level ke tiep.</summary>
        public int UpgradeCost(int level) => Mathf.FloorToInt(upgradeCostBase * Mathf.Pow(ClampLevel(level), upgradeCostExponent));
    }
}
