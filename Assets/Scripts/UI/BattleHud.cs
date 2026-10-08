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
        [Tooltip("Pho ban: mo ban do Chinh Phat; dang chuan bi Chinh Phat thi thanh nut Roi Chinh Phat.")]
        [SerializeField] Button conquestButton;
        [SerializeField] Text conquestLabel;
        [Tooltip("Nang thanh tri len cap ke (GDD muc 2) — chi o sanh giua wave.")]
        [SerializeField] Button castleUpgradeButton;
        [SerializeField] Text castleUpgradeLabel;
        [Tooltip("Bang so sanh cap truoc/sau. Khong gan thi nut nang thang nhu cu.")]
        [SerializeField] CastleUpgradePanel castleUpgradePanel;
        [SerializeField] ConquestMapScreen conquestMap;
        [Tooltip("Bang ket qua sau moi wave (thang/thua + vang). Khong gan thi HUD chi hien dong chu nhu cu.")]
        [SerializeField] ResultPopup resultPopup;
        [SerializeField] WaveBiome biome;
        [Tooltip("Roi trang bi khi thang wave (`Armory.RollDrop`).")]
        [SerializeField] EquipmentConfig equipment;

        [Header("Ruong AFK (GDD muc 8)")]
        [Tooltip("Vang AFK/gio tinh theo vang thang wave cao nhat.")]
        [SerializeField] WaveTuning tuning;
        [SerializeField] Button chestButton;
        [SerializeField] Image chestImage;
        [SerializeField] Text chestTimer;
        [SerializeField] Sprite chestClosed;
        [SerializeField] Sprite chestGlowing;
        [SerializeField] Sprite chestFull;
        float _chestRefreshAt;
        /// <summary>Bang ket qua dang mo la cua tran Chinh Phat: dong bang thi ve ban do Chinh Phat, khong ve sanh.</summary>
        bool _backToConquestMap;
        bool _newBiome;   // vua thang wave thuong: dong bang ket qua thi chuyen canh sang nen moi

        void Start()
        {
            waves.WaveStarted += _ =>
            {
                ShowMessage("");
                if (resultPopup != null) resultPopup.Hide();
                if (castleUpgradePanel != null) castleUpgradePanel.Close();
                Refresh();
            };
            waves.WaveCleared += (wave, gold) =>
            {
                string hint = !waves.LastWasConquest ? $"Xong wave {wave} — nâng cấp rồi đánh tiếp"
                    : waves.LastShards > 0 ? $"Hạ thành địch — Chinh Phạt thắng · +{waves.LastShards} mảnh"
                    : "Hạ thành địch — Chinh Phạt thắng";
                _backToConquestMap = waves.LastWasConquest;
                _newBiome = !waves.LastWasConquest;
                var drop = Armory.RollDrop(equipment, wave);
                string dropName = drop != null ? $"{EquipmentConfig.SlotName(drop.slot)} {drop.rarity}" : "";
                // Co popup thi thuong (vang + do roi) nam trong o cua popup, khong in them chu len san.
                if (resultPopup != null)
                    resultPopup.Show(true, gold, hint, drop != null ? equipment.Icon(drop.slot) : null, dropName);
                else
                {
                    ShowMessage(drop != null ? $"{hint}\nNhận trang bị: {dropName}" : hint);
                    AdvanceBiome();
                }
                Refresh();
            };
            waves.Defeated += wave =>
            {
                string hint = waves.LastWasConquest ? "Thành mình thất thủ — Chinh Phạt thua" : $"Thua wave {wave} — phát triển thêm rồi đánh lại";
                _backToConquestMap = waves.LastWasConquest;
                if (resultPopup != null) resultPopup.Show(false, 0, hint);
                else ShowMessage(hint);
                Refresh();
            };
            if (resultPopup != null)
                resultPopup.Closed += () =>
                {
                    AdvanceBiome();
                    if (!_backToConquestMap) return;
                    _backToConquestMap = false;
                    if (conquestMap != null) conquestMap.Open();
                };
            castle.Health.Damaged += _ => RefreshCastle();
            economy.Changed += _ => { RefreshGold(); RefreshCastleUpgrade(); };

            startButton.onClick.AddListener(waves.StartWave);
            gachaButton.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.Gacha));
            unitsButton.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.Units));
            if (chestButton != null) chestButton.onClick.AddListener(ClaimChest);
            if (conquestButton != null) conquestButton.onClick.AddListener(OnConquestButton);
            if (castleUpgradeButton != null) castleUpgradeButton.onClick.AddListener(OnCastleUpgradeButton);
            if (castleUpgradePanel != null) castleUpgradePanel.UpgradePressed += UpgradeCastle;
            castle.TierChanged += Refresh;

            ShowMessage("");
            Refresh();
        }

        void AdvanceBiome()
        {
            if (!_newBiome) return;
            _newBiome = false;
            if (biome != null) biome.Advance();
        }

        public void Refresh()
        {
            waveText.text = WaveManager.InConquest ? "Chinh Phạt" : $"Wave {waves.NextWave}";
            startLabel.text = WaveManager.InConquest ? "Tấn công thành" : $"Đánh wave {waves.NextWave}";
            startButton.gameObject.SetActive(!waves.IsFighting);
            foreach (var button in HiddenWhileFighting) button.gameObject.SetActive(!waves.IsFighting);
            RefreshCastle();
            RefreshGold();
            RefreshChest();
            if (conquestLabel != null) conquestLabel.text = WaveManager.InConquest ? "Rời Chinh Phạt" : "Chinh Phạt";
            RefreshCastleUpgrade();
        }

        Button[] HiddenWhileFighting => new[] { gachaButton, unitsButton, chestButton, conquestButton, castleUpgradeButton };

        void RefreshCastleUpgrade()
        {
            if (castleUpgradeButton == null) return;
            var next = castle.NextTier;
            // Da toi da / scene cu khong co bang cap / dang chuan bi Chinh Phat: an nut.
            if (next == null || WaveManager.InConquest)
            {
                castleUpgradeButton.gameObject.SetActive(false);
                return;
            }
            string blocker = castle.UpgradeBlocker();
            castleUpgradeLabel.text = blocker ?? $"Nâng {next.name} · {next.cost}";
            // Co bang thi nut chi MO bang (luon bam duoc de xem dieu kien); bang tu xam nut Nang.
            castleUpgradeButton.interactable = castleUpgradePanel != null || blocker == null && economy.Gold >= next.cost;
            if (castleUpgradePanel != null && castleUpgradePanel.IsOpen) castleUpgradePanel.Refresh(castle, economy.Gold);
        }

        void OnCastleUpgradeButton()
        {
            if (castleUpgradePanel == null) UpgradeCastle();
            else if (!waves.IsFighting && castle.NextTier != null && !castle.IsUpgrading)
            {
                castleUpgradePanel.Open(castle, economy.Gold);
                Sfx.Play("ui-open");
            }
        }

        void UpgradeCastle()
        {
            var next = castle.NextTier;
            if (waves.IsFighting || next == null || castle.UpgradeBlocker() != null) return;
            if (!economy.TrySpend(next.cost))
            {
                ShowMessage("Không đủ vàng nâng thành");
                return;
            }
            if (castleUpgradePanel != null) castleUpgradePanel.Close();
            castle.Upgrade();
            Sfx.Play("upgrade");
            ShowMessage($"Thành lên cấp {castle.TierLevel}: {next.name}");
        }

        void OnConquestButton()
        {
            if (waves.IsFighting) return;
            if (WaveManager.InConquest)
            {
                waves.LeaveConquest();
                Refresh();
            }
            else if (conquestMap != null) conquestMap.Open();
        }

        // Dong ho tren ruong nhay theo phut — soat moi giay la du, khong can moi frame.
        void Update()
        {
            if (chestButton == null || Time.unscaledTime < _chestRefreshAt) return;
            _chestRefreshAt = Time.unscaledTime + 1f;
            RefreshChest();
        }

        void RefreshChest()
        {
            if (chestButton == null) return;
            float hours = AfkChest.Hours;
            var stage = AfkChest.Current;
            chestImage.sprite = stage == AfkChest.Stage.Full ? chestFull : stage == AfkChest.Stage.Glowing ? chestGlowing : chestClosed;
            int minutes = Mathf.FloorToInt(hours * 60f);
            chestTimer.text = hours >= AfkChest.CapHours ? "ĐẦY" : $"{minutes / 60}g {minutes % 60:00}p";
            chestButton.interactable = AfkChest.PendingGold(tuning) > 0;
        }

        void ClaimChest()
        {
            var reward = AfkChest.Claim(tuning, equipment);
            Sfx.Play("coin");
            economy.Add(reward.gold);
            string dropName = reward.drop != null ? $"{EquipmentConfig.SlotName(reward.drop.slot)} {reward.drop.rarity}" : "";
            string hint = reward.shards > 0 ? $"+{reward.shards} mảnh cho unit ngẫu nhiên" : "Rương tích tối đa 12 giờ";
            if (resultPopup != null)
                resultPopup.ShowReward("RƯƠNG AFK", reward.gold, hint, reward.drop != null ? equipment.Icon(reward.drop.slot) : null, dropName);
            else
                ShowMessage($"Rương AFK: +{reward.gold} vàng" + (dropName != "" ? $", {dropName}" : ""));
            RefreshChest();
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
