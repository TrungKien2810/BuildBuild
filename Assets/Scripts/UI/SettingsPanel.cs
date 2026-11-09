using BuildBuild.Core;
using BuildBuild.Meta;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>
    /// Man Cai dat (GDD 10.4, hang icon sanh). Bon thu nguoi choi khong co cach nao cham toi truoc day:
    /// am luong nhac (`Sfx.MusicGain`, truoc day la hang so 0,4), tat tieng SFX, xoa tien trinh (truoc day
    /// phai doi acc o `AdminPanel` F1), va Log out ve man dang nhap (`Meta/Accounts`).
    ///
    /// Xoa tien trinh la thao tac KHONG the hoan tac nen nut phai bam HAI lan: lan dau doi chu thanh
    /// "Tap again to wipe", qua 3 giay khong bam thi tu tro lai.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        const float VolumeStep = 0.1f;
        const float ConfirmSeconds = 3f;

        [SerializeField] GameObject root;
        [SerializeField] Text volumeText;
        [SerializeField] Button volumeDownButton;
        [SerializeField] Button volumeUpButton;
        [SerializeField] Button muteButton;
        [SerializeField] Text muteLabel;
        [SerializeField] Button resetButton;
        [SerializeField] Text resetLabel;
        [Tooltip("Choi lai day huong dan tan thu tu buoc 1. Khong bat buoc: scene cu chua co nut nay thi an.")]
        [SerializeField] Button guideButton;
        [SerializeField] Button logoutButton;
        [SerializeField] Button closeButton;
        [SerializeField] Text pathText;

        float _confirmUntil;
        /// <summary>Han bam lai cua nut Log Out — cung ly do voi `_confirmUntil` (bo tran dang danh la khong hoan tac).</summary>
        float _logoutUntil;
        bool _resumeOnClose;

        public bool IsOpen => root != null && root.activeSelf;

        void Awake()
        {
            volumeDownButton.onClick.AddListener(() => SetVolume(Sfx.MusicGain - VolumeStep));
            volumeUpButton.onClick.AddListener(() => SetVolume(Sfx.MusicGain + VolumeStep));
            muteButton.onClick.AddListener(() => { Sfx.Muted = !Sfx.Muted; Refresh(); });
            resetButton.onClick.AddListener(OnReset);
            if (guideButton != null) guideButton.onClick.AddListener(OnReplayGuide);
            logoutButton.onClick.AddListener(OnLogout);
            closeButton.onClick.AddListener(Close);
            Close();
        }

        public void Open()
        {
            if (IsOpen) return;
            root.SetActive(true);
            _resumeOnClose = !GameSpeed.Paused;
            GameSpeed.Pause();
            _confirmUntil = 0f;
            _logoutUntil = 0f;
            Refresh();
        }

        public void Close()
        {
            if (root != null) root.SetActive(false);
            if (_resumeOnClose) GameSpeed.Resume();
            _resumeOnClose = false;
            _confirmUntil = 0f;
        }

        void SetVolume(float value)
        {
            Sfx.MusicGain = value;
            Refresh();
        }

        void OnReset()
        {
            if (Time.unscaledTime < _confirmUntil)
            {
                Close();
                SaveSystem.ResetProgress();
                // Nap lai scene de moi he thong doc save moi (cung cach doi acc o `AdminPanel`).
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                return;
            }
            _confirmUntil = Time.unscaledTime + ConfirmSeconds;
            Refresh();
        }

        /// <summary>
        /// Dang xuat cung la thao tac KHONG hoan tac (bo tran dang danh, phai dang nhap lai moi choi tiep) nen cung
        /// phai bam HAI lan nhu nut Xoa tien trinh ngay canh no — truoc day mot lan bam la mat tran.
        /// </summary>
        void OnLogout()
        {
            if (Time.unscaledTime < _logoutUntil)
            {
                Logout();
                return;
            }
            _logoutUntil = Time.unscaledTime + ConfirmSeconds;
            Sfx.Play("ui-open");
            Refresh();
        }

        /// <summary>
        /// Choi lai day huong dan (`Meta/Tutorial`). Chi dat co chu khong mo the ngay: HUD chi hien the khi
        /// da thoat tran va da dong bang nay, neu khong the se nam duoi lop phu roi bi bo qua trong im lang.
        /// </summary>
        void OnReplayGuide()
        {
            Tutorial.Restart();
            Sfx.Play("ui-open");
            Refresh();
        }

        /// <summary>Ve man dang nhap. Khong xoa gi: save cua acc van nam trong file `save-&lt;ten acc&gt;.json`.</summary>
        void Logout()
        {
            Close();
            // Chot tran dang danh TRUOC khi xoa phien: neu de WaveManager.OnDisable tu xu ly thi ket qua thua
            // Chinh Phat bi ghi vao save "main" thay vi acc that.
            if (WaveManager.Current != null) WaveManager.Current.Surrender();
            Accounts.Logout();
            GameSpeed.Resume();
            SceneManager.LoadScene(SceneNames.Login);
        }

        void Update()
        {
            if (!IsOpen) return;
            // Het gio xac nhan thi tra nut ve trang thai thuong, khong de "Tap again" nam mai.
            bool expired = _confirmUntil > 0f && Time.unscaledTime >= _confirmUntil
                || _logoutUntil > 0f && Time.unscaledTime >= _logoutUntil;
            if (expired) Refresh();
        }

        void Refresh()
        {
            bool confirming = Time.unscaledTime < _confirmUntil;
            if (_confirmUntil > 0f && !confirming) _confirmUntil = 0f;
            bool signingOut = Time.unscaledTime < _logoutUntil;
            if (_logoutUntil > 0f && !signingOut) _logoutUntil = 0f;

            volumeText.text = $"{Mathf.RoundToInt(Sfx.MusicGain * 100f)}%";
            muteLabel.text = Sfx.Muted ? "Sound: OFF" : "Sound: ON";
            resetLabel.text = confirming ? "Tap again to wipe" : "Reset Progress";
            // Nut Log Out khong co tham chieu chu trong scene (dung chung khung nut), nen loi nhac bam-lan-hai
            // nam o dong trang thai ngay duoi no thay vi doi chu tren nut.
            pathText.text = signingOut
                ? "Tap Log Out again to sign out.\nThe battle in progress is abandoned — Gold, items and levels are kept."
                : Tutorial.RestartPending
                    ? "Guide restarted — close Settings and step 1 will show.\nIt replays on this account only."
                    : $"Signed in as {Accounts.Current}.\nProgress is saved on this device, per account.\nReset Progress wipes this account only.";
        }
    }
}
