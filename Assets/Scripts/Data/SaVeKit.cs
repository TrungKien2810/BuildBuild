using UnityEngine;

namespace BuildBuild.Data
{
    /// <summary>
    /// Bo Sa Ve (kit duyet 2026-10-06, so CHUA can): Thach Quyen (don cham vua) → Tu Sa (chiu don tich tang giam sat thuong,
    /// toi da 5) → Tram Sa Quyen (quat dat + cham) → Boi Thach (tieu 2 tang hoi mau) → Sa Thanh Tran Tuyen (vung cat:
    /// dich cham, dong minh bot sat thuong dan xa, ban than giam moi sat thuong). Khong tang so quai chan.
    /// </summary>
    public class SaVeKit : ScriptableObject
    {
        [Header("Tu Sa (noi tai, 1 sao)")]
        public int sandMax = 5;
        public float sandResistPerStack = 0.04f;
        public float sandMinInterval = 0.5f;

        [Header("Tram Sa Quyen (phu 1, 3 sao)")]
        public float slamCooldown = 8f;
        public float slamPower = 1.5f;
        public float slamSlow = 0.3f;
        public float slamSlowSeconds = 3f;

        [Header("Boi Thach (phu 2, 3 sao)")]
        public int healCost = 2;
        public float healPct = 0.15f;
        public float healCooldown = 10f;

        [Header("Sa Thanh Tran Tuyen (chieu cuoi, mana)")]
        public float fortPower = 1f;
        public float fortRadius = 1.5f;
        public float fortDuration = 6f;
        public float fortSelfResist = 0.15f;
        public float fortAllyRangedResist = 0.25f;

        [Header("Clip than")]
        public SpriteAnimationClip slamCast = new SpriteAnimationClip { name = "skill", loop = false };
        public SpriteAnimationClip healCast = new SpriteAnimationClip { name = "skill", loop = false };
        public SpriteAnimationClip fortCast = new SpriteAnimationClip { name = "skill", loop = false };

        [Header("FX")]
        public Sprite[] quake, sandCore, heal, fortTelegraph, fortLoop, fortFade;
    }
}
