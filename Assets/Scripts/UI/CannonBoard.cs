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

        [Header("Bang chon phao")]
        [SerializeField] GameObject pickerPanel;
        [SerializeField] CannonDefinition[] roster;
        [Tooltip("Cung thu tu voi roster: bam de lap phao do len be dang chon.")]
        [SerializeField] Button[] rosterButtons;
        [Tooltip("Cung thu tu voi roster: dong trang thai (level + chi so, hoac 'Chua so huu').")]
        [SerializeField] Text[] rosterInfo;
        [SerializeField] Button cancelButton;

        CannonMount _pending;

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
                SaveLoadout();
                return;
            }

            _pending = mount;
            RefreshPicker();
            pickerPanel.SetActive(true);
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
        }

        void OnWaveEnded(int wave, int gold) => ShowFrames(true);

        void OnDefeated(int wave) => ShowFrames(true);

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
