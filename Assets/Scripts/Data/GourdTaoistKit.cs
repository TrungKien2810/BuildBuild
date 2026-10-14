using UnityEngine;

namespace BuildBuild.Data
{
    /// <summary>
    /// Bo Dao Si Ho Lo (kit duyet 2026-10-05, so CHUA can): Linh Khi Dan tich Khi (tran 6) → Thanh Khi Ho Than
    /// tao khien khi chinh minh giai hieu ung → Thu Ta hut Doc/Chay/Cham → Luyen Khi Pha tieu 2 Khi ban no + ap Doc →
    /// Can Khon Khai Ho thanh tay vung roi tieu Khi phun non 2s. Khi rieng, khong thay mana; giu qua wave, reset tran moi.
    /// </summary>
    public class GourdTaoistKit : ScriptableObject
    {
        [Header("Khi (tai nguyen rieng)")]
        public int qiMax = 6;

        [Header("Thu Ta (phu 1, 3 sao)")]
        public float suctionCooldown = 8f;
        public float suctionRadius = 3f;
        public int suctionMaxAllies = 2;

        [Header("Luyen Khi Pha (phu 2, 3 sao)")]
        public int compressedCost = 2;
        public float compressedCooldown = 10f;
        public float compressedRadius = 0.8f;

        [Header("Thanh Khi Ho Than (noi tai, 1 sao)")]
        public float shieldPct = 0.06f;
        public float shieldDuration = 4f;
        public float shieldPerAllyCooldown = 3f;

        [Header("Can Khon Khai Ho (chieu cuoi, mana)")]
        public float ultCleanseRadius = 3f;
        public int ultCleanseMax = 6;
        public float ultDuration = 2f;
        [Tooltip("Tam non phun phu het be sau san (11 don vi); chieu dai X nay bang be ngang art non (~3,9 world o o 256): " +
            "de 3 thi vet trung hut so voi hinh ve.")]
        public float ultConeLength = 4f;
        public int ultTicks = 4;

        [Header("Clip than")]
        public SpriteAnimationClip suctionCast = new SpriteAnimationClip { name = "skill", loop = false };
        public SpriteAnimationClip compressedCast = new SpriteAnimationClip { name = "skill", loop = false };
        public SpriteAnimationClip ultCast = new SpriteAnimationClip { name = "skill", loop = false };

        [Header("FX")]
        public Sprite[] suction, shield, compressedBolt, compressedBlast, cone;
    }
}
