using UnityEngine;

namespace BuildBuild.Data
{
    /// <summary>
    /// Bo chieu Phap su sam set (`Stormcaller`, SSR, GAME_DESIGN.md muc 3, chot 2026-10-01). Chieu xin Loi Bao chay bang
    /// thanh mana cua `HeroDefinition` (SkillKind.ThunderStorm: `skillPower` = he so sat thuong moi cot, `skillDuration` =
    /// choang). Moi so la gia tri KHOI DIEM Claude de xuat, CHUA can bang.
    /// </summary>
    [CreateAssetMenu(menuName = "BuildBuild/Stormcaller Definition", fileName = "Stormcaller_")]
    public class StormcallerDefinition : ScriptableObject
    {
        [Header("Xich Loi (don thuong)")]
        [Tooltip("So lan nay toi da sau quai dau tien.")]
        [Min(0)] public int chainBounces = 3;
        [Tooltip("Ban kinh tim quai ke tiep tu diem vua trung.")]
        public float chainRadius = 2f;
        [Tooltip("Moi lan nay giam ti le nay sat thuong (0,3 = -30%).")]
        [Range(0f, 1f)] public float chainFalloff = 0.3f;
        [Tooltip("Tia set noi hai diem (keo dan theo khoang cach).")]
        public Sprite[] boltFrames;
        [Tooltip("Chop tai quai trung set.")]
        public Sprite[] hitFrames;

        [Header("Qua Tai (noi tai, passiveStar)")]
        [Tooltip("Moi tia set trung quai +1 tang; du so nay thi no.")]
        [Min(1)] public int maxCharges = 3;
        [Tooltip("Don no tang: +ti le sat thuong.")]
        public float overloadBonus = 0.6f;
        [Tooltip("Don no tang: choang (boss/tinh anh khong choang).")]
        public float overloadStun = 0.3f;
        [Tooltip("Te Liet: quai dang choang an them ti le sat thuong set.")]
        public float paralyzeBonus = 0.35f;
        [Tooltip("Set vao khien (Khien Phep, khien hut): phan sat thuong vao khien nhan he so nay.")]
        public float shieldMultiplier = 2f;
        [Tooltip("Icon tang dien tren dau quai: o N = N tang.")]
        public Sprite[] chargePipFrames;
        [Tooltip("Vu no tren dat (dung chung no tang + Loi Bo).")]
        public Sprite[] burstFrames;

        [Header("Cot Thu Loi (chieu phu, minorStar)")]
        public float rodCooldown = 9f;
        public float rodLifetime = 6f;
        public float rodTickInterval = 1f;
        [Tooltip("Quai trong ban kinh nay (tinh toi mep than) bi cot giat moi nhip.")]
        public float rodRadius = 1.2f;
        [Tooltip("Sat thuong moi nhip giat = don thuong x he so nay.")]
        public float rodPower = 0.5f;
        [Tooltip("Xich Loi nay qua cot: them so lan nay, lan nay do khong giam sat thuong.")]
        [Min(0)] public int rodBonusBounces = 2;
        [Tooltip("Cot dong (lap trong luc song).")]
        public Sprite[] rodFrames;

        [Header("Loi Bo (chieu phu 2, secondMinorStar)")]
        public float blinkCooldown = 8f;
        [Tooltip("Quai ap sat trong khoang nay (tinh toi mep than) thi hoa set lui.")]
        public float blinkTrigger = 0.8f;
        public float blinkDistance = 2f;
        [Tooltip("Vu no o cho cu: ban kinh, choang, sat thuong = don thuong x he so.")]
        public float blinkBurstRadius = 1.2f;
        public float blinkStun = 0.5f;
        public float blinkPower = 0.5f;
        [Tooltip("Chop bien mat / hien ra.")]
        public Sprite[] blinkFrames;

        [Header("Loi Bao (chieu xin)")]
        [Min(1)] public int stormBolts = 5;
        [Tooltip("Giay giua hai cot set lien tiep.")]
        public float stormInterval = 0.12f;
        [Tooltip("Cot set tu troi giang xuong (pivot o diem cham dat).")]
        public Sprite[] strikeFrames;
    }
}
