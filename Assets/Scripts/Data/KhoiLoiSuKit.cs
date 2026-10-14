using UnityEngine;
using BuildBuild.Combat;

namespace BuildBuild.Data
{
    /// <summary>
    /// Bo Khoi Loi Su (kit duyet 2026-10-06, chon Ho Chu, so CHUA can): Giac Day (roi danh) → Nhat Khoi Dong Hanh
    /// (1 roi MocKhoi, mau rieng, chan toi da 3, hong thi sua) → Quet Moc (roi quet vung) → Ho Chu (roi ve gat dich ap sat
    /// chu) → Khoi Vuong Giang Tran (roi bung khop DaiKhoi, nen song + danh quet). Dung 1 roi, giu mau hien tai.
    /// </summary>
    public class KhoiLoiSuKit : ScriptableObject
    {
        [Header("Roi (noi tai, 1 sao)")]
        public HeroDefinition puppet;
        public SpriteAnimationSet grandSet;
        public Hero unitPrefab;
        public float controlRadius = 4f;
        public float repairSeconds = 8f;

        [Header("Quet Moc (phu 1, 3 sao)")]
        public float sweepCooldown = 8f;
        public float sweepPower = 1.5f;
        public float sweepRadius = 1f;

        [Header("Ho Chu (phu 2, 3 sao)")]
        public float guardCooldown = 12f;
        public float guardRadius = 1.5f;
        public float guardShove = 0.8f;

        [Header("Khoi Vuong Giang Tran (chieu cuoi, mana)")]
        public float grandPower = 3f;
        public float grandRadius = 1.5f;
        public float grandStun = 1f;
        public float grandDuration = 10f;

        [Header("Clip than (chu)")]
        public SpriteAnimationClip deployCast = new SpriteAnimationClip { name = "skill", loop = false };
        public SpriteAnimationClip sweepCast = new SpriteAnimationClip { name = "skill", loop = false };
        public SpriteAnimationClip guardCast = new SpriteAnimationClip { name = "skill", loop = false };
        public SpriteAnimationClip grandCast = new SpriteAnimationClip { name = "skill", loop = false };

        // Roi la mot `Hero` day du nen clip danh / quet cua no nam trong BO cua chinh no (`puppet.animations.skill` =
        // MocKhoi/quet-moc, `grandSet.skill` = DaiKhoi/slam). Ba clip duoi day khong thuoc bo nao (art rieng cho tung
        // trang thai) nen phai khai o day; thieu chung la roi "dung hinh" dung luc dang lam gi do — nguoi dung thay
        // bo animation cua roi thieu (2026-10-09).
        [Header("Clip cua roi")]
        [Tooltip("Puppet Companion: the puppet unfolds and stands up (MocKhoi/deploy).")]
        public SpriteAnimationClip puppetDeploy = new SpriteAnimationClip { name = "skill", loop = false };
        [Tooltip("Master Guard: the puppet returns to parry for its master (MocKhoi/ho-chu-parry).")]
        public SpriteAnimationClip puppetParry = new SpriteAnimationClip { name = "skill", loop = false };
        [Tooltip("Puppet King Descent: unfold into a giant puppet (DaiKhoi/transform) — keep the clip duration because "
            + "the first slam waits for this clip to finish.")]
        public SpriteAnimationClip grandTransform = new SpriteAnimationClip { name = "skill", loop = false };

        [Header("FX")]
        public Sprite[] cable, guardDust, impact, grandWave;
    }
}
