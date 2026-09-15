using BuildBuild.Combat;
using UnityEngine;

namespace BuildBuild.Data
{
    /// <summary>
    /// So cua bo chieu trieu hoi (Bonecaller, GAME_DESIGN.md muc 3). Moi so ben duoi la gia tri KHOI DIEM
    /// Claude de xuat, CHUA can bang — chinh trong Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "BuildBuild/Summoner Definition", fileName = "Summoner_")]
    public class SummonerDefinition : ScriptableObject
    {
        [Tooltip("Prefab Hero dung chung — linh trieu hoi la Hero voi HeroDefinition rieng.")]
        public Hero unitPrefab;

        [Header("Linh xuong (hai nguon dung chung)")]
        public HeroDefinition skeleton;
        public HeroDefinition bigSkeleton;
        [Tooltip("Toi da bao nhieu linh xuong con song MOI nguon.")]
        public int capPerSource = 6;
        [Tooltip("Xac du dieu kien thu N cua mot nguon (dem ca khi nguon day) la xuong lon, roi bo dem ve 0.")]
        public int bigEvery = 4;

        [Header("Noi tai")]
        [Tooltip("Quai chet trong ban kinh nay quanh phap su moi duoc tinh.")]
        public float raiseRadius = 4f;
        [Range(0f, 1f)] public float raiseChance = 0.35f;
        [Tooltip("Linh cua noi tai song bao lau (giay).")]
        public float passiveLifetime = 10f;

        [Header("Chieu thuong — vong an ky (cooldown an)")]
        public float ringCooldown = 12f;
        [Tooltip("Ban kinh ngang; chieu doc bang mot nua (bau duc 2:1 nhu FrostZone).")]
        public float ringRadius = 1.6f;
        public float ringDamage = 10f;
        public float markDuration = 10f;
        [Tooltip("FX vong lap bao lau (giay) — chi de nhin, an ky ap ngay luc cast.")]
        public float ringFxDuration = 1.5f;
        public Sprite[] ringFrames;
        [Tooltip("FX lap tren dau quai dang mang an ky.")]
        public Sprite[] markFrames;

        [Header("Chieu xin — ky si tu than")]
        public HeroDefinition deathKnight;
        [Tooltip("Linh xuong trong ban kinh nay quanh ky si duoc buff.")]
        public float auraRadius = 4f;
        public float auraDamageBonus = 0.5f;
        public float auraAttackRateBonus = 0.3f;
    }
}
