using BuildBuild.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>
    /// HUD tran dau: mau lau dai, wave, vang, nut danh wave tiep va nut sang man gacha.
    ///
    /// Nut gacha chi bam duoc khi KHONG dang danh: WaveManager song trong scene Battle, roi scene
    /// giua wave la huy wave do. Giua hai wave (sau thang/thua) thi an toan.
    /// </summary>
    public class BattleHud : MonoBehaviour
    {
        [SerializeField] WaveManager waves;
        [SerializeField] Castle castle;
        [SerializeField] Economy economy;

        [Header("Widget")]
        [SerializeField] Image castleHpFill;
        [SerializeField] Text castleHpText;
        [SerializeField] Text waveText;
        [SerializeField] Text goldText;
        [SerializeField] Text messageText;
        [SerializeField] Button startButton;
        [SerializeField] Text startLabel;
        [SerializeField] Button gachaButton;
        [Tooltip("Bang ket qua sau moi wave (thang/thua + vang). Khong gan thi HUD chi hien dong chu nhu cu.")]
        [SerializeField] ResultPopup resultPopup;

        void Start()
        {
            waves.WaveStarted += _ =>
            {
                ShowMessage("");
                if (resultPopup != null) resultPopup.Hide();
                Refresh();
            };
            waves.WaveCleared += (wave, gold) =>
            {
                string hint = waves.AllCleared
                    ? "Đã qua hết các wave!"
                    : $"Xong wave {wave} — nâng cấp rồi đánh tiếp";
                ShowMessage(hint);
                if (resultPopup != null) resultPopup.Show(true, gold, hint);
                Refresh();
            };
            waves.Defeated += wave =>
            {
                string hint = $"Thua wave {wave} — phát triển thêm rồi đánh lại";
                ShowMessage(hint);
                if (resultPopup != null) resultPopup.Show(false, 0, hint);
                Refresh();
            };
            castle.Health.Damaged += _ => RefreshCastle();
            economy.Changed += _ => RefreshGold();

            startButton.onClick.AddListener(waves.StartWave);
            gachaButton.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.Gacha));

            ShowMessage("");
            Refresh();
        }

        public void Refresh()
        {
            waveText.text = $"Wave {waves.NextWave}";
            startLabel.text = $"Đánh wave {waves.NextWave}";
            startButton.gameObject.SetActive(!waves.IsFighting && !waves.AllCleared);
            gachaButton.interactable = !waves.IsFighting;
            RefreshCastle();
            RefreshGold();
        }

        void RefreshCastle()
        {
            var hp = castle.Health;
            castleHpFill.fillAmount = hp.Fraction;
            castleHpText.text = $"{Mathf.CeilToInt(hp.Current)} / {Mathf.CeilToInt(hp.Max)}";
        }

        void RefreshGold() => goldText.text = economy.Gold.ToString("N0");

        void ShowMessage(string text) => messageText.text = text;
    }
}
