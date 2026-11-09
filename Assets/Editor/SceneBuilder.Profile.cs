using BuildBuild.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BuildBuild.Editor
{
    public static partial class SceneBuilder
    {
        /// <summary>Bang nguoi choi: bam huy hieu chan dung o goc tren-trai HUD. Nut dong ten "Close" de Esc (BackKey) dong duoc.</summary>
        static PlayerProfileScreen BuildPlayerProfileScreen(RectTransform canvas, Button openButton)
        {
            var root = FullScreen("PlayerProfileScreen", canvas);
            var screen = root.gameObject.AddComponent<PlayerProfileScreen>();
            var dim = Img("Dim", root, UiSprite("chrome/dim-tile"), Center, Vector2.zero, Vector2.zero);
            Stretch(dim.rectTransform, 0, 0, 0, 0);
            dim.color = new Color(0, 0, 0, .85f);
            dim.raycastTarget = true;
            var panel = Img("Panel", root, UiSprite("panels/panel-hero"), Center, Vector2.zero, new Vector2(1100, 660), sliced: true);
            panel.raycastTarget = true;

            // Dau bang: chan dung trong khung (cung avatar voi huy hieu HUD) + Lv + thanh XP + phan thuong cap ke.
            var frame = Img("Portrait", panel.transform, UiSprite("panels/portrait-frame-big"), TopLeft, new Vector2(64, -56), new Vector2(160, 160), sliced: true);
            var portrait = Img("Avatar", frame.transform, UiSprite("avatars/avatar-frostweaver"), Center, Vector2.zero, new Vector2(112, 112));
            portrait.preserveAspect = true;
            var title = Label("Title", panel.transform, "", 48, TopLeft, new Vector2(256, -52), new Vector2(300, 60));
            title.alignment = TextAnchor.MiddleLeft;
            SetRef(screen, "title", title);
            var xpFrame = Img("Xp", panel.transform, UiSprite("chrome/bar-frame"), TopLeft, new Vector2(256, -122), new Vector2(740, 44), sliced: true);
            var xpFill = Img("Fill", xpFrame.transform, UiSprite("chrome/bar-fill-xp"), Center, Vector2.zero, Vector2.zero);
            Stretch(xpFill.rectTransform, 6 * UiScale, 5 * UiScale, 4 * UiScale, 4 * UiScale);
            xpFill.type = Image.Type.Filled;
            xpFill.fillMethod = Image.FillMethod.Horizontal;
            SetRef(screen, "xpFill", xpFill);
            SetRef(screen, "xpText", Label("Value", xpFrame.transform, "", 26, Center, Vector2.zero, new Vector2(740, 44)));
            var next = Label("Next", panel.transform, "", 26, TopLeft, new Vector2(256, -176), new Vector2(740, 40));
            next.alignment = TextAnchor.MiddleLeft;
            SetRef(screen, "nextText", next);

            // Luoi chi so 2 cot x 3 hang: moi o la mot stat-strip, nhan ben trai + gia tri ben phai (Refresh chi ghi gia tri).
            string[] labels = { "Best wave", "Boss Rush best", "Boss Rush clears", "Conquest streak", "Units owned", "Free Summons" };
            var values = new Text[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                var cell = Img($"Stat{i + 1}", panel.transform, UiSprite("panels/stat-strip"), TopLeft,
                    new Vector2(64 + (i % 2) * 492, -270 - (i / 2) * 84), new Vector2(480, 72), sliced: true);
                var name = Label("Name", cell.transform, labels[i], 26, MidLeft, new Vector2(24, 0), new Vector2(280, 40));
                name.alignment = TextAnchor.MiddleLeft;
                var value = Label("Value", cell.transform, "", 30, MidRight, new Vector2(-24, 0), new Vector2(170, 40));
                value.alignment = TextAnchor.MiddleRight;
                values[i] = value;
            }
            SetRefs(screen, "statValues", values);

            SetRef(screen, "closeButton", MakeButton("Close", panel.transform, "Close", Bottom, new Vector2(0, 34), new Vector2(380, 80), out _));
            SetRef(screen, "openButton", openButton);
            SetRef(screen, "root", root.gameObject);
            return screen;
        }
    }
}
