using UnityEngine;

namespace BuildBuild.Data
{
    /// <summary>
    /// Tai nguyen trieu hoi linh trong tran (GAME_DESIGN.md muc 2, IDEAS #1): tich theo THOI GIAN va theo SO QUAI BI
    /// GIET, co tran. Ten tai nguyen, toc tich va co giu qua wave khong deu CHUA CHOT (plan muc 6, cau 1) — so o day
    /// la so tam cua plan: +1/giay, +2/kill, tran 100, het wave ve 0.
    /// </summary>
    [CreateAssetMenu(menuName = "BuildBuild/Summon Config", fileName = "SummonConfig")]
    public class SummonConfig : ScriptableObject
    {
        [Header("Tai nguyen")]
        [Tooltip("Cong moi giay trong luc dang danh wave.")]
        public float resourcePerSecond = 1f;
        [Tooltip("Cong moi con quai bi giet (tinh luc het mau, khong cho animation death).")]
        public float resourcePerKill = 2f;
        public float resourceCap = 100f;
        [Tooltip("Het wave co giu tai nguyen sang wave sau khong. CHUA CHOT — tam false (ve 0).")]
        public bool keepBetweenWaves;

        [Header("Tha top")]
        [Tooltip("Khoang cach theo truc y (tren man) giua hai con trong mot top — lech nhau de khong dung chong.")]
        public float squadSpacing = 0.4f;
        [Tooltip("FX bung len duoi chan moi con vua ra san. Trong = ve placeholder bang code (SummonBurst).")]
        public Sprite[] spawnFxFrames;
    }
}
