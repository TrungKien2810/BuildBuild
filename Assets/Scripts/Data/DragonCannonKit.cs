using UnityEngine;
using BuildBuild.Combat;

namespace BuildBuild.Data
{
    /// <summary>
    /// Bo "Long Viêm" cua Dau Rong Da (phao SSR, the giao viec `docs/giao-viec/asset-phao.md` muc 4.16 +
    /// `tempart/CannonDragonHead/brief.md` — ban thiet ke cua ben art). So o day la TAM de thu, chua can.
    ///
    /// Khac moi phao khac trong game o cho: no KHONG ban tung phat. No phun mot non lua lien tuc `breathSeconds`
    /// giay roi nghi `restSeconds`, sat thuong tinh theo DPS (khong phu thuoc so frame animation), moi quai trong non
    /// bi Thiêu Đốt. Trung quai thi day thanh Long Nộ; thanh day -> Long Tức (luong lon xuyen ca dai + dat chay).
    /// Quai chet luc dang chay vi phao nay thi no lan (Hỏa Bạo).
    ///
    /// Vi sao tach thanh asset rieng thay vi nhet ~15 field vao `CannonDefinition`: `CannonDefinition` da phai giu
    /// 4 kieu ra don khac nhau (Projectile/Chain/Cone/Vortex) cho ca 12 phao; them mot bo luat chi MOT phao dung vao
    /// do la bien no thanh "moi thu cua moi phao". Kit nay chi phao Long Viêm doc, va `Cannon` chi can biet mot field
    /// `dragonKit` khac null (xem `Cannon.Initialise`).
    /// </summary>
    [CreateAssetMenu(menuName = "BuildBuild/Dragon Cannon Kit", fileName = "DragonCannonKit")]
    public class DragonCannonKit : ScriptableObject
    {
        [Header("Nhip phun (giay)")]
        [Tooltip("Phun lien tuc bao lau roi moi nghi.")]
        public float breathSeconds = 2.5f;
        [Tooltip("Nghi lay hoi giua hai dot phun. Khong co asset cho khoang nghi — than ve idle.")]
        public float restSeconds = 0.5f;
        [Tooltip("Sat thuong tinh theo nhip nay (giay), khong theo frame: DPS x nhip. Nho = so sat thuong nhay nhieu.")]
        public float tickSeconds = 0.25f;

        [Header("Hinh non")]
        [Tooltip("Nua goc non phun (do). Tam non la huong nong, dai bang `CannonDefinition.range`. 30 do = phu gan het " +
            "chieu cao lan o tam 5,5 (lan rong 5,5 world) — nho hon thi quai o hang tren/duoi khong bao gio bi dot.")]
        public float coneHalfAngle = 30f;

        [Header("Thiêu Đốt (moi quai trung non)")]
        [Tooltip("Han cua Chay moi lan trung (giay). Trung lai thi lam moi, khong cong tang.")]
        public float burnSeconds = 3f;
        [Tooltip("Chay gay bao nhieu lan D moi giay (0,2 = 20% D).")]
        public float burnShare = 0.2f;

        [Header("Long Nộ (thanh 0..rageMax)")]
        public float rageMax = 100f;
        [Tooltip("Diem cong moi giay khi luong phun co trung it nhat MOT quai (khong nhan theo so quai).")]
        public float rageGainPerSecond = 40f;
        [Tooltip("Khong trung quai trong `rageDecayDelay` giay thi bat dau tut.")]
        public float rageDecayDelay = 2f;
        public float rageDecayPerSecond = 20f;
        [Tooltip("Ba nguong chia ba muc Long Nộ, tang dan. Muc 0 = duoi nguong dau.")]
        public float[] rageThresholds = { 33f, 66f };
        [Tooltip("He so DPS theo ba muc Long Nộ (1 / 1,4 / 1,8).")]
        public float[] rageDamage = { 1f, 1.4f, 1.8f };
        [Tooltip("He so TAM theo ba muc Long Nộ (1 / 1,15 / 1,3). Non dai hon, khong thanh vong AOE.")]
        public float[] rageRange = { 1f, 1.15f, 1.3f };

        [Header("Long Tức (thanh day)")]
        [Tooltip("Lay hoi truoc khi phong (giay) — nam trong clip `skill` nen khong doi hinh them lan nua.")]
        public float ultInhaleSeconds = 0.3f;
        [Tooltip("Luong lon ton tai bao lau (giay).")]
        public float ultSeconds = 0.6f;
        [Tooltip("Moi quai an MOT lan `ultPower` x D cho ca luong, khong an theo frame.")]
        public float ultPower = 3f;
        [Tooltip("Luong lon phu xa hon non thuong bao nhieu lan (chi la hinh + vung trung).")]
        public float ultRangeScale = 1.35f;
        public float ultBreathScale = 1.4f;
        [Tooltip("Dat chay de lai: so vung rai doc theo dai phun, ban kinh moi vung, ton tai bao lau.")]
        public int ultGroundPools = 3;
        public float ultGroundRadius = 1f;
        public float ultGroundSeconds = 3f;
        [Tooltip("Dat chay gay bao nhieu lan D moi giay (0,25 = 25% D).")]
        public float ultGroundShare = 0.25f;

        [Header("Hỏa Bạo (quai chet luc dang chay)")]
        [Tooltip("Ban kinh vu no (world).")]
        public float burstRadius = 1.2f;
        [Tooltip("Vu dau gay bao nhieu lan D; moi luot sau nhan `burstFalloff`.")]
        public float burstPower = 1f;
        public float burstFalloff = 0.5f;
        [Tooltip("Toi da bao nhieu luot no tinh ca vu dau (3 = 1D / 0,5D / 0,25D roi dung).")]
        public int burstChainMax = 3;

        [Header("Hinh")]
        [Tooltip("Ba muc lua theo Long Nộ: thap / trung / cao (moi bo 6 frame, loop).")]
        public Sprite[] breathLow, breathMid, breathHigh;
        [Tooltip("Luong lon cua Long Tức (6 frame, loop).")]
        public Sprite[] breathUlt;
        [Tooltip("Lua bam tren than quai dang chay (4 frame).")]
        public Sprite[] burnFrames;
        [Tooltip("Dat chay de lai sau Long Tức (4 frame).")]
        public Sprite[] groundFrames;
        [Tooltip("Vu no Hỏa Bạo (4 frame).")]
        public Sprite[] burstFrames;
        [Tooltip("Tieng phun lua, lap lai theo nhip ngan trong luc phun.")]
        public string breathSfx = "cannon-fire";
    }
}
