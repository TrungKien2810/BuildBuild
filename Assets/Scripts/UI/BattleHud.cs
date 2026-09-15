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

        void Start()
        {
            waves.WaveStarted += _ => { ShowMessage(""); Refresh(); };
            waves.WaveCleared += (wave, gold) =>
            {
                ShowMessage(waves.AllCleared
                    ? "Chiến thắng — đã qua hết các wave!"
                    : $"Thắng wave {wave}: +{gold:N0} vàng — nâng cấp rồi đánh tiếp");
                Refresh();
            };
            waves.Defeated += wave => { ShowMessage($"Thua wave {wave} — phát triển thêm rồi đánh lại"); Refresh(); };
            castle.Health.Damaged += _ => RefreshCastle();
            economy.Changed += _ => RefreshGold();

            startButton.onClick.AddListener(waves.StartWave);
            gachaButton.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.Gacha));

            ShowMessage("");
            Refresh();
        }

        public void Refresh()
        {
            waveText.text = $"{waves.NextWave}";
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
