using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuildBuild.Core;
using BuildBuild.Data;
using BuildBuild.Meta;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>
    /// Man Doi quan (GDD muc 10.2 + 10.3 gop mot man): 3 tab Hero / Linh / Tru, trai la luoi the, phai la chi tiet unit
    /// dang chon — chi so hien tai va sau khi nang, nut Nang level (vang) va Nang sao (mang), danh sach ky nang voi dong
    /// "Mo o N★" cho noi tai / chieu phu chua mo. Nang level chi tang chi so; mo ky nang chi nho sao (chot 2026-09-28).
    ///
    /// Hero va linh noi voi the qua `unitId`; so nhan cua level/sao lay tu `Progression` — cung ham `Hero.Initialise`
    /// dung, nen so tren man nay la so tren san. Phao giu level + gia rieng trong `CannonDefinition`.
    /// </summary>
    public class UnitsScreen : MonoBehaviour
    {
        static readonly (string name, UnitCategory category)[] Tabs =
        {
            ("Hero", UnitCategory.Hero),
            ("Lính", UnitCategory.Soldier),
            ("Trụ", UnitCategory.Cannon),
        };

        static readonly Color TabIdle = new Color(0.6f, 0.6f, 0.6f);
        const string Up = "#7CFC7C";
        const string Locked = "#9A9A9A";

        [SerializeField] GachaConfig config;
        [SerializeField] Economy economy;
        [Tooltip("Moi unit gacha (ca unit chua vao pool) — luoi hien du, unit chua so huu thi xam.")]
        [SerializeField] UnitDefinition[] units;
        [Tooltip("Chi so hero + linh (SoldierDefinition), khop the qua `unitId`.")]
        [SerializeField] HeroDefinition[] heroes;
        [SerializeField] CannonDefinition[] cannons;
        [SerializeField] EquipmentConfig equipment;

        [Header("Widget")]
        [SerializeField] Text goldText;
        [Tooltip("3 tab, cung thu tu voi `Tabs`.")]
        [SerializeField] Button[] tabButtons;
        [SerializeField] Button backButton;
        [SerializeField] Transform grid;
        [SerializeField] UnitCardView cardTemplate;

        [Header("Chi tiet")]
        [SerializeField] Image detailIcon;
        [SerializeField] Text detailName;
        [SerializeField] Text detailSub;
        [SerializeField] Text detailStats;
        [SerializeField] Text detailSkills;
        [SerializeField] Button levelButton;
        [SerializeField] Text levelLabel;
        [SerializeField] Button starButton;
        [SerializeField] Text starLabel;

        [Header("Trang bi (chi hero)")]
        [Tooltip("Hai nut doi cot giua giua Ky nang va Trang bi (GDD 10.3). Linh / tru an ca hai.")]
        [SerializeField] Button skillsTab;
        [SerializeField] Button gearTab;
        [SerializeField] GameObject gearView;
        [SerializeField] Transform slotRow;
        [SerializeField] Transform bagRow;
        [SerializeField] EquipCellView cellTemplate;
        [SerializeField] Text gearInfo;
        [SerializeField] Button upgradeButton;
        [SerializeField] Text upgradeLabel;
        [SerializeField] Button unequipButton;
        [Tooltip("Vien o trang bi theo do hiem, thu tu N, R, SR, SSR (dung chung vien the unit).")]
        [SerializeField] Sprite[] rarityFrames;

        readonly List<(UnitDefinition unit, UnitCardView card)> _cards = new List<(UnitDefinition, UnitCardView)>();
        UnitDefinition _selected;
        bool _showGear;
        EquipmentSlot _slot;
        const int BagShown = 8;

        public static string Stars(int stars) =>
            new string('★', Mathf.Clamp(stars, 0, Progression.MaxStars)) + new string('☆', Mathf.Max(0, Progression.MaxStars - stars));

        void Start()
        {
            cardTemplate.gameObject.SetActive(false);
            for (int i = 0; i < tabButtons.Length && i < Tabs.Length; i++)
            {
                int tab = i;
                tabButtons[i].GetComponentInChildren<Text>().text = Tabs[i].name;
                tabButtons[i].onClick.AddListener(() => SelectTab(tab));
            }
            backButton.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.Battle));
            levelButton.onClick.AddListener(LevelUp);
            starButton.onClick.AddListener(StarUp);
            cellTemplate.gameObject.SetActive(false);
            skillsTab.onClick.AddListener(() => { _showGear = false; Refresh(); });
            gearTab.onClick.AddListener(() => { _showGear = true; Refresh(); });
            upgradeButton.onClick.AddListener(UpgradeGear);
            unequipButton.onClick.AddListener(() =>
            {
                var item = SelectedGear();
                if (item != null) Armory.Unequip(item);
                Refresh();
            });
            economy.Changed += _ => Refresh();
            SelectTab(0);
        }

        void SelectTab(int tab)
        {
            for (int i = 0; i < tabButtons.Length; i++)
                tabButtons[i].image.color = i == tab ? Color.white : TabIdle;

            foreach (var entry in _cards) Destroy(entry.card.gameObject);
            _cards.Clear();

            // Da so huu len dau, roi do hiem cao truoc.
            var list = units.Where(u => u != null && u.category == Tabs[tab].category)
                .OrderBy(u => Owned(u) == null)
                .ThenByDescending(u => u.rarity)
                .ThenBy(u => u.displayName);
            foreach (var unit in list)
            {
                var card = Instantiate(cardTemplate, grid);
                card.gameObject.SetActive(true);
                card.Bind(unit, Select);
                _cards.Add((unit, card));
            }
            Select(_cards.Count > 0 ? _cards[0].unit : null);
        }

        void Select(UnitDefinition unit)
        {
            _selected = unit;
            Refresh();
        }

        static OwnedUnit Owned(UnitDefinition unit) => SaveSystem.Data.Find(unit.id);

        HeroDefinition HeroOf(UnitDefinition unit) => heroes.FirstOrDefault(h => h != null && h.unitId == unit.id);
        CannonDefinition CannonOf(UnitDefinition unit) => cannons.FirstOrDefault(c => c != null && c.unitId == unit.id);

        void Refresh()
        {
            goldText.text = economy.Gold.ToString("N0");
            foreach (var (unit, card) in _cards)
                card.Show(unit, Owned(unit), config.shardsPerStar, unit == _selected);
            RefreshDetail();
        }

        void RefreshDetail()
        {
            var unit = _selected;
            bool any = unit != null;
            detailIcon.enabled = any && unit.icon != null;
            levelButton.gameObject.SetActive(any);
            starButton.gameObject.SetActive(any);
            bool heroTab = any && unit.category == UnitCategory.Hero;
            skillsTab.gameObject.SetActive(heroTab);
            gearTab.gameObject.SetActive(heroTab);
            bool gear = heroTab && _showGear;
            gearView.SetActive(gear);
            detailSkills.gameObject.SetActive(!gear);
            skillsTab.image.color = gear ? TabIdle : Color.white;
            gearTab.image.color = gear ? Color.white : TabIdle;
            if (!any)
            {
                detailName.text = detailSub.text = detailStats.text = detailSkills.text = "";
                return;
            }

            var owned = Owned(unit);
            var hero = HeroOf(unit);
            var cannon = CannonOf(unit);
            detailIcon.sprite = unit.icon;
            detailIcon.color = owned != null ? Color.white : TabIdle;
            detailName.text = hero != null && hero.displayName != unit.displayName ? $"{unit.displayName} · {hero.displayName}" : unit.displayName;

            int level = owned != null ? Mathf.Max(1, owned.level) : 1;
            int stars = owned?.stars ?? 0;
            detailSub.text = owned == null
                ? $"{unit.rarity}   ·   Chưa sở hữu — quay gacha để có"
                : $"{unit.rarity}   ·   Lv {level}   ·   {Stars(stars)}   ·   Mảnh {owned.shards}/{config.shardsPerStar}";

            int maxLevel = cannon != null ? cannon.maxLevel : Progression.MaxLevel;
            bool maxed = level >= maxLevel;
            detailStats.text = cannon != null ? CannonStats(cannon, level, maxed)
                : hero != null ? HeroStats(hero, level, maxed, heroTab ? equipment : null)
                : "";
            detailSkills.text = hero != null ? Skills(hero, stars) : "";
            if (gear) RefreshGear(unit, owned);

            int cost = cannon != null ? cannon.UpgradeCost(level) : Progression.LevelUpCost(level);
            levelButton.interactable = owned != null && !maxed && economy.Gold >= cost;
            levelLabel.text = owned == null ? "Nâng level"
                : maxed ? "Level tối đa"
                : $"Nâng level\n{cost:N0} vàng{(economy.Gold >= cost ? "" : " · thiếu")}";

            starButton.interactable = Progression.CanStarUp(owned, config.shardsPerStar);
            starLabel.text = owned != null && owned.stars >= Progression.MaxStars ? "Sao tối đa"
                : $"Nâng sao\nMảnh {owned?.shards ?? 0}/{config.shardsPerStar}";
        }

        /// <summary>So hien tai, kem so sau khi len 1 level (mau xanh) neu con nang duoc.</summary>
        static string Row(string label, float now, float next, bool showNext, string format = "0.#") =>
            showNext && !Mathf.Approximately(now, next)
                ? $"{label}: {now.ToString(format)}  <color={Up}>→ {next.ToString(format)}</color>"
                : $"{label}: {now.ToString(format)}";

        /// <summary>Cung cong thuc `Hero.Initialise` (level + sao + trang bi); `equipment` null = linh, khong co trang bi.</summary>
        static string HeroStats(HeroDefinition hero, int level, bool maxed, EquipmentConfig equipment)
        {
            Progression.Multipliers(hero.unitId, equipment, out float health, out float damage, out float rate);
            var sb = new StringBuilder();
            sb.AppendLine(Row("Máu", hero.maxHealth * health, hero.maxHealth * (health + Progression.LevelBonus), !maxed, "0"));
            sb.AppendLine(Row(hero.healsAllies ? "Hồi máu" : "Sát thương", hero.damage * damage,
                hero.damage * (damage + Progression.LevelBonus), !maxed));
            sb.AppendLine($"Tốc đánh: {hero.attackRate * rate:0.##}/s   ·   Tầm: {hero.range:0.#}");
            if (hero is SoldierDefinition soldier)
                sb.AppendLine($"Mỗi lần triệu hồi: {soldier.squadSize} con   ·   Hồi chiêu: {soldier.summonCooldown:0.#}s");
            sb.Append($"<color={Locked}>Level +{Progression.LevelBonus * 100f:0}% · Sao +{Progression.StarBonus * 100f:0}% máu và sát thương gốc</color>");
            return sb.ToString();
        }

        static string CannonStats(CannonDefinition cannon, int level, bool maxed)
        {
            var sb = new StringBuilder();
            sb.AppendLine(Row("Sát thương", Progression.CannonDamage(cannon, level), Progression.CannonDamage(cannon, level + 1), !maxed));
            sb.AppendLine($"Tốc bắn: {cannon.fireRate:0.##}/s   ·   Tầm: {cannon.range:0.#}");
            if (cannon.aoeRadius > 0f) sb.AppendLine($"Nổ lan: bán kính {cannon.aoeRadius:0.#}");
            if (cannon.slowPercent > 0f) sb.AppendLine($"Làm chậm {cannon.slowPercent * 100f:0}% trong {cannon.slowDuration:0.#}s");
            sb.Append($"<color={Locked}>Level +{cannon.damagePerLevel * 100f:0}% · Sao +{Progression.StarBonus * 100f:0}% sát thương gốc</color>");
            return sb.ToString();
        }

        /// <summary>Chieu cuoi luon mo; noi tai / chieu phu ghi "Mo o N★" khi chua du sao. O mo ta trong = hero khong co.</summary>
        static string Skills(HeroDefinition hero, int stars)
        {
            var sb = new StringBuilder();
            void Add(string tag, string text, int needStars)
            {
                if (string.IsNullOrEmpty(text)) return;
                bool open = stars >= needStars;
                sb.AppendLine(open
                    ? $"<b>{tag}</b> — {text}"
                    : $"<color={Locked}><b>{tag}</b> · Mở ở {needStars}★ — {text}</color>");
            }
            Add("Chiêu cuối", hero.ultimateText, 0);
            Add("Nội tại", hero.passiveText, hero.passiveStar);
            Add("Chiêu phụ", hero.minorText, hero.minorStar);
            Add("Chiêu phụ", hero.secondMinorText, hero.secondMinorStar);
            return sb.Length == 0 ? "" : "KỸ NĂNG\n" + sb.ToString().TrimEnd();
        }

        void LevelUp()
        {
            var owned = _selected != null ? Owned(_selected) : null;
            if (owned == null) return;
            var cannon = CannonOf(_selected);
            int level = Mathf.Max(1, owned.level);
            if (level >= (cannon != null ? cannon.maxLevel : Progression.MaxLevel)) return;
            // TrySpend da ghi save + ban Changed -> Refresh; level phai tang TRUOC de Refresh doc so moi.
            int cost = cannon != null ? cannon.UpgradeCost(level) : Progression.LevelUpCost(level);
            if (economy.Gold < cost) return;
            owned.level = level + 1;
            economy.TrySpend(cost);
        }

        // ---------------------------------------------------------------- trang bi

        OwnedEquipment SelectedGear() => _selected != null ? Armory.Equipped(_selected.id, _slot) : null;

        static string StatName(EquipmentSlot slot) => slot switch
        {
            EquipmentSlot.Weapon => "sát thương",
            EquipmentSlot.Ring => "tốc đánh",
            _ => "máu",
        };

        string Describe(OwnedEquipment item) =>
            $"+{equipment.Bonus(item.slot, item.rarity, item.level) * 100f:0.#}% {StatName(item.slot)}";

        void RefreshGear(UnitDefinition unit, OwnedUnit owned)
        {
            foreach (Transform child in slotRow) Destroy(child.gameObject);
            foreach (Transform child in bagRow) Destroy(child.gameObject);

            for (int i = 0; i < 4; i++)
            {
                var slot = (EquipmentSlot)i;
                var item = Armory.Equipped(unit.id, slot);
                string text = item != null
                    ? $"{EquipmentConfig.SlotName(slot)} Lv{item.level}\n{Describe(item)}"
                    : $"{EquipmentConfig.SlotName(slot)}\nTrống";
                Cell(slotRow).Show(rarityFrames[item != null ? (int)item.rarity : 0], equipment.Icon(slot), text,
                    item != null, slot == _slot, () => { _slot = slot; Refresh(); });
            }

            var selected = SelectedGear();
            var bag = Armory.Bag(_slot);
            if (owned != null)
                foreach (var item in bag.Take(BagShown))
                {
                    var target = item;
                    Cell(bagRow).Show(rarityFrames[(int)item.rarity], equipment.Icon(item.slot),
                        $"{item.rarity} Lv{item.level}\n{Describe(item)}", true, false,
                        () => { Armory.Equip(target, unit.id); Refresh(); });
                }

            string slotName = EquipmentConfig.SlotName(_slot);
            gearInfo.text = owned == null ? "Chưa sở hữu hero — chưa mặc được đồ."
                : (selected != null ? $"{slotName} {selected.rarity} Lv {selected.level}: {Describe(selected)}"
                    : $"{slotName}: trống")
                + $"\nTúi: {bag.Count} món {slotName.ToLower()}"
                + (bag.Count > BagShown ? $" (hiện {BagShown} món tốt nhất)" : "")
                + (bag.Count > 0 ? " — bấm để mặc" : " — thắng wave để nhận đồ");

            bool maxed = selected != null && selected.level >= equipment.maxLevel;
            int cost = selected != null ? equipment.UpgradeCost(selected.rarity, selected.level) : 0;
            upgradeButton.interactable = selected != null && !maxed && economy.Gold >= cost;
            upgradeLabel.text = selected == null ? "Nâng cấp"
                : maxed ? "Đồ tối đa"
                : $"Nâng cấp\n{cost:N0} vàng";
            unequipButton.interactable = selected != null;
        }

        EquipCellView Cell(Transform parent)
        {
            var cell = Instantiate(cellTemplate, parent);
            cell.gameObject.SetActive(true);
            return cell;
        }

        void UpgradeGear()
        {
            var item = SelectedGear();
            if (item == null || item.level >= equipment.maxLevel) return;
            int cost = equipment.UpgradeCost(item.rarity, item.level);
            if (economy.Gold < cost) return;
            item.level++;              // tang TRUOC: TrySpend ghi save + ban Changed -> Refresh doc so moi
            economy.TrySpend(cost);
        }

        void StarUp()
        {
            var owned = _selected != null ? Owned(_selected) : null;
            if (Progression.TryStarUp(owned, config.shardsPerStar)) Refresh();
        }
    }
}
