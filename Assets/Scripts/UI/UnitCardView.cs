using System;
using BuildBuild.Data;
using BuildBuild.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>
    /// Mot the trong luoi man Doi quan (GDD muc 10.2): vien theo do hiem, icon, ten, vien `Lv N`, hang sao, thanh manh.
    /// Unit chua so huu dung khung xam `card-frame-locked` + o khoa, van hien hinh (xam) de nguoi choi biet co gi.
    /// The dang chon co vien vang (`Outline` tren nen the).
    /// </summary>
    public class UnitCardView : MonoBehaviour
    {
        static readonly Color Locked = new Color(0.35f, 0.35f, 0.35f);

        [SerializeField] Button button;
        [SerializeField] Image background;
        [SerializeField] Image frame;
        [SerializeField] Image icon;
        [SerializeField] Text nameText;
        [SerializeField] Outline selectedOutline;
        [Tooltip("Icon vai tro goc tren trai (GDD 10.2); an khi unit khong co vai (linh, phao).")]
        [SerializeField] Image roleIcon;

        [Header("Vien theo do hiem")]
        [SerializeField] Sprite frameN;
        [SerializeField] Sprite frameR;
        [SerializeField] Sprite frameSR;
        [SerializeField] Sprite frameSSR;
        [SerializeField] Sprite frameLocked;

        [Header("Level / sao / manh (an khi chua so huu)")]
        [SerializeField] GameObject levelBadge;
        [SerializeField] Text levelText;
        [SerializeField] Image[] stars;
        [SerializeField] Sprite starOn;
        [SerializeField] Sprite starOff;
        [SerializeField] GameObject shardBar;
        [SerializeField] RectTransform shardFill;
        [SerializeField] Image shardFillImage;
        [SerializeField] Sprite shardFillSprite;
        [SerializeField] Sprite shardFullSprite;
        [SerializeField] GameObject lockIcon;

        public void Bind(UnitDefinition unit, Action<UnitDefinition> onClick) =>
            button.onClick.AddListener(() => onClick(unit));

        public void Show(UnitDefinition unit, OwnedUnit owned, int shardsPerStar, bool selected, Sprite role)
        {
            bool has = owned != null;
            roleIcon.sprite = role;
            roleIcon.enabled = role != null;
            frame.sprite = !has ? frameLocked : unit.rarity switch
            {
                Rarity.SSR => frameSSR,
                Rarity.SR => frameSR,
                Rarity.R => frameR,
                _ => frameN,
            };
            icon.sprite = unit.icon;
            icon.enabled = unit.icon != null;

            var tint = has ? Color.white : Locked;
            background.color = icon.color = tint;
            nameText.text = has ? unit.displayName : $"{unit.displayName}\nChưa sở hữu";
            lockIcon.SetActive(!has);
            levelBadge.SetActive(has);
            shardBar.SetActive(has);
            foreach (var star in stars) star.gameObject.SetActive(has);
            if (has)
            {
                levelText.text = $"Lv {Mathf.Max(1, owned.level)}";
                SetStars(stars, owned.stars, starOn, starOff);
                SetShards(shardFill, shardFillImage, owned, shardsPerStar, shardFillSprite, shardFullSprite);
            }
            selectedOutline.enabled = selected;
        }

        /// <summary>
        /// Dong bang chon hero / linh tren san: "Lv N" + 5 sao (sao dong `row` o `stars[row*5 ..]`); chua so huu thi ghi
        /// thay va an sao.
        /// </summary>
        public static void ShowLevelAndStars(Text level, Image[] stars, int row, OwnedUnit owned, Sprite on, Sprite off)
        {
            level.text = owned == null ? "Chưa sở hữu" : $"Lv {Mathf.Max(1, owned.level)}";
            for (int k = 0; k < 5; k++)
            {
                var star = stars[row * 5 + k];
                star.enabled = owned != null;
                star.sprite = owned != null && k < owned.stars ? on : off;
            }
        }

        public static void SetStars(Image[] row, int count, Sprite on, Sprite off)
        {
            for (int i = 0; i < row.Length; i++)
                row[i].sprite = i < count ? on : off;
        }

        /// <summary>Ruot thanh manh keo theo anchorMax.x (sprite sliced, khong dung fillAmount); du manh nang sao thi doi mau xanh.</summary>
        public static void SetShards(RectTransform fill, Image fillImage, OwnedUnit owned, int shardsPerStar, Sprite normal, Sprite full)
        {
            float t = owned == null || owned.stars >= Progression.MaxStars ? 1f
                : Mathf.Clamp01(owned.shards / (float)Mathf.Max(1, shardsPerStar));
            fill.anchorMax = new Vector2(t, 1f);
            fill.gameObject.SetActive(t > 0f);
            fillImage.sprite = t >= 1f ? full : normal;
        }
    }
}
