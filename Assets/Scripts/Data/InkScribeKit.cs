using UnityEngine;

namespace BuildBuild.Data
{
    /// <summary>
    /// Bo Ngu But Su (kit duyet 2026-10-05, so CHUA can): Mac Diem → Tam But Thanh An (3 dau no them) →
    /// Hoanh But ve net chan duong tien → Tran Son An trien xuong giu chan → Hoa Long Diem Tinh (mac long quet dai roi no).
    /// Net muc la vung phep, khong can duong; dich bay mien net dat.
    /// </summary>
    public class InkScribeKit : ScriptableObject
    {
        [Header("Tam But Thanh An (noi tai, 1 sao)")]
        public int sealMax = 3;
        public float sealSeconds = 5f;
        public float sealPopPower = 0.8f;
        public float sealMinInterval = 0.5f;

        [Header("Hoanh But (phu 1, 3 sao)")]
        public float strokeCooldown = 7f;
        public float strokeLength = 2.4f;
        [Tooltip("Khong con do be day vung trung: net phu het be sau san (Battlefield.InLaneBand). Giu lai cho ban ve cu.")]
        public float strokeWidth = 0.3f;
        public float strokeAhead = 0.7f;
        public float strokeDuration = 5f;
        public int strokeMax = 2;

        [Header("Tran Son An (phu 2, 3 sao)")]
        public float sealCooldown = 10f;
        public float sealRadius = 1.2f;
        public float sealStun = 1.2f;

        [Header("Hoa Long Diem Tinh (chieu cuoi, mana)")]
        public float dragonLength = 4.5f;
        [Tooltip("Khong con do be day vung quet: mac long quet het be sau san (Battlefield.InLaneBand). Giu lai cho ban ve cu.")]
        public float dragonWidth = 1.2f;
        public float dragonBlast = 1f;

        [Header("Clip than")]
        public SpriteAnimationClip horizontalCast = new SpriteAnimationClip { name = "skill", loop = false };
        public SpriteAnimationClip sealCast = new SpriteAnimationClip { name = "skill", loop = false };
        public SpriteAnimationClip dragonCast = new SpriteAnimationClip { name = "skill", loop = false };

        [Header("FX")]
        public Sprite[] brushMarks, horizontalStroke, sealZone, sealBurst, dragonPainting, dragonPath, dragonImpact;

        /// <summary>Mac long bay doc duong quet (art tam o, dau quay TRAI).</summary>
        public Sprite[] inkDragon;
    }
}
