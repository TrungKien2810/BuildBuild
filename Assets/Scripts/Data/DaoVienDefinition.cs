using UnityEngine;

namespace BuildBuild.Data
{
    /// <summary>Hero nao trong bo Dao Vien (`HeroDefinition.swornBrother`). None = khong thuoc bo.</summary>
    public enum SwornBrother { None, LuuBi, QuanVu, TruongPhi }

    /// <summary>
    /// Bo chieu Dao Vien — Luu Bi / Quan Vu / Truong Phi (SSR, GAME_DESIGN.md muc 3, nguoi dung duyet kit 2026-10-02). MOT asset
    /// dung chung cho ca ba (`HeroDefinition.daoVien`) vi noi tai Ket Nghia la cua chung; moi hero doc phan cua minh. Chieu xin
    /// chay bang thanh mana cua `HeroDefinition` (SkillKind.DaoVien: `skillPower` / `skillRadius` / `skillDuration` doi nghia
    /// theo hero, xem tung muc). Moi so la gia tri KHOI DIEM Claude de xuat, CHUA can bang.
    /// </summary>
    [CreateAssetMenu(menuName = "BuildBuild/Dao Vien Definition", fileName = "DaoVien_")]
    public class DaoVienDefinition : ScriptableObject
    {
        [Header("Ket Nghia (noi tai chung, passiveStar)")]
        [Tooltip("Tu 2/3 anh em tren san: ca ba +ti le cong nay.")]
        public float bondDamage = 0.1f;
        [Tooltip("Tu 2/3 anh em tren san: ca ba +ti le mau toi da nay.")]
        public float bondHealth = 0.1f;
        [Tooltip("Du 3/3: them +ti le toc hoi mana.")]
        public float bondManaRate = 0.25f;

        [Header("Luu Bi — Nhan Duc (noi tai)")]
        public float virtueRadius = 2.5f;
        [Tooltip("Hero trong ban kinh +ti le toc hoi mana; anh em ket nghia nhan doi.")]
        public float virtueManaRate = 0.15f;

        [Header("Luu Bi — An Duc (chieu thuong, minorStar)")]
        public float graceCooldown = 9f;
        [Tooltip("Hoi ti le mau toi da cua dong minh mat mau nhieu nhat + giai choang / troi / cham / Thieu Hon.")]
        public float graceHeal = 0.15f;
        [Tooltip("Clip than gio kiem ban phuc (ten \"skill\"). hitFrame = o ban phuc. Dung chung voi chieu xin.")]
        public SpriteAnimationClip blessBody = new SpriteAnimationClip { name = "skill", loop = false };
        [Tooltip("Vong hoi mau hong dao tren dat (pivot tai chan).")]
        public Sprite[] healRingFrames;

        [Header("Luu Bi — Dong Sinh Cong Tu (chieu phu 2, secondMinorStar)")]
        [Tooltip("Anh em nga xuong lan dau moi wave nam bao lau truoc khi duoc keo day.")]
        public float reviveDelay = 1f;
        public float reviveHealth = 0.3f;
        [Tooltip("Bat tu sau khi day.")]
        public float reviveInvulnerable = 2f;
        [Tooltip("Dai lua dao cuon keo day (pivot tai chan).")]
        public Sprite[] silkFrames;

        [Header("Luu Bi — Dao Vien Ket Nghia (chieu xin; skillPower = ti le hoi mau toi da, skillDuration = buff)")]
        [Tooltip("Moi dong minh +ti le cong trong skillDuration.")]
        public float oathDamage = 0.3f;
        [Tooltip("Anh em ket nghia +mana.")]
        public float oathBrotherMana = 2f;
        [Tooltip("So chum hoa dao rai khap man.")]
        public int petalClusters = 10;
        [Tooltip("Chum canh hoa dao roi (lap).")]
        public Sprite[] petalFrames;

        [Header("Co FX (x UnitScale; 1 = dung co art canh hero nhu GIF duyet)")]
        [Tooltip("Song dao, hon rong, vet chem Thac Dao. Nguoi dung 2026-10-02: 1 thi nhin nho trong game.")]
        public float quanVuFxScale = 1.6f;
        [Tooltip("Vong song am + vet nut dat cua Duong Duong Kieu Hong. Nguoi dung 2026-10-02: ve theo ban kinh chieu (x3) qua to.")]
        public float roarFxScale = 1f;

        [Header("Quan Vu — Vo Thanh (noi tai)")]
        [Tooltip("Don thuong chem quat trung toi da so quai nay.")]
        public int cleaveTargets = 3;
        [Tooltip("Quai lech toi da nay theo chieu sau so voi Quan Vu thi van nam trong quat.")]
        public float cleaveHalfDepth = 0.5f;
        [Tooltip("Moi quai bi Quan Vu ha +1 tang Uy: +cong, +hut mau; mat khi het wave.")]
        public float awePerStack = 0.05f;
        public float lifestealPerStack = 0.02f;
        public int maxStacks = 6;

        [Header("Quan Vu — Thanh Long Yen Nguyet (chieu thuong, minorStar)")]
        public float waveCooldown = 8f;
        [Tooltip("Sat thuong moi quai = don thuong x he so nay.")]
        public float wavePower = 2f;
        public float waveDistance = 6f;
        public float waveSpeed = 7f;
        public float waveHalfDepth = 0.5f;
        [Tooltip("Clip than vung dao phong song (ten \"skill\"). hitFrame = o quet. Dung chung voi chieu xin.")]
        public SpriteAnimationClip slashBody = new SpriteAnimationClip { name = "skill", loop = false };
        [Tooltip("Song dao trang khuyet (lap, ve bay sang trai, pivot tai chan).")]
        public Sprite[] crescentFrames;

        [Header("Quan Vu — Thac Dao Ke (chieu phu 2, secondMinorStar)")]
        [Tooltip("Mau duoi ti le nay thi lui gia thua (mot lan moi wave).")]
        public float feintThreshold = 0.4f;
        public float feintRetreat = 1f;
        public float feintRetreatTime = 0.35f;
        public float feintPower = 3f;
        [Tooltip("Tam chem quay lai, tinh tu chan toi mep than quai.")]
        public float feintReach = 1.6f;
        [Tooltip("Vet chem cong lon (tam o).")]
        public Sprite[] slashTrailFrames;

        [Header("Quan Vu — Thanh Long Hien (chieu xin; skillPower = he so don thuong)")]
        [Tooltip("Sat thuong x (1 + ti le mau da mat x he so nay).")]
        public float dragonMissingHealthBonus = 1f;
        [Tooltip("Moi quai bi ha hoan ti le thanh mana.")]
        public float dragonKillMana = 0.1f;
        public float dragonDistance = 9f;
        public float dragonSpeed = 8f;
        public float dragonHalfDepth = 0.6f;
        [Tooltip("Hon rong luc (lap, ve bay sang trai, tam o).")]
        public Sprite[] dragonFrames;

        [Header("Truong Phi — Van Nhan Dich (noi tai)")]
        public float bulwarkRadius = 1.5f;
        [Tooltip("Moi quai trong ban kinh -ti le sat thuong nhan.")]
        public float bulwarkPerEnemy = 0.06f;
        public float bulwarkMax = 0.3f;

        [Header("Truong Phi — Bat Xa Mau (chieu thuong, minorStar)")]
        public float pierceCooldown = 7f;
        [Tooltip("Dam xuyen dai bao nhieu tinh tu chan.")]
        public float pierceLength = 2f;
        public float pierceHalfDepth = 0.4f;
        public float piercePower = 1.5f;
        [Tooltip("Quai trung nhan them ti le sat thuong trong `pierceBreakSeconds`.")]
        public float pierceArmorBreak = 0.15f;
        public float pierceBreakSeconds = 4f;
        [Tooltip("Clip than xoay xa mau nap luc roi dam (ten \"skill\"). hitFrame = o dam.")]
        public SpriteAnimationClip pierceBody = new SpriteAnimationClip { name = "skill", loop = false };
        [Tooltip("Vet dam xuyen (ve sang trai, tam o).")]
        public Sprite[] pierceFrames;

        [Header("Truong Phi — Tuy Tuu (chieu phu 2, secondMinorStar)")]
        public float drinkCooldown = 15f;
        [Tooltip("Chi uong khi mau duoi ti le nay.")]
        public float drinkThreshold = 0.6f;
        public float drinkHeal = 0.2f;
        [Tooltip("-ti le sat thuong nhan + mien choang trong `drinkSeconds`.")]
        public float drinkReduction = 0.3f;
        public float drinkSeconds = 5f;
        [Tooltip("Clip than tu bau ruou (ten \"skill\"). hitFrame = o uong.")]
        public SpriteAnimationClip drinkBody = new SpriteAnimationClip { name = "skill", loop = false };

        [Header("Truong Phi — Duong Duong Kieu Hong (chieu xin; skillRadius = ban kinh, skillPower = he so, skillDuration = choang)")]
        public float roarPush = 2f;
        public float roarArmorBreak = 0.3f;
        public float roarBreakSeconds = 6f;
        [Tooltip("Boss khong bi hat / choang: chi cham ti le nay trong `roarBreakSeconds`.")]
        public float roarBossSlow = 0.5f;
        [Tooltip("Truong Phi khieu khich ca vung + -ti le sat thuong nhan trong `roarBreakSeconds`.")]
        public float roarReduction = 0.5f;
        [Tooltip("Vong song am lan ra (tam o).")]
        public Sprite[] roarWaveFrames;
        [Tooltip("Vet nut dat (tam o, dat tai chan).")]
        public Sprite[] groundCrackFrames;
    }
}
