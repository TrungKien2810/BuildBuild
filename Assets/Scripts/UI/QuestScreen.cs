using System.Collections.Generic;
using BuildBuild.Core;
using BuildBuild.Data;
using BuildBuild.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>
    /// Man Nhiem vu Hanh trinh (GAME_DESIGN.md muc 11), mo tu nut "Quests" tren HUD sanh. Mot moc mot lan:
    /// 3 nhiem vu + o thuong cuoi moc (tang tuong / ve chon tuong); moc sau chi mo khi da nhan HET thuong moc truoc.
    ///
    /// Lua o `QuestLog`, noi dung o `QuestCatalog` — man nay chi doc tien do, ve 4 dong, va noi 2 thu:
    ///   - VANG cong qua `Economy` (de HUD nghe `Economy.Changed`), tuong/manh/ve do `QuestLog` ghi thang vao save;
    ///   - phan hoi khi nhan thuong dung lai `ResultPopup.ShowReward` (o icon + ten), khong tu ve bang moi.
    /// </summary>
    public class QuestScreen : MonoBehaviour
    {
        const int RowCount = QuestCatalog.RowsPerStage;

        [SerializeField] WaveManager waves;
        [SerializeField] Economy economy;
        [SerializeField] BattleHud hud;
        [Tooltip("Pool gacha — tra bac / loai / ten cua unit de dem hero, quy manh va hien ten qua.")]
        [SerializeField] GachaConfig gacha;
        [Tooltip("Roster hero — lay chan dung cho o thuong va bang chon tuong.")]
        [SerializeField] HeroDefinition[] roster;
        [SerializeField] ResultPopup resultPopup;

        [Header("Bang nhiem vu")]
        [SerializeField] GameObject root;
        [SerializeField] Text chapterText;
        [SerializeField] Button[] chapterButtons;
        [SerializeField] Image[] rewardIcons;
        int _viewStage;
        [SerializeField] Sprite questIcon;
        [SerializeField] Sprite goldRewardIcon, shardRewardIcon, voucherRewardIcon;
        [SerializeField] Text hintText;
        [SerializeField] Button closeButton;

        [Header("4 dong: 3 nhiem vu + o thuong cuoi moc")]
        [SerializeField] Text[] rowTitles;
        [SerializeField] Text[] rowRewards;
        [SerializeField] Text[] rowProgress;
        [SerializeField] Image[] rowBars;
        [SerializeField] Button[] rowClaims;
        [SerializeField] GameObject[] rowChecks;

        [Header("Ve chon tuong")]
        [SerializeField] GameObject voucherPanel;
        [SerializeField] Button[] voucherSlots;
        [SerializeField] Image[] voucherAvatars;
        [SerializeField] Text[] voucherLabels;
        [SerializeField] Button voucherClose;

        readonly Quest[] _rows = new Quest[RowCount];
        string[] _voucherPool = new string[0];

        /// <summary>Co o bam nhan duoc hoac con ve chua chon — HUD dung de bat cham do tren nut Quests.</summary>
        public bool HasClaimable => QuestLog.ClaimableCount(gacha) > 0 || QuestLog.PendingVoucher.Length > 0;

        void Start()
        {
            closeButton.onClick.AddListener(Close);
            for (int i = 0; i < chapterButtons.Length; i++)
            {
                int stage = i + 1;
                chapterButtons[i].onClick.AddListener(() => { _viewStage = stage; Refresh(); });
            }
            voucherClose.onClick.AddListener(() => voucherPanel.SetActive(false));
            for (int i = 0; i < rowClaims.Length; i++)
            {
                int index = i;
                rowClaims[i].onClick.AddListener(() => Claim(index));
            }
            for (int i = 0; i < voucherSlots.Length; i++)
            {
                int index = i;
                voucherSlots[i].onClick.AddListener(() => PickVoucher(index));
            }
            root.SetActive(false);
            voucherPanel.SetActive(false);
        }

        public void Open()
        {
            if (waves != null && waves.IsFighting) return;
            _viewStage = QuestLog.Stage;
            root.SetActive(true);
            Sfx.Play("ui-open");
            voucherPanel.SetActive(false);
            Refresh();
            // Ve con treo tu lan truoc (nguoi choi bam "Later") phai hien lai ngay, khong the mat.
            if (QuestLog.PendingVoucher.Length > 0) ShowVoucher(QuestLog.PendingVoucher);
        }

        void Close()
        {
            root.SetActive(false);
            if (hud != null) hud.Refresh();
        }

        void Refresh()
        {
            int stage = _viewStage;
            for (int i = 0; i < chapterButtons.Length; i++)
                chapterButtons[i].image.color = i + 1 == stage ? new Color(1f, .82f, .45f) : i + 1 > QuestLog.Stage ? new Color(.55f, .58f, .65f) : Color.white;
            chapterText.text = $"Chapter {stage} / {QuestCatalog.StageCount}";

            int row = 0;
            foreach (var quest in QuestCatalog.All)
            {
                if (quest.stage != stage) continue;
                if (row < RowCount) _rows[row++] = quest;
            }
            for (int i = 0; i < RowCount; i++) RefreshRow(i);

            bool allClaimed = true;
            foreach (var quest in QuestCatalog.All)
                if (!QuestLog.IsClaimed(quest.id)) { allClaimed = false; break; }
            hintText.text = stage > QuestLog.Stage ? "Preview - claim every reward in the previous chapter to unlock." : allClaimed ? "Journey complete — every chapter claimed."
                : stage >= QuestCatalog.StageCount ? "Claim all remaining rewards to complete your journey."
                : $"Next chapter: {RewardText(NextStageReward(stage))}";
        }

        void RefreshRow(int index)
        {
            var quest = _rows[index];
            if (quest == null) return;

            int target = Mathf.Max(1, quest.target);
            int progress = QuestLog.Progress(quest, gacha);
            bool claimed = QuestLog.IsClaimed(quest.id);

            var hero = FindHero(quest.reward.hero);
            rewardIcons[index].sprite = hero != null ? hero.avatar
                : !string.IsNullOrEmpty(quest.reward.voucher) ? voucherRewardIcon
                : quest.reward.shards > 0 ? shardRewardIcon : quest.reward.gold > 0 ? goldRewardIcon : questIcon;
            rowTitles[index].text = quest.title;
            rowClaims[index].GetComponentInChildren<Text>().text = quest.stage > QuestLog.Stage ? "Locked" : QuestLog.CanClaim(quest, gacha) ? "Claim" : quest.IsChapterReward ? "Finish Tasks" : "Go";
            // O thuong cuoi moc dem bang SO NHIEM VU da nhan, khong phai bang chi so — chu thich ro cho nguoi choi.
            rowProgress[index].text = quest.IsChapterReward ? $"{progress}/{target} quests" : $"{Mathf.Min(progress, target)}/{target}";
            rowRewards[index].text = RewardText(quest.reward);
            rowBars[index].fillAmount = Mathf.Clamp01((float)progress / target);
            rowTitles[index].color = claimed ? new Color(0.62f, 0.66f, 0.72f) : Color.white;
            rowRewards[index].color = claimed ? new Color(0.55f, 0.58f, 0.64f) : new Color(0.86f, 0.88f, 0.92f);

            rowClaims[index].interactable = QuestLog.CanClaim(quest, gacha) || (!claimed && quest.stage <= QuestLog.Stage && !quest.IsChapterReward);
            rowClaims[index].gameObject.SetActive(!claimed);
            rowChecks[index].SetActive(claimed);
        }

        void Claim(int index)
        {
            var quest = _rows[index];
            if (!QuestLog.CanClaim(quest, gacha))
            {
                if (quest.stage <= QuestLog.Stage && !quest.IsChapterReward)
                {
                    Close();
                    if (hud != null) hud.GoToQuest(quest.goal);
                }
                return;
            }

            var claim = QuestLog.Claim(quest, gacha);
            if (claim.gold > 0 && economy != null) economy.Add(claim.gold);

            if (quest.reward.voucher.Length > 0)
            {
                // Chua co gi de bao: bang chon tuong chinh la phan hoi, va no mo ngay.
                ShowVoucher(quest.reward.voucher);
                AfterClaim();
                return;
            }

            ShowClaimPopup(quest, claim);
            AfterClaim();
        }

        /// <summary>Vang da cong qua `Economy` o tren; o day chi con phan hoi bang hinh + cap nhat lai man.</summary>
        void ShowClaimPopup(Quest quest, QuestClaim claim)
        {
            string hint = claim.heroIsNew ? $"{claim.heroName} joined your roster"
                : claim.shards > 0 && claim.heroName.Length > 0 ? $"Already owned — +{claim.shards} Shards for {claim.heroName}"
                : claim.shards > 0 ? $"+{claim.shards} Shards"
                : "";
            if (claim.gold > 0 && hint.Length > 0) hint = $"+{claim.gold:N0} Gold · {hint}";

            var hero = FindHero(quest.reward.hero);
            if (resultPopup != null)
                resultPopup.ShowReward(quest.title, claim.gold, hint, hero != null ? hero.avatar : null, claim.heroName);
        }

        void ShowVoucher(string voucher)
        {
            _voucherPool = QuestCatalog.VoucherPool(voucher);
            for (int i = 0; i < voucherSlots.Length; i++)
            {
                bool show = i < _voucherPool.Length;
                voucherSlots[i].gameObject.SetActive(show);
                if (!show) continue;

                string unitId = _voucherPool[i];
                var hero = FindHero(unitId);
                bool owned = Owned(unitId);
                voucherAvatars[i].sprite = hero != null ? hero.avatar : null;
                voucherAvatars[i].enabled = voucherAvatars[i].sprite != null;
                // Da so huu thi noi thang ra so manh quy doi, de khong ai bam nham roi thay "mat luot".
                voucherLabels[i].text = owned ? $"{NameOf(unitId)}\n+{QuestCatalog.ShardsFor(RarityOf(unitId))} Shards" : NameOf(unitId);
                voucherLabels[i].color = owned ? new Color(0.72f, 0.76f, 0.82f) : Color.white;
            }
            voucherPanel.SetActive(true);
        }

        void PickVoucher(int index)
        {
            if (index >= _voucherPool.Length) return;
            string unitId = _voucherPool[index];
            var claim = QuestLog.UseVoucher(unitId, gacha);
            if (claim.heroName.Length == 0) return;

            voucherPanel.SetActive(false);
            string hint = claim.heroIsNew ? $"{claim.heroName} joined your roster" : $"Already owned — +{claim.shards} Shards";
            var hero = FindHero(unitId);
            if (resultPopup != null)
                resultPopup.ShowReward("New Hero", 0, hint, hero != null ? hero.avatar : null, claim.heroName);
            AfterClaim();
        }

        void AfterClaim()
        {
            _viewStage = QuestLog.Stage;
            Refresh();
            if (hud != null) hud.Refresh();
        }

        string RewardText(QuestReward reward)
        {
            var parts = new List<string>();
            if (reward.gold > 0) parts.Add($"{reward.gold:N0} Gold");
            if (!string.IsNullOrEmpty(reward.hero)) parts.Add($"Hero: {NameOf(reward.hero)}");
            if (!string.IsNullOrEmpty(reward.voucher))
                parts.Add(reward.voucher == "sr" ? "SR Hero (choose 1 of 4)" : "SSR Hero (choose 1 of 8)");
            if (reward.shards > 0) parts.Add($"{reward.shards} Shards");
            return parts.Count == 0 ? "—" : string.Join(" + ", parts);
        }

        static QuestReward NextStageReward(int stage)
        {
            foreach (var quest in QuestCatalog.All)
                if (quest.stage == stage + 1 && quest.IsChapterReward) return quest.reward;
            return new QuestReward();
        }

        static bool Owned(string unitId) => SaveSystem.Data.owned.Exists(o => o.id == unitId);

        HeroDefinition FindHero(string unitId)
        {
            if (string.IsNullOrEmpty(unitId) || roster == null) return null;
            foreach (var hero in roster)
                if (hero != null && hero.unitId == unitId) return hero;
            return null;
        }

        string NameOf(string unitId)
        {
            var unit = FindUnit(unitId);
            if (unit != null && !string.IsNullOrEmpty(unit.displayName)) return unit.displayName;
            var hero = FindHero(unitId);
            return hero != null ? hero.displayName : unitId;
        }

        Rarity RarityOf(string unitId)
        {
            var unit = FindUnit(unitId);
            return unit != null ? unit.rarity : Rarity.N;
        }

        UnitDefinition FindUnit(string unitId)
        {
            if (gacha == null || string.IsNullOrEmpty(unitId)) return null;
            foreach (var unit in gacha.pool)
                if (unit != null && unit.id == unitId) return unit;
            return null;
        }
    }
}
