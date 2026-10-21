using System.Collections;
using System.Collections.Generic;
using BuildBuild.Core;
using BuildBuild.Data;
using BuildBuild.Meta;
using UnityEngine;
using UnityEngine.InputSystem;
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
            ("Mixed", null),
            ("Hero", UnitCategory.Hero),
            ("Soldiers", UnitCategory.Soldier),
            ("Towers", UnitCategory.Cannon),
        };

        static readonly Color TabIdle = new Color(0.6f, 0.6f, 0.6f);

        [SerializeField] GachaConfig config;
        [SerializeField] Economy economy;
        [Tooltip("Hero co `unitId` — tra vai cua the ket qua qua `unitId`.")]
        [SerializeField] HeroDefinition[] heroes;
        [SerializeField] HeroRoleCatalog roles;

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
        bool _revealing;
        bool _fast;

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
            titleText.text = $"Summon · {Banners[banner].name}";
            string rarities = $"N {config.rateN}%  ·  R {config.rateR}%  ·  SR {config.rateSR}%  ·  SSR {config.rateSSR}%";
            ratesText.text = Banners[banner].only == null
                ? $"{rarities}   |   Hero {config.heroShare * 100f:0}% · Soldiers {config.soldierShare * 100f:0}% · Towers {(1f - config.heroShare - config.soldierShare) * 100f:0}%"
                : $"{rarities}   |   Only {Banners[banner].name}";
            Refresh();
        }

        HeroRole RoleOf(UnitDefinition unit) =>
            System.Array.Find(heroes, h => h != null && h.unitId == unit.id)?.role ?? HeroRole.None;

        void Roll(int count, int cost)
        {
            if (_revealing) return;

            // Luot quay mien phi (tang moi 5 cap nguoi choi) chi dung cho quay 1 — quay 10 van tru vang day du
            // de khong bien mot ve thanh muoi con.
            bool free = count == 1 && PlayerLevel.FreeRolls > 0;
            if (free)
            {
                SaveSystem.Data.freeRolls--;
                SaveSystem.Save();
            }
            else if (!economy.TrySpend(cost)) return;

            Sfx.Play("gacha-roll");
            foreach (var card in _shown) Destroy(card.gameObject);
            _shown.Clear();

            foreach (var result in _gacha.Roll(count, Banners[_banner].only))
            {
                var card = Instantiate(cardTemplate, resultGrid);
                card.gameObject.SetActive(true);
                card.Show(result, roles.IconOf(RoleOf(result.unit)));
                card.FaceDown();
                _shown.Add(card);
            }
            StartCoroutine(Reveal());
        }

        /// <summary>
        /// Chia the up roi lat tung the theo thu tu. SR/SSR tu co nhip nin + chop (GachaCardView); SSR rung ca luoi.
        /// Cham man hinh khi dang lat = tua nhanh x6.
        /// </summary>
        IEnumerator Reveal()
        {
            _revealing = true;
            _fast = false;
            Refresh();

            foreach (var card in _shown)
            {
                StartCoroutine(card.Deal(Step));
                yield return Wait(0.04f);
            }
            yield return Wait(0.2f);

            foreach (var card in _shown)
            {
                yield return card.Reveal(Step, card.Rarity == Rarity.SSR ? () => StartCoroutine(Shake()) : null);
                yield return Wait(card.Rarity >= Rarity.SR ? 0.3f : 0.1f);
            }

            _revealing = false;
            Refresh();
        }

        void Update()
        {
            var pointer = Pointer.current;
            if (_revealing && pointer != null && pointer.press.wasPressedThisFrame) _fast = true;
        }

        float Step() => Time.unscaledDeltaTime * (_fast ? 6f : 1f);

        IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds; t += Step()) yield return null;
        }

        /// <summary>Rung luoi ket qua khi lat trung SSR.</summary>
        IEnumerator Shake()
        {
            var grid = (RectTransform)resultGrid;
            Vector2 home = grid.anchoredPosition;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.35f)
            {
                grid.anchoredPosition = home + Random.insideUnitCircle * 14f * (1f - t);
                yield return null;
            }
            grid.anchoredPosition = home;
        }

        void Refresh()
        {
            var save = SaveSystem.Data;
            goldText.text = economy.Gold.ToString("N0");
            // +1: luot duoc dam bao cung tinh — bo dem 30 nghia la luot KE TIEP chac chan SR.
            pityText.text = $"Guaranteed SR within {config.softPity + 1 - save.rollsSinceSR} rolls  ·  SSR within {config.hardPity + 1 - save.rollsSinceSSR} rolls";
            // Luot mien phi (moi 5 cap nguoi choi) hien ngay tren tieu de + nut quay 1, khong an trong tooltip.
            int free = PlayerLevel.FreeRolls;
            titleText.text = $"Summon · {Banners[_banner].name}" + (free > 0 ? $"  ·  {free} free" : "");
            // Nut xam khong du ro: ghi thang "thieu vang" de khong tuong la game do.
            rollOneButton.interactable = !_revealing && (free > 0 || economy.Gold >= config.singleCost);
            rollTenButton.interactable = !_revealing && economy.Gold >= config.tenCost;
            rollOneLabel.text = free > 0 ? $"Roll 1\nFREE ×{free}"
                : $"Roll 1\n{config.singleCost:N0} Gold{(_revealing || rollOneButton.interactable ? "" : " · missing")}";
            rollTenLabel.text = $"Roll 10\n{config.tenCost:N0} Gold{(_revealing || rollTenButton.interactable ? "" : " · missing")}";
        }
    }
}
