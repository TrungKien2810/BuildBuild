using UnityEngine;

namespace BuildBuild.Data
{
    /// <summary>
    /// Kit boss wave 30 Rong Ma Hoa (`Dreadwyrm`, GAME_DESIGN.md muc 5, kit nguoi dung chot 2026-09-30). Xac Khong Dau
    /// (-25% sat thuong) nam o `EnemyDefinition.damageReduction`. GDD chua chot so: moi so la gia tri KHOI DIEM Claude de
    /// xuat, CHUA can bang. Khoang cach do bang world (da nhan UnitScale), tren mat dat.
    /// </summary>
    [CreateAssetMenu(menuName = "BuildBuild/Dreadwyrm Definition", fileName = "Dreadwyrm_")]
    public class DreadwyrmDefinition : ScriptableObject
    {
        [Header("Don thuong — can / vuot quet")]
        [Tooltip("Frame cham: toi da `cleaveTargets` unit phe ta gan nhat quanh MOM (tinh ca muc tieu chinh).")]
        public int cleaveTargets = 3;
        public float cleaveRadius = 1.6f;
        public Sprite[] clawFrames;

        [Header("Hoi Tho Ma Hoa")]
        public float breathInterval = 12f;
        [Tooltip("Sat thuong tuc thi len moi hero trong dai lua (nhan he so boss).")]
        public float breathDamage = 60f;
        [Tooltip("Dai lua tinh tu mom ve phia truoc (world).")]
        public float breathLength = 4.5f;
        [Tooltip("Nua be ngang dai lua theo chieu sau mat dat (world, tren mat dat).")]
        public float breathHalfWidth = 0.8f;
        public Sprite[] breathFrames;
        [Header("Dat chay sau Hoi Tho")]
        public float burnDuration = 4f;
        public float burnDamagePerSecond = 15f;
        [Tooltip("Chay bam theo hero bao nhieu giay sau khi buoc ra khoi dat chay.")]
        public float burnLingerSeconds = 2f;
        [Tooltip("Hero dung trong dat chay bi Cam hoi mau (khong hoi mau/mana) chung nay giay sau moi tick.")]
        public float scorchSeconds = 1f;
        [Tooltip("So vung dat chay rai deu doc dai lua.")]
        public int burnPatches = 3;
        public float burnRadius = 0.8f;
        public Sprite[] burnFrames;

        [Header("Vo Canh Cuong Phong")]
        public float galeInterval = 16f;
        [Tooltip("Hat lui moi unit phe ta trong ban kinh nay quanh rong.")]
        public float galeRadius = 3f;
        [Tooltip("Quang hat lui ~1 o (world).")]
        public float galeShove = 1.2f;
        public float galeShoveSeconds = 0.25f;
        public Sprite[] galeFrames;

        [Header("Goi Hon Lua")]
        public float soulInterval = 20f;
        [Tooltip("0,25 = +25% sat thuong cho quai ho tong bi boc lua ma (toi khi chet).")]
        public float soulDamageBonus = 0.25f;
        [Tooltip("Quai bi boc lua chet thi no: sat thuong (nhan he so boss) len hero trong ban kinh.")]
        public float soulBurstDamage = 30f;
        public float soulBurstRadius = 1f;
        public Sprite[] soulAuraFrames;
        public Sprite[] soulBurstFrames;
        [Tooltip("Ma Hoa sinh them moi lan gam (ngau nhien trong khoang, gom ca hai dau).")]
        public int wispMin = 2;
        public int wispMax = 3;
        public EnemyDefinition wisp;

        [Header("Noi Gian — duoi nguong mau")]
        [Range(0f, 1f)] public float enrageBelow = 0.3f;
        public float enrageAttackRateBonus = 0.5f;
        [Tooltip("Hoi Tho hoi nhanh gap bay nhieu lan khi noi gian.")]
        public float enrageBreathSpeedup = 2f;
        [Tooltip("Lua chuyen cam-ruc: phu mau ca than.")]
        public Color enrageTint = new Color(1f, 0.72f, 0.45f);

        [Header("Clip than — hitFrame la frame chieu ra")]
        [Tooltip("Ngoc dau ~0,5 s bao truoc (frame 1-3) roi phun o hitFrame.")]
        public SpriteAnimationClip breathBody = new SpriteAnimationClip { name = "breath", loop = false };
        [Tooltip("Dang canh ~0,5 s bao truoc roi dap canh o hitFrame.")]
        public SpriteAnimationClip galeBody = new SpriteAnimationClip { name = "gale", loop = false };
        public SpriteAnimationClip roarBody = new SpriteAnimationClip { name = "roar", loop = false };

        [Header("Diem neo tren art (world tu pivot, art quay TRAI)")]
        [Tooltip("Mom breath-v4 frame 4, PPU100 x scale2, ground377.")]
        public Vector2 mouth = new Vector2(-2.68f, 2.22f);
        [Tooltip("Mom luc can (attack-3) — vet vuot hien o day.")]
        public Vector2 bite = new Vector2(-2.74f, 1.98f);
    }
}
