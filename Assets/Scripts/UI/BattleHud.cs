using BuildBuild.Core;
using BuildBuild.Data;
using BuildBuild.Meta;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>
    /// HUD tran dau: mau lau dai, wave, vang, nut danh wave tiep va nut sang man gacha.
    ///
    /// Nut nao KHONG dung duoc trong tran thi AN HAN khi dang danh (khong chi xam) de man hinh thoang cho nguoi
    /// choi xem tran — nguoi dung chot 2026-09-29, ap ca cho nut them sau nay: them vao `HiddenWhileFighting`.
    /// Gacha/Doi quan roi scene Battle (huy wave dang danh) nen thuoc nhom nay.
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
        [SerializeField] Button unitsButton;
        [Tooltip("Bang ket qua sau moi wave (thang/thua + vang). Khong gan thi HUD chi hien dong chu nhu cu.")]
        [SerializeField] ResultPopup resultPopup;
        [Tooltip("Roi trang bi khi thang wave (`Armory.RollDrop`).")]
        [SerializeField] EquipmentConfig equipment;

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
                var drop = Armory.RollDrop(equipment, wave);
                if (drop != null) hint += $"\nNhận trang bị: {EquipmentConfig.SlotName(drop.slot)} {drop.rarity}";
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
            unitsButton.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.Units));

            ShowMessage("");
            Refresh();
        }

        public void Refresh()
        {
            waveText.text = $"Wave {waves.NextWave}";
            startLabel.text = $"Đánh wave {waves.NextWave}";
            startButton.gameObject.SetActive(!waves.IsFighting && !waves.AllCleared);
            foreach (var button in HiddenWhileFighting) button.gameObject.SetActive(!waves.IsFighting);
            RefreshCastle();
            RefreshGold();
        }

        Button[] HiddenWhileFighting => new[] { gachaButton, unitsButton };

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
