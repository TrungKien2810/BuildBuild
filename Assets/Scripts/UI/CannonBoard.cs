using BuildBuild.Combat;
using BuildBuild.Core;
using BuildBuild.Data;
using BuildBuild.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>
    /// 3 be phao tren phao dai (GAME_DESIGN.md muc 2 + 4). Cham giong het o hero (`HeroBoard`, nguoi dung chot
    /// 2026-09-28): be trong -> bang chon phao DANG SO HUU; be dang co phao -> go phao. Chi doi duoc giua cac wave.
    /// Nang level KHONG lam o be — o man Doi quan (`UnitsScreen`), nhu hero.
    ///
    /// So huu + level doc tu save theo `CannonDefinition.unitId` (cung id voi gacha). Moi phao chi lap tren MOT be:
    /// phao dang o be khac thi dong do khoa, nhu hero dang dung tren san.
    ///
    /// Doi hinh phao nam trong save (`SaveData.cannons`): sang man gacha roi quay lai van con nguyen.
    /// </summary>
    public class CannonBoard : MonoBehaviour
    {
        [SerializeField] WaveManager waves;
        [SerializeField] CannonMount[] mounts;

        /// <summary>So be phao dang co — luc chien Chinh Phat dem phao theo so nay.</summary>
        public int MountCount => mounts.Length;

        [Header("Bang chon phao")]
        [SerializeField] GameObject pickerPanel;
        [SerializeField] CannonDefinition[] roster;
        [Tooltip("Cung thu tu voi roster: bam de lap phao do len be dang chon.")]
        [SerializeField] Button[] rosterButtons;
        [Tooltip("Cung thu tu voi roster: dong trang thai (level + chi so, hoac 'Chua so huu').")]
        [SerializeField] Text[] rosterInfo;
        [SerializeField] Button cancelButton;

        CannonMount _pending;
        /// <summary>Phao tren thanh NPC cua tran Chinh Phat dang danh.</summary>
        readonly System.Collections.Generic.List<Cannon> _rivals = new System.Collections.Generic.List<Cannon>();

        void Start()
        {
            for (int i = 0; i < roster.Length; i++)
            {
                int index = i;
                if (i < rosterButtons.Length) rosterButtons[i].onClick.AddListener(() => Choose(index));
            }
            cancelButton.onClick.AddListener(ClosePicker);
            waves.WaveStarted += OnWaveStarted;
            waves.WaveCleared += OnWaveEnded;
            waves.Defeated += OnDefeated;
            ClosePicker();
            RestoreLoadout();
        }

        void OnDestroy()
        {
            if (waves != null)
            {
                waves.WaveStarted -= OnWaveStarted;
                waves.WaveCleared -= OnWaveEnded;
                waves.Defeated -= OnDefeated;
            }
        }

        public void OnMountClicked(CannonMount mount)
        {
            if (waves.IsFighting) return;

            if (mount.Occupant != null)
            {
                mount.Assign(null, 1);
                Sfx.Play("unit-remove");
                SaveLoadout();
                return;
            }

            _pending = mount;
            RefreshPicker();
            pickerPanel.SetActive(true);
            Sfx.Play("ui-open");
        }

        /// <summary>Admin panel: so huu ca roster phao (test khi chua quay gacha ra).</summary>
        public void DevOwnAll()
        {
            foreach (var cannon in roster)
                if (cannon != null && SaveSystem.Data.Find(cannon.unitId) == null)
                    SaveSystem.Data.owned.Add(new OwnedUnit { id = cannon.unitId });
            SaveSystem.Save();
            if (pickerPanel.activeSelf) RefreshPicker();
        }

        static OwnedUnit Owned(CannonDefinition cannon) => cannon != null ? SaveSystem.Data.Find(cannon.unitId) : null;

        static int LevelOf(CannonDefinition cannon)
        {
            var owned = Owned(cannon);
            return owned != null ? Mathf.Max(1, owned.level) : 1;
        }

        void Choose(int index)
        {
            if (_pending == null || Owned(roster[index]) == null || OnMount(roster[index])) return;

            _pending.Assign(roster[index], LevelOf(roster[index]));
            Sfx.Play("unit-place");
            SaveLoadout();
            ClosePicker();
        }

        void RefreshPicker()
        {
            for (int i = 0; i < roster.Length; i++)
            {
                var cannon = roster[i];
                var owned = Owned(cannon);
                int level = LevelOf(cannon);

                if (i < rosterButtons.Length)
                    rosterButtons[i].interactable = owned != null && !OnMount(cannon);

                if (i < rosterInfo.Length)
                    rosterInfo[i].text = owned == null
                        ? "Chưa sở hữu — quay gacha để nhận"
                        : $"Lv {level}   ST {Progression.CannonDamage(cannon, level):0.#}   {cannon.fireRate:0.#}/s   Tầm {cannon.range:0.#}{OnMountNote(cannon)}";
            }
        }

        bool OnMount(CannonDefinition cannon)
        {
            foreach (var mount in mounts)
                if (mount.Definition == cannon) return true;
            return false;
        }

        string OnMountNote(CannonDefinition cannon)
        {
            for (int i = 0; i < mounts.Length; i++)
                if (mounts[i].Definition == cannon) return $"   (bệ {i + 1})";
            return "";
        }

        void OnWaveStarted(int wave)
        {
            ClosePicker();
            ShowFrames(false);
            if (WaveManager.InConquest) SpawnRivals();
        }

        void OnWaveEnded(int wave, int gold) => OnDefeated(wave);

        void OnDefeated(int wave)
        {
            ShowFrames(true);
            foreach (var cannon in _rivals)
                if (cannon != null) Destroy(cannon.gameObject);
            _rivals.Clear();
        }

        /// <summary>
        /// Chinh Phat: moi be co mot phao NPC tai vi tri doi xung tren thanh NPC, loai boc ngau nhien tu roster, level theo
        /// save nhu phao minh — TAM, chua suy tu luc chien.
        /// </summary>
        void SpawnRivals()
        {
            var match = ConquestMatch.Current;
            var pool = System.Array.FindAll(roster, c => c != null);
            if (match == null || pool.Length == 0) return;
            for (int i = 0; i < mounts.Length; i++)
            {
                var mount = mounts[i];
                var target = match.Target;
                var definition = target == null ? pool[Random.Range(0, pool.Length)]
                    : i < target.cannons.Count ? ConquestMap.Find(roster, target.cannons[i]) : null;   // be ngoai so phao luc Do Tham: de trong
                if (definition == null) continue;
                var go = new GameObject("RivalCannon_" + definition.name);
                go.transform.position = match.Mirror(mount.transform.position);
                go.AddComponent<SpriteRenderer>().sortingOrder = SortOrder.Castle + 1;
                go.AddComponent<SpriteAnimator>();
                var cannon = go.AddComponent<Cannon>();
                cannon.Initialise(definition, target != null ? target.cannonLevel : LevelOf(definition), Faction.Npc);
                _rivals.Add(cannon);
            }
        }

        void ShowFrames(bool show)
        {
            foreach (var mount in mounts) mount.ShowFrame(show);
        }

        void RestoreLoadout()
        {
            var loadout = SaveSystem.Data.cannons;
            for (int i = 0; i < mounts.Length; i++)
            {
                CannonDefinition definition = null;
                if (i < loadout.Count)
                    definition = System.Array.Find(roster, c => c != null && c.name == loadout[i]);
                // Phao bi xoa khoi kho (admin) thi be de trong.
                if (definition != null && Owned(definition) == null) definition = null;
                mounts[i].Assign(definition, LevelOf(definition));
            }
        }

        void SaveLoadout()
        {
            var loadout = SaveSystem.Data.cannons;
            loadout.Clear();
            foreach (var mount in mounts) loadout.Add(mount.Definition != null ? mount.Definition.name : "");
            SaveSystem.Save();
        }

        void ClosePicker()
        {
            _pending = null;
            pickerPanel.SetActive(false);
        }
    }
}
