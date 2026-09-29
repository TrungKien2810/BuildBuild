using System.Collections.Generic;
using BuildBuild.Core;
using BuildBuild.Data;
using BuildBuild.Meta;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>
    /// Man trieu hoi (GDD muc 6): 4 banner (tong hop / hero / linh / tru), quay 1 / quay 10, hien the ket qua va bo
    /// dem pity. Banner chi doi LOAI unit ra; gia, ti le do hiem va pity dung chung.
    /// </summary>
    public class GachaScreen : MonoBehaviour
    {
        /// <summary>Thu tu = thu tu `bannerButtons`. `only` null = banner tong hop.</summary>
        static readonly (string name, UnitCategory? only)[] Banners =
        {
            ("Tổng hợp", null),
            ("Hero", UnitCategory.Hero),
            ("Lính", UnitCategory.Soldier),
            ("Trụ", UnitCategory.Cannon),
        };

        static readonly Color TabIdle = new Color(0.6f, 0.6f, 0.6f);

        [SerializeField] GachaConfig config;
        [SerializeField] Economy economy;

        [Header("Widget")]
        [SerializeField] Text titleText;
        [SerializeField] Text goldText;
        [SerializeField] Text ratesText;
        [SerializeField] Text pityText;
        [Tooltip("4 tab banner, cung thu tu voi `Banners`.")]
        [SerializeField] Button[] bannerButtons;
        [SerializeField] Button rollOneButton;
        [SerializeField] Text rollOneLabel;
        [SerializeField] Button rollTenButton;
        [SerializeField] Text rollTenLabel;
        [SerializeField] Button backButton;
        [SerializeField] Transform resultGrid;
        [SerializeField] GachaCardView cardTemplate;

        GachaSystem _gacha;
        int _banner;
        readonly List<GachaCardView> _shown = new List<GachaCardView>();

        void Start()
        {
            _gacha = new GachaSystem(config, SaveSystem.Data);
            cardTemplate.gameObject.SetActive(false);

            for (int i = 0; i < bannerButtons.Length && i < Banners.Length; i++)
            {
                int banner = i;
                bannerButtons[i].GetComponentInChildren<Text>().text = Banners[i].name;
                bannerButtons[i].onClick.AddListener(() => SelectBanner(banner));
            }
            rollOneButton.onClick.AddListener(() => Roll(1, config.singleCost));
            rollTenButton.onClick.AddListener(() => Roll(10, config.tenCost));
            backButton.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.Battle));
            economy.Changed += _ => Refresh();

            SelectBanner(0);
        }

        void SelectBanner(int banner)
        {
            _banner = banner;
            for (int i = 0; i < bannerButtons.Length; i++)
                bannerButtons[i].image.color = i == banner ? Color.white : TabIdle;
            titleText.text = $"Triệu hồi · {Banners[banner].name}";
            string rarities = $"N {config.rateN}%  ·  R {config.rateR}%  ·  SR {config.rateSR}%  ·  SSR {config.rateSSR}%";
            ratesText.text = Banners[banner].only == null
                ? $"{rarities}   |   Hero {config.heroShare * 100f:0}% · Lính {config.soldierShare * 100f:0}% · Trụ {(1f - config.heroShare - config.soldierShare) * 100f:0}%"
                : $"{rarities}   |   Chỉ ra {Banners[banner].name}";
            Refresh();
        }

        void Roll(int count, int cost)
        {
            if (!economy.TrySpend(cost)) return;

            foreach (var card in _shown) Destroy(card.gameObject);
            _shown.Clear();

            foreach (var result in _gacha.Roll(count, Banners[_banner].only))
            {
                var card = Instantiate(cardTemplate, resultGrid);
                card.gameObject.SetActive(true);
                card.Show(result);
                _shown.Add(card);
            }
            Refresh();
        }

        void Refresh()
        {
            var save = SaveSystem.Data;
            goldText.text = economy.Gold.ToString("N0");
            // +1: luot duoc dam bao cung tinh — bo dem 30 nghia la luot KE TIEP chac chan SR.
            pityText.text = $"Chắc chắn SR trong {config.softPity + 1 - save.rollsSinceSR} lượt  ·  SSR trong {config.hardPity + 1 - save.rollsSinceSSR} lượt";
            // Nut xam khong du ro: ghi thang "thieu vang" de khong tuong la game do.
            rollOneButton.interactable = economy.Gold >= config.singleCost;
            rollTenButton.interactable = economy.Gold >= config.tenCost;
            rollOneLabel.text = $"Quay 1\n{config.singleCost:N0} vàng{(rollOneButton.interactable ? "" : " · thiếu")}";
            rollTenLabel.text = $"Quay 10\n{config.tenCost:N0} vàng{(rollTenButton.interactable ? "" : " · thiếu")}";
        }
    }
}
