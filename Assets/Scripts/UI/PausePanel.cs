using System;
using BuildBuild.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>
    /// Bang tam dung (GDD muc 10.1, nut co va co dau hang). Dung `GameSpeed` de dung ca tran — khong tu
    /// ghi `Time.timeScale` de khong ghi de muc tua nguoi choi da chon.
    ///
    /// Leave Battle hien canh bao truoc khi xac nhan: tinh thua va tro ve sanh, giu vang/do/cap.
    /// Nut co ngoai HUD mo thang buoc xac nhan; nut Pause mo Resume / Leave Battle.
    /// </summary>
    public class PausePanel : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] Button resumeButton;
        [SerializeField] Button surrenderButton;
        [SerializeField] Text surrenderLabel;
        [SerializeField] Text titleText;
        [SerializeField] Text hintText;
        [SerializeField] Text resumeLabel;
        bool _confirmingLeave;

        /// <summary>Nguoi choi da xac nhan dau hang — `BattleHud` goi `WaveManager.Surrender()`.</summary>
        public event Action SurrenderPressed;

        public bool IsOpen => root != null && root.activeSelf;

        void Awake()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (surrenderButton != null) surrenderButton.onClick.AddListener(RequestLeave);
            Close();
        }

        public void Open(bool canSurrender, bool confirmLeave = false)
        {
            root.SetActive(true);
            if (surrenderButton != null) surrenderButton.gameObject.SetActive(canSurrender);
            _confirmingLeave = confirmLeave;
            RefreshLabels();
            GameSpeed.Pause();
        }

        void RequestLeave()
        {
            if (_confirmingLeave) SurrenderPressed?.Invoke();
            else
            {
                _confirmingLeave = true;
                RefreshLabels();
            }
        }

        void RefreshLabels()
        {
            if (titleText != null) titleText.text = _confirmingLeave ? "Leave battle?" : "Paused";
            if (hintText != null) hintText.text = _confirmingLeave
                ? "This battle will count as a defeat.\nGold, items and levels are kept.\nReturn to the lobby?"
                : "The battle is paused.\nResume when you are ready.";
            if (resumeLabel != null) resumeLabel.text = _confirmingLeave ? "Keep Fighting" : "Resume";
            if (surrenderLabel != null) surrenderLabel.text = _confirmingLeave ? "Confirm Leave" : "Leave Battle";
        }

        public void Close()
        {
            if (root != null) root.SetActive(false);
        }

        /// <summary>Dong bang VA chay lai tran. Cac nut khac (dau hang) tu goi `GameSpeed.Resume` truoc.</summary>
        public void Resume()
        {
            Close();
            GameSpeed.Resume();
        }
    }
}
