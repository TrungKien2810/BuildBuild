using BuildBuild.Data;
using BuildBuild.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>Mot the ket qua gacha: vien theo do hiem, icon unit, ten, va ghi chu MOI / +manh.</summary>
    public class GachaCardView : MonoBehaviour
    {
        [SerializeField] Image frame;
        [SerializeField] Image icon;
        [SerializeField] Text nameText;
        [SerializeField] Text noteText;

        [Header("Vien theo do hiem")]
        [SerializeField] Sprite frameN;
        [SerializeField] Sprite frameR;
        [SerializeField] Sprite frameSR;
        [SerializeField] Sprite frameSSR;

        public void Show(GachaResult result)
        {
            var unit = result.unit;
            frame.sprite = unit.rarity switch
            {
                Rarity.SSR => frameSSR,
                Rarity.SR => frameSR,
                Rarity.R => frameR,
                _ => frameN,
            };
            icon.sprite = unit.icon;
            icon.enabled = unit.icon != null;
            nameText.text = $"{unit.displayName}\n{unit.rarity}";
            noteText.text = result.isNew ? "MỚI"
                : result.starUp ? $"+{result.shardsGained} mảnh · LÊN SAO"
                : $"+{result.shardsGained} mảnh";
        }
    }
}
