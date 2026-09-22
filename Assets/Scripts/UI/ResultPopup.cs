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

        [Header("Dong phan thuong")]
        [Tooltip("Ca dong (nen + icon vang + so) — thua thi an di, khong hien '+0 vang'.")]
        [SerializeField] GameObject rewardRow;
        [SerializeField] Text goldText;

        [SerializeField] Text hintText;
        [SerializeField] Button closeButton;

        void Awake()
        {
            closeButton.onClick.AddListener(Hide);
            Hide();
        }

        /// <param name="gold">Vang nhan duoc cua wave vua danh. Thua thi truyen 0 — dong phan thuong se an.</param>
        public void Show(bool victory, int gold, string hint)
        {
            ribbon.sprite = victory ? victoryRibbon : defeatRibbon;
            titleText.text = victory ? "CHIẾN THẮNG" : "THẤT BẠI";
            titleText.color = victory ? new Color(0.28f, 0.20f, 0.09f) : new Color(0.86f, 0.86f, 0.90f);
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
