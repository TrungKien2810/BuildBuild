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

        [Header("Bo loc vai")]
        [SerializeField] HeroRoleCatalog roles;
        [Tooltip("Nut dau = Tat ca, sau do cung thu tu `roles.entries`. Nut dang chon dung sprite `roundActive`. "
            + "Dong khong khop vai bi an; Content co VerticalLayoutGroup nen cac dong con lai tu don len.")]
        [SerializeField] Button[] roleButtons;
        [SerializeField] Sprite roundNormal;
        [SerializeField] Sprite roundActive;

        HeroSlot _pending;
        HeroRole? _roleFilter;    // null = Tat ca; giu qua cac lan mo bang
        /// <summary>Hero phe NPC cua tran Chinh Phat dang danh.</summary>
        readonly System.Collections.Generic.List<Hero> _rivals = new System.Collections.Generic.List<Hero>();

        // Quan du bi Chinh Phat (nguoi dung chot lai 2026-10-02): MOT dong ho chung cho hai phe, diem moi `ReinforceSeconds`.
        // Moi lan diem: thanh NPC tung `ReinforceGroup` hero du bi (`ConquestTarget.reserves`); phe ta +1 o du bi — tran
        // PAUSE, bang chon hero mo ra voi be tuong cua nguoi choi (da so huu, CHUA co trong tran tu dau, chua goi ra), chon
        // xong hero ra tu cong thanh minh roi tran chay tiep. "Huy" = bo luot do. So TAM.
        // Moi phe chi DUNG MOT hero chi vien moi tran (nguoi dung 2026-10-02: nhieu qua mat tap trung vao combat).
        const float ReinforceSeconds = 10f;
        const int ReinforceGroup = 1;
        const int ReinforceLimit = 1;
        static readonly Color ReinforceColor = new Color(1f, 0.45f, 0.4f);
        static readonly Color AllyReinforceColor = new Color(0.45f, 0.85f, 1f);

        [Header("Chi vien Chinh Phat")]
        [SerializeField] Button reinforceButton;
        [SerializeField] Text reinforceLabel;

        readonly System.Collections.Generic.Queue<HeroDefinition> _rivalReserves = new System.Collections.Generic.Queue<HeroDefinition>();
        /// <summary>Hero phe ta da goi ra lam chi vien trong wave nay — moi hero chi ra mot lan.</summary>
        readonly System.Collections.Generic.HashSet<HeroDefinition> _called = new System.Collections.Generic.HashSet<HeroDefinition>();
        /// <summary>Hero tiep vien phe ta dang tren san (khong thuoc o nao).</summary>
        readonly System.Collections.Generic.List<Hero> _allies = new System.Collections.Generic.List<Hero>();
        float _nextReinforce;
        /// <summary>Bang chon dang mo cho o du bi (tran dang pause), khong phai cho o 9 o.</summary>
        bool _reinforcing;
        float _resumeScale = 1f;

        void Start()
        {
            for (int i = 0; i < rosterButtons.Length; i++)
            {
                int index = i;
                rosterButtons[i].onClick.AddListener(() => Choose(index));
            }
            cancelButton.onClick.AddListener(ClosePicker);
            for (int i = 0; i < roleButtons.Length; i++)
            {
                HeroRole? role = i == 0 ? (HeroRole?)null : roles.entries[i - 1].role;
                roleButtons[i].onClick.AddListener(() => { _roleFilter = role; ApplyRoleFilter(); });
            }
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
                Sfx.Play("unit-remove");
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
            ApplyRoleFilter();
            pickerPanel.SetActive(true);
            Sfx.Play("ui-open");
        }

        void ApplyRoleFilter()
        {
            for (int i = 0; i < roleButtons.Length; i++)
                roleButtons[i].image.sprite = (i == 0 ? _roleFilter == null : _roleFilter == roles.entries[i - 1].role) ? roundActive : roundNormal;
            for (int i = 0; i < rosterButtons.Length; i++)
                rosterButtons[i].gameObject.SetActive(_roleFilter == null || roster[i].role == _roleFilter);
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
            if (_reinforcing)
            {
                Sfx.Play("hero-pick");
                CallReinforcement(roster[index]);
                ClosePicker();
                return;
            }
            if (_pending == null || !Owned(roster[index])) return;

            _pending.Assign(roster[index]);
            Sfx.Play("hero-pick");
            Sfx.Play("unit-place");
            Spawn(_pending);        // dat xong thay hero dung o o ngay, khong doi toi wave
            SaveLineup();
            ClosePicker();
        }

        // Dau wave la moc hoi sinh chinh: hero chet giua wave phai cho het wave moi tro lai.
        void OnWaveStarted(int wave)
        {
            ClosePicker();
            SpawnAll();
            if (WaveManager.InConquest)
            {
                SpawnRivals();
                _nextReinforce = Time.time + ReinforceSeconds;
            }
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
            Wukong.DespawnAll();
            foreach (var rival in _rivals)
                if (rival != null) Destroy(rival.gameObject);
            _rivals.Clear();
            foreach (var ally in _allies)
                if (ally != null) Destroy(ally.gameObject);
            _allies.Clear();
            _rivalReserves.Clear();
            _called.Clear();
            foreach (var slot in slots) Spawn(slot);
        }

        /// <summary>
        /// Chinh Phat: doi hinh NPC co bang ay hero so voi o dang co hero cua minh, boc ngau nhien KHONG lap tu roster, dung
        /// o doi xung qua tam man. Chi so lay theo save nhu hero minh (`Hero.Initialise`) — TAM, chua suy tu luc chien.
        /// </summary>
        void SpawnRivals()
        {
            var match = ConquestMatch.Current;
            if (match == null) return;
            if (match.Target != null)
            {
                // Doi hinh da do tham: o i cua thanh NPC dung doi xung voi o i cua minh.
                for (int i = 0; i < slots.Length && i < match.Target.heroes.Count; i++)
                {
                    var definition = ConquestMap.Find(roster, match.Target.heroes[i]);
                    if (definition == null) continue;
                    var rival = Instantiate(heroPrefab, match.Mirror(slots[i].transform.position), Quaternion.identity);
                    rival.Initialise(definition, side: Faction.Npc);
                    rival.Aggressive = true;
                    _rivals.Add(rival);
                }
                foreach (var name in match.Target.reserves)
                {
                    if (_rivalReserves.Count >= ReinforceLimit) break;
                    var definition = ConquestMap.Find(roster, name);
                    if (definition != null) _rivalReserves.Enqueue(definition);
                }
                return;
            }
            var pool = new System.Collections.Generic.List<HeroDefinition>(System.Array.FindAll(roster, d => d != null));
            foreach (var slot in slots)
            {
                if (slot.Definition == null || pool.Count == 0) continue;
                int pick = Random.Range(0, pool.Count);
                var hero = Instantiate(heroPrefab, match.Mirror(slot.transform.position), Quaternion.identity);
                hero.Initialise(pool[pick], side: Faction.Npc);
                hero.Aggressive = true;
                pool.RemoveAt(pick);
                _rivals.Add(hero);
            }
        }

        /// <summary>Be tuong chi vien phe ta: da so huu, KHONG dung trong 9 o tu dau tran, chua goi ra wave nay.</summary>
        bool CanReinforce(HeroDefinition definition) =>
            definition != null && Owned(definition) && !OnBoard(definition) && !_called.Contains(definition);

        bool AllyPoolLeft() => _called.Count < ReinforceLimit && System.Array.Exists(roster, CanReinforce);

        void Update()
        {
            var match = ConquestMatch.Current;
            bool live = match != null && WaveManager.Fighting;
            reinforceButton.gameObject.SetActive(live);
            if (!live || _reinforcing) return;

            bool rivalHasReserves = match.Rival != null && _rivalReserves.Count > 0;
            bool allyHasReserves = match.Home != null && AllyPoolLeft();
            if ((rivalHasReserves || allyHasReserves) && Time.time >= _nextReinforce)
            {
                _nextReinforce = Time.time + ReinforceSeconds;
                if (rivalHasReserves) SendRivalReinforcements(match.Rival);
                if (allyHasReserves) OpenReinforcePicker();
            }

            reinforceButton.interactable = false;   // chi la dong ho, chon quan o bang pause
            int wait = Mathf.CeilToInt(Mathf.Max(0f, _nextReinforce - Time.time));
            reinforceLabel.text = rivalHasReserves || allyHasReserves ? $"Dự bị +1 sau {wait}s" : "Hết quân dự bị";
        }

        void SendRivalReinforcements(Castle rival)
        {
            var body = Gate(rival, out float x);
            for (int i = 0; i < ReinforceGroup && _rivalReserves.Count > 0; i++)
            {
                var hero = Instantiate(heroPrefab, new Vector3(x, Battlefield.RandomY(), 0f), Quaternion.identity);
                hero.Initialise(_rivalReserves.Dequeue(), side: Faction.Npc);
                hero.Aggressive = true;
                _rivals.Add(hero);
            }
            FloatingNumber.Spawn(new Vector3(body.center.x, body.max.y, 0f), "Quân tiếp viện!", ReinforceColor);
        }

        /// <summary>+1 o du bi: pause tran, mo bang chon voi be tuong chi vien.</summary>
        void OpenReinforcePicker()
        {
            _reinforcing = true;
            _pending = null;
            _resumeScale = Time.timeScale;
            Time.timeScale = 0f;
            for (int i = 0; i < rosterButtons.Length; i++)
            {
                bool inBattle = OnBoard(roster[i]) || _called.Contains(roster[i]);
                rosterButtons[i].interactable = CanReinforce(roster[i]);
                MarkSkillSlot(i, inBattle);
                UnitCardView.ShowLevelAndStars(rosterLevels[i], rosterStars, i, Progression.Owned(roster[i].unitId), starOn, starOff);
            }
            ApplyRoleFilter();
            pickerPanel.SetActive(true);
            Sfx.Play("ui-open");
        }

        /// <summary>Hero chi vien chon tu bang pause ra tu cong thanh minh.</summary>
        void CallReinforcement(HeroDefinition definition)
        {
            var match = ConquestMatch.Current;
            var home = match != null ? match.Home : null;
            if (home == null || !CanReinforce(definition)) return;
            _called.Add(definition);
            var body = Gate(home, out float x);
            var hero = Instantiate(heroPrefab, new Vector3(x, Battlefield.RandomY(), 0f), Quaternion.identity);
            hero.Initialise(definition, equipment);
            hero.Aggressive = true;
            _allies.Add(hero);
            FloatingNumber.Spawn(new Vector3(body.center.x, body.max.y, 0f), "Chi viện!", AllyReinforceColor);
        }

        /// <summary>Than thanh + x cong ra (mep than quay ve phia dich: thanh NPC mep trai, thanh minh mep phai).</summary>
        static Bounds Gate(Castle castle, out float x)
        {
            var body = castle.transform.Find("Body").GetComponent<SpriteRenderer>().bounds;
            x = castle.Side == Faction.Npc ? body.min.x : body.max.x;
            return body;
        }

        /// <summary>Do bo hero dang dung o o (neu con) va dung lai mot hero day mau tu `Definition`.</summary>
        void Spawn(HeroSlot slot)
        {
            if (slot.Occupant != null) Destroy(slot.Occupant.gameObject);
            slot.SetOccupant(null);
            if (slot.Definition == null) return;

            var hero = Instantiate(heroPrefab, slot.transform.position, Quaternion.identity);
            hero.Initialise(slot.Definition, equipment);
            hero.Aggressive = WaveManager.InConquest && WaveManager.Fighting;   // Chinh Phat: tien danh thanh NPC
            slot.SetOccupant(hero);
        }

        /// <summary>
        /// Dev: tha mot hero NPC (phe lanh chua Chinh Phat) ngau nhien trong roster o phia phai man, cung hang voi mot o
        /// ngau nhien — thu hero hai phe trong tran thuong. Chu dong tien danh (Chinh Phat), dau phe = vong do duoi chan.
        /// </summary>
        public void DevSpawnNpc()
        {
            var pool = System.Array.FindAll(roster, d => d != null);
            if (pool.Length == 0 || slots.Length == 0 || Camera.main == null) return;
            var row = slots[Random.Range(0, slots.Length)].transform.position;
            float x = Camera.main.ViewportToWorldPoint(new Vector3(0.85f, 0f, 0f)).x;
            var hero = Instantiate(heroPrefab, new Vector3(x, row.y, row.z), Quaternion.identity);
            hero.Initialise(pool[Random.Range(0, pool.Length)], side: Faction.Npc);
            hero.Aggressive = true;
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
            if (_reinforcing) Time.timeScale = _resumeScale;   // dong bang chon o du bi = tran chay tiep
            _reinforcing = false;
            _pending = null;
            pickerPanel.SetActive(false);
        }
    }
}
