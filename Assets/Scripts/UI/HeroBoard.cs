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
    /// `roster` la moi hero co art; chi hero DA SO HUU (`HeroDefinition.unitId` co trong save) moi bam
    /// chon duoc — nhu bang phao. Doi hinh cu trong save co hero chua so huu thi o do de trong.
    /// </summary>
    public class HeroBoard : MonoBehaviour
    {
        [SerializeField] WaveManager waves;
        [SerializeField] Hero heroPrefab;
        [SerializeField] HeroSlot[] slots;

        [Header("Bang chon hero")]
        [SerializeField] GameObject pickerPanel;
        [SerializeField] HeroDefinition[] roster;
        [Tooltip("Trang bi hero dang mac cong chi so luc dat xuong san (`Hero.Initialise`).")]
        [SerializeField] EquipmentConfig equipment;
        [Tooltip("Cung thu tu voi roster.")]
        [SerializeField] Button[] rosterButtons;
        [Tooltip(
            "Cung thu tu voi roster; o cua hero khong co ky nang de trong. O skill doi sang " +
            "`slotSelectedSprite` khi hero do DANG dung tren san — day la dau hieu duy nhat trong bang " +
            "cho biet vi sao dong do bam khong duoc.")]
        [SerializeField] Image[] rosterSkillSlots;
        [SerializeField] Sprite slotSkillSprite;
        [SerializeField] Sprite slotSelectedSprite;
        [SerializeField] Button cancelButton;
        [Tooltip("\"Lv N\" moi dong, cung thu tu `roster`.")]
        [SerializeField] Text[] rosterLevels;
        [Tooltip("5 sao moi dong: dong i o [i*5 .. i*5+4].")]
        [SerializeField] Image[] rosterStars;
        [SerializeField] Sprite starOn;
        [SerializeField] Sprite starOff;

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
            {
                bool onBoard = OnBoard(roster[i]);
                rosterButtons[i].interactable = !onBoard && Owned(roster[i]);
                MarkSkillSlot(i, onBoard);
                UnitCardView.ShowLevelAndStars(rosterLevels[i], rosterStars, i, Progression.Owned(roster[i].unitId), starOn, starOff);
            }
            pickerPanel.SetActive(true);
        }

        /// <summary>O skill cua dong `index`: vien vang khi hero da dung tren san, vien da khi con chon duoc.</summary>
        void MarkSkillSlot(int index, bool onBoard)
        {
            if (rosterSkillSlots == null || index >= rosterSkillSlots.Length) return;
            var slot = rosterSkillSlots[index];
            if (slot == null) return;                       // hero khong co ky nang -> o khoa, khong doi
            var sprite = onBoard ? slotSelectedSprite : slotSkillSprite;
            if (sprite != null) slot.sprite = sprite;
        }

        void Choose(int index)
        {
            if (_pending == null || !Owned(roster[index])) return;

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

        // Thua wave cung la moc "het wave": ve o, day mau, mana ve goc y nhu luc thang.
        void OnDefeated(int wave)
        {
            SpawnAll();
            ShowFrames(true);
        }

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
            hero.Initialise(slot.Definition, equipment);
            slot.SetOccupant(hero);
        }

        void RestoreLineup()
        {
            var lineup = SaveSystem.Data.lineup;
            for (int i = 0; i < slots.Length && i < lineup.Count; i++)
            {
                var definition = System.Array.Find(roster, d => d != null && d.name == lineup[i]);
                if (definition == null || !Owned(definition)) continue;   // bi xoa khoi roster / chua so huu -> o trong
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

        static bool Owned(HeroDefinition definition) =>
            !string.IsNullOrEmpty(definition.unitId) && SaveSystem.Data.Find(definition.unitId) != null;

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
