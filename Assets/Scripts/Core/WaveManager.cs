using System;
using System.Collections;
using System.Collections.Generic;
using BuildBuild.Combat;
using BuildBuild.Data;
using BuildBuild.Meta;
using UnityEngine;

namespace BuildBuild.Core
{
    /// <summary>
    /// Danh TUNG wave mot (GAME_DESIGN.md muc 1): nguoi choi bam bat dau -> spawn het quai cua wave ->
    /// diet het thi THANG, cong vang roi DUNG lai de nguoi choi di nang cap cho wave sau.
    /// Thua (lau dai sup) cung dung tai wave do — lan bam sau danh lai dung wave vua thua, khong ve wave 1.
    /// Lau dai hoi day mau NGAY khi wave ket thuc (thang lan thua), khong doi den luc bam danh wave sau.
    ///
    /// Lop nay khong tu cham vao vi tien; no chi cong vang qua `Economy`.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        [SerializeField] WaveTuning tuning;
        [SerializeField] Enemy enemyPrefab;
        [SerializeField] Castle castle;
        [SerializeField] Economy economy;

        [Header("Duong di cua quai")]
        [Tooltip("Diem quai xuat hien (mep phai) — chi dung x; y boc ngau nhien trong dai dat cua Battlefield.")]
        [SerializeField] Transform spawnPoint;
        [SerializeField] Battlefield battlefield;

        readonly List<Enemy> _alive = new List<Enemy>();
        Coroutine _loop;

        /// <summary>
        /// Wave se danh o lan bam tiep theo (dang danh thi la wave hien tai). Chi tang khi THANG. Nam trong
        /// save chu khong trong component: sang man gacha la scene Battle bi huy, quay lai van dung wave cu.
        /// </summary>
        public int NextWave
        {
            get => Mathf.Max(1, SaveSystem.Data.nextWave);
            private set
            {
                SaveSystem.Data.nextWave = value;
                SaveSystem.Save();
            }
        }
        /// <summary>Ban tinh cua `IsFighting` cho unit khong giu tham chieu (Hero chi hoi mana khi dang danh).</summary>
        public static bool Fighting { get; private set; }
        public bool IsFighting { get => Fighting; private set => Fighting = value; }
        public bool AllCleared => tuning != null && NextWave > tuning.totalWaves;

        public event Action<int> WaveStarted;
        /// <summary>Tham so: wave vua thang, vang thuong.</summary>
        public event Action<int, int> WaveCleared;
        /// <summary>Tham so: wave vua thua.</summary>
        public event Action<int> Defeated;

        void OnEnable()
        {
            if (castle != null) castle.Destroyed += OnCastleDestroyed;
        }

        void OnDisable()
        {
            if (castle != null) castle.Destroyed -= OnCastleDestroyed;
            Fighting = false;   // roi scene giua wave: co tinh khong duoc treo sang lan vao sau
        }

        public void StartWave()
        {
            if (IsFighting || AllCleared) return;
            if (tuning == null || enemyPrefab == null || spawnPoint == null)
            {
                Debug.LogError("[WaveManager] Thieu tuning / enemyPrefab / spawnPoint — khong chay duoc.");
                return;
            }

            ClearAlive();
            IsFighting = true;
            _loop = StartCoroutine(RunWave(NextWave));
        }

        IEnumerator RunWave(int wave)
        {
            WaveStarted?.Invoke(wave);

            yield return SpawnWave(wave);

            // Wave xong khi khong con con nao song.
            while (IsFighting && _alive.Count > 0) yield return null;
            if (!IsFighting) yield break;

            int gold = tuning.WaveGold(wave);
            if (economy != null) economy.Add(gold);
            IsFighting = false;
            NextWave = wave + 1;
            if (castle != null) castle.Repair();   // day mau ngay khi het tran, khong doi den luc bam danh tiep
            WaveCleared?.Invoke(wave, gold);
        }

        IEnumerator SpawnWave(int wave)
        {
            var band = tuning.BandFor(wave);
            var queue = BuildSpawnQueue(wave, band);
            if (queue.Count == 0)
            {
                Debug.LogWarning($"[WaveManager] Wave {wave} khong co loai quai nao trong bang.");
                yield break;
            }

            foreach (var entry in queue)
            {
                if (!IsFighting) yield break;
                Spawn(entry.definition, entry.health, entry.damage);
                yield return new WaitForSeconds(tuning.spawnInterval);
            }
        }

        struct SpawnEntry
        {
            public EnemyDefinition definition;
            public float health;
            public float damage;
        }

        /// <summary>
        /// Thu tu spawn cua mot wave (GAME_DESIGN.md muc 5): quai thuong boc ngau nhien tu bang, tinh anh (`eliteCount`)
        /// rai deu giua doan, boss ra SAU hai con ho tong dau tien (khong co ho tong thi ra ngay). Boss dung
        /// `bossStatMultiplier` thay cho cong thuc len thang theo wave.
        /// </summary>
        List<SpawnEntry> BuildSpawnQueue(int wave, WaveBand band)
        {
            var queue = new List<SpawnEntry>();
            if (band == null) return queue;

            float hp = tuning.HealthMultiplier(wave);
            float dmg = tuning.DamageMultiplier(wave);

            if (band.enemies != null && band.enemies.Length > 0)
            {
                int count = tuning.RegularCount(wave, band);
                for (int i = 0; i < count; i++)
                    queue.Add(new SpawnEntry
                    {
                        definition = band.enemies[UnityEngine.Random.Range(0, band.enemies.Length)],
                        health = hp,
                        damage = dmg,
                    });
            }

            if (tuning.elite != null && band.eliteCount > 0)
                for (int i = 0; i < band.eliteCount; i++)
                {
                    int at = (queue.Count + 1) * (i + 1) / (band.eliteCount + 1);
                    queue.Insert(Mathf.Clamp(at, 0, queue.Count), new SpawnEntry { definition = tuning.elite, health = hp, damage = dmg });
                }

            if (band.hasBoss && tuning.boss != null)
                queue.Insert(Mathf.Min(2, queue.Count), new SpawnEntry
                {
                    definition = tuning.boss,
                    health = band.bossStatMultiplier,
                    damage = band.bossStatMultiplier,
                });

            return queue;
        }

        void Spawn(EnemyDefinition definition, float healthMultiplier, float damageMultiplier)
        {
            var position = spawnPoint.position;
            if (battlefield != null) position.y = battlefield.RandomLaneY();
            var enemy = Instantiate(enemyPrefab, position, Quaternion.identity, transform);
            enemy.Initialise(definition, healthMultiplier, damageMultiplier);
            if (castle != null) enemy.SetTarget(castle.Health);
            EnemyExtras.Attach(enemy, damageMultiplier);    // giap, vien da ban xa, co che boss

            enemy.Died += OnEnemyDied;
            _alive.Add(enemy);
        }

        void OnEnemyDied(Enemy enemy, int gold)
        {
            enemy.Died -= OnEnemyDied;
            _alive.Remove(enemy);
            if (economy != null) economy.Add(gold);
        }

        void OnCastleDestroyed()
        {
            if (!IsFighting) return;
            IsFighting = false;
            if (_loop != null) StopCoroutine(_loop);
            ClearAlive();
            if (castle != null) castle.Repair();   // day mau ngay khi het tran, khong doi den luc bam danh tiep
            Defeated?.Invoke(NextWave);
        }

        /// <summary>Admin panel: nhay toi wave bat ky — chi giua hai wave.</summary>
        public void DevSetWave(int wave)
        {
            if (IsFighting || tuning == null) return;
            NextWave = Mathf.Clamp(wave, 1, tuning.totalWaves);
        }

        /// <summary>Admin panel: giet moi quai dang song (van cong vang, van tinh la thang neu da spawn het).</summary>
        public void DevKillAll()
        {
            foreach (var enemy in _alive.ToArray())
                if (enemy != null && !enemy.IsDying) enemy.Health.TakeDamage(enemy.Health.Current);
        }

        void ClearAlive()
        {
            foreach (var enemy in _alive)
            {
                if (enemy == null) continue;
                enemy.Died -= OnEnemyDied;
                Destroy(enemy.gameObject);
            }
            _alive.Clear();
        }
    }
}
