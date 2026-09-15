using System.Collections.Generic;
using BuildBuild.Core;
using BuildBuild.Data;
using BuildBuild.Meta;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>Man trieu hoi (GDD muc 6): quay 1 / quay 10, hien the ket qua va bo dem pity.</summary>
    public class GachaScreen : MonoBehaviour
    {
        [SerializeField] GachaConfig config;
        [SerializeField] Economy economy;

        [Header("Widget")]
        [SerializeField] Text goldText;
        [SerializeField] Text ratesText;
        [SerializeField] Text pityText;
        [SerializeField] Button rollOneButton;
        [SerializeField] Text rollOneLabel;
        [SerializeField] Button rollTenButton;
        [SerializeField] Text rollTenLabel;
        [SerializeField] Button backButton;
        [SerializeField] Transform resultGrid;
        [SerializeField] GachaCardView cardTemplate;

        GachaSystem _gacha;
        readonly List<GachaCardView> _shown = new List<GachaCardView>();

        void Start()
        {
            _gacha = new GachaSystem(config, SaveSystem.Data);
            cardTemplate.gameObject.SetActive(false);

            rollOneButton.onClick.AddListener(() => Roll(1, config.singleCost));
            rollTenButton.onClick.AddListener(() => Roll(10, config.tenCost));
            backButton.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.Battle));
            economy.Changed += _ => Refresh();

            rollOneLabel.text = $"Quay 1\n{config.singleCost:N0} vàng";
            rollTenLabel.text = $"Quay 10\n{config.tenCost:N0} vàng";
            ratesText.text = $"N {config.rateN}%  ·  R {config.rateR}%  ·  SR {config.rateSR}%  ·  SSR {config.rateSSR}%";
            Refresh();
        }

        void Roll(int count, int cost)
        {
            if (!economy.TrySpend(cost)) return;

            foreach (var card in _shown) Destroy(card.gameObject);
            _shown.Clear();

            foreach (var result in _gacha.Roll(count))
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
            rollOneButton.interactable = economy.Gold >= config.singleCost;
            rollTenButton.interactable = economy.Gold >= config.tenCost;
        }
    }
}
