using BuildBuild.Combat;
using BuildBuild.Core;
using BuildBuild.Data;
using BuildBuild.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace BuildBuild.UI
{
    /// <summary>
    /// 9 o dat hero tren san (GAME_DESIGN.md muc 2). Cham o trong -> bang chon hero; cham o dang co
    /// hero -> go hero ra. Chi doi hero duoc khi KHONG dang danh (chuan bi giua cac wave), va moi hero
    /// chi dung o MOT o.
    ///
    /// O la DIEM XUAT HIEN, khong phai cho dung co dinh: moi dau wave VA moi lan thang wave, moi o co
    /// doi hinh deu spawn LAI mot hero moi day mau tai dung vi tri o — ke ca hero da chet. Trong wave
    /// thi hero tu di danh va co the chet; lop nay khong keo ho ve o giua tran.
    ///
    /// Doi hinh nam trong save (`SaveData.lineup`): sang man gacha roi quay lai van con nguyen.
    ///
    /// `roster` hien la moi hero co art, chua loc theo so huu trong save — gacha chua ra duoc hero co
    /// bo art chien dau nao.
    /// </summary>
    public class HeroBoard : MonoBehaviour
    {
        [SerializeField] WaveManager waves;
        [SerializeField] Hero heroPrefab;
        [SerializeField] HeroSlot[] slots;

        [Header("Bang chon hero")]
        [SerializeField] GameObject pickerPanel;
        [SerializeField] HeroDefinition[] roster;
        [Tooltip("Cung thu tu voi roster.")]
        [SerializeField] Button[] rosterButtons;
        [SerializeField] Button cancelButton;

        HeroSlot _pending;

        void Start()
        {
            for (int i = 0; i < rosterButtons.Length; i++)
            {
                int index = i;
                rosterButtons[i].onClick.AddListener(() => Choose(index));
            }
            cancelButton.onClick.AddListener(ClosePicker);
            waves.WaveStarted += OnWaveStarted;
            waves.WaveCleared += OnWaveCleared;
            waves.Defeated += OnDefeated;
            ClosePicker();
            RestoreLineup();
        }

        void OnDestroy()
        {
            if (waves == null) return;
            waves.WaveStarted -= OnWaveStarted;
            waves.WaveCleared -= OnWaveCleared;
            waves.Defeated -= OnDefeated;
        }

        public void OnSlotClicked(HeroSlot slot)
        {
            if (waves.IsFighting) return;

            if (slot.Definition != null)
            {
                slot.Assign(null);
                Spawn(slot);        // khong con doi hinh -> chi don than xac cu di
                SaveLineup();
                return;
            }

            _pending = slot;
            for (int i = 0; i < rosterButtons.Length; i++)
                rosterButtons[i].interactable = !OnBoard(roster[i]);
            pickerPanel.SetActive(true);
        }

        void Choose(int index)
        {
            if (_pending == null) return;

            _pending.Assign(roster[index]);
            Spawn(_pending);        // dat xong thay hero dung o o ngay, khong doi toi wave
            SaveLineup();
            ClosePicker();
        }

        // Dau wave la moc hoi sinh chinh: hero chet giua wave phai cho het wave moi tro lai.
        void OnWaveStarted(int wave)
        {
            ClosePicker();
            SpawnAll();
            ShowFrames(false);
        }

        // Thang wave: hero dang tan ra khap san ve lai o, day mau, de nguoi choi nhin dung doi hinh khi chuan bi.
        void OnWaveCleared(int wave, int gold)
        {
            SpawnAll();
            ShowFrames(true);
        }

        void OnDefeated(int wave) => ShowFrames(true);

        // Khung o trong chi hien luc chuan bi — giua tran khong dat hero duoc, khung chi lam roi san.
        void ShowFrames(bool show)
        {
            foreach (var slot in slots) slot.ShowFrame(show);
        }

        // Dung lai doi hinh = moc "het wave": don sach linh trieu hoi truoc (GDD muc 3).
        void SpawnAll()
        {
            Necromancer.DespawnAll();
            foreach (var slot in slots) Spawn(slot);
        }

        /// <summary>Do bo hero dang dung o o (neu con) va dung lai mot hero day mau tu `Definition`.</summary>
        void Spawn(HeroSlot slot)
        {
            if (slot.Occupant != null) Destroy(slot.Occupant.gameObject);
            slot.SetOccupant(null);
            if (slot.Definition == null) return;

            var hero = Instantiate(heroPrefab, slot.transform.position, Quaternion.identity);
            hero.Initialise(slot.Definition);
            slot.SetOccupant(hero);
        }

        void RestoreLineup()
        {
            var lineup = SaveSystem.Data.lineup;
            for (int i = 0; i < slots.Length && i < lineup.Count; i++)
            {
                var definition = System.Array.Find(roster, d => d != null && d.name == lineup[i]);
                if (definition == null) continue;   // hero bi xoa khoi roster thi o de trong
                slots[i].Assign(definition);
                Spawn(slots[i]);
            }
        }

        void SaveLineup()
        {
            var lineup = SaveSystem.Data.lineup;
            lineup.Clear();
            foreach (var slot in slots) lineup.Add(slot.Definition != null ? slot.Definition.name : "");
            SaveSystem.Save();
        }

        bool OnBoard(HeroDefinition definition)
        {
            foreach (var slot in slots)
                if (slot.Definition == definition) return true;
            return false;
        }

        void ClosePicker()
        {
            _pending = null;
            pickerPanel.SetActive(false);
        }
    }
}
