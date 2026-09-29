using System;
using BuildBuild.Data;
using BuildBuild.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>
    /// Mot the trong luoi man Doi quan (GDD muc 10.2): vien theo do hiem, icon, ten, `Lv N` + sao, mang. Unit chua so huu
    /// xam toan bo nhung van hien hinh de nguoi choi biet co gi. The dang chon co vien vang (`Outline` tren nen the).
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

        [Header("Vien theo do hiem")]
        [SerializeField] Sprite frameN;
        [SerializeField] Sprite frameR;
        [SerializeField] Sprite frameSR;
        [SerializeField] Sprite frameSSR;

        public void Bind(UnitDefinition unit, Action<UnitDefinition> onClick) =>
            button.onClick.AddListener(() => onClick(unit));

        public void Show(UnitDefinition unit, OwnedUnit owned, int shardsPerStar, bool selected)
        {
            frame.sprite = unit.rarity switch
            {
                Rarity.SSR => frameSSR,
                Rarity.SR => frameSR,
                Rarity.R => frameR,
                _ => frameN,
            };
            icon.sprite = unit.icon;
            icon.enabled = unit.icon != null;

            var tint = owned != null ? Color.white : Locked;
            background.color = frame.color = icon.color = tint;
            nameText.text = owned == null
                ? $"{unit.displayName}\nChưa sở hữu"
                : $"{unit.displayName}\nLv {Mathf.Max(1, owned.level)}  {UnitsScreen.Stars(owned.stars)}\nMảnh {owned.shards}/{shardsPerStar}";
            selectedOutline.enabled = selected;
        }
    }
}
