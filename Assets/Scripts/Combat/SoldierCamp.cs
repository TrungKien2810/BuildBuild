using System;
using System.Collections.Generic;
using BuildBuild.Core;
using BuildBuild.Data;
using BuildBuild.Meta;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Bai linh sau thanh (GAME_DESIGN.md muc 2, IDEAS #1). Giu phan LUAT cua trieu hoi + DOI LINH; phan cham/keo nam o
    /// `SoldierPost` (moi cho dung mot post), bang chon o `SoldierBoard`.
    ///
    ///   - Doi linh giong doi hero (nguoi dung 2026-09-28): moi cho dung giu MOT loai linh DA SO HUU hoac de trong, doi
    ///     giua cac wave, luu save (`SaveData.soldiers`). Save chua co doi linh thi xep san cac loai da so huu theo roster.
    ///   - KHONG co tai nguyen (nguoi dung 2026-09-26): moi loai linh co vong HOI CHIEU rieng tren dau. Dau wave
    ///     moi vong RONG va day dan trong luc danh; day thi keo tha duoc, tha xong vong ve rong. Het wave vong cung ve rong.
    ///   - Keo tha ra san -> sinh NGUYEN TOP `squadSize` con quanh diem tha, lech nhau theo y, keo vao trong san
    ///     bang `Battlefield.Clamp`.
    ///   - Het wave (thang lan thua) -> don sach linh, nhu linh xuong cua Bonecaller.
    ///
    /// Tren san moi con linh la mot `Hero` (SoldierDefinition ke thua HeroDefinition): tu duoi quai, bi danh,
    /// chan quai (`Blocker`, giu 3). Linh tam xa lay diem tha lam goc day xich.
    /// </summary>
    public class SoldierCamp : MonoBehaviour
    {
        [SerializeField] WaveManager waves;
        [SerializeField] SummonConfig config;
        [Tooltip("Prefab Hero dung chung — linh la Hero voi SoldierDefinition.")]
        [SerializeField] Hero unitPrefab;
        [Tooltip("Moi loai linh chon duoc o bai (bang chon cua `SoldierBoard` cung thu tu).")]
        [SerializeField] SoldierDefinition[] roster;
        [Tooltip("So cho dung o bai (SoldierPost), tu tren xuong.")]
        [SerializeField] int postCount = 3;

        /// <summary>Bai linh cua scene dang mo — Admin panel doc de day hoi chieu khi test.</summary>
        public static SoldierCamp Current { get; private set; }

        readonly List<Hero> _summoned = new List<Hero>();
        SoldierDefinition[] _lineup = Array.Empty<SoldierDefinition>();
        float[] _cooldownLeft = Array.Empty<float>();
        // Chinh Phat: bai linh phe NPC — tu tha top tai vach thanh NPC moi khi vong day.
        SoldierDefinition[] _rivalLineup = Array.Empty<SoldierDefinition>();
        float[] _rivalCooldownLeft = Array.Empty<float>();

        public int RosterCount => roster != null ? roster.Length : 0;
        public SoldierDefinition Soldier(int index) => roster[index];

        /// <summary>Loai linh dang o cho dung <paramref name="post"/>; null = cho trong.</summary>
        public SoldierDefinition Assigned(int post) => post >= 0 && post < _lineup.Length ? _lineup[post] : null;

        /// <summary>Ban ra khi mot top vua ra san. Tham so: loai linh, cac con vua sinh.</summary>
        public event Action<SoldierDefinition, IReadOnlyList<Hero>> Summoned;

        void Awake()
        {
            Current = this;
            _lineup = new SoldierDefinition[postCount];
            _cooldownLeft = new float[postCount];
            RestoreLineup();
        }

        void Start()
        {
            if (waves == null) return;
            waves.WaveStarted += OnWaveStarted;
            waves.WaveCleared += OnWaveCleared;
            waves.Defeated += OnDefeated;
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
            if (waves == null) return;
            waves.WaveStarted -= OnWaveStarted;
            waves.WaveCleared -= OnWaveCleared;
            waves.Defeated -= OnDefeated;
        }

        void Update()
        {
            if (!WaveManager.Fighting) return;
            for (int i = 0; i < _cooldownLeft.Length; i++)
                _cooldownLeft[i] = Mathf.Max(0f, _cooldownLeft[i] - Time.deltaTime);
            UpdateRivals();
        }

        void UpdateRivals()
        {
            var match = ConquestMatch.Current;
            if (match == null || match.Rival == null) return;
            for (int i = 0; i < _rivalLineup.Length; i++)
            {
                _rivalCooldownLeft[i] -= Time.deltaTime;
                if (_rivalCooldownLeft[i] > 0f) continue;
                var soldier = _rivalLineup[i];
                _rivalCooldownLeft[i] = soldier.summonCooldown;
                SpawnSquad(soldier, new Vector3(match.Rival.transform.position.x, Battlefield.RandomY(), 0f), Faction.Npc);
            }
        }

        /// <summary>0..1 cho vong hoi chieu tren dau linh o cho dung <paramref name="post"/>: 1 = day, keo tha duoc.</summary>
        public float CooldownFraction(int post)
        {
            var soldier = Assigned(post);
            if (soldier == null) return 0f;
            if (soldier.summonCooldown <= 0f) return 1f;
            return 1f - Mathf.Clamp01(_cooldownLeft[post] / soldier.summonCooldown);
        }

        /// <summary>Save da co `unitId` cua loai linh nay (quay gacha ra roi).</summary>
        public static bool Owned(SoldierDefinition soldier) =>
            soldier != null && !string.IsNullOrEmpty(soldier.unitId) && SaveSystem.Data.Find(soldier.unitId) != null;

        /// <summary>Loai linh nay dang dung o mot cho nao do — moi loai chi o MOT cho, nhu hero.</summary>
        public bool InLineup(SoldierDefinition soldier) => soldier != null && Array.IndexOf(_lineup, soldier) >= 0;

        /// <summary>Chi trieu hoi linh da so huu, trong luc danh wave, khi vong day.</summary>
        public bool CanSummon(int post) =>
            WaveManager.Fighting && Owned(Assigned(post)) && _cooldownLeft[post] <= 0f && unitPrefab != null;

        /// <summary>Dat <paramref name="soldier"/> vao cho dung <paramref name="post"/> (null = bo chon) va luu save. Chi giua wave.</summary>
        public void Assign(int post, SoldierDefinition soldier)
        {
            if (WaveManager.Fighting || post < 0 || post >= _lineup.Length) return;
            if (soldier != null && (!Owned(soldier) || InLineup(soldier))) return;
            _lineup[post] = soldier;
            SaveLineup();
        }

        void RestoreLineup()
        {
            var saved = SaveSystem.Data.soldiers;
            if (saved.Count == 0)
            {
                // Save cu (truoc khi co doi linh): xep san moi loai da so huu, nhu bai cu tu hien linh so huu.
                int post = 0;
                foreach (var soldier in roster)
                    if (post < _lineup.Length && Owned(soldier)) _lineup[post++] = soldier;
                return;
            }
            for (int i = 0; i < _lineup.Length && i < saved.Count; i++)
            {
                var soldier = Array.Find(roster, s => s != null && s.name == saved[i]);
                _lineup[i] = Owned(soldier) ? soldier : null;   // bi xoa khoi roster / chua so huu -> cho trong
            }
        }

        void SaveLineup()
        {
            var saved = SaveSystem.Data.soldiers;
            saved.Clear();
            foreach (var soldier in _lineup) saved.Add(soldier != null ? soldier.name : "");
            SaveSystem.Save();
        }

        /// <summary>
        /// Tha top linh o cho dung <paramref name="post"/> tai <paramref name="point"/> (world). Tra ve false — khong tru gi —
        /// neu chua trieu hoi duoc hoac diem tha nam ngoai san.
        /// </summary>
        public bool TrySummon(int post, Vector3 point)
        {
            point.z = 0f;
            if (!CanSummon(post) || !Battlefield.OnField(point)) return false;

            var soldier = _lineup[post];
            _cooldownLeft[post] = soldier.summonCooldown;
            // Sinh truoc roi moi ban event: `Summoned?.Invoke(.., SpawnSquad(..))` bo qua ca SpawnSquad khi chua ai nghe.
            var squad = SpawnSquad(soldier, point);
            Summoned?.Invoke(soldier, squad);
            return true;
        }

        /// <summary>
        /// Top xep doc quanh diem tha (lech theo y, hang le lui nua buoc cho khoi thanh mot duong ke), moi con keo
        /// vao trong san. Vi tri luc Initialise la goc day xich cua linh tam xa.
        /// </summary>
        List<Hero> SpawnSquad(SoldierDefinition soldier, Vector3 centre, Faction side = Faction.Player)
        {
            var squad = new List<Hero>(soldier.squadSize);
            float spacing = config != null ? config.squadSpacing : 0.4f;
            for (int i = 0; i < soldier.squadSize; i++)
            {
                var offset = new Vector3(i % 2 == 1 ? -spacing * 0.5f : 0f, (i - (soldier.squadSize - 1) * 0.5f) * spacing, 0f);
                var position = Battlefield.Clamp(centre + offset, Battlefield.PersonalSpace * 0.5f, clampRight: true);
                var unit = Instantiate(unitPrefab, position, Quaternion.identity);
                unit.Initialise(soldier, side: side);
                unit.Aggressive = WaveManager.InConquest;   // Chinh Phat: hai phe tien danh thanh doi phuong
                SummonBurst.Spawn(unit.transform.position, unit.BodyRadius, config != null ? config.spawnFxFrames : null);
                _summoned.Add(unit);
                squad.Add(unit);
            }
            return squad;
        }

        // Dau wave: bai sach, moi vong RONG — phai cho vong day moi tha duoc top dau tien.
        void OnWaveStarted(int wave)
        {
            ResetCamp();
            if (WaveManager.InConquest) PickRivals();
        }

        /// <summary>
        /// Chinh Phat: bai NPC co bang ay cho dung nhu bai minh, moi cho mot loai boc ngau nhien tu roster (khong can so
        /// huu), vong bat dau RONG nhu phe ta — TAM, chua suy tu luc chien.
        /// </summary>
        void PickRivals()
        {
            var pool = Array.FindAll(roster, s => s != null);
            if (pool.Length == 0) return;
            var target = ConquestMatch.Current?.Target;
            // Co thanh tren ban do: dung so loai linh luc Do Tham da tinh vao luc chien NPC, khong lap day moi cho.
            int count = target != null && target.soldiers.Count > 0 ? Mathf.Min(postCount, target.soldiers.Count) : postCount;
            _rivalLineup = new SoldierDefinition[count];
            _rivalCooldownLeft = new float[count];
            for (int i = 0; i < count; i++)
            {
                _rivalLineup[i] = target != null && target.soldiers.Count > 0
                    ? ConquestMap.Find(pool, target.soldiers[i % target.soldiers.Count]) ?? pool[0]
                    : pool[UnityEngine.Random.Range(0, pool.Length)];
                _rivalCooldownLeft[i] = _rivalLineup[i].summonCooldown;
            }
        }

        // Het wave cung reset: giua hai wave hoi chieu dung dem, de nguyen thi vong dung o muc do dang.
        void OnWaveCleared(int wave, int gold) => ResetCamp();

        void OnDefeated(int wave) => ResetCamp();

        void ResetCamp()
        {
            DespawnAll();
            _rivalLineup = Array.Empty<SoldierDefinition>();
            _rivalCooldownLeft = Array.Empty<float>();
            for (int i = 0; i < _cooldownLeft.Length; i++)
                _cooldownLeft[i] = _lineup[i] != null ? _lineup[i].summonCooldown : 0f;
        }

        void DespawnAll()
        {
            foreach (var unit in _summoned)
                if (unit != null) Destroy(unit.gameObject);
            _summoned.Clear();
        }

        /// <summary>Admin panel: day moi vong hoi chieu.</summary>
        public void DevFill() => Array.Clear(_cooldownLeft, 0, _cooldownLeft.Length);
    }
}
