using BuildBuild.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>
    /// The cua day huong dan tan thu (duoi san, tren nut Fight): "2 / 8" + tieu de + noi dung + nut can chi.
    ///
    /// Noi dung va buoc nao do `Meta/Tutorial` quyet; lop nay chi ve, va KHONG tu quyet gi tien buoc:
    /// - "Got it" -> `Tutorial.Dismiss` (bo qua buoc nay, sang buoc sau);
    /// - "Skip guide" -> `Tutorial.Skip` (bo ca day).
    /// Viec nguoi choi LAM dung viec (bam Fight, mo ruong...) cung tien buoc, nhung do `BattleHud` goi
    /// `Tutorial.Complete` — the nay khong nghe su kien gameplay nao.
    /// </summary>
    public class TipPanel : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [Tooltip("\"2 / 8\" — buoc dang xem tren tong so buoc.")]
        [SerializeField] Text stepText;
        [SerializeField] Text titleText;
        [SerializeField] Text bodyText;
        [SerializeField] Button dismissButton;
        [Tooltip("Bo ca day huong dan. Khong bat buoc: scene cu chua co nut nay thi chi thieu duong bo qua ca day.")]
        [SerializeField] Button skipButton;

        readonly TutorialPointer _pointer = new TutorialPointer();

        public bool IsOpen => root != null && root.activeSelf;

        void Awake()
        {
            if (dismissButton != null) dismissButton.onClick.AddListener(Dismiss);
            if (skipButton != null) skipButton.onClick.AddListener(SkipGuide);
            Close();
        }

        public void Show(TutorialStep step, int index, int count, Transform target)
        {
            if (root == null) return;
            if (stepText != null) stepText.text = $"{index + 1} / {count}";
            if (titleText != null) titleText.text = step.title;
            if (bodyText != null) bodyText.text = step.body;
            root.SetActive(true);
            _pointer.PointAt(target);
        }

        /// <summary>
        /// Dong the bang nut "Got it": ghi nhan buoc da qua. KHONG tu mo the ke tiep — nguoi choi vua doc
        /// xong mot the, mo ngay the thu hai la hai chi dan lien tiep dan vao mat; the sau hien o luot
        /// nghi ke tiep (`BattleHud.ShowNextTip`).
        /// </summary>
        void Dismiss()
        {
            Tutorial.Dismiss();
            Close();
        }

        void SkipGuide()
        {
            Tutorial.Skip();
            Close();
        }

        void Update() => _pointer.Tick();

        public void Close()
        {
            if (root != null) root.SetActive(false);
            _pointer.Release();
        }
    }
}
