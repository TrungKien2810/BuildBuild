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
    /// Lau dai hoi day mau truoc moi wave.
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
            if (castle != null) castle.Repair();
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
            WaveCleared?.Invoke(wave, gold);
        }

        IEnumerator SpawnWave(int wave)
        {
            var band = tuning.BandFor(wave);
            if (band == null || band.enemies == null || band.enemies.Length == 0)
            {
                Debug.LogWarning($"[WaveManager] Wave {wave} khong co loai quai nao trong bang.");
                yield break;
            }

            int count = tuning.EnemyCount(wave);
            float hp = tuning.HealthMultiplier(wave);
            float dmg = tuning.DamageMultiplier(wave);

            for (int i = 0; i < count && IsFighting; i++)
            {
                Spawn(band.enemies[UnityEngine.Random.Range(0, band.enemies.Length)], hp, dmg);
                yield return new WaitForSeconds(tuning.spawnInterval);
            }
        }

        void Spawn(EnemyDefinition definition, float healthMultiplier, float damageMultiplier)
        {
            var position = spawnPoint.position;
            if (battlefield != null) position.y = battlefield.RandomLaneY();
            var enemy = Instantiate(enemyPrefab, position, Quaternion.identity, transform);
            enemy.Initialise(definition, healthMultiplier, damageMultiplier);
            if (castle != null) enemy.SetTarget(castle.Health);

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
