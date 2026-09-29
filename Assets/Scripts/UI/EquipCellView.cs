using System;
using UnityEngine;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>Mot o trang bi trong man Doi quan — dung chung cho 4 o dang mac va cac mon trong tui.</summary>
    public class EquipCellView : MonoBehaviour
    {
        static readonly Color Empty = new Color(0.4f, 0.4f, 0.4f);

        [SerializeField] Button button;
        [SerializeField] Image frame;
        [SerializeField] Image icon;
        [SerializeField] Text label;
        [SerializeField] Outline selectedOutline;

        public void Show(Sprite frameSprite, Sprite iconSprite, string text, bool filled, bool selected, Action onClick)
        {
            frame.sprite = frameSprite;
            icon.sprite = iconSprite;
            icon.enabled = iconSprite != null;
            frame.color = icon.color = filled ? Color.white : Empty;
            label.text = text;
            selectedOutline.enabled = selected;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick());
        }
    }
}
