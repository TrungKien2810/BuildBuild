using UnityEngine;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>
    /// Bang thong bao ket qua sau moi wave: THANG hay THUA, va thang thi duoc bao nhieu vang.
    ///
    /// Truoc day hai thong tin nay chi la mot dong chu chay ngang giua man (`BattleHud.messageText`),
    /// nguoi choi de bo qua. Bang nay chan man bang mot lop toi (`dim`) nen phai bam "Tiep tuc" moi
    /// danh wave sau — do la cho de nguoi choi doc so vang roi quyet dinh nang cap.
    ///
    /// Sprite lay tu `Assets/Art/Generated/UI/panels/` (dot UI thu 3). Ruy bang thang/thua dung CHUNG
    /// mot `Image`, chi doi sprite — hai file da duoc cat trung khit bbox (80x27) nen doi khong xe khung.
    /// Crest thang/thua (`lobby/victory-crest`, `lobby/defeat-crest`, dot UI C 5) cung dung chung mot `Image`.
    /// </summary>
    public class ResultPopup : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [Tooltip("Lop toi phu kin man — vua lam noi bang vua chan bam nham xuong san ben duoi.")]
        [SerializeField] GameObject dim;
        [Tooltip("Vong tia sang sau bang. Chi hien khi THANG.")]
        [SerializeField] GameObject rays;
        [SerializeField] GameObject sparkle;

        [Header("Ruy bang ket qua")]
        [SerializeField] Image ribbon;
        [SerializeField] Sprite victoryRibbon;
        [SerializeField] Sprite defeatRibbon;
        [SerializeField] Text titleText;
        [SerializeField] Image crest;
        [SerializeField] Sprite victoryCrest;
        [SerializeField] Sprite defeatCrest;

        [Header("Dong phan thuong")]
        [Tooltip("O thuong (khung reward-slot + icon vang + so) — thua thi an di, khong hien '+0 vang'.")]
        [SerializeField] GameObject rewardRow;
        [SerializeField] Text goldText;
        [Tooltip("O do roi (khung reward-slot-rare + icon trang bi + ten). Khong roi do thi an, o vang ve giua.")]
        [SerializeField] GameObject dropSlot;
        [SerializeField] Image dropIcon;
        [SerializeField] Text dropText;

        [SerializeField] Text hintText;
        [SerializeField] Button closeButton;

        void Awake()
        {
            closeButton.onClick.AddListener(Hide);
            Hide();
        }

        /// <param name="gold">Vang nhan duoc cua wave vua danh. Thua thi truyen 0 — dong phan thuong se an.</param>
        /// <param name="drop">Icon trang bi vua roi (null = khong roi); `dropName` ghi duoi icon, vd "Nhẫn R".</param>
        public void Show(bool victory, int gold, string hint, Sprite drop = null, string dropName = "")
        {
            bool hasDrop = drop != null;
            dropSlot.SetActive(hasDrop);
            dropIcon.sprite = drop;
            dropText.text = dropName;
            var goldRect = (RectTransform)rewardRow.transform;
            goldRect.anchoredPosition = new Vector2(hasDrop ? -80f : 0f, goldRect.anchoredPosition.y);
            ribbon.sprite = victory ? victoryRibbon : defeatRibbon;
            titleText.text = victory ? "CHIẾN THẮNG" : "THẤT BẠI";
            // Chu thang mau kem sang: long ruy bang thang trong suot, tia sang vang cua crest nam ngay sau chu.
            titleText.color = victory ? new Color(1f, 0.95f, 0.78f) : new Color(0.86f, 0.86f, 0.90f);
            if (crest != null) crest.sprite = victory ? victoryCrest : defeatCrest;
            rewardRow.SetActive(gold > 0);
            goldText.text = "+" + gold.ToString("N0");
            hintText.text = hint;
            if (rays != null) rays.SetActive(victory);
            if (sparkle != null) sparkle.SetActive(victory);
            dim.SetActive(true);
            root.SetActive(true);
        }

        public void Hide()
        {
            root.SetActive(false);
            dim.SetActive(false);
        }
    }
}
