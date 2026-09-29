using UnityEngine;

namespace BuildBuild.Data
{
    /// <summary>
    /// So cua bo chieu Wyrmspear (Long Ky Binh, SSR — GAME_DESIGN.md muc 3, chot 2026-09-27). Moi so la gia tri KHOI DIEM
    /// Claude de xuat, CHUA can bang — chinh trong Inspector. Chieu lon (thanh mana) nam o `HeroDefinition.manaMax`.
    /// </summary>
    [CreateAssetMenu(menuName = "BuildBuild/Dragon Rider Definition", fileName = "DragonRider_")]
    public class DragonRiderDefinition : ScriptableObject
    {
        [Header("Noi tai Nghich Lan (ca hai giai doan)")]
        [Tooltip("Moi don cua Wyrmspear vao boss/tinh anh cong them phan nay cua MAU TOI DA muc tieu (0,02 = 2%).")]
        public float reverseScalePercent = 0.02f;

        [Header("GD1 — chieu thuong Xuyen Long (cooldown an)")]
        public float pierceCooldown = 7f;
        [Tooltip("He so x sat thuong don thuong. Bo qua giap (Health.Armor).")]
        public float piercePower = 2.5f;
        [Tooltip("Chieu dai duong xuyen tinh tu chan Wyrmspear.")]
        public float pierceLength = 5f;
        [Tooltip("Nua be ngang duong xuyen theo truc y.")]
        public float pierceHalfWidth = 0.45f;
        public SpriteAnimationClip pierceBody = new SpriteAnimationClip { name = "skill", loop = false, hitFrame = 4 };
        [Tooltip("Duong xuyen 4 o: 1-2 dau luong lap, 3 doan than, 4 tan.")]
        public Sprite[] pierceFx;

        [Header("GD1 -> GD2 — chieu lon lan dau: Long Hoa (bien hinh VINH VIEN trong wave)")]
        [Tooltip("Bo animation Long Nhan (idle/walk=hover/attack/hurt/death; skill = Thien Dia Long Giang).")]
        public SpriteAnimationSet dragonmanAnimations;
        [Tooltip("Tam danh GD2 (dam qua dau rong).")]
        public float dragonmanRange = 2f;
        [Tooltip("FX dat nut sang luc bien hinh / rong troi len.")]
        public Sprite[] crackFx;

        [Header("GD2 — Long Giap / Long Uy / Long No")]
        [Tooltip("Long Giap: +mau toi da (0,6 = +60%).")]
        public float armorHealthBonus = 0.6f;
        [Tooltip("Long Giap: giam moi sat thuong nhan (0,35 = -35%).")]
        public float armorReduction = 0.35f;
        [Tooltip("Long Uy: +cong.")]
        public float aweDamageBonus = 0.6f;
        [Tooltip("Long Uy: ban kinh tren mat dat quanh Wyrmspear.")]
        public float aweRadius = 2.5f;
        [Tooltip("Long Uy: quai trong tam giam cong.")]
        public float aweWeaken = 0.2f;
        [Tooltip("Long Uy: boss/tinh anh trong tam nhan them sat thuong tu MOI nguon.")]
        public float aweVulnerability = 0.15f;
        [Min(1)] public int rageMaxStacks = 7;
        [Tooltip("Long No: moi tang giam sat thuong nhan.")]
        public float rageReductionPerStack = 0.05f;
        [Tooltip("Long No: moi tang cong them.")]
        public float rageDamagePerStack = 0.08f;
        [Tooltip("Long No: moi tang hut mau (hoi phan nay sat thuong THAT gay ra). Nguoi dung them 2026-09-28, GDD chua ghi so.")]
        public float rageLifestealPerStack = 0.03f;
        [Tooltip("Khoang toi thieu giua hai lan cong tang — khong thi mot bay quai danh la day 7 tang ngay.")]
        public float rageStackInterval = 0.5f;
        [Tooltip("Khong chiu don bay lau thi bat dau roi tang.")]
        public float rageDecayDelay = 4f;
        [Tooltip("Giay moi tang roi sau khi het `rageDecayDelay`.")]
        public float rageDecayInterval = 1f;
        [Tooltip("Long Khe: dia long huong phan nay cua Long Giap + Long Uy.")]
        public float pactShare = 0.5f;

        [Header("GD2 — chieu thuong 1 Thien Long Lien Thuong (nguoi, cooldown an)")]
        public float lanceCooldown = 6f;
        [Tooltip("He so x sat thuong don thuong moi nhat (5 nhat). Moi nhat vao boss an Nghich Lan.")]
        public float lancePower = 0.8f;
        [Tooltip("He so nhat cuoi — xuyen ca duong phia sau.")]
        public float lanceFinalPower = 1.5f;
        [Tooltip("Clip than: 8 o art xep lai thanh 5 nhat (xem SceneBuilder).")]
        public SpriteAnimationClip lanceBody = new SpriteAnimationClip { name = "skill", loop = false };
        [Tooltip("Frame (tinh tu 1, trong `lanceBody`) cua 5 nhat dam.")]
        public int[] lanceHitFrames = { 2, 4, 6, 8, 10 };
        [Tooltip("Vet dam 4 o (o 4 = tia trung dich).")]
        public Sprite[] lanceFx;

        [Header("GD2 — chieu thuong 2 Dia Long Chan (rong, cooldown an)")]
        public float quakeCooldown = 8f;
        [Tooltip("Ban kinh mat dat quanh dia long.")]
        public float quakeRadius = 1.8f;
        [Tooltip("He so x sat thuong don thuong CUA RONG.")]
        public float quakePower = 1.5f;
        [Tooltip("Choang. Tinh anh/boss choang mot nua.")]
        public float quakeStun = 1.5f;
        [Tooltip("Pha giap: quai nhan them phan sat thuong nay (0,3 = +30%) trong `armorBreakDuration`.")]
        public float armorBreak = 0.3f;
        public float armorBreakDuration = 5f;
        public SpriteAnimationClip quakeBody = new SpriteAnimationClip { name = "skill", loop = false, hitFrame = 4 };
        [Tooltip("Gai da trang moc vong quanh rong.")]
        public Sprite[] quakeFx;
        [Tooltip("Sao choang lap tren dau quai (Enemy/stunned-status).")]
        public Sprite[] stunFrames;

        [Header("GD2 — chieu lon Thien Dia Long Giang (thanh mana)")]
        [Tooltip("Sat thuong goc (da nhan cong hien tai cua Wyrmspear) = damage x `descentPower`.")]
        public float descentPower = 5f;
        [Tooltip("Cong them phan nay mau toi da moi quai trung.")]
        public float descentMaxHealthPercent = 0.08f;
        [Tooltip("Ban kinh song chan quanh muc tieu.")]
        public float descentRadius = 1.8f;
        [Tooltip("Tam chon muc tieu (quai MAU CAO NHAT trong tam).")]
        public float descentRange = 7f;
        [Tooltip("Quai thuong/tinh anh duoi phan mau nay thi bi KET LIEU.")]
        public float executeThreshold = 0.2f;
        [Tooltip("Boss: khong ket lieu, nhan sat thuong x he so nay.")]
        public float bossMultiplier = 2f;
        [Tooltip("Ha duoc it nhat mot muc tieu thi hoan lai bay nhieu mana.")]
        public float killManaRefund = 2f;
        [Tooltip("Dia long cam dau xuong dat — chay cung luc Long Nhan bat len, cot da troi luc Long Nhan cham dat.")]
        public SpriteAnimationClip descentDragon = new SpriteAnimationClip { name = "skill", loop = false };
        [Tooltip("Frame (tinh tu 1) clip than Long Nhan cam giao xuong dat — luc ra don.")]
        public int descentHitFrame = 6;
        [Tooltip("Vet dat nat duoi Long Nhan dung `crackFx`. Cot da trang + song chan la cua DIA LONG: rong bi ha thi khong co.")]
        public Sprite[] pillarFx;
        public Sprite[] shockwaveFx;
        [Tooltip("Cot da hat tung quai con song trong `descentRadius` len bay cao (world). Boss khong bi nhac.")]
        public float pillarKnockUpHeight = 1.2f;
        [Tooltip("Thoi gian bay len roi roi xuong (giay) — khong di, khong danh.")]
        public float pillarKnockUpDuration = 0.8f;

        [Header("Bach Dia Long")]
        [Tooltip("Hero cua dia long (chi so, animation). Wyrmspear goi no bang prefab ben duoi.")]
        public HeroDefinition dragon;
        [Tooltip("Prefab Hero dung chung (Prefabs/Hero.prefab).")]
        public BuildBuild.Combat.Hero unitPrefab;
        [Tooltip("Rong troi len tu dat — clip ten \"skill\" de Hero mo khoa khi het.")]
        public SpriteAnimationClip emerge = new SpriteAnimationClip { name = "skill", loop = false };
        [Tooltip("Dia long dung phia TRUOC Wyrmspear (phia quai) bay nhieu unit.")]
        public float dragonForward = 1.1f;
        [Tooltip("Bi ha thi chui xuong dat, bay nhieu giay sau troi lai.")]
        public float dragonRespawn = 15f;
    }
}
