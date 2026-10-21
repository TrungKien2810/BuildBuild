using BuildBuild.Core;
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
        [SerializeField] Button statsButton;
        [SerializeField] ContributionPanel contributionPanel;

        /// <summary>Le an toan tren/duoi vung chu goi y va le hai ben cua bang.</summary>
        const float HintMargin = 12f;
        /// <summary>
        /// Tran chieu cao cua bang: bang moc mep TREN o +210 (tam man 1080) va chi no xuong duoi, nen 680 van con
        /// ~70 px tu mep duoi bang toi mep man — hang nut 76 px cuoi cung van nam gon trong man.
        /// </summary>
        const float MaxPanelHeight = 680f;

        RectTransform _panel;
        float _panelBaseHeight;
        float _panelBaseY;
        Vector2 _hintBaseSize;
        /// <summary>Mep duoi o thuong, toa do tinh tu tam panel (o thuong neo Center).</summary>
        float _rewardBottom;
        /// <summary>Mep tren cua hang nut, tinh tu mep duoi panel (nut neo Bottom).</summary>
        float _buttonTop;

        /// <summary>Nguoi choi bam dong bang (khong tinh `Hide` tu code luc bat dau wave).</summary>
        public event System.Action Closed;

        void Awake()
        {
            // Bo cuc doc tu chinh scene (SceneBuilder.BuildResultPopup) chu khong chep so: bang doi kich thuoc
            // thi cac moc nay tu theo.
            _panel = hintText.rectTransform.parent as RectTransform;
            if (_panel != null)
            {
                _panelBaseHeight = _panel.sizeDelta.y;
                _panelBaseY = _panel.anchoredPosition.y;
            }
            _hintBaseSize = hintText.rectTransform.sizeDelta;
            var reward = (RectTransform)rewardRow.transform;
            _rewardBottom = reward.anchoredPosition.y - reward.sizeDelta.y * reward.pivot.y;
            var button = (RectTransform)closeButton.transform;
            _buttonTop = button.anchoredPosition.y + button.sizeDelta.y;

            closeButton.onClick.AddListener(() =>
            {
                Hide();
                Closed?.Invoke();
            });
            statsButton.onClick.AddListener(OpenStats);
            Hide();
        }

        /// <param name="gold">Vang nhan duoc cua wave vua danh. Thua thi truyen 0 — dong phan thuong se an.</param>
        /// <param name="drop">Icon trang bi vua roi (null = khong roi); `dropName` ghi duoi icon, vd "Nhẫn R".</param>
        public void Show(bool victory, int gold, string hint, Sprite drop = null, string dropName = "")
        {
            Sfx.Play(victory ? "jingle-win" : "jingle-lose");
            bool hasDrop = drop != null;
            if (hasDrop) Sfx.Play("item-drop");
            dropSlot.SetActive(hasDrop);
            dropIcon.sprite = drop;
            dropText.text = dropName;
            var goldRect = (RectTransform)rewardRow.transform;
            goldRect.anchoredPosition = new Vector2(hasDrop ? -80f : 0f, goldRect.anchoredPosition.y);
            ribbon.sprite = victory ? victoryRibbon : defeatRibbon;
            titleText.text = victory ? "Victory" : "Defeat";
            // Chu thang mau navy toi tren long ruy bang thang dac.
            titleText.color = victory ? new Color(0.08f, 0.12f, 0.27f) : new Color(0.86f, 0.86f, 0.90f);
            if (crest != null) crest.sprite = victory ? victoryCrest : defeatCrest;
            statsButton.gameObject.SetActive(true);
            rewardRow.SetActive(gold > 0);
            goldText.text = "+" + gold.ToString("N0");
            hintText.text = hint;
            LayoutHint();
            if (rays != null) rays.SetActive(victory);
            if (sparkle != null) sparkle.SetActive(victory);
            dim.SetActive(true);
            root.SetActive(true);
        }

        /// <summary>
        /// Bang NHAN THUONG khong phai ket qua wave (ruong AFK): ruy bang thang + <paramref name="title"/>, khong nut thong ke.
        /// </summary>
        public void ShowReward(string title, int gold, string hint, Sprite drop = null, string dropName = "")
        {
            Show(true, gold, hint, drop, dropName);
            titleText.text = title;
            statsButton.gameObject.SetActive(false);
        }

        public void Hide()
        {
            root.SetActive(false);
            dim.SetActive(false);
        }

        /// <summary>
        /// Chu goi y dai (loi ket + ghi chu len cap + moc/ky luc = 3-5 dong) truoc day ve tran ra ngoai bang va
        /// de len hang nut: `Label` cua SceneBuilder de `horizontalOverflow = Overflow` nen khong tu xuong dong.
        /// O day cho chu xuong dong trong be rong cua o, do chieu cao that roi keo MEP DUOI bang xuong vua du;
        /// mep tren giu nguyen nen ruy bang / crest khong nhay.
        /// </summary>
        void LayoutHint()
        {
            if (_panel == null) return;
            var rect = hintText.rectTransform;
            hintText.resizeTextForBestFit = false;
            hintText.horizontalOverflow = HorizontalWrapMode.Wrap;
            // Tra o ve be rong cua builder TRUOC khi do: preferredHeight cat dong theo be rong hien tai cua o.
            rect.sizeDelta = _hintBaseSize;

            float needed = Mathf.Max(_hintBaseSize.y, hintText.preferredHeight);
            float bandTop = _rewardBottom - HintMargin;
            float height = Mathf.Clamp(2f * (needed + _buttonTop + HintMargin - bandTop), _panelBaseHeight, MaxPanelHeight);
            float bandBottom = -height * 0.5f + _buttonTop + HintMargin;
            float band = bandTop - bandBottom;

            // Dai hon ca bang cao toi da: thu chu lai cho vua vung, khong de tran ra ngoai panel.
            hintText.resizeTextForBestFit = needed > band;
            if (hintText.resizeTextForBestFit)
            {
                hintText.resizeTextMinSize = 14;
                hintText.resizeTextMaxSize = hintText.fontSize;
            }
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, Mathf.Min(needed, band));
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, (bandTop + bandBottom) * 0.5f);
            _panel.sizeDelta = new Vector2(_panel.sizeDelta.x, height);
            _panel.anchoredPosition = new Vector2(_panel.anchoredPosition.x, _panelBaseY - (height - _panelBaseHeight) * 0.5f);
        }

        void OpenStats()
        {
            root.SetActive(false);
            dim.SetActive(false);
            if (contributionPanel != null) contributionPanel.Show(this);
        }

        public void ShowAgain()
        {
            dim.SetActive(true);
            root.SetActive(true);
        }
    }
}
