using System;
using System.Collections.Generic;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Bai linh sau thanh (GAME_DESIGN.md muc 2, IDEAS #1). Giu phan LUAT cua trieu hoi; phan cham/keo nam o
    /// `SoldierPost` (moi loai linh mot cho dung o bai), thanh tai nguyen o `SummonHud`.
    ///
    ///   - Tai nguyen trieu hoi tich theo thoi gian + theo so quai bi giet, co tran (`SummonConfig`). Chi tich
    ///     trong luc dang danh wave.
    ///   - Moi loai linh co vong HOI CHIEU rieng. Dau wave moi vong day san — tai nguyen (ve 0) moi la cai chan.
    ///   - Vong day + du tai nguyen -> keo tha ra san -> sinh NGUYEN TOP `squadSize` con quanh diem tha, lech
    ///     nhau theo y, keo vao trong san bang `Battlefield.Clamp`.
    ///   - Het wave (thang lan thua) -> don sach linh, nhu linh xuong cua Bonecaller. Tai nguyen ve 0 tru khi
    ///     `keepBetweenWaves` (CHUA CHOT).
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
        [Tooltip("Cac loai linh mang vao tran, cung thu tu voi cho dung o bai (SoldierPost).")]
        [SerializeField] SoldierDefinition[] roster;

        /// <summary>Bai linh cua scene dang mo — Admin panel doc de nap tai nguyen / hoi chieu khi test.</summary>
        public static SoldierCamp Current { get; private set; }

        readonly List<Hero> _summoned = new List<Hero>();
        float[] _cooldownLeft = Array.Empty<float>();

        public float Resource { get; private set; }
        public float ResourceCap => config != null ? config.resourceCap : 0f;
        public int RosterCount => roster != null ? roster.Length : 0;
        public SoldierDefinition Soldier(int index) => roster[index];

        /// <summary>Ban ra khi mot top vua ra san. Tham so: loai linh, cac con vua sinh.</summary>
        public event Action<SoldierDefinition, IReadOnlyList<Hero>> Summoned;

        void Awake()
        {
            Current = this;
            _cooldownLeft = new float[RosterCount];
        }

        void Start()
        {
            if (waves == null) return;
            waves.WaveStarted += OnWaveStarted;
            waves.WaveCleared += OnWaveCleared;
            waves.Defeated += OnDefeated;
        }

        void OnEnable() => Enemy.Killed += OnEnemyKilled;

        void OnDisable() => Enemy.Killed -= OnEnemyKilled;

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
            if (!WaveManager.Fighting || config == null) return;
            AddResource(config.resourcePerSecond * Time.deltaTime);
            for (int i = 0; i < _cooldownLeft.Length; i++)
                _cooldownLeft[i] = Mathf.Max(0f, _cooldownLeft[i] - Time.deltaTime);
        }

        /// <summary>0..1 cho vong hoi chieu tren dau linh o bai: 1 = day, keo tha duoc (neu du tai nguyen).</summary>
        public float CooldownFraction(int index)
        {
            var soldier = roster[index];
            if (soldier.summonCooldown <= 0f) return 1f;
            return 1f - Mathf.Clamp01(_cooldownLeft[index] / soldier.summonCooldown);
        }

        public bool CanAfford(int index) => Resource >= roster[index].summonCost;

        /// <summary>Chi trieu hoi trong luc danh wave, khi vong day va du tai nguyen.</summary>
        public bool CanSummon(int index) =>
            WaveManager.Fighting && index >= 0 && index < RosterCount && roster[index] != null
            && _cooldownLeft[index] <= 0f && CanAfford(index) && unitPrefab != null;

        /// <summary>
        /// Tha top linh <paramref name="index"/> tai <paramref name="point"/> (world). Tra ve false — khong tru gi —
        /// neu chua trieu hoi duoc hoac diem tha nam ngoai san.
        /// </summary>
        public bool TrySummon(int index, Vector3 point)
        {
            point.z = 0f;
            if (!CanSummon(index) || !Battlefield.OnField(point)) return false;

            var soldier = roster[index];
            Resource -= soldier.summonCost;
            _cooldownLeft[index] = soldier.summonCooldown;
            Summoned?.Invoke(soldier, SpawnSquad(soldier, point));
            return true;
        }

        /// <summary>
        /// Top xep doc quanh diem tha (lech theo y, hang le lui nua buoc cho khoi thanh mot duong ke), moi con keo
        /// vao trong san. Vi tri luc Initialise la goc day xich cua linh tam xa.
        /// </summary>
        List<Hero> SpawnSquad(SoldierDefinition soldier, Vector3 centre)
        {
            var squad = new List<Hero>(soldier.squadSize);
            float spacing = config != null ? config.squadSpacing : 0.4f;
            for (int i = 0; i < soldier.squadSize; i++)
            {
                var offset = new Vector3(i % 2 == 1 ? -spacing * 0.5f : 0f, (i - (soldier.squadSize - 1) * 0.5f) * spacing, 0f);
                var position = Battlefield.Clamp(centre + offset, Battlefield.PersonalSpace * 0.5f, clampRight: true);
                var unit = Instantiate(unitPrefab, position, Quaternion.identity);
                unit.Initialise(soldier);
                SummonBurst.Spawn(unit.transform.position, unit.BodyRadius, config != null ? config.spawnFxFrames : null);
                _summoned.Add(unit);
                squad.Add(unit);
            }
            return squad;
        }

        void AddResource(float amount)
        {
            if (config == null || amount <= 0f) return;
            Resource = Mathf.Min(config.resourceCap, Resource + amount);
        }

        void OnEnemyKilled(Enemy enemy)
        {
            if (WaveManager.Fighting && config != null) AddResource(config.resourcePerKill);
        }

        // Dau wave: bai sach, moi vong day san. Tai nguyen giu hay khong do `OnWaveEnded` da xu ly.
        void OnWaveStarted(int wave)
        {
            DespawnAll();
            Array.Clear(_cooldownLeft, 0, _cooldownLeft.Length);
        }

        void OnWaveCleared(int wave, int gold) => OnWaveEnded();

        void OnDefeated(int wave) => OnWaveEnded();

        void OnWaveEnded()
        {
            DespawnAll();
            if (config == null || !config.keepBetweenWaves) Resource = 0f;
        }

        void DespawnAll()
        {
            foreach (var unit in _summoned)
                if (unit != null) Destroy(unit.gameObject);
            _summoned.Clear();
        }

        /// <summary>Admin panel: nap day tai nguyen va day moi vong hoi chieu.</summary>
        public void DevFill()
        {
            Resource = ResourceCap;
            Array.Clear(_cooldownLeft, 0, _cooldownLeft.Length);
        }
    }
}
