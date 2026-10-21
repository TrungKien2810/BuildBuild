using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuildBuild.Meta
{
    /// <summary>
    /// Moc thuong SAU wave 30 + ky luc wave.
    ///
    /// Wave trong game VON KHONG GIOI HAN (`WaveTuning.BandFor` tu xu ly wave qua bang: chia het 10 dung
    /// bang boss, con lai don ve bang cuoi; `bossMultiplierPer10Waves` leo thang) — nhung qua wave 30 nguoi
    /// choi khong con muc tieu hay phan thuong nao, danh tiep chi de xem minh chiu duoc den dau. Lop nay bien
    /// "danh tiep" thanh co thuong: moi 5 wave mot moc (vang + manh) va ghi ky luc vao save.
    ///
    /// Nhan `addGold` dang uy quyen chu khong tu goi `Economy`: `Meta` khong duoc biet `Core` (vong namespace
    /// da ghi o `IDEAS.md` "No kien truc"), cung ly do `PlayerLevel` chi ban su kien.
    /// </summary>
    public static class WaveMilestones
    {
        /// <summary>Moc bat dau tinh SAU wave nay — wave 30 la boss cuoi cua bang wave.</summary>
        public const int FromWave = 30;
        public const int EveryWaves = 5;

        public static bool IsMilestone(int wave) => wave > FromWave && wave % EveryWaves == 0;

        /// <summary>Vang moc — leo theo wave de wave 60 van con dang danh.</summary>
        public static int GoldFor(int wave) => 200 + 20 * wave;

        /// <summary>Manh cho mot unit dang so huu; moc tron chuc (40/50/...) gap doi.</summary>
        public static int ShardsFor(int wave) => wave % 10 == 0 ? 2 : 1;

        /// <summary>Ghi ky luc wave cao nhat. Tra ve true neu vua lap ky luc moi.</summary>
        public static bool Record(int wave)
        {
            if (wave <= SaveSystem.Data.highestWave) return false;
            SaveSystem.Data.highestWave = wave;
            return true;
        }

        /// <summary>
        /// Tra thuong moc neu wave nay la moc. Tra ve dong ghi chu de HUD in vao bang ket qua; null khi
        /// khong phai moc (hoac acc chua so huu unit nao nen khong cong duoc manh).
        /// </summary>
        public static string MaybeGrant(int wave, Action<int> addGold)
        {
            if (!IsMilestone(wave)) return null;

            int gold = GoldFor(wave);
            addGold?.Invoke(gold);

            var data = SaveSystem.Data;
            int shards = 0;
            if (data.owned.Count > 0)
            {
                shards = ShardsFor(wave);
                for (int i = 0; i < shards; i++) data.owned[Random.Range(0, data.owned.Count)].shards++;
                SaveSystem.Save();
            }

            return shards > 0
                ? $"Wave {wave} milestone:  +{gold} Gold  ·  +{shards} Shards"
                : $"Wave {wave} milestone:  +{gold} Gold";
        }
    }
}
