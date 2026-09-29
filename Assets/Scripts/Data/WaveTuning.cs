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
        [Tooltip("He so nhan chi so boss: wave 10 = 1, wave 20 = 2, wave 30 = 3.5. Thay cho cong thuc len thang theo wave.")]
        public float bossStatMultiplier = 1f;
        [Tooltip("-1 = so quai thuong theo cong thuc (5 + wave/2). >= 0 = dung so nay (wave boss 10/20/30: 6, 4, 6 ho tong — boss luon co quai nho di kem de buff).")]
        public int fixedCount = -1;
        [Tooltip("So tinh anh (`WaveTuning.elite`) spawn THEM trong wave. GDD: tu wave 12 la 1, wave 21+ la 2, khong co o wave boss.")]
        public int eliteCount;

        public bool Contains(int wave) => wave >= fromWave && wave <= toWave;
    }

    /// <summary>
    /// Cong thuc len thang + bang wave. Tat ca so lay tu GAME_DESIGN.md muc 5 va muc 7.
    ///
    ///   Mau quai   = Mau goc x (1 + 0.12 x wave)
    ///   Sat thuong = Goc      x (1 + 0.08 x wave)
    ///   So luong   = 5 + floor(wave / 2)
    ///   Vang wave  = 80 + 18 x wave (+ bossGoldReward o wave boss) — CHI khi thang, giet quai khong ra vang
    ///
    /// Khong gioi han wave (nguoi dung chot 2026-09-29): qua het bang thi wave chia het cho 10 dung band boss CUOI, con lai
    /// dung band thuong CUOI; mau / sat thuong / so luong van len theo cong thuc tren, he so boss cong
    /// `bossMultiplierPer10Waves` moi 10 wave tinh tu band boss cuoi.
    /// </summary>
    [CreateAssetMenu(menuName = "BuildBuild/Wave Tuning", fileName = "WaveTuning")]
    public class WaveTuning : ScriptableObject
    {
        [Header("Len thang theo wave")]
        public float healthPerWave = 0.12f;
        public float damagePerWave = 0.08f;
        public int baseEnemyCount = 5;

        [Header("Kinh te")]
        [Tooltip("Vang thang wave = base + perWave x wave. Nguon vang DUY NHAT trong tran (giet quai khong ra vang).")]
        public int waveGoldBase = 80;
        public int waveGoldPerWave = 18;
        [Tooltip("Cong them khi thang wave co boss.")]
        public int bossGoldReward = 200;

        [Header("Nhip do")]
        [Tooltip("Giay giua hai lan spawn trong cung mot wave.")]
        public float spawnInterval = 1.2f;
        [Tooltip("Giay nghi giua hai wave.")]
        public float timeBetweenWaves = 4f;

        [Header("Bang wave")]
        public WaveBand[] bands;

        [Header("Quai dac biet")]
        [Tooltip("Qua het bang wave: he so chi so boss cong them moi 10 wave (wave 30 = 3.5 -> 40 = 5 -> 50 = 6.5 ...).")]
        public float bossMultiplierPer10Waves = 1.5f;
        [Tooltip("Boss cua wave co `hasBoss`.")]
        public EnemyDefinition boss;
        [Tooltip("Tinh anh cua `WaveBand.eliteCount` (Gatebreaker).")]
        public EnemyDefinition elite;

        public float HealthMultiplier(int wave) => 1f + healthPerWave * wave;
        public float DamageMultiplier(int wave) => 1f + damagePerWave * wave;
        public int EnemyCount(int wave) => baseEnemyCount + wave / 2;

        /// <summary>So quai THUONG cua wave (khong tinh boss va tinh anh).</summary>
        public int RegularCount(int wave, WaveBand band) =>
            band != null && band.fixedCount >= 0 ? band.fixedCount : EnemyCount(wave);
        public int WaveGold(int wave) =>
            waveGoldBase + waveGoldPerWave * wave + (BandFor(wave) is { hasBoss: true } ? bossGoldReward : 0);

        public WaveBand BandFor(int wave)
        {
            if (bands == null || bands.Length == 0) return null;
            foreach (var b in bands)
                if (b.Contains(wave)) return b;
            bool bossWave = wave % 10 == 0;
            for (int i = bands.Length - 1; i >= 0; i--)
                if (bands[i].hasBoss == bossWave) return bands[i];
            return bands[bands.Length - 1];
        }

        /// <summary>He so chi so boss o wave nay: trong bang dung `bossStatMultiplier`, qua bang thi cong them moi 10 wave.</summary>
        public float BossMultiplier(int wave, WaveBand band)
        {
            int past = band.Contains(wave) ? 0 : (wave - band.toWave) / 10;
            return band.bossStatMultiplier + bossMultiplierPer10Waves * Mathf.Max(0, past);
        }
    }
}
