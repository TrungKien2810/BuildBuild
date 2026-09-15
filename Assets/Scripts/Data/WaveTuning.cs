using System;
using UnityEngine;

namespace BuildBuild.Data
{
    /// <summary>Thanh phan quai cua mot khoang wave (GAME_DESIGN.md muc 5).</summary>
    [Serializable]
    public class WaveBand
    {
        [Tooltip("Ap dung cho wave tu (bao gom) …")]
        public int fromWave = 1;
        [Tooltip("… den (bao gom).")]
        public int toWave = 3;
        [Tooltip("Cac loai quai co the xuat hien trong khoang nay.")]
        public EnemyDefinition[] enemies;
        [Tooltip("Wave nay co boss khong (wave 10 / 20 / 30).")]
        public bool hasBoss;
        [Tooltip("He so nhan chi so boss: wave 10 = 1, wave 20 = 2, wave 30 = 3.5.")]
        public float bossStatMultiplier = 1f;

        public bool Contains(int wave) => wave >= fromWave && wave <= toWave;
    }

    /// <summary>
    /// Cong thuc len thang + bang wave. Tat ca so lay tu GAME_DESIGN.md muc 5 va muc 7.
    ///
    ///   Mau quai   = Mau goc x (1 + 0.12 x wave)
    ///   Sat thuong = Goc      x (1 + 0.08 x wave)
    ///   So luong   = 5 + floor(wave / 2)
    ///   Vang wave  = 50 + 10 x wave
    /// </summary>
    [CreateAssetMenu(menuName = "BuildBuild/Wave Tuning", fileName = "WaveTuning")]
    public class WaveTuning : ScriptableObject
    {
        [Header("Len thang theo wave")]
        public float healthPerWave = 0.12f;
        public float damagePerWave = 0.08f;
        public int baseEnemyCount = 5;

        [Header("Kinh te")]
        public int waveGoldBase = 50;
        public int waveGoldPerWave = 10;
        public int bossGoldReward = 200;

        [Header("Nhip do")]
        public int totalWaves = 30;
        [Tooltip("Giay giua hai lan spawn trong cung mot wave.")]
        public float spawnInterval = 1.2f;
        [Tooltip("Giay nghi giua hai wave.")]
        public float timeBetweenWaves = 4f;

        [Header("Bang wave")]
        public WaveBand[] bands;

        public float HealthMultiplier(int wave) => 1f + healthPerWave * wave;
        public float DamageMultiplier(int wave) => 1f + damagePerWave * wave;
        public int EnemyCount(int wave) => baseEnemyCount + wave / 2;
        public int WaveGold(int wave) => waveGoldBase + waveGoldPerWave * wave;

        public WaveBand BandFor(int wave)
        {
            if (bands == null) return null;
            foreach (var b in bands)
                if (b.Contains(wave)) return b;
            return bands.Length > 0 ? bands[bands.Length - 1] : null;
        }
    }
}
