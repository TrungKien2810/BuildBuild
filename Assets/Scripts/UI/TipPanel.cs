using BuildBuild.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>
    /// Khung goi y ngu canh (duoi san, tren nut Fight): chan dung + tieu de + noi dung + nut "Got it".
    /// Noi dung va dieu kien hien do `Meta/ContextTips` quyet; lop nay chi ve.
    /// </summary>
    public class TipPanel : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] Text titleText;
        [SerializeField] Text bodyText;
        [SerializeField] Button dismissButton;

        public bool IsOpen => root != null && root.activeSelf;

        string _id;

        void Awake()
        {
            if (dismissButton != null) dismissButton.onClick.AddListener(Dismiss);
            Close();
        }

        public void Show(ContextTip tip)
        {
            if (root == null) return;
            _id = tip.id;
            titleText.text = tip.title;
            bodyText.text = tip.body;
            root.SetActive(true);
        }

        /// <summary>
        /// Danh dau "da xem" o day chu khong phai luc `Show`: goi y bi man phu che (hoac bi dong khi doi scene)
        /// ma van ghi `seenTips` la mat han mot y huong dan moi acc chi co MOT lan.
        /// </summary>
        void Dismiss()
        {
            if (!string.IsNullOrEmpty(_id)) ContextTips.MarkSeen(_id);
            _id = "";
            Close();
        }

        public void Close()
        {
            if (root != null) root.SetActive(false);
        }
    }
}
