using BuildBuild.Core;
using BuildBuild.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>
    /// Bang ky nang toan tai khoan (GDD 10.4, trong Town): 3 nhanh CO DINH, moi nhanh 10 cap, mua bang vang.
    /// Noi dung + gia do `Meta/SkillBoard` quyet; lop nay chi ve va goi `TryUpgrade`.
    /// Cong % duoc ap o `Progression.Multipliers` (dung mot cho, nen moi hero/linh deu an).
    /// </summary>
    public class SkillBoardScreen : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] Economy economy;

        [Header("Widget")]
        [SerializeField] Text goldText;
        [SerializeField] Text summaryText;
        [SerializeField] Button closeButton;
        [SerializeField] GameObject[] rowRoots;
        [SerializeField] Text[] rowNames;
        [SerializeField] Text[] rowBonuses;
        [SerializeField] Button[] rowButtons;
        [SerializeField] Text[] rowLabels;
        [SerializeField] Image[] rankBars;

        static readonly BoardNode[] Nodes = { BoardNode.Attack, BoardNode.Health, BoardNode.Haste };

        public bool IsOpen => root != null && root.activeSelf;
        public event System.Action Upgraded;

        void Awake()
        {
            closeButton.onClick.AddListener(Close);
            for (int i = 0; i < rowButtons.Length; i++)
            {
                int index = i;
                rowButtons[i].onClick.AddListener(() => Upgrade(index));
            }
            Close();
        }

        public void Open()
        {
            if (root == null) return;
            root.SetActive(true);
            // Lan kich hoat DAU TIEN moi chay Awake, ma Awake lai tu Close() (man hinh dung an san khi vao scene).
            // `TownScreen.Awake` dong man con nay TRUOC khi Awake cua no chay, nen SetActive o tren chi vua dan
            // toi "Awake tu tat chinh no" -> lan bam Skill Board dau tien khong thay gi. Awake chi chay mot lan.
            if (!root.activeSelf) root.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (root != null) root.SetActive(false);
        }

        public void Refresh()
        {
            goldText.text = economy.Gold.ToString("N0");
            summaryText.text = SkillBoard.Summary();

            for (int i = 0; i < rowRoots.Length && i < Nodes.Length; i++)
            {
                var node = Nodes[i];
                int rank = SkillBoard.Rank(node);
                bool maxed = SkillBoard.Maxed(node);
                int cost = SkillBoard.Cost(node);
                bool afford = economy.Gold >= cost;

                rowRoots[i].SetActive(true);
                rankBars[i].fillAmount = (float)rank / SkillBoard.MaxRank;
                rowNames[i].text = $"{SkillBoard.Name(node)}   ·   Rank {rank}/{SkillBoard.MaxRank}";
                string stat = node == BoardNode.Attack ? "Damage" : node == BoardNode.Health ? "Army HP" : "Attack speed";
                rowBonuses[i].text = maxed
                    ? $"{stat}: +{(SkillBoard.Bonus(node) * 100f).ToString("0.#")}%  ·  Maximum rank"
                    : $"{stat}: +{(SkillBoard.Bonus(node) * 100f).ToString("0.#")}%  →  +{((rank + 1) * SkillBoard.PerRank(node) * 100f).ToString("0.#")}%";
                rowLabels[i].text = maxed ? "Maxed" : afford ? $"Upgrade\n{cost:N0} Gold" : $"Need {cost - economy.Gold:N0}\nmore Gold";
                rowLabels[i].color = maxed || afford ? Color.white : new Color(1f, 0.55f, 0.5f);
                rowButtons[i].interactable = !maxed && afford;
            }
        }

        void Upgrade(int index)
        {
            if (index < 0 || index >= Nodes.Length) return;
            if (SkillBoard.TryUpgrade(Nodes[index], price => economy.TrySpend(price)))
            {
                Sfx.Play("upgrade");
                Refresh();
                Upgraded?.Invoke();
            }
            else
            {
                Sfx.Play("ui-error");
            }
        }
    }
}
