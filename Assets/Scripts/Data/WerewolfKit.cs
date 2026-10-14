using UnityEngine;

namespace BuildBuild.Data
{
    /// <summary>
    /// Bo Nguoi Soi SR (kit v2 duyet 2026-10-04, so CHUA can): dang nguoi Quyen Kich nhanh yeu de nap mana →
    /// Nguyet Bien (1 LAN/tran, vinh vien: +mau/cong, doi Vuot Soi + mo phu) → Huyết Thuc (hut mau, du thanh khien
    /// Mau Du) + Tru Nguyet (bi vay tru, dich Khiep So) + Ngoam Ha (can duoi 30%). Khong tro lai nguoi; giu dang qua wave.
    /// </summary>
    public class WerewolfKit : ScriptableObject
    {
        [Header("Quyen Kich (don nguoi)")]
        public float humanRateMult = 1.4f;
        public float humanPower = 0.6f;
        public float humanManaPerHit = 0.5f;

        [Header("Vuot Soi (don soi)")]
        public float wolfPower = 1.2f;
        public int sweepEvery = 3;
        public float sweepPower = 0.6f;

        [Header("Huyet Thuc (noi tai, 1 sao)")]
        public float leechHuman = 0.12f;
        public float leechWolf = 0.25f;
        public float shieldCapPct = 0.15f;
        public float shieldFadeSeconds = 4f;

        [Header("Tru Nguyet (phu 1, 3 sao, chi soi)")]
        public float howlCooldown = 12f;
        public float howlRadius = 1.5f;
        public float howlFear = 0.25f;
        public float howlSeconds = 4f;

        [Header("Ngoam Ha (phu 2, 3 sao, chi soi)")]
        public float biteCooldown = 8f;
        public float biteThreshold = 0.3f;
        public float bitePower = 3f;

        [Header("Nguyet Bien (chieu cuoi, mana)")]
        public float transformHealPct = 0.3f;
        public float hpBonus = 0.4f;
        public float atkBonus = 0.3f;

        [Header("Art soi (bo Anim_WerewolfWolf + clip bien hinh)")]
        public SpriteAnimationSet wolfSet;
        public SpriteAnimationClip transformClip = new SpriteAnimationClip { name = "skill", loop = false };
        public SpriteAnimationClip howlClip = new SpriteAnimationClip { name = "skill", loop = false };
        public SpriteAnimationClip biteClip = new SpriteAnimationClip { name = "skill", loop = false };

        [Header("FX")]
        public Sprite[] punchImpact, clawSlash, clawSweep, biteImpact, howlWave, bloodSiphon, bloodShield, transformBurst;
    }
}
