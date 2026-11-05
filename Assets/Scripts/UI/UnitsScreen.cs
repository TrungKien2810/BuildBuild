using System.Collections.Generic;
using System.Linq;
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
    ///
    /// Art UI huong C dot 5 (2026-09-29): tab/nut tron doi SPRITE khi chon (khong tint), unit dung tren be da (frame idle
    /// dau tien, khong co thi icon), sao / Lv / thanh manh la widget, chi so la cac o `stat-chip` (icon + ten + so).
    /// </summary>
    public class UnitsScreen : MonoBehaviour
    {
        static readonly (string name, UnitCategory category)[] Tabs =
        {
            ("Hero", UnitCategory.Hero),
            ("Soldiers", UnitCategory.Soldier),
            ("Towers", UnitCategory.Cannon),
        };

        static readonly Color TabIdle = new Color(0.6f, 0.6f, 0.6f);
        const string Up = "#7CFC7C";
        const string Locked = "#9A9A9A";

        [SerializeField] GachaConfig config;
        [SerializeField] Economy economy;
        [Tooltip("Moi unit gacha (ca unit chua vao pool) — luoi hien du, unit chua so huu thi xam.")]
        [SerializeField] UnitDefinition[] units;
        [Tooltip("Pivot CHAN (0..1) cua frame idle dau tung unit, cung thu tu `units` — SceneBuilder do tu alpha PNG. "
            + "Pivot trong sprite khong dung duoc: x luon giua o, y lech (Hawkfletch 2 px, Frostweaver 12 px).")]
        [SerializeField] Vector2[] footPivots;
        [Tooltip("Chi so hero + linh (SoldierDefinition), khop the qua `unitId`.")]
        [SerializeField] HeroDefinition[] heroes;
        [SerializeField] CannonDefinition[] cannons;
        [SerializeField] EquipmentConfig equipment;
        [SerializeField] HeroRoleCatalog roles;

        [Header("Widget")]
        [SerializeField] Text goldText;
        [Tooltip("3 tab, cung thu tu voi `Tabs`.")]
        [SerializeField] Button[] tabButtons;
        [SerializeField] Sprite tabNormal;
        [SerializeField] Sprite tabActive;
        [SerializeField] Button backButton;
        [SerializeField] Transform grid;
        [SerializeField] UnitCardView cardTemplate;

        [Header("Bo loc vai (chi tab Hero)")]
        [SerializeField] GameObject roleFilter;
        [Tooltip("Nut dau = Tat ca, sau do cung thu tu `roles.entries`. Nut dang chon dung sprite `roundActive`.")]
        [SerializeField] Button[] roleButtons;

        [Header("Chi tiet")]
        [SerializeField] Image detailIcon;
        [SerializeField] Text detailName;
        [SerializeField] Text detailSub;
        [Tooltip("Khoi ky nang: cac dong icon + chu. An o tab Trang bi.")]
        [SerializeField] GameObject detailSkills;
        [Tooltip("Moi dong ky nang: icon ben trai, chu ben phai; dong = cha cua o chu.")]
        [SerializeField] Image[] skillIcons;
        [SerializeField] Text[] skillTexts;
        [SerializeField] Image[] detailStars;
        [SerializeField] Sprite starOn;
        [SerializeField] Sprite starOff;
        [SerializeField] GameObject detailLevelBadge;
        [SerializeField] Text detailLevel;
        [SerializeField] GameObject detailShardBar;
        [SerializeField] RectTransform detailShardFill;
        [SerializeField] Image detailShardFillImage;
        [SerializeField] Text detailShardText;
        [SerializeField] Sprite shardFillSprite;
        [SerializeField] Sprite shardFullSprite;

        [Header("O chi so (2 cot)")]
        [SerializeField] GameObject[] chipRoots;
        [SerializeField] Image[] chipIcons;
        [SerializeField] Text[] chipLabels;
        [SerializeField] Text[] chipValues;
        [Tooltip("Dong xam canh tieu de CHI SO: % tang moi level / sao.")]
        [SerializeField] Text statNote;
        [SerializeField] Sprite iconHp;
        [SerializeField] Sprite iconDamage;
        [SerializeField] Sprite iconAttackSpeed;
        [SerializeField] Sprite iconRange;
        [SerializeField] Sprite iconSquad;
        [SerializeField] Sprite iconCooldown;
        [SerializeField] Sprite iconSplash;
        [SerializeField] Sprite iconSlow;
        [SerializeField] Button levelButton;
        [SerializeField] Text levelLabel;
        [SerializeField] Button starButton;
        [SerializeField] Text starLabel;

        [Header("Trang bi (chi hero)")]
        [Tooltip("Hai nut doi cot giua giua Ky nang va Trang bi (GDD 10.3). Linh / tru an ca hai.")]
        [SerializeField] Button skillsTab;
        [SerializeField] Button gearTab;
        [SerializeField] GameObject tabDivider;
        [SerializeField] Sprite roundNormal;
        [SerializeField] Sprite roundActive;
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
        int _tab;
        HeroRole? _roleFilter;    // null = Tat ca
        bool _showGear;
        EquipmentSlot _slot;
        const int BagShown = 8;

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
            for (int i = 0; i < roleButtons.Length; i++)
            {
                HeroRole? role = i == 0 ? (HeroRole?)null : roles.entries[i - 1].role;
                roleButtons[i].onClick.AddListener(() => { _roleFilter = role; SelectTab(_tab); });
            }
            levelButton.onClick.AddListener(LevelUp);
            starButton.onClick.AddListener(StarUp);
            cellTemplate.gameObject.SetActive(false);
            skillsTab.onClick.AddListener(() => { _showGear = false; Refresh(); });
            gearTab.onClick.AddListener(() => { _showGear = true; Refresh(); });
            upgradeButton.onClick.AddListener(UpgradeGear);
            unequipButton.onClick.AddListener(() =>
            {
                var item = SelectedGear();
                if (item != null)
                {
                    Armory.Unequip(item);
                    Sfx.Play("unequip");
                }
                Refresh();
            });
            economy.Changed += _ => Refresh();
            SelectTab(0);
        }

        void SelectTab(int tab)
        {
            _tab = tab;
            for (int i = 0; i < tabButtons.Length; i++)
                tabButtons[i].image.sprite = i == tab ? tabActive : tabNormal;
            bool heroTab = Tabs[tab].category == UnitCategory.Hero;
            roleFilter.SetActive(heroTab);
            for (int i = 0; i < roleButtons.Length; i++)
                roleButtons[i].image.sprite = (i == 0 ? _roleFilter == null : _roleFilter == roles.entries[i - 1].role) ? roundActive : roundNormal;

            foreach (var entry in _cards) Destroy(entry.card.gameObject);
            _cards.Clear();

            // Da so huu len dau, roi do hiem cao truoc.
            var list = units.Where(u => u != null && u.category == Tabs[tab].category)
                .Where(u => !heroTab || _roleFilter == null || RoleOf(u) == _roleFilter)
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
        HeroRole RoleOf(UnitDefinition unit) => HeroOf(unit)?.role ?? HeroRole.None;

        void Refresh()
        {
            goldText.text = economy.Gold.ToString("N0");
            foreach (var (unit, card) in _cards)
                card.Show(unit, Owned(unit), config.shardsPerStar, unit == _selected, roles.IconOf(RoleOf(unit)));
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
            tabDivider.SetActive(heroTab);
            bool gear = heroTab && _showGear;
            gearView.SetActive(gear);
            detailSkills.SetActive(!gear);
            skillsTab.image.sprite = gear ? roundNormal : roundActive;
            gearTab.image.sprite = gear ? roundActive : roundNormal;
            if (!any)
            {
                detailName.text = detailSub.text = statNote.text = "";
                ShowSkills(null, 0);
                ShowChips(new List<(Sprite, string, string)>());
                detailLevelBadge.SetActive(false);
                detailShardBar.SetActive(false);
                foreach (var star in detailStars) star.gameObject.SetActive(false);
                return;
            }

            var owned = Owned(unit);
            var hero = HeroOf(unit);
            var cannon = CannonOf(unit);
            var anims = cannon != null ? cannon.animations : hero != null ? hero.animations : null;
            detailIcon.sprite = anims != null && anims.idle.IsValid ? anims.idle.frames[0] : unit.icon;
            detailIcon.enabled = detailIcon.sprite != null;
            if (detailIcon.sprite != null) StandOnPedestal(detailIcon.rectTransform, detailIcon.sprite, FootPivot(unit, detailIcon.sprite));
            detailIcon.color = owned != null ? Color.white : TabIdle;
            detailName.text = hero != null && hero.displayName != unit.displayName ? $"{unit.displayName} · {hero.displayName}" : unit.displayName;

            int level = owned != null ? Mathf.Max(1, owned.level) : 1;
            int stars = owned?.stars ?? 0;
            var role = RoleOf(unit);
            string kind = role == HeroRole.None ? Tabs[TabOf(unit)].name : roles.NameOf(role);
            detailSub.text = owned == null
                ? $"{unit.rarity}   ·   {kind}   ·   Not Owned — roll gacha to obtain"
                : $"{unit.rarity}   ·   {kind}";
            detailLevelBadge.SetActive(owned != null);
            detailShardBar.SetActive(owned != null);
            foreach (var star in detailStars) star.gameObject.SetActive(owned != null);
            if (owned != null)
            {
                detailLevel.text = $"Lv {level}";
                UnitCardView.SetStars(detailStars, stars, starOn, starOff);
                UnitCardView.SetShards(detailShardFill, detailShardFillImage, owned, config.shardsPerStar, shardFillSprite, shardFullSprite);
                detailShardText.text = stars >= Progression.MaxStars ? "Max" : $"{owned.shards}/{config.shardsPerStar}";
            }

            bool maxed = level >= Progression.MaxLevel(stars);
            string note = "";
            ShowChips(cannon != null ? CannonStats(cannon, level, maxed, out note)
                : hero != null ? HeroStats(hero, level, maxed, heroTab ? equipment : null, out note)
                : new List<(Sprite, string, string)>());
            statNote.text = note;
            ShowSkills(hero, stars);
            if (gear) RefreshGear(unit, owned);

            int cost = cannon != null ? cannon.UpgradeCost(level) : Progression.LevelUpCost(level);
            levelButton.interactable = owned != null && !maxed && economy.Gold >= cost;
            levelLabel.text = owned == null ? "Level Up"
                : maxed ? $"Max Lv {level}\nStar Up to Unlock More"
                : $"Level Up\n{cost:N0} Gold{(economy.Gold >= cost ? "" : " · missing")}";

            starButton.interactable = Progression.CanStarUp(owned, config.shardsPerStar);
            starLabel.text = owned != null && owned.stars >= Progression.MaxStars ? "Max Stars"
                : $"Star Up\nShards {owned?.shards ?? 0}/{config.shardsPerStar}";
        }

        /// <summary>Pivot chan builder da do cho unit; icon du phong (khong co frame idle) thi dung giua day anh.</summary>
        Vector2 FootPivot(UnitDefinition unit, Sprite sprite)
        {
            int i = System.Array.IndexOf(units, unit);
            bool measured = sprite != unit.icon && footPivots != null && i >= 0 && i < footPivots.Length && footPivots[i] != Vector2.zero;
            return measured ? footPivots[i] : new Vector2(0.5f, 0f);
        }

        /// <summary>
        /// Anchor cua `detailIcon` nam o tam mat be; pivot rect = pivot chan cua unit thi chan dung giua be du nhan vat
        /// nam lech trong o. O nho (<= 160 px) phong 2x nhu tren san.
        /// </summary>
        static void StandOnPedestal(RectTransform rect, Sprite sprite, Vector2 foot)
        {
            var size = sprite.rect.size;
            rect.sizeDelta = size * (size.y <= 160f ? 2f : 1f);
            rect.pivot = foot;
        }

        /// <summary>Vi tri tab cua unit (thu tu `Tabs`) — enum `UnitCategory` khong cung thu tu voi tab.</summary>
        static int TabOf(UnitDefinition unit) => System.Array.FindIndex(Tabs, t => t.category == unit.category);

        /// <summary>So hien tai, kem so sau khi len 1 level (mau xanh) neu con nang duoc.</summary>
        static string Value(float now, float next, bool showNext, string format = "0.#") =>
            showNext && !Mathf.Approximately(now, next)
                ? $"{now.ToString(format)} <color={Up}>→ {next.ToString(format)}</color>"
                : now.ToString(format);

        void ShowChips(List<(Sprite icon, string label, string value)> rows)
        {
            for (int i = 0; i < chipRoots.Length; i++)
            {
                bool on = i < rows.Count;
                chipRoots[i].SetActive(on);
                if (!on) continue;
                chipIcons[i].sprite = rows[i].icon;
                chipLabels[i].text = rows[i].label;
                chipValues[i].text = rows[i].value;
            }
        }

        /// <summary>Cung cong thuc `Hero.Initialise` (level + sao + trang bi); `equipment` null = linh, khong co trang bi.</summary>
        List<(Sprite, string, string)> HeroStats(HeroDefinition hero, int level, bool maxed, EquipmentConfig equipment, out string note)
        {
            Progression.Multipliers(hero.unitId, equipment, out float health, out float damage, out float rate);
            var rows = new List<(Sprite, string, string)>
            {
                (iconHp, "HP", Value(hero.maxHealth * health, hero.maxHealth * (health + Progression.LevelBonus), !maxed, "0")),
                (hero.healsAllies ? iconHp : iconDamage, hero.healsAllies ? "Heal" : "Damage",
                    Value(hero.damage * damage, hero.damage * (damage + Progression.LevelBonus), !maxed)),
                (iconAttackSpeed, "Attack Speed", $"{hero.attackRate * rate:0.##}/s"),
                (iconRange, "Range", $"{hero.range:0.#}"),
            };
            if (hero is SoldierDefinition soldier)
            {
                rows.Add((iconSquad, "Per Summon", $"{soldier.squadSize} con"));
                rows.Add((iconCooldown, "Cooldown", $"{soldier.summonCooldown:0.#}s"));
            }
            note = $"Level +{Progression.LevelBonus * 100f:0}% · Stars +{Progression.StarBonus * 100f:0}% base HP, Damage";
            return rows;
        }

        List<(Sprite, string, string)> CannonStats(CannonDefinition cannon, int level, bool maxed, out string note)
        {
            var kit = cannon.dragonKit;
            var rows = new List<(Sprite, string, string)>
            {
                (iconDamage, kit != null ? "Damage / s" : "Damage",
                    Value(Progression.CannonDamage(cannon, level), Progression.CannonDamage(cannon, level + 1), !maxed)),
            };
            // Dau Rong Da khong co "toc ban": no phun lien tuc theo nhip rieng nen dong do doi thanh nhip phun.
            rows.Add(kit != null
                ? (iconAttackSpeed, "Breath", $"{kit.breathSeconds:0.#}s · rest {kit.restSeconds:0.#}s")
                : (iconAttackSpeed, "Attack Speed", $"{cannon.fireRate:0.##}/s"));
            rows.Add((iconRange, "Range", $"{cannon.range:0.#}"));
            if (kit != null)
                rows.Add((iconSplash, "Burn", $"{kit.burnShare * 100f:0}% DPS · {kit.burnSeconds:0.#}s"));
            if (cannon.aoeRadius > 0f) rows.Add((iconSplash, "Splash Damage", $"radius {cannon.aoeRadius:0.#}"));
            if (cannon.slowPercent > 0f) rows.Add((iconSlow, "Slow", $"{cannon.slowPercent * 100f:0}% · {cannon.slowDuration:0.#}s"));
            note = $"Level +{cannon.damagePerLevel * 100f:0}% · Stars +{Progression.StarBonus * 100f:0}% base Damage";
            return rows;
        }

        /// <summary>Chieu cuoi luon mo; noi tai / chieu phu ghi "Mo o N★" khi chua du sao. O mo ta trong = hero khong co.</summary>
        void ShowSkills(HeroDefinition hero, int stars)
        {
            int row = 0;
            void Add(Sprite icon, string tag, string text, int needStars)
            {
                if (string.IsNullOrEmpty(text) || row >= skillTexts.Length) return;
                bool open = stars >= needStars;
                skillIcons[row].sprite = icon;
                skillIcons[row].enabled = icon != null;
                skillIcons[row].color = open ? Color.white : TabIdle;
                skillTexts[row].text = open
                    ? $"<b>{tag}</b> — {text}"
                    : $"<color={Locked}><b>{tag}</b> · Unlock at {needStars}★ — {text}</color>";
                skillTexts[row].transform.parent.gameObject.SetActive(true);
                row++;
            }
            if (hero != null)
            {
                Add(hero.ultimateIcon, "Ultimate", hero.ultimateText, 0);
                Add(hero.passiveIcon, "Passive", hero.passiveText, hero.passiveStar);
                Add(hero.minorIcon, "Secondary", hero.minorText, hero.minorStar);
                Add(hero.secondMinorIcon, "Secondary", hero.secondMinorText, hero.secondMinorStar);
                Add(hero.thirdMinorIcon, "Secondary", hero.thirdMinorText, hero.thirdMinorStar);
            }
            for (int i = row; i < skillTexts.Length; i++) skillTexts[i].transform.parent.gameObject.SetActive(false);
        }

        void LevelUp()
        {
            var owned = _selected != null ? Owned(_selected) : null;
            if (owned == null) return;
            var cannon = CannonOf(_selected);
            int level = Mathf.Max(1, owned.level);
            if (level >= Progression.MaxLevel(owned.stars)) return;
            // TrySpend da ghi save + ban Changed -> Refresh; level phai tang TRUOC de Refresh doc so moi.
            int cost = cannon != null ? cannon.UpgradeCost(level) : Progression.LevelUpCost(level);
            if (economy.Gold < cost) return;
            owned.level = level + 1;
            economy.TrySpend(cost);
            Sfx.Play("upgrade");
        }

        // ---------------------------------------------------------------- trang bi

        OwnedEquipment SelectedGear() => _selected != null ? Armory.Equipped(_selected.id, _slot) : null;

        static string StatName(EquipmentSlot slot) => slot switch
        {
            EquipmentSlot.Weapon => "Damage",
            EquipmentSlot.Ring => "Attack Speed",
            _ => "HP",
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
                    : $"{EquipmentConfig.SlotName(slot)}\nEmpty";
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
                        () => { Armory.Equip(target, unit.id); Sfx.Play("equip"); Refresh(); });
                }

            string slotName = EquipmentConfig.SlotName(_slot);
            gearInfo.text = owned == null ? "Hero not owned — cannot equip gear yet."
                : (selected != null ? $"{slotName} {selected.rarity} Lv {selected.level}: {Describe(selected)}"
                    : $"{slotName}: empty")
                + $"\nBag: {bag.Count} {slotName.ToLower()} items"
                + (bag.Count > BagShown ? $" (showing the best {BagShown} items)" : "")
                + (bag.Count > 0 ? " — press to equip" : " — win Waves to obtain gear");

            bool maxed = selected != null && selected.level >= equipment.maxLevel;
            int cost = selected != null ? equipment.UpgradeCost(selected.rarity, selected.level) : 0;
            upgradeButton.interactable = selected != null && !maxed && economy.Gold >= cost;
            upgradeLabel.text = selected == null ? "Upgrade"
                : maxed ? "Gear Maxed"
                : $"Upgrade\n{cost:N0} Gold{(economy.Gold >= cost ? "" : " · missing")}";
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
            Sfx.Play("upgrade");
        }

        void StarUp()
        {
            var owned = _selected != null ? Owned(_selected) : null;
            if (!Progression.TryStarUp(owned, config.shardsPerStar)) return;
            Sfx.Play("upgrade");
            Refresh();
        }
    }
}
