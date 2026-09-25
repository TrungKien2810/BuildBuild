using UnityEngine;

namespace BuildBuild.Data
{
    /// <summary>
    /// Ba co che cua boss (GAME_DESIGN.md muc 5). Giap -30% nam o `EnemyDefinition.damageReduction`.
    /// Ban kinh Chan Dia GDD khong ghi — so Claude de xuat, CHUA can bang.
    /// </summary>
    [CreateAssetMenu(menuName = "BuildBuild/Boss Definition", fileName = "Boss_")]
    public class BossDefinition : ScriptableObject
    {
        [Header("Chan Dia — dap dat")]
        public float slamInterval = 15f;
        public float slamDamage = 80f;
        [Tooltip("Hero / linh phe ta trong ban kinh nay (do tren mat dat) an don va bi choang.")]
        public float slamRadius = 2f;
        public float slamStun = 2f;
        [Tooltip("Vet nut / bui duoi chan boss luc dap.")]
        public Sprite[] slamFrames;

        [Header("Tieng Thet — buff toc chay moi quai")]
        public float roarInterval = 20f;
        [Tooltip("0,3 = +30% toc chay.")]
        public float roarHaste = 0.3f;
        public float roarDuration = 5f;
        [Tooltip("Song am toa ra quanh boss.")]
        public Sprite[] roarFrames;
        [Tooltip("FX lap tren dau moi quai dang duoc buff.")]
        public Sprite[] roarBuffFrames;

        [Header("Noi Gian — duoi nguong mau")]
        [Range(0f, 1f)] public float enrageBelow = 0.3f;
        [Tooltip("0,5 = +50% toc danh. Noi gian thi mien lam cham.")]
        public float enrageAttackRateBonus = 0.5f;
    }
}
