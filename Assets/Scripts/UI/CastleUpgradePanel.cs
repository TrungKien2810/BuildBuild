using System;
using BuildBuild.Core;
using BuildBuild.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>
    /// Bang nang thanh tri (GDD muc 2): anh cap hien tai -> cap sau, mau + so be phao truoc/sau, dieu kien mo, nut nang.
    /// Chi hien thong tin; tru vang + nang that do `BattleHud` lam khi nhan `UpgradePressed`.
    /// </summary>
    public class CastleUpgradePanel : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] Image currentImage;
        [SerializeField] Image nextImage;
        [SerializeField] Text currentName;
        [SerializeField] Text nextName;
        [SerializeField] Text healthText;
        [SerializeField] Text mountsText;
        [SerializeField] Text conditionText;
        [SerializeField] Button upgradeButton;
        [SerializeField] Text upgradeLabel;
        [SerializeField] Button closeButton;

        public event Action UpgradePressed;
        public bool IsOpen => root.activeSelf;

        void Awake()
        {
            upgradeButton.onClick.AddListener(() => UpgradePressed?.Invoke());
            closeButton.onClick.AddListener(Close);
            Close();
        }

        public void Open(Castle castle, int gold)
        {
            root.SetActive(true);
            Refresh(castle, gold);
        }

        public void Close() => root.SetActive(false);

        public void Refresh(Castle castle, int gold)
        {
            var current = castle.CurrentTier;
            var next = castle.NextTier;
            if (current == null || next == null)
            {
                Close();
                return;
            }
            currentImage.sprite = current.intact;
            nextImage.sprite = next.intact;
            currentName.text = $"Level {castle.TierLevel} · {current.name}";
            nextName.text = $"Level {castle.TierLevel + 1} · {next.name}";
            healthText.text = $"{current.maxHealth:0}  →  {next.maxHealth:0}";
            mountsText.text = $"{current.mounts.Length}  →  {next.mounts.Length}";

            int best = SaveSystem.Data.nextWave - 1;
            bool unlocked = best >= next.unlockWave;
            conditionText.text = next.unlockWave <= 0 ? "No Requirements"
                : unlocked ? $"Wave {next.unlockWave} Won"
                : $"Win Wave {next.unlockWave} to Unlock (best {Mathf.Max(best, 0)})";
            conditionText.color = unlocked ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.55f, 0.5f);

            bool afford = gold >= next.cost;
            upgradeLabel.text = $"Upgrade · {next.cost}";
            upgradeLabel.color = afford || !unlocked ? Color.white : new Color(1f, 0.55f, 0.5f);
            upgradeButton.interactable = unlocked && afford;
        }
    }
}
