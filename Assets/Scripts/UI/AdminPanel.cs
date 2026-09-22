using BuildBuild.Combat;
using BuildBuild.Core;
using BuildBuild.Meta;
using UnityEngine;

namespace BuildBuild.UI
{
    /// <summary>
    /// Bang test nhanh trong luc Play (IMGUI, khong can dung UI trong SceneBuilder). Nut "ADMIN" goc duoi-trai
    /// hoac phim F1 de mo/dong. Moi thao tac goi thang vao he thong that — khong co duong tat rieng.
    /// </summary>
    public class AdminPanel : MonoBehaviour
    {
        [SerializeField] WaveManager waves;
        [SerializeField] Castle castle;
        [SerializeField] Economy economy;
        [SerializeField] BattleHud hud;

        static readonly float[] Speeds = { 0.5f, 1f, 2f, 4f };

        bool _open;
        string _waveInput = "1";
        Vector2 _ownedScroll;

        void OnDisable() => Time.timeScale = 1f;   // roi scene luc dang tua nhanh: khong treo sang scene khac

        void OnGUI()
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F1)
            {
                _open = !_open;
                e.Use();
            }

            // Ve theo khung 1920x1080 nhu HUD.
            float scale = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float left = 24f, bottom = 1080f - 24f;

            if (GUI.Button(new Rect(left, bottom - 48f, 120f, 48f), _open ? "ADMIN ▼" : "ADMIN ▲")) _open = !_open;
            if (!_open) return;

            const float panelHeight = 720f;
            GUILayout.BeginArea(new Rect(left, bottom - 48f - 12f - panelHeight, 360f, panelHeight), GUI.skin.box);
            GUILayout.Label($"Wave tiep: {waves.NextWave}   {(waves.IsFighting ? "(dang danh)" : "")}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1.000 vang")) economy.Add(1000);
            if (GUILayout.Button("+10.000 vang")) economy.Add(10000);
            GUILayout.EndHorizontal();

            GUI.enabled = !waves.IsFighting;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Wave -1")) SetWave(waves.NextWave - 1);
            if (GUILayout.Button("Wave +1")) SetWave(waves.NextWave + 1);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            _waveInput = GUILayout.TextField(_waveInput, GUILayout.Width(80f));
            if (GUILayout.Button("Nhay toi wave") && int.TryParse(_waveInput, out int wave)) SetWave(wave);
            GUILayout.EndHorizontal();
            GUI.enabled = true;

            GUI.enabled = waves.IsFighting;
            if (GUILayout.Button("Giet het quai")) waves.DevKillAll();
            GUI.enabled = true;

            if (GUILayout.Button("Day mana moi hero / linh"))
                foreach (var hero in Hero.Alive.ToArray()) hero.DevFillMana();

            if (GUILayout.Button("Hoi day mau lau dai"))
            {
                castle.Repair();
                hud.Refresh();
            }
            castle.Health.Invulnerable = GUILayout.Toggle(castle.Health.Invulnerable, " Lau dai bat tu");

            GUILayout.Label($"Toc do game: x{Time.timeScale:0.#}");
            GUILayout.BeginHorizontal();
            foreach (float speed in Speeds)
                if (GUILayout.Button($"x{speed:0.#}")) Time.timeScale = speed;
            GUILayout.EndHorizontal();

            GUILayout.Label("Da so huu (xoa khoi kho — khong hoan vang/thach):");
            var owned = SaveSystem.Data.owned;
            _ownedScroll = GUILayout.BeginScrollView(_ownedScroll, GUILayout.Height(160f));
            for (int i = owned.Count - 1; i >= 0; i--)
            {
                var unit = owned[i];
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{unit.id}  Lv{unit.level} {unit.stars}★ {unit.shards}mv", GUILayout.Width(240f));
                if (GUILayout.Button("Xoa", GUILayout.Width(60f)))
                {
                    owned.RemoveAt(i);
                    SaveSystem.Save();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        void SetWave(int wave)
        {
            waves.DevSetWave(wave);
            _waveInput = waves.NextWave.ToString();
            hud.Refresh();
        }
    }
}
