using BuildBuild.Combat;
using BuildBuild.Core;
using BuildBuild.Data;
using BuildBuild.Meta;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>
    /// HUD tran dau: mau lau dai, wave, vang, cap nguoi choi, 4 nut dieu khien tran, ruong AFK, 2 che do phu,
    /// Town va man Nhiem vu.
    ///
    /// HAI KENH THONG BAO, dung muc dich khac nhau (dung tron):
    ///   - `messageText` — banner giua man: KET QUA tran (nhieu dong), o lai cho toi khi tran sau bat dau;
    ///   - `Toast` — phan hoi ngan, tu tat: "Not enough Gold", "+1 Free Summon", doi uu tien muc tieu.
    /// Truoc day moi thu deu do vao banner nen cai sau xoa cai truoc va popup ket qua che mat.
    ///
    /// File nay dang la HUB cua HUD tran (~400 dong). Neu phai sua tiep, viec tach dau tien nen lam la phan CAP
    /// NGUOI CHOI (`levelText`/`xpFill`/`xpText` + `RefreshPlayer`) ra mot component rieng, khong phai tach bay gio.
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

        [Header("Nguoi choi (GDD 10.0)")]
        [Tooltip("Cap nguoi choi, vd `Lv 3` — `Meta/PlayerLevel`.")]
        [SerializeField] Text levelText;
        [SerializeField] Image xpFill;
        [Tooltip("So XP trong cap hien tai: `27 / 150`.")]
        [SerializeField] Text xpText;
        /// <summary>Ghi chu len cap cua wave dang chay — gan vao bang ket qua thay vi de mot dong rieng bi popup che.</summary>
        string _levelNote;

        [Header("Dieu khien tran (GDD 10.1)")]
        [SerializeField] Button pauseButton;
        [Tooltip("La co dau hang: cung mo bang tam dung o buoc XAC NHAN, khong thua ngay.")]
        [SerializeField] Button surrenderButton;
        [SerializeField] Button speedButton;
        [SerializeField] Text speedLabel;
        [SerializeField] Button menuButton;
        [Tooltip("Bang tam dung — dong thoi la buoc XAC NHAN cho dau hang.")]
        [SerializeField] PausePanel pausePanel;
        [SerializeField] SettingsPanel settingsPanel;
        [Tooltip("Thong bao ngan giua tren man (len cap, thieu vang, ve mien phi).")]
        [SerializeField] Toast toast;
        [Tooltip("Boss Rush: 3 hiep boss lien tiep tren cung mot thanh mau.")]
        [SerializeField] Button bossRushButton;
        [SerializeField] ChallengeScreen challengeScreen;
        [Tooltip("Goi y ngu canh (`Meta/ContextTips`) — moi y hien mot lan trong doi acc.")]
        [SerializeField] TipPanel tipPanel;
        [Tooltip("Thu thach ngay (`Meta/DailyChallenge`) — mot wave, thua thi duoc thu lai trong ngay.")]
        [SerializeField] Button dailyButton;
        [SerializeField] Text dailyLabel;
        [Tooltip("Town (GDD 10.4): hub mo Cua hang + Bang ky nang.")]
        [SerializeField] Button townButton;
        [SerializeField] TownScreen townScreen;
        [Tooltip("Uu tien muc tieu cua don danh thuong (GDD 10.1 nut Auto).")]
        [SerializeField] Button targetButton;
        [SerializeField] Text targetLabel;
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
        [Tooltip("Man Nhiem vu Hanh trinh (GDD muc 11), mo tu nut Quests.")]
        [SerializeField] QuestScreen questScreen;
        [SerializeField] Button questButton;
        [Tooltip("Cham do tren nut Quests: con o bam nhan duoc hoac con ve chon tuong chua dung.")]
        [SerializeField] GameObject questDot;
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
                _levelNote = null;
                if (resultPopup != null) resultPopup.Hide();
                if (castleUpgradePanel != null) castleUpgradePanel.Close();
                if (pausePanel != null && pausePanel.IsOpen) pausePanel.Resume();
                if (settingsPanel != null && settingsPanel.IsOpen) settingsPanel.Close();
                Refresh();
            };
            waves.WaveCleared += (wave, gold) =>
            {
                string hint = waves.LastWasBossRush ? "Boss Rush Cleared — three bosses on one line"
                    : waves.LastWasDaily ? "Daily Challenge Cleared — back tomorrow for a new one"
                    : !waves.LastWasConquest ? $"Wave {wave} Complete — upgrade, then keep fighting"
                    : waves.LastShards > 0 ? $"Enemy Castle Defeated — Conquest Victory · +{waves.LastShards} Shards"
                    : "Enemy Castle Defeated — Conquest Victory";
                _backToConquestMap = waves.LastWasConquest;
                _newBiome = !waves.LastWasConquest && !waves.LastWasBossRush && !waves.LastWasDaily;
                // Hai che do phu thuong theo luat wave 30 — 30 chia het cho `EquipmentConfig.guaranteedEvery`
                // (10) nen CHAC CHAN roi do, dung nhu loi man chuan bi hua. Truoc day `Mathf.Max(wave, 30)`
                // khong bao dam boi so 10 (acc dang o wave 42 thi Boss Rush lai 35%).
                int dropWave = waves.LastWasBossRush || waves.LastWasDaily ? 30 : wave;
                var drop = Armory.RollDrop(equipment, dropWave);
                string dropName = drop != null ? $"{EquipmentConfig.SlotName(drop.slot)} {drop.rarity}" : "";
                // Len cap da cong vang rieng (`OnPlayerLevelUp`) — o day chi ghi chu vao bang ket qua.
                if (_levelNote != null) { hint += "\n" + _levelNote; _levelNote = null; }
                // Moc sau wave 30 / ky luc moi (`WaveMilestones`): vang da cong, day chi la dong thong bao.
                if (!string.IsNullOrEmpty(waves.LastBonus))
                {
                    hint += "\n" + waves.LastBonus;
                    ShowToast(waves.LastBonus);
                }
                // Co popup thi thuong (vang + do roi) nam trong o cua popup, khong in them chu len san.
                if (resultPopup != null)
                    resultPopup.Show(true, gold, hint, drop != null ? equipment.Icon(drop.slot) : null, dropName);
                else
                {
                    ShowMessage(drop != null ? $"{hint}\nEquipment Received: {dropName}" : hint);
                    AdvanceBiome();
                }
                Refresh();
                // Con popup thi goi y phai doi popup dong: `tipPanel` duoc dung TRUOC cac man phu nen nam DUOI,
                // va `ContextTips.MarkSeen` chi chay khi nguoi choi bam "Got it" — hien ngay luc nay la mat luon.
                if (resultPopup == null) ShowNextTip();
            };
            waves.Defeated += wave =>
            {
                string hint = waves.LastWasBossRush ? "Your Castle Fell — Boss Rush Failed"
                    : waves.LastWasDaily ? "Daily Challenge Failed — you can retry today"
                    : waves.LastWasConquest ? "Your Castle Fell — Conquest Defeat"
                    : $"Wave {wave} Lost — grow stronger, then retry";
                _backToConquestMap = waves.LastWasConquest;
                if (resultPopup != null) resultPopup.Show(false, 0, hint);
                else ShowMessage(hint);
                Refresh();
                if (resultPopup == null) ShowNextTip();
            };
            if (resultPopup != null)
                resultPopup.Closed += () =>
                {
                    AdvanceBiome();
                    if (_backToConquestMap)
                    {
                        _backToConquestMap = false;
                        if (conquestMap != null) conquestMap.Open();
                        return;   // ban do Chinh Phat vua mo va no cung phu kin man: goi y se hien o luot sau
                    }
                    ShowNextTip();
                };
            castle.Health.Damaged += _ => RefreshCastle();
            economy.Changed += _ => { RefreshGold(); RefreshCastleUpgrade(); };

            startButton.onClick.AddListener(waves.StartWave);
            gachaButton.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.Gacha));
            unitsButton.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.Units));
            if (chestButton != null) chestButton.onClick.AddListener(ClaimChest);
            if (conquestButton != null) conquestButton.onClick.AddListener(OnConquestButton);
            if (questButton != null) questButton.onClick.AddListener(() => { if (questScreen != null) questScreen.Open(); });
            if (bossRushButton != null) bossRushButton.onClick.AddListener(OnBossRush);
            if (dailyButton != null) dailyButton.onClick.AddListener(OnDaily);
            if (townButton != null) townButton.onClick.AddListener(() => { if (townScreen != null) townScreen.Open(); });
            if (targetButton != null) targetButton.onClick.AddListener(() =>
            {
                Targeting.Cycle();
                ShowToast($"Targeting: {Targeting.Description}");
            });
            Targeting.Changed += RefreshTargeting;
            RefreshTargeting();
            if (castleUpgradeButton != null) castleUpgradeButton.onClick.AddListener(OnCastleUpgradeButton);
            if (castleUpgradePanel != null) castleUpgradePanel.UpgradePressed += UpgradeCastle;
            castle.TierChanged += Refresh;
            PlayerLevel.Changed += RefreshPlayer;
            PlayerLevel.LeveledUp += OnPlayerLevelUp;

            if (pauseButton != null) pauseButton.onClick.AddListener(OpenPause);
            if (surrenderButton != null) surrenderButton.onClick.AddListener(() =>
            {
                if (pausePanel != null && waves.IsFighting) pausePanel.Open(true, true);
            });
            if (speedButton != null) speedButton.onClick.AddListener(GameSpeed.Cycle);
            if (menuButton != null) menuButton.onClick.AddListener(OpenSettings);
            if (pausePanel != null) pausePanel.SurrenderPressed += OnSurrender;
            GameSpeed.Changed += RefreshSpeed;
            RefreshSpeed();

            ShowMessage("");
            Refresh();
            if (TownScreen.ConsumeReturn() && townScreen != null) townScreen.Open();
            else ShowNextTip();
        }

        void OnBossRush()
        {
            if (waves.IsFighting || WaveManager.InConquest) return;
            if (resultPopup != null) resultPopup.Hide();
            challengeScreen.Open(false);
            Refresh();
        }

        public void GoToQuest(QuestGoal goal)
        {
            if (goal == QuestGoal.OwnHeroes || goal == QuestGoal.GachaRolls)
                SceneManager.LoadScene(SceneNames.Gacha);
            else if (goal == QuestGoal.MaxHeroLevel)
                SceneManager.LoadScene(SceneNames.Units);
            else if (goal == QuestGoal.ConquestWins)
                OnConquestButton();
            else
                ShowToast(goal == QuestGoal.AfkClaims ? "Open the AFK chest in the lobby to claim stored Gold." : "Prepare your army, then press Start to clear campaign waves.");
        }

        void OnDaily()
        {
            if (waves.IsFighting || WaveManager.InConquest) return;
            if (resultPopup != null) resultPopup.Hide();
            challengeScreen.Open(true);
            Refresh();
        }

        /// <summary>Hien goi y ngu canh ke tiep (moi y mot lan trong doi acc) — xem `Meta/ContextTips`.</summary>
        void ShowNextTip()
        {
            if (tipPanel == null || tipPanel.IsOpen) return;
            var tip = ContextTips.Next();
            if (tip == null) return;
            tipPanel.Show(tip.Value);
        }

        void OnPlayerLevelUp(int level)
        {
            // Vang do `WaveManager.PayLevelReward` tra (Core), o day chi hien thi — HUD khong duoc tu cong tien.
            int gold = PlayerLevel.GoldFor(level);
            string note = $"Player Level {level}! +{gold} Gold";
            if (PlayerLevel.GrantsFreeRoll(level)) note += "  ·  +1 Free Summon";
            _levelNote = _levelNote == null ? note : _levelNote + "  ·  " + note;
            ShowToast(note);
            RefreshPlayer();
        }

        void ShowToast(string text)
        {
            if (toast != null) toast.Show(text);
        }

        void OpenPause()
        {
            // Ngoai tran thi khong co gi de dung: bam co luc do se dong bang ca sanh (timeScale 0) ma chang co
            // tac dung gi — 4 nut dieu khien tran vi vay cung bi an khi khong danh (`RefreshBattleControls`).
            if (pausePanel == null || !waves.IsFighting) return;
            pausePanel.Open(true);
        }

        void OpenSettings()
        {
            if (settingsPanel != null) settingsPanel.Open();
        }

        void OnSurrender()
        {
            if (pausePanel != null) pausePanel.Close();
            GameSpeed.Resume();
            waves.Surrender();
            if (resultPopup != null) resultPopup.Hide();
            _backToConquestMap = false;
            ShowToast("Returned to lobby — gold, items and levels kept");
        }

        void RefreshSpeed()
        {
            if (speedLabel != null) speedLabel.text = GameSpeed.Label;
        }

        void RefreshTargeting()
        {
            // Ghi ca ten che do: icon vong tron + "Near" khong noi duoc day la nut chon muc tieu.
            if (targetLabel != null) targetLabel.text = $"Auto {Targeting.ShortLabel}";
        }

        void RefreshPlayer()
        {
            if (levelText == null) return;
            levelText.text = $"Lv {PlayerLevel.Level}";
            int need = PlayerLevel.XpFor(PlayerLevel.Level);
            if (xpText != null) xpText.text = $"{PlayerLevel.Xp} / {need}";
            if (xpFill != null) xpFill.fillAmount = need > 0 ? Mathf.Clamp01((float)PlayerLevel.Xp / need) : 0f;
        }

        void OnDestroy()
        {
            // Su kien static: khong go ra thi ban ghi sang scene sau vo mot component da huy.
            PlayerLevel.Changed -= RefreshPlayer;
            PlayerLevel.LeveledUp -= OnPlayerLevelUp;
            GameSpeed.Changed -= RefreshSpeed;
            Targeting.Changed -= RefreshTargeting;
            if (pausePanel != null) pausePanel.SurrenderPressed -= OnSurrender;
        }

        void AdvanceBiome()
        {
            if (!_newBiome) return;
            _newBiome = false;
            if (biome != null) biome.Advance();
        }

        public void Refresh()
        {
            // Che do phu dung chung mot thanh: bang Stage phai noi dung dang choi cai gi, khong thi Boss Rush
            // hien "Wave 5" trong khi that ra dang danh ba con boss.
            waveText.text = WaveManager.InConquest ? "Conquest"
                : WaveManager.InBossRush ? "Boss Rush"
                : WaveManager.InDaily ? "Daily" : $"Wave {waves.NextWave}";
            startLabel.text = WaveManager.InConquest ? "Attack Castle" : $"Fight Wave {waves.NextWave}";
            startButton.gameObject.SetActive(!waves.IsFighting);
            foreach (var button in HiddenWhileFighting) button.gameObject.SetActive(!waves.IsFighting);
            RefreshBattleControls();
            RefreshCastle();
            RefreshGold();
            RefreshPlayer();
            RefreshTargeting();
            RefreshChest();
            if (conquestLabel != null) conquestLabel.text = WaveManager.InConquest ? "Leave Conquest" : "Conquest";
            // Boss Rush khong danh trong luc dang chuan bi Chinh Phat (hai che do dung chung mot thanh).
            if (bossRushButton != null) bossRushButton.interactable = !WaveManager.InConquest;
            // Thu thach ngay: thang roi thi khoa nut den ngay mai (thua thi van bam lai duoc).
            if (dailyButton != null)
            {
                dailyButton.interactable = !WaveManager.InConquest;
                if (dailyLabel != null) dailyLabel.text = DailyChallenge.Cleared ? "Done" : "Daily";
            }
            RefreshCastleUpgrade();
            if (questDot != null && questScreen != null) questDot.SetActive(questScreen.HasClaimable);
        }

        Button[] HiddenWhileFighting => new[] { gachaButton, unitsButton, chestButton, conquestButton, castleUpgradeButton, questButton, bossRushButton, dailyButton, townButton };

        /// <summary>
        /// 4 nut dieu khien tran chi co nghia khi dang danh. Truoc day chung hien ca o sanh: bam co o sanh thi
        /// dong bang ca man (timeScale 0) du khong co tran nao, con la co / nut auto thi khong ai hieu de lam gi.
        /// </summary>
        void RefreshBattleControls()
        {
            bool fighting = waves.IsFighting;
            if (pauseButton != null) pauseButton.gameObject.SetActive(fighting);
            if (surrenderButton != null) surrenderButton.gameObject.SetActive(fighting);
            if (speedButton != null) speedButton.gameObject.SetActive(fighting);
            if (targetButton != null) targetButton.gameObject.SetActive(fighting);
        }

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
            castleUpgradeLabel.text = blocker ?? $"Upgrade {next.name} · {next.cost}";
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
                // Chi toast: truoc day dong nay viet ca vao banner lan toast nen thong bao hien hai lan.
                ShowToast("Not enough Gold");
                return;
            }
            if (castleUpgradePanel != null) castleUpgradePanel.Close();
            castle.Upgrade();
            Sfx.Play("upgrade");
            ShowMessage($"Fence reached level {castle.TierLevel}: {next.name}");
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
            chestTimer.text = hours >= AfkChest.CapHours ? "Full" : $"{minutes / 60}g {minutes % 60:00}p";
            chestButton.interactable = AfkChest.PendingGold(tuning) > 0;
        }

        void ClaimChest()
        {
            var reward = AfkChest.Claim(tuning, equipment);
            Sfx.Play("coin");
            economy.Add(reward.gold);
            string dropName = reward.drop != null ? $"{EquipmentConfig.SlotName(reward.drop.slot)} {reward.drop.rarity}" : "";
            string hint = reward.shards > 0 ? $"+{reward.shards} Shards for a random unit" : "Chest stores up to 12 hours";
            if (resultPopup != null)
                resultPopup.ShowReward("AFK Chest", reward.gold, hint, reward.drop != null ? equipment.Icon(reward.drop.slot) : null, dropName);
            else
                ShowMessage($"AFK Chest: +{reward.gold} Gold" + (dropName != "" ? $", {dropName}" : ""));
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
