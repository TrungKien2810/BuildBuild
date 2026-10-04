using UnityEngine;

namespace BuildBuild.Data
{
    /// <summary>
    /// Ba chieu cua boss wave 20 Phap su Orc lo lung (`OrcShaman`, GAME_DESIGN.md muc 5). Don thuong tam xa (cau lua,
    /// uu tien hero / linh, het muc tieu moi ban tuong) la luat quai ban xa co san cua `Enemy` + `EnemyExtras`.
    /// Moi so la gia tri KHOI DIEM Claude de xuat, CHUA can bang (GDD chua chot so).
    /// </summary>
    [CreateAssetMenu(menuName = "BuildBuild/Shaman Definition", fileName = "Shaman_")]
    public class ShamanDefinition : ScriptableObject
    {
        [Header("Mieng Ho Lua — danh dau roi no")]
        public float pitInterval = 12f;
        [Tooltip("Chi nham cum hero / linh trong tam nay quanh shaman.")]
        public float pitRange = 7f;
        [Tooltip("Ban kinh vung no (tren mat dat, toi MEP than unit).")]
        public float pitRadius = 1.2f;
        [Tooltip("Giay tu luc danh dau toi luc no — thoi gian nguoi choi keo hero ra.")]
        public float pitDelay = 1.5f;
        public float pitDamage = 100f;
        [Tooltip("Vong danh dau LAP tren dat trong `pitDelay`.")]
        public Sprite[] pitMarkFrames;
        public Sprite[] pitBurstFrames;

        [Header("Khien Phep — boc khien dong minh")]
        public float shieldInterval = 18f;
        [Tooltip("Luong sat thuong khien hut (nhan he so sat thuong boss cua wave).")]
        public float shieldAmount = 400f;
        public float shieldDuration = 6f;
        [Tooltip("Tim boss khac (Minotaur) truoc, khong co thi quai gan nhat trong tam nay.")]
        public float shieldRange = 6f;
        public Sprite[] shieldFrames;

        [Header("Dich Chuyen — ne hero can chien")]
        public float teleportCooldown = 10f;
        [Tooltip("Hero / linh CAN CHIEN den gan hon khoang nay (tren mat dat) thi bien mat.")]
        public float teleportTrigger = 1.5f;
        [Tooltip("Hien lai xa hon bay nhieu ve phia phai (bi kep trong mep san).")]
        public float teleportDistance = 3f;

        [Header("Clip than")]
        [Tooltip("Niem chieu — dung chung cho Ho Lua va Khien. hitFrame = frame gio tay cao nhat (chieu ra o day).")]
        public SpriteAnimationClip castBody = new SpriteAnimationClip { name = "cast", loop = false };
        [Tooltip("Bien mat trong lua. Het clip thi doi cho.")]
        public SpriteAnimationClip vanishBody = new SpriteAnimationClip { name = "vanish", loop = false };
        [Tooltip("Hien ra = clip bien mat choi nguoc.")]
        public SpriteAnimationClip appearBody = new SpriteAnimationClip { name = "appear", loop = false };
    }
}
