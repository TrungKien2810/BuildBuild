using UnityEngine;

namespace BuildBuild.Data
{
    /// <summary>
    /// Bo Vu Nu Hong Lien (kit duyet 2026-10-06, so CHUA can): Phien Hoa (don thuong) → Gieo Hong Lien (moi don thu 3
    /// dat 1 nu, toi da 3) → Song Phien Vu (lua quat vung hep) → Lien Tam Dan (kich no 1 nu gan muc tieu) →
    /// Hong Lien Dai Vu (tung nu lon + no toan bo). Nu nam cho, khong bam dich, khong chan duong.
    /// </summary>
    public class HongLienKit : ScriptableObject
    {
        [Header("Gieo Hong Lien (noi tai, 1 sao)")]
        public int seedEvery = 3;
        public int budMax = 3;
        public float budSeconds = 6f;

        [Header("Song Phien Vu (phu 1, 3 sao)")]
        public float fanCooldown = 8f;
        public float fanPower = 1.5f;
        [Tooltip("Tam voi cua luoi lua theo X (world) — luoi phu HET be sau san nen so nay chi con chieu dai " +
            "(2026-10-10: 2 -> 3).")]
        public float fanLength = 3f;

        [Header("Lien Tam Dan (phu 2, 3 sao)")]
        public float detonateCooldown = 6f;
        public float detonatePower = 2f;
        [Tooltip("Nua be ngang vung no (world X) — vung no theo COT phu het be sau san (2026-10-10: 0,8 -> 1,1).")]
        public float detonateRadius = 1.1f;

        [Header("Hong Lien Dai Vu (chieu cuoi, mana)")]
        public float grandPower = 3f;
        [Tooltip("Nua be ngang vung no lon (world X) — vung theo COT phu het be sau san (2026-10-10: 1 -> 1,4).")]
        public float grandRadius = 1.4f;

        [Header("Clip than")]
        public SpriteAnimationClip fanCast = new SpriteAnimationClip { name = "skill", loop = false };
        public SpriteAnimationClip detonateCast = new SpriteAnimationClip { name = "skill", loop = false };
        public SpriteAnimationClip grandCast = new SpriteAnimationClip { name = "skill", loop = false };

        [Header("FX")]
        public Sprite[] fanFlame, lotusBud, lotusBloom, lotusFade, greatBud, greatBloom;
    }
}
