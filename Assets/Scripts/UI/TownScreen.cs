using BuildBuild.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using BuildBuild.Meta;

namespace BuildBuild.UI
{
    /// <summary>
    /// Town (GDD 10.4): man HUB mo tu HUD sanh, gom cac cong trinh con. Hien co 2: **Shop** va **Skill Board**.
    ///
    /// Shop la SCENE rieng (`SceneNames.Shop`, tu 2026-10-20) vi cua hang co nhieu loai hang (daily/tuan/thang/
    /// uu dai) — nhet bon tab vao mot bang phu trong scene tran thi vua chat HUD vua kho mo rong. Bam cong trinh
    /// Shop la doi scene, Back to Town tai mo hub qua co ConsumeReturn. Skill Board van la bang phu.
    ///
    /// Thu tu ve: hub duoc dung TRUOC man con trong hierarchy, nen man con nam DE tren — mo man con thi hub van o
    /// duoi, dong man con la thay hub lai (khong phai mo lai tu dau).
    /// </summary>
    public class TownScreen : MonoBehaviour
    {
        static bool _returnFromShop;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetReturn() => _returnFromShop = false;

        public static void ReturnFromShop()
        {
            _returnFromShop = true;
            SceneManager.LoadScene(SceneNames.Battle);
        }

        public static bool ConsumeReturn()
        {
            bool pending = _returnFromShop;
            _returnFromShop = false;
            return pending;
        }
        [SerializeField] GameObject root;
        [SerializeField] Button shopButton;
        [SerializeField] Button boardButton;
        [SerializeField] Button closeButton;
        [SerializeField] SkillBoardScreen board;
        [SerializeField] Text progressText;

        public bool IsOpen => root != null && root.activeSelf;

        void Awake()
        {
            if (shopButton != null) shopButton.onClick.AddListener(OpenShop);
            if (boardButton != null) boardButton.onClick.AddListener(OpenBoard);
            closeButton.onClick.AddListener(Close);
            if (board != null) board.Upgraded += RefreshProgress;
            Close();
        }

        public void Open()
        {
            if (root == null) return;
            root.SetActive(true);
            RefreshProgress();
        }

        void RefreshProgress()
        {
            if (progressText != null) progressText.text = $"Army training: {SkillBoard.Rank(BoardNode.Attack) + SkillBoard.Rank(BoardNode.Health) + SkillBoard.Rank(BoardNode.Haste)}/30 ranks learned  |  {SkillBoard.Summary()}";
        }

        void OnDestroy()
        {
            if (board != null) board.Upgraded -= RefreshProgress;
        }

        public void Close()
        {
            if (root != null) root.SetActive(false);
            // Dong hub thi dong luon man con: lan sau mo Town phai thay dung man hub, khong roi vao bang ky nang.
            if (board != null && board.IsOpen) board.Close();
        }

        void OpenShop()
        {
            Sfx.Play("ui-open");
            SceneManager.LoadScene(SceneNames.Shop);
        }

        void OpenBoard()
        {
            if (board == null) return;
            Sfx.Play("ui-open");
            board.Open();
        }
    }
}
