using System;
using BuildBuild.Core;
using BuildBuild.Data;
using BuildBuild.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    public class ChallengeScreen : MonoBehaviour
    {
        [SerializeField] WaveManager waves;
        [SerializeField] WaveTuning tuning;
        [SerializeField] GameObject root;
        [SerializeField] Text title, subtitle, rules, reward, status, startLabel;
        [SerializeField] Text[] nodeNames, nodeStates;
        [SerializeField] Image[] nodeIcons;
        [SerializeField] Sprite[] dailyIcons;
        [SerializeField] Button startButton, closeButton;
        bool _daily;
        float _refreshAt;
        string _previewDay;

        void Awake()
        {
            startButton.onClick.AddListener(Begin);
            closeButton.onClick.AddListener(Close);
            Close();
        }

        public void Open(bool daily)
        {
            if (waves.IsFighting || WaveManager.InConquest) return;
            _daily = daily;
            root.SetActive(true);
            // Man nay gan component len CHINH root, va `Awake` tu `Close()` (man hinh dung an san khi vao scene).
            // Lan kich hoat dau tien co the chay Awake ngay trong SetActive tren, va Awake tat lai — nen phai bat
            // lai lan hai. Cung khuon voi `SkillBoardScreen.Open` (da dinh loi "bam lan dau khong thay gi").
            if (!root.activeSelf) root.SetActive(true);
            _previewDay = DailyChallenge.DayKey;
            Refresh();
            Sfx.Play("ui-open");
        }

        public void Close() => root.SetActive(false);

        void Update()
        {
            if (Time.unscaledTime < _refreshAt) return;
            Refresh();
        }

        void Refresh()
        {
            _refreshAt = Time.unscaledTime + 1f;
            title.text = _daily ? "Daily Challenge" : "Boss Rush";
            subtitle.text = _daily ? $"{DailyChallenge.DayKey}  /  {DailyChallenge.Title}"
                : "Three encounters. One castle. A single continuous run.";
            rules.text = _daily ? DailyChallenge.Rule + "\nLose? Retry for free before the day ends."
                : "Defeat each boss and its escorts in order.\nCastle health carries over between rounds. Rewards on full clear.";
            int gold = 500;
            foreach (int wave in new[] { 10, 20, 30 }) gold += tuning.WaveGold(wave);
            reward.text = _daily ? $"{DailyChallenge.GoldReward:N0} Gold  +  {DailyChallenge.Shards} random owned-unit shards\nOne equipment drop on victory. Rewards once per day."
                : $"{gold:N0} Gold  +  equipment on full clear\nReplay freely. Campaign wave progress is preserved.";
            var data = SaveSystem.Data;
            var left = DateTime.Now.Date.AddDays(1) - DateTime.Now;
            status.text = _daily ? (DailyChallenge.Cleared ? "CLEARED - rewards received" : "AVAILABLE - rewards unclaimed")
                + $"   |   New challenge in {left.Hours:00}:{left.Minutes:00}:{left.Seconds:00}"
                : $"Personal best: {data.bossRushBest}/3 rounds   |   Full clears: {data.bossRushClears}";
            for (int i = 0; i < 3; i++)
            {
                int wave = _daily ? DailyChallenge.Wave : (i + 1) * 10;
                var band = tuning.BandFor(wave);
                var boss = band != null && band.boss != null ? band.boss : tuning.boss;
                var clip = boss != null && boss.animations != null ? boss.animations.idle : null;
                nodeIcons[i].sprite = _daily ? dailyIcons[i] : clip != null && clip.IsValid ? clip.frames[0] : null;
                nodeIcons[i].enabled = nodeIcons[i].sprite != null;
                nodeNames[i].text = _daily ? new[] { "1. Prepare", "2. Challenge", "3. Collect" }[i]
                    : $"Round {i + 1} / Wave {wave}\n{(boss != null ? boss.displayName : "Boss")}";
                nodeStates[i].text = _daily ? (i == 0 ? "Check your army" : i == 1 ? DailyChallenge.Title
                    : DailyChallenge.Cleared ? "Rewards received" : "Win to claim automatically")
                    : (band != null && band.extraBosses != null && band.extraBosses.Length > 0 ? "Additional boss + escorts\n" : "Boss + escorts\n")
                    + (i < data.bossRushBest ? "Best run: defeated" : "Not yet defeated");
                nodeIcons[i].color = _daily || i < data.bossRushBest ? Color.white : new Color(.7f, .7f, .7f);
            }
            startButton.interactable = !_daily || !DailyChallenge.Cleared;
            startLabel.text = _daily ? DailyChallenge.Cleared ? "Completed Today" : "Start Challenge" : "Start Full Run";
        }

        void Begin()
        {
            if (_daily && _previewDay != DailyChallenge.DayKey)
            {
                _previewDay = DailyChallenge.DayKey;
                Refresh();
                return;
            }
            if (waves.IsFighting || WaveManager.InConquest || (_daily && DailyChallenge.Cleared)) return;
            Close();
            if (_daily) waves.StartDailyChallenge(); else waves.StartBossRush();
        }
    }
}
