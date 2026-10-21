using BuildBuild.Core;
using BuildBuild.Meta;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>
    /// Man Cai dat (GDD 10.4, hang icon sanh). Ba thu nguoi choi khong co cach nao cham toi truoc day:
    /// am luong nhac (`Sfx.MusicGain`, truoc day la hang so 0,4), tat tieng SFX, va xoa tien trinh
    /// (truoc day phai doi acc o `AdminPanel` F1).
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
        [SerializeField] Button closeButton;
        [SerializeField] Text pathText;

        float _confirmUntil;
        bool _resumeOnClose;

        public bool IsOpen => root != null && root.activeSelf;

        void Awake()
        {
            volumeDownButton.onClick.AddListener(() => SetVolume(Sfx.MusicGain - VolumeStep));
            volumeUpButton.onClick.AddListener(() => SetVolume(Sfx.MusicGain + VolumeStep));
            muteButton.onClick.AddListener(() => { Sfx.Muted = !Sfx.Muted; Refresh(); });
            resetButton.onClick.AddListener(OnReset);
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
                // Nap lai scene de moi he thong doc save moi (cung cach `SaveSystem.SwitchAccount`).
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                return;
            }
            _confirmUntil = Time.unscaledTime + ConfirmSeconds;
            Refresh();
        }

        void Update()
        {
            if (!IsOpen) return;
            // Het gio xac nhan thi tra nut ve trang thai thuong, khong de "Tap again" nam mai.
            if (_confirmUntil > 0f && Time.unscaledTime >= _confirmUntil) Refresh();
        }

        void Refresh()
        {
            bool confirming = Time.unscaledTime < _confirmUntil;
            if (_confirmUntil > 0f && !confirming) _confirmUntil = 0f;

            volumeText.text = $"{Mathf.RoundToInt(Sfx.MusicGain * 100f)}%";
            muteLabel.text = Sfx.Muted ? "Sound: OFF" : "Sound: ON";
            resetLabel.text = confirming ? "Tap again to wipe" : "Reset Progress";
            pathText.text = "Progress is saved automatically on this device.\nReset Progress permanently deletes this account's progress.";
        }
    }
}
