using BuildBuild.Core;
using BuildBuild.Data;
using BuildBuild.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>
    /// 3 be phao tren phao dai (GAME_DESIGN.md muc 2 + 4). Cham be -> bang chon phao: lap phao DANG SO HUU, go phao,
    /// hoac nang level. Chi doi duoc giua cac wave (gia dinh giong hero — GDD khong noi ro, plan muc 6 cau 3).
    ///
    /// So huu + level doc tu save theo `CannonDefinition.unitId` (cung id voi gacha). Moi phao chi lap tren MOT be.
    /// Nang level theo so trong `CannonDefinition` (GDD muc 7: gia floor(50 x n^1,5), +8%/level, toi da 20) — phao dang
    /// lap an so moi ngay.
    ///
    /// Doi hinh phao nam trong save (`SaveData.cannons`): sang man gacha roi quay lai van con nguyen.
    /// </summary>
    public class CannonBoard : MonoBehaviour
    {
        [SerializeField] WaveManager waves;
        [SerializeField] Economy economy;
        [SerializeField] CannonMount[] mounts;

        [Header("Bang chon phao")]
        [SerializeField] GameObject pickerPanel;
        [SerializeField] CannonDefinition[] roster;
        [Tooltip("Cung thu tu voi roster: bam de lap phao do len be dang chon.")]
        [SerializeField] Button[] rosterButtons;
        [Tooltip("Cung thu tu voi roster: dong trang thai (level + chi so, hoac 'Chua so huu').")]
        [SerializeField] Text[] rosterInfo;
        [Tooltip("Cung thu tu voi roster.")]
        [SerializeField] Button[] upgradeButtons;
        [SerializeField] Text[] upgradeLabels;
        [SerializeField] Button removeButton;
        [SerializeField] Button cancelButton;

        CannonMount _pending;

        void Start()
        {
            for (int i = 0; i < roster.Length; i++)
            {
                int index = i;
                if (i < rosterButtons.Length) rosterButtons[i].onClick.AddListener(() => Choose(index));
                if (i < upgradeButtons.Length) upgradeButtons[i].onClick.AddListener(() => Upgrade(index));
            }
            removeButton.onClick.AddListener(Remove);
            cancelButton.onClick.AddListener(ClosePicker);
            waves.WaveStarted += OnWaveStarted;
            waves.WaveCleared += OnWaveEnded;
            waves.Defeated += OnDefeated;
            if (economy != null) economy.Changed += OnGoldChanged;
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
            if (economy != null) economy.Changed -= OnGoldChanged;
        }

        public void OnMountClicked(CannonMount mount)
        {
            if (waves.IsFighting) return;
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
            if (_pending == null || Owned(roster[index]) == null) return;

            // Phao dang o be khac thi chuyen sang be nay (moi phao chi co mot khau).
            foreach (var mount in mounts)
                if (mount != _pending && mount.Definition == roster[index]) mount.Assign(null, 1);

            _pending.Assign(roster[index], LevelOf(roster[index]));
            SaveLoadout();
            ClosePicker();
        }

        void Remove()
        {
            if (_pending == null) return;
            _pending.Assign(null, 1);
            SaveLoadout();
            ClosePicker();
        }

        void Upgrade(int index)
        {
            var cannon = roster[index];
            var owned = Owned(cannon);
            if (owned == null || owned.level >= cannon.maxLevel) return;
            if (economy == null || !economy.TrySpend(cannon.UpgradeCost(owned.level))) return;

            owned.level = Mathf.Max(1, owned.level) + 1;
            SaveSystem.Save();
            foreach (var mount in mounts)
                if (mount.Definition == cannon) mount.Occupant.SetLevel(owned.level);
            RefreshPicker();
        }

        void RefreshPicker()
        {
            for (int i = 0; i < roster.Length; i++)
            {
                var cannon = roster[i];
                var owned = Owned(cannon);
                int level = LevelOf(cannon);

                if (i < rosterButtons.Length)
                    rosterButtons[i].interactable = owned != null && (_pending == null || _pending.Definition != cannon);

                if (i < rosterInfo.Length)
                    rosterInfo[i].text = owned == null
                        ? "Chưa sở hữu — quay gacha để nhận"
                        : $"Lv {level}   ST {cannon.DamageAt(level):0.#}   {cannon.fireRate:0.#}/s   Tầm {cannon.range:0.#}{OnMountNote(cannon)}";

                if (i < upgradeButtons.Length)
                {
                    bool maxed = level >= cannon.maxLevel;
                    int cost = cannon.UpgradeCost(level);
                    upgradeButtons[i].interactable = owned != null && !maxed && economy != null && economy.Gold >= cost;
                    if (i < upgradeLabels.Length) upgradeLabels[i].text = maxed ? "Tối đa" : $"Nâng {cost}";
                }
            }
            removeButton.gameObject.SetActive(_pending != null && _pending.Occupant != null);
        }

        string OnMountNote(CannonDefinition cannon)
        {
            for (int i = 0; i < mounts.Length; i++)
                if (mounts[i].Definition == cannon) return $"   (bệ {i + 1})";
            return "";
        }

        void OnGoldChanged(int _)
        {
            if (pickerPanel.activeSelf) RefreshPicker();
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
