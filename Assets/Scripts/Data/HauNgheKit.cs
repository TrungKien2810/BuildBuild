using UnityEngine;

namespace BuildBuild.Data
{
    /// <summary>
    /// Bo Hau Nghe (SSR Xa thu, kit V2 duyet 2026-10-06 — so CHUA can):
    ///   - Nhat Tien (don thuong, dan ten): mot ten vao muc tieu dang nham.
    ///   - Thien Nhan Xa Nhat (noi tai, 1 sao): ten trung MUC TIEU DANG NHAM tich Nhat An (toi da `sealMax`);
    ///     moi an cong `sealDamagePerStack` sat thuong len chinh muc tieu do; du an thi chieu phu KE TIEP chuyen
    ///     sang dang Thien Cung (bien doi rieng tung chieu) roi tieu het an. Doi muc tieu / muc tieu chet thi mat tang.
    ///   - Nhat Luan Tran Tuyen (phu 1): ten ghim dat -> vung nhat luan bao truoc, dich trong vung bi cham;
    ///     o lai het nhp bao truoc thi bi giu chan MOT lan.
    ///   - Cuu Tien Quy Nhat (phu 2): 3 nhp x 3 ten hoi tu mot muc tieu; bi ngat thi mat cac nhp con lai.
    ///   - Nhat Boc Lien Hoan (phu 3): ten no tai muc tieu thanh 3 tia ngan cham toi da 3 dich gan do.
    ///   - Xa Nhat (chieu cuoi, mana): khoa huong, giuong cung bao truoc `aimSeconds` roi ban ten lon xuyen doan lane;
    ///     moi dich tren duong an `shotPower` ATK mot lan, muc tieu da nham an them `sealBonus` ATK moi Nhat An.
    /// So nam o day; moi thong so deu la so TAM cua Claude (nguoi dung se can lai sau khi thu Play).
    /// </summary>
    public class HauNgheKit : ScriptableObject
    {
        [Header("Thien Nhan Xa Nhat (noi tai, 1 sao)")]
        [Tooltip("So Nhat An toi da tren mot muc tieu — du an thi chieu phu ke tiep thanh dang Thien Cung.")]
        public int sealMax = 3;
        [Tooltip("Moi Nhat An cong them bao nhieu (0,12 = +12%) sat thuong len chinh muc tieu dang nham.")]
        public float sealDamagePerStack = 0.12f;
        [Tooltip("Nhat An ton tai bao lau ke tu lan trung cuoi (het thi mat het tang).")]
        public float sealSeconds = 5f;

        [Header("Nhat Luan Tran Tuyen (phu 1, 3 sao)")]
        public float luanCooldown = 10f;
        [Tooltip("Ban kinh vung nhat luan (world).")]
        public float luanRadius = 1.3f;
        [Tooltip("Thoi gian bao truoc truoc khi vung bat dau hoat dong.")]
        public float luanTelegraph = 0.6f;
        [Tooltip("Vung ton tai bao lau sau khi bao truoc xong.")]
        public float luanDuration = 3f;
        public float luanSlow = 0.35f;
        [Tooltip("Giu chan MOT lan cho dich o lai het nhp bao truoc (tinh anh mot nua; boss chi bi cham).")]
        public float luanRoot = 0.9f;
        [Tooltip("Dang Thien Cung: vung tach thanh 3 diem ghim — do dai dai chan (world).")]
        public float luanLineLength = 3.4f;

        [Header("Cuu Tien Quy Nhat (phu 2, 3 sao)")]
        public float cuuTienCooldown = 12f;
        public int cuuTienVolleys = 3;
        public int cuuTienArrowsPerVolley = 3;
        public float cuuTienVolleyGap = 0.35f;
        [Tooltip("He so ATK moi nhp (3 ten cong lai thanh mot nhp).")]
        public float cuuTienVolleyPower = 0.6f;
        [Tooltip("Dang Thien Cung: nhp cuoi hop thanh mot mui nang xuyen sang MOT dich phia sau.")]
        public float cuuTienEmpoweredPower = 1.6f;
        [Tooltip("Mui nang xuyen sang dich phia sau an them bao nhieu ATK.")]
        public float cuuTienPiercePower = 0.8f;

        [Header("Nhat Boc Lien Hoan (phu 3, 3 sao)")]
        public float bocCooldown = 9f;
        [Tooltip("Ban kinh tim dich cho 3 tia (world).")]
        public float bocRadius = 1.6f;
        public int bocTines = 3;
        [Tooltip("He so ATK cua vu no tai muc tieu.")]
        public float bocImpactPower = 1f;
        [Tooltip("He so ATK cua moi tia.")]
        public float bocTinePower = 0.6f;
        [Tooltip("Dang Thien Cung: tia ha duoc dich thi them MOT vu no phu (toi da 1 lan moi lan dung).")]
        public float bocEchoPower = 0.5f;
        public float bocEchoRadius = 1f;

        [Header("Xa Nhat (chieu cuoi, mana)")]
        [Tooltip(
            "Thoi gian GIU NGAM sau frame cham cua clip cast. Tong thoi gian bao truoc = (hitFrame-1)/fps + so nay " +
            "(kit ghi 1 giay -> clip 4 frame @130 ms = 0,52 + 0,5).")]
        public float aimHoldSeconds = 0.5f;
        [Tooltip("Chieu dai doan lane ten xuyen qua (world).")]
        public float shotLength = 6f;
        [Tooltip("He so ATK cho MOI dich tren duong (mot lan).")]
        public float shotPower = 3f;
        [Tooltip("Muc tieu dang nham an them bao nhieu ATK cho MOI Nhat An, roi xoa het an.")]
        public float sealBonus = 1f;
        [Tooltip("Nua be day vung trung cua ten (world).")]
        public float shotHalfWidth = 0.6f;

        [Header("Clip than")]
        public SpriteAnimationClip luanCast = new SpriteAnimationClip { name = "skill", loop = false };
        public SpriteAnimationClip cuuTienCast = new SpriteAnimationClip { name = "skill", loop = false };
        public SpriteAnimationClip bocCast = new SpriteAnimationClip { name = "skill", loop = false };
        public SpriteAnimationClip xaNhatCast = new SpriteAnimationClip { name = "skill", loop = false };

        [Header("FX")]
        [Tooltip("Ten nhat bay (don thuong) — gan vao `HeroDefinition.projectileFrames`.")]
        public Sprite[] arrow;
        [Tooltip("Impact khi ten trung dich.")]
        public Sprite[] hit;
        [Tooltip("Sun Seal mark on the Enemy (loop until expired).")]
        public Sprite[] seal;
        [Tooltip("Cung nhat mo canh sau lung khi co du 3 an (dang Thien Cung).")]
        public Sprite[] skyBow;
        [Tooltip("Vung nhat luan tren dat.")]
        public Sprite[] luanGround;
        [Tooltip("Dang Thien Cung: vung tach thanh dai 3 diem ghim.")]
        public Sprite[] luanEmpowered;
        [Tooltip("Chin anh ten hoi tu (mot nhp).")]
        public Sprite[] volley;
        [Tooltip("Nhp cuoi dang Thien Cung: mui ten nang hop ba ten.")]
        public Sprite[] volleyEmpowered;
        [Tooltip("3 tia ngan cua Nhat Boc.")]
        public Sprite[] chain;
        [Tooltip("Vu no tai muc tieu cua Nhat Boc (dung lai khi tia ha dich).")]
        public Sprite[] burst;
        [Tooltip("Vach nham truoc khi ban Xa Nhat.")]
        public Sprite[] aim;
        [Tooltip("Ten khong lo cua Xa Nhat.")]
        public Sprite[] sunArrow;
    }
}
