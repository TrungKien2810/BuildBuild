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
        [Header("Don thuong — quet vung")]
        [Tooltip("Frame cham cua don thuong: MOI hero / linh trong ban kinh nay (do tu chan boss toi mep than, tren mat dat) an du sat thuong, khong chi muc tieu.")]
        public float cleaveRadius = 1.5f;

        [Header("Chan Dia — dap dat")]
        public float slamInterval = 15f;
        public float slamDamage = 80f;
        [Tooltip("Hero / linh phe ta trong ban kinh nay quanh DAU BUA (do tren mat dat) an don va bi choang.")]
        public float slamRadius = 2f;
        [Tooltip("Khoang tu chan boss toi dau bua o frame cham dat cua `slamBody`, phia boss dang nhin (don vi the gioi). " +
            "Art slam-5: ~112 art-px truoc pivot x UnitScale 2 / 100 PPU.")]
        public float slamReach = 2.24f;
        public float slamStun = 2f;
        [Tooltip("Vet nut / bui tai dau bua luc dap.")]
        public Sprite[] slamFrames;
        [Tooltip("Clip THAN boss luc dap. hitFrame = frame chan cham dat — sat thuong + choang + vet nut ra o frame do.")]
        public SpriteAnimationClip slamBody = new SpriteAnimationClip { name = "slam", loop = false };

        [Header("Tieng Thet — buff toc chay moi quai")]
        public float roarInterval = 20f;
        [Tooltip("0,3 = +30% toc chay.")]
        public float roarHaste = 0.3f;
        public float roarDuration = 5f;
        [Tooltip("Song am toa ra quanh boss.")]
        public Sprite[] roarFrames;
        [Tooltip("FX lap tren dau moi quai dang duoc buff.")]
        public Sprite[] roarBuffFrames;
        [Tooltip("Clip THAN boss luc gam. hitFrame = frame ha mom rong nhat — buff + song am ra o frame do.")]
        public SpriteAnimationClip roarBody = new SpriteAnimationClip { name = "roar", loop = false };

        [Header("Noi Gian — duoi nguong mau")]
        [Range(0f, 1f)] public float enrageBelow = 0.3f;
        [Tooltip("0,5 = +50% toc danh. Noi gian thi mien lam cham.")]
        public float enrageAttackRateBonus = 0.5f;
    }
}
