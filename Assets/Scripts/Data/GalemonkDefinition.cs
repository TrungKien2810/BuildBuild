using UnityEngine;

namespace BuildBuild.Data
{
    /// <summary>
    /// Bo chieu Vo Tang Gio (`Galemonk`, SR, GAME_DESIGN.md muc 3, nguoi dung duyet 2026-10-01). Chieu xin Long Quyen chay
    /// bang thanh mana cua `HeroDefinition` (SkillKind.Whirlwind: `skillPower` = sat thuong moi giay theo don thuong,
    /// `skillRadius` = ban kinh hut, `skillDuration` = giu). Moi so la gia tri KHOI DIEM Claude de xuat, CHUA can bang.
    /// </summary>
    [CreateAssetMenu(menuName = "BuildBuild/Galemonk Definition", fileName = "Galemonk_")]
    public class GalemonkDefinition : ScriptableObject
    {
        [Header("Chuong Phong (don thuong)")]
        [Tooltip("Quai trung dan gio bi day lui ngan nay (tinh anh / boss khong bi day).")]
        public float boltPush = 0.3f;
        public float boltSpeed = 7f;
        [Tooltip("Dan gio (lap, ve quay trai).")]
        public Sprite[] boltFrames;
        [Tooltip("Gio no khi trung (dung chung dan + song).")]
        public Sprite[] hitFrames;

        [Header("Thuan Phong (noi tai, passiveStar)")]
        [Tooltip("Quai bi gio day/hut: lam cham ti le nay.")]
        [Range(0f, 1f)] public float slowPercent = 0.2f;
        public float slowDuration = 2f;

        [Header("Phong Tao (chieu phu, minorStar)")]
        public float waveCooldown = 10f;
        [Tooltip("Song chay doc lane theo huong hero nhin, xa toi da quang nay.")]
        public float waveDistance = 6f;
        public float waveSpeed = 7f;
        [Tooltip("Quai lech toi da nay theo chieu sau (truc y) so voi duong song thi van trung.")]
        public float waveHalfDepth = 0.5f;
        public float wavePush = 1.5f;
        [Tooltip("Sat thuong moi quai = don thuong x he so nay.")]
        public float wavePower = 1.5f;
        [Tooltip("Buc tuong gio (lap, ve quay trai, pivot tai chan).")]
        public Sprite[] waveFrames;

        [Header("Long Quyen (chieu xin)")]
        [Tooltip("Tim cum quai dong nhat trong quang nay quanh hero.")]
        public float tornadoSearchRange = 6f;
        [Tooltip("Giay keo quai ve tam truoc khi giu.")]
        public float tornadoPullTime = 0.35f;
        public float tornadoTickInterval = 0.5f;
        [Tooltip("Loc xoay (lap, pivot tai chan).")]
        public Sprite[] tornadoFrames;
    }
}
