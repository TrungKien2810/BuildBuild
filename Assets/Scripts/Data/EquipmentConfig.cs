using UnityEngine;

namespace BuildBuild.Data
{
    /// <summary>Them o moi o CUOI enum: save luu o bang so.</summary>
    public enum EquipmentSlot { Weapon, Armor, Helmet, Ring }

    /// <summary>
    /// So cua trang bi (nguoi dung chot 2026-09-28: chi hero, 4 o, CHI cong chi so, do hiem N/R/SR/SSR, co nang cap).
    /// Mot mon = (o, do hiem, level) luu trong save — khong co asset rieng tung mon, moi so nam o day. So la Claude dat
    /// TAM, chua can. Chi so cong theo % tren chi so goc cua hero de hero sat thuong 12 va 40 cung huong nhu nhau.
    /// </summary>
    [CreateAssetMenu(menuName = "BuildBuild/Equipment Config", fileName = "EquipmentConfig")]
    public class EquipmentConfig : ScriptableObject
    {
        [Header("Chi so bac N level 1 (0,06 = +6%)")]
        [Tooltip("Vu khi: + sat thuong (hoac luong hoi cua hero hoi mau).")]
        public float weaponDamage = 0.06f;
        [Tooltip("Giap: + mau toi da.")]
        public float armorHealth = 0.08f;
        [Tooltip("Mu: + mau toi da (it hon giap).")]
        public float helmetHealth = 0.05f;
        [Tooltip("Nhan: + toc danh.")]
        public float ringAttackRate = 0.04f;

        [Header("Do hiem + level")]
        [Tooltip("He so nhan chi so theo do hiem, thu tu N, R, SR, SSR.")]
        public float[] rarityMultiplier = { 1f, 1.8f, 3f, 5f };
        [Tooltip("Moi level them bay nhieu phan chi so level 1 (0,1 = +10%).")]
        public float perLevel = 0.1f;
        public int maxLevel = 10;
        [Tooltip("Gia nang level n -> n+1 = upgradeCostBase x he so do hiem x n.")]
        public int upgradeCostBase = 60;

        [Header("Roi khi thang wave")]
        [Tooltip("Xac suat roi 1 mon o wave thuong.")]
        [Range(0f, 1f)] public float dropChance = 0.35f;
        [Tooltip("Wave chia het cho so nay (wave boss 10/20/30) chac chan roi, it nhat bac R.")]
        public int guaranteedEvery = 10;
        [Tooltip("Ti trong N/R/SR/SSR o wave 1 — noi suy tuyen tinh toi `lateWeights` o `lateWave`. Phai GIAM DAN theo do hiem o ca hai dau (bac cao roi it hon bac thap o moi wave).")]
        public float[] earlyWeights = { 75f, 20f, 5f, 0f };
        public float[] lateWeights = { 50f, 32f, 14f, 4f };
        public int lateWave = 30;

        [Header("Icon (TAM: icon chi so co san, cho art rieng — GD5)")]
        public Sprite weaponIcon;
        public Sprite armorIcon;
        public Sprite helmetIcon;
        public Sprite ringIcon;

        public Sprite Icon(EquipmentSlot slot) => slot switch
        {
            EquipmentSlot.Weapon => weaponIcon,
            EquipmentSlot.Armor => armorIcon,
            EquipmentSlot.Helmet => helmetIcon,
            _ => ringIcon,
        };

        public static string SlotName(EquipmentSlot slot) => slot switch
        {
            EquipmentSlot.Weapon => "Weapon",
            EquipmentSlot.Armor => "Armor",
            EquipmentSlot.Helmet => "Helmet",
            _ => "Ring",
        };

        /// <summary>Phan tram chi so cua mot mon (0,06 = +6%).</summary>
        public float Bonus(EquipmentSlot slot, Rarity rarity, int level)
        {
            float basePercent = slot switch
            {
                EquipmentSlot.Weapon => weaponDamage,
                EquipmentSlot.Armor => armorHealth,
                EquipmentSlot.Helmet => helmetHealth,
                _ => ringAttackRate,
            };
            return basePercent * rarityMultiplier[(int)rarity] * (1f + perLevel * (Mathf.Clamp(level, 1, maxLevel) - 1));
        }

        public int UpgradeCost(Rarity rarity, int level) =>
            Mathf.RoundToInt(upgradeCostBase * rarityMultiplier[(int)rarity] * Mathf.Max(1, level));
    }
}
