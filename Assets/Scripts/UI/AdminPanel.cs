using BuildBuild.Combat;
using BuildBuild.Core;
using BuildBuild.Meta;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        [SerializeField] CannonBoard cannons;

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

            const float panelHeight = 760f;
            GUILayout.BeginArea(new Rect(left, bottom - 48f - 12f - panelHeight, 360f, panelHeight), GUI.skin.box);
            GUILayout.Label($"Next wave: {waves.NextWave}   {(waves.IsFighting ? "(fighting)" : "")}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1.000 Gold")) economy.Add(1000);
            if (GUILayout.Button("+10.000 Gold")) economy.Add(10000);
            GUILayout.EndHorizontal();

            GUI.enabled = !waves.IsFighting;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Wave -1")) SetWave(waves.NextWave - 1);
            if (GUILayout.Button("Wave +1")) SetWave(waves.NextWave + 1);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            _waveInput = GUILayout.TextField(_waveInput, GUILayout.Width(80f));
            if (GUILayout.Button("Jump to wave") && int.TryParse(_waveInput, out int wave)) SetWave(wave);
            GUILayout.EndHorizontal();
            GUI.enabled = true;

            GUI.enabled = waves.IsFighting;
            if (GUILayout.Button("Kill All Enemies")) waves.DevKillAll();
            if (SoldierCamp.Current != null && GUILayout.Button("Soldiers: Fill Cooldown Ring")) SoldierCamp.Current.DevFill();
            GUI.enabled = true;

            GUI.enabled = !waves.IsFighting;
            if (GUILayout.Button(WaveManager.InConquest ? "Leave Conquest" : "Conquest (Enter Preparation)"))
            {
                if (WaveManager.InConquest) waves.LeaveConquest();
                else waves.EnterConquest();
                hud.Refresh();
            }
            GUI.enabled = true;
            if (GUILayout.Button($"Reset Conquest Attempts ({ConquestMap.AttacksLeft}/{ConquestMap.AttacksPerDay})"))
                ConquestMap.DevResetAttacks();

            if (GUILayout.Button("Spawn 1 NPC Hero (Red Faction)"))
            {
                var board = FindAnyObjectByType<HeroBoard>();
                if (board != null) board.DevSpawnNpc();
            }

            if (GUILayout.Button("Refill mana for all heroes / soldiers"))
                foreach (var hero in Hero.Alive.ToArray()) hero.DevFillMana();

            if (cannons != null && GUILayout.Button("Own All 3 Cannons (Test Cannon Mounts)")) cannons.DevOwnAll();
            GUILayout.Label($"Account: {SaveSystem.Account}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Main")) SwitchAccount(SaveSystem.MainAccount);
            if (GUILayout.Button("Test (all heroes)")) SwitchAccount(SaveSystem.TestAccount);
            if (GUILayout.Button("Fresh (new game)")) SwitchAccount(SaveSystem.FreshAccount);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("AFK Chest: +4 Hours"))
            {
                AfkChest.DevAddHours(4f);
                hud.Refresh();
            }
            if (GUILayout.Button("Refill castle HP"))
            {
                castle.Repair();
                hud.Refresh();
            }
            castle.Health.Invulnerable = GUILayout.Toggle(castle.Health.Invulnerable, " Invulnerable Castle");

            // Dev: cho 1 mon moi o, do hiem ngau nhien — test man Doi quan khi chua thang wave nao.
            if (GUILayout.Button("+4 Gear Items (1 per Slot)"))
                for (int slot = 0; slot < 4; slot++)
                    Armory.Add((BuildBuild.Data.EquipmentSlot)slot, (BuildBuild.Data.Rarity)Random.Range(0, 4));

            GUILayout.Label($"Game Speed: x{Time.timeScale:0.#}");
            GUILayout.BeginHorizontal();
            foreach (float speed in Speeds)
                if (GUILayout.Button($"x{speed:0.#}")) Time.timeScale = speed;
            GUILayout.EndHorizontal();

            GUILayout.Label("Owned (Remove from Inventory — No Gold/Shard Refund):");
            var owned = SaveSystem.Data.owned;
            _ownedScroll = GUILayout.BeginScrollView(_ownedScroll, GUILayout.Height(160f));
            for (int i = owned.Count - 1; i >= 0; i--)
            {
                var unit = owned[i];
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{unit.id}  Lv{unit.level} {unit.stars}★ {unit.shards}sh", GUILayout.Width(160f));
                // Dev: nang mien phi de test mo skill theo sao; hero tren san an so moi tu wave sau (HeroBoard dung lai).
                if (GUILayout.Button("+Lv", GUILayout.Width(40f)) && unit.level < Progression.MaxLevel(unit.stars))
                {
                    unit.level = Mathf.Max(1, unit.level) + 1;
                    SaveSystem.Save();
                }
                if (GUILayout.Button("+★", GUILayout.Width(40f)) && unit.stars < Progression.MaxStars)
                {
                    unit.stars++;
                    SaveSystem.Save();
                }
                if (GUILayout.Button("Remove", GUILayout.Width(60f)))
                {
                    owned.RemoveAt(i);
                    SaveSystem.Save();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        // Nap lai scene: HeroBoard, Economy, bai linh... deu doc save trong Start.
        static void SwitchAccount(string account)
        {
            SaveSystem.SwitchAccount(account);
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        void SetWave(int wave)
        {
            waves.DevSetWave(wave);
            _waveInput = waves.NextWave.ToString();
            hud.Refresh();
        }
    }
}
