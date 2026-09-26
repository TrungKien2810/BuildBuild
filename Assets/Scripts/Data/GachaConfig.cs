using UnityEngine;

namespace BuildBuild.Data
{
    /// <summary>So lieu gacha theo GAME_DESIGN.md muc 6. Doi so o day, khong doi trong code.</summary>
    [CreateAssetMenu(menuName = "BuildBuild/Gacha Config", fileName = "GachaConfig")]
    public class GachaConfig : ScriptableObject
    {
        [Header("Ti le do hiem (%)")]
        public float rateN = 60f;
        public float rateR = 30f;
        public float rateSR = 9f;
        public float rateSSR = 1f;

        [Tooltip("Sau khi ra do hiem: xac suat ra hero (con lai la linh + phao).")]
        [Range(0f, 1f)] public float heroShare = 0.6f;
        [Tooltip(
            "Sau khi ra do hiem: xac suat ra LINH TRIEU HOI; phan con lai sau hero + linh la phao. GDD muc 6 moi ghi " +
            "60% hero / 40% phao — phan cua linh CHUA CHOT, tam chia doi 40% cua phao.")]
        [Range(0f, 1f)] public float soldierShare = 0.2f;

        [Header("Gia")]
        public int singleCost = 500;
        public int tenCost = 4500;

        [Header("Pity")]
        [Tooltip("So luot lien tiep khong SR+ truoc khi luot ke tiep chac chan SR.")]
        public int softPity = 30;
        [Tooltip("So luot lien tiep khong SSR truoc khi luot ke tiep chac chan SSR.")]
        public int hardPity = 80;

        [Header("Trung lap")]
        public int shardsPerDuplicate = 20;
        public int shardsPerStar = 100;

        [Header("Pool")]
        public UnitDefinition[] pool;

        /// <summary>`value01` trong [0,1] -> do hiem theo bang ti le.</summary>
        public Rarity RollRarity(float value01)
        {
            float x = value01 * (rateN + rateR + rateSR + rateSSR);
            if ((x -= rateSSR) < 0f) return Rarity.SSR;
            if ((x -= rateSR) < 0f) return Rarity.SR;
            if ((x -= rateR) < 0f) return Rarity.R;
            return Rarity.N;
        }
    }
}
