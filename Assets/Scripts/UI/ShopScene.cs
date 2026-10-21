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
    /// Scene Cua hang (GDD 10.4) — thay cho bang phu `ShopScreen` trong scene Battle (2026-10-20): cua hang
    /// gio la mot man rieng nhu Doi quan, de sau nay them loai hang moi chi phai them tab + mot nhanh trong
    /// `Shop.Offers`, khong phai nhet them bang vao HUD tran.
    ///
    /// Bon tab = bon gia tri `ShopKind`: Daily · Weekly · Monthly · Offers. Chu ky lam moi va noi dung do
    /// `Meta/Shop` quyet; lop nay chi ve + goi `TryBuy`, khong tu tru vang.
    /// </summary>
    public class ShopScene : MonoBehaviour
    {
        const float MessageSeconds = 5f;

        [SerializeField] Economy economy;
        [SerializeField] EquipmentConfig equipment;
        [Tooltip("Pool unit (`GachaConfig.pool`) — save chi luu unitId, bang nay cho bac + ten + icon.")]
        [SerializeField] UnitDefinition[] units;

        [Header("Khung")]
        [SerializeField] Text goldText;
        [SerializeField] Text hintText;
        [SerializeField] Text messageText;
        [SerializeField] Button backButton;
        [SerializeField] Button[] tabButtons;
        [SerializeField] Sprite tabNormal;
        [SerializeField] Sprite tabActive;

        [Header("Dong hang (toi da `Shop.MaxSlots`)")]
        [SerializeField] GameObject[] rowRoots;
        [SerializeField] Image[] rowIcons;
        [SerializeField] Text[] rowNames;
        [SerializeField] Text[] rowDetails;
        [SerializeField] Button[] rowBuyButtons;
        [SerializeField] Text[] rowBuyLabels;

        static readonly string[] TabNames = { "Daily", "Weekly", "Monthly", "Offers" };

        ShopKind _kind = ShopKind.Daily;
        ShopOffer[] _offers;
        float _messageUntil;
        float _refreshAt;
        string _periodKey;

        void Start()
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                int index = i;
                tabButtons[i].onClick.AddListener(() => Select(index));
            }
            backButton.onClick.AddListener(TownScreen.ReturnFromShop);
            for (int i = 0; i < rowBuyButtons.Length; i++)
            {
                int index = i;
                rowBuyButtons[i].onClick.AddListener(() => Buy(index));
            }
            economy.Changed += _ => Refresh();
            Select(0);
        }

        void Select(int tab)
        {
            _kind = (ShopKind)Mathf.Clamp(tab, 0, TabNames.Length - 1);
            for (int i = 0; i < tabButtons.Length; i++)
                tabButtons[i].image.sprite = i == tab ? tabActive : tabNormal;
            Sfx.Play("ui-open");
            Refresh();
        }

        public void Refresh()
        {
            _offers = Shop.Offers(_kind, RarityOf);
            _periodKey = Shop.KeyOf(_kind);
            _refreshAt = Time.unscaledTime + 1f;
            goldText.text = economy.Gold.ToString("N0");
            hintText.text = $"{Shop.RestockLabel(_kind)}   ·   {Shop.Remaining(_kind, RarityOf)} left";

            for (int i = 0; i < rowRoots.Length; i++)
            {
                var offer = _offers != null && i < _offers.Length ? _offers[i] : default;
                bool show = offer.price > 0;
                rowRoots[i].SetActive(show);
                if (!show) continue;

                bool sold = Shop.Bought(_kind, i);
                if (offer.IsGear)
                {
                    rowIcons[i].sprite = equipment != null ? equipment.Icon(offer.slot) : null;
                    rowNames[i].text = equipment != null ? EquipmentConfig.SlotName(offer.slot) : offer.slot.ToString();
                    string stat = offer.slot == EquipmentSlot.Weapon ? "damage" : offer.slot == EquipmentSlot.Ring ? "attack speed" : "health";
                    float bonus = equipment != null ? equipment.Bonus(offer.slot, offer.rarity, 1) * 100 : 0;
                    rowDetails[i].text = $"{offer.rarity} / Lv.1 / +{bonus:0.#}% {stat} - equip in Army";
                }
                else
                {
                    var unit = Unit(offer.heroId);
                    rowIcons[i].sprite = unit != null ? unit.icon : null;
                    rowNames[i].text = unit != null && !string.IsNullOrEmpty(unit.displayName) ? unit.displayName : offer.heroId;
                    int owned = SaveSystem.Data.Find(offer.heroId)?.shards ?? 0;
                    rowDetails[i].text = $"{(unit != null ? unit.rarity.ToString() : "?")} / +{offer.shards} shards / Owned: {owned} - star up in Army";
                }
                // Icon khong co (unit ngoai pool, thieu art) thi TAT Image: Image khong sprite ve mot o TRANG duc.
                rowIcons[i].enabled = rowIcons[i].sprite != null;

                bool afford = economy.Gold >= offer.price;
                rowBuyLabels[i].text = sold ? "Sold" : $"{offer.price:N0} Gold";
                rowBuyLabels[i].color = sold || afford ? Color.white : new Color(1f, 0.55f, 0.5f);
                rowBuyButtons[i].interactable = !sold && afford;
            }
        }

        void Buy(int index)
        {
            if (_periodKey != Shop.KeyOf(_kind))
            {
                Refresh();
                ShowMessage("New stock has arrived. Please choose an item again.");
                return;
            }
            if (_offers == null || index >= _offers.Length) return;
            if (Shop.TryBuy(_kind, index, _offers[index], price => economy.TrySpend(price)))
            {
                Sfx.Play("coin");
                var bought = _offers[index];
                ShowMessage(bought.IsGear ? $"{bought.rarity} {EquipmentConfig.SlotName(bought.slot)} added to inventory. Equip in Army."
                    : $"+{bought.shards} shards for {Unit(bought.heroId)?.displayName ?? bought.heroId}. Star up in Army.");
                Refresh();
            }
            else
            {
                Sfx.Play("ui-error");
                ShowMessage("Not enough Gold");
            }
        }

        void ShowMessage(string text)
        {
            if (messageText == null) return;
            messageText.text = text;
            _messageUntil = Time.unscaledTime + MessageSeconds;
        }

        void Update()
        {
            if (Time.unscaledTime >= _refreshAt) Refresh();
            if (messageText == null || messageText.text.Length == 0 || Time.unscaledTime < _messageUntil) return;
            messageText.text = "";
        }

        UnitDefinition Unit(string unitId) =>
            string.IsNullOrEmpty(unitId) ? null : units.FirstOrDefault(u => u != null && u.id == unitId);

        Rarity RarityOf(string unitId) => Unit(unitId)?.rarity ?? Rarity.N;
    }
}
