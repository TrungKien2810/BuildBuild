using BuildBuild.Core;
using BuildBuild.Data;
using BuildBuild.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BuildBuild.Editor
{
    public static partial class SceneBuilder
    {
        [MenuItem("BuildBuild/Scenes/Rebuild Meta UI")]
        public static void RebuildMetaUI()
        {
            _font = PixelFont.Asset;
            var tuning = AssetDatabase.LoadAssetAtPath<WaveTuning>(DataDir + "/WaveTuning.asset");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Enemy.prefab");
            BuildBattleScene(tuning, prefab.GetComponent<BuildBuild.Combat.Enemy>());
            BuildShopScene();
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath(SceneNames.Battle));
        }

        static ChallengeScreen BuildChallengeScreen(RectTransform canvas, WaveManager waves, WaveTuning tuning)
        {
            var root = Rect("ChallengeScreen", canvas, Center, Vector2.zero, new Vector2(RefWidth, RefHeight));
            var screen = root.gameObject.AddComponent<ChallengeScreen>();
            var dim = Img("Dim", root, UiSprite("chrome/dim-tile"), Center, Vector2.zero, Vector2.zero);
            Stretch(dim.rectTransform, 0, 0, 0, 0);
            dim.color = new Color(0, 0, 0, .85f);
            dim.raycastTarget = true;
            var panel = Img("Panel", root, UiSprite("panels/panel-hero"), Center, Vector2.zero, new Vector2(1440, 920), sliced: true);
            panel.raycastTarget = true;
            SetRef(screen, "title", Label("Title", panel.transform, "", 42, Top, new Vector2(0, -24), new Vector2(1280, 60)));
            SetRef(screen, "subtitle", Label("Subtitle", panel.transform, "", 26, Top, new Vector2(0, -92), new Vector2(1320, 50)));
            var names = new Object[3]; var states = new Object[3]; var icons = new Object[3];
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 440;
                if (i < 2) Label("Path", panel.transform, ">", 36, Top, new Vector2(x + 220, -280), new Vector2(40, 60));
                var node = Img("Encounter" + i, panel.transform, UiSprite("panels/portrait-frame-big"), Top, new Vector2(x, -164), new Vector2(392, 330), sliced: true);
                var icon = Img("Boss", node.transform, null, Top, new Vector2(0, -8), new Vector2(190, 170));
                icon.preserveAspect = true; icons[i] = icon;
                names[i] = Label("Name", node.transform, "", 26, Top, new Vector2(0, -174), new Vector2(370, 90));
                states[i] = Label("Progress", node.transform, "", 22, Bottom, new Vector2(0, 8), new Vector2(370, 56));
            }
            SetRef(screen, "rules", Label("Rules", panel.transform, "", 26, Top, new Vector2(0, -516), new Vector2(1340, 82)));
            var reward = Img("Reward", panel.transform, UiSprite("panels/reward-row"), Top, new Vector2(0, -614), new Vector2(1320, 100), sliced: true);
            Img("Gold", reward.transform, UiSprite("icons/icon-gold"), MidLeft, new Vector2(48, 0), new Vector2(64, 64));
            SetRef(screen, "reward", Label("Value", reward.transform, "", 24, Center, new Vector2(35, 0), new Vector2(1160, 94)));
            SetRef(screen, "status", Label("Status", panel.transform, "", 24, Top, new Vector2(0, -728), new Vector2(1340, 48)));
            SetRef(screen, "closeButton", MakeButton("Back", panel.transform, "Back to Lobby", Bottom, new Vector2(-260, 34), new Vector2(380, 80), out _));
            SetRef(screen, "startButton", MakeButton("Start", panel.transform, "", Bottom, new Vector2(260, 34), new Vector2(420, 80), out var startLabel));
            SetRef(screen, "startLabel", startLabel);
            SetRefs(screen, "nodeNames", names); SetRefs(screen, "nodeStates", states); SetRefs(screen, "nodeIcons", icons);
            SetRefs(screen, "dailyIcons", new Object[] { UiSprite("icons/icon-board-attack"), UiSprite("icons/icon-quest-scroll"), UiSprite("icons/icon-chest") });
            SetRef(screen, "root", root.gameObject); SetRef(screen, "waves", waves); SetRef(screen, "tuning", tuning);
            return screen;
        }
    }
}
