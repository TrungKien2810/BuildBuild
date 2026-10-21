using System;
using BuildBuild.Data;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuildBuild.Meta
{
    /// <summary>
    /// Ruong AFK tren san sanh (GDD muc 8): tich thuong theo thoi gian thuc ke tu lan nhan truoc, tran 12 gio. Moc thoi gian
    /// nam trong save (`SaveData.afkSince`, giay Unix) nen tat game van tich. Muc thuong/gio theo wave cao nhat da thang
    /// (`nextWave - 1` — thua khong lui wave). Thuong: vang + co hoi manh + co hoi trang bi. Moi so la TAM (Claude, chua can).
    /// </summary>
    public static class AfkChest
    {
        /// <summary>Tich toi da 12 gio — de lau hon khong them gi.</summary>
        public const float CapHours = 12f;
        /// <summary>Moc doi hinh ruong: duoi 1 gio dong, 1-8 gio he sang, tu 8 gio day tran.</summary>
        public const float GlowHours = 1f, FullHours = 8f;
        /// <summary>Vang moi gio = he so nay x vang thang wave cao nhat.</summary>
        const float GoldPerHourOfWaveReward = 0.5f;
        /// <summary>Moi tron 2 gio: 25% duoc 1 manh cho mot unit dang so huu (ngau nhien).</summary>
        const float ShardEveryHours = 2f, ShardChance = 0.25f;
        /// <summary>Moi tron 4 gio: mot luot roi trang bi nhu thang wave thuong (co hoi theo `EquipmentConfig`).</summary>
        const float DropEveryHours = 4f;

        public enum Stage { Closed, Glowing, Full }

        public struct Reward
        {
            public int gold;
            public int shards;
            public OwnedEquipment drop;
        }

        static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>Gio da tich (0..`CapHours`). Save chua co moc (acc cu / acc moi) thi bat dau dem tu bay gio.</summary>
        public static float Hours
        {
            get
            {
                var data = SaveSystem.Data;
                if (data.afkSince <= 0)
                {
                    data.afkSince = Now;
                    SaveSystem.Save();
                }
                return Mathf.Clamp((Now - data.afkSince) / 3600f, 0f, CapHours);
            }
        }

        public static Stage Current => Hours >= FullHours ? Stage.Full : Hours >= GlowHours ? Stage.Glowing : Stage.Closed;

        static int BestWave => Mathf.Max(1, SaveSystem.Data.nextWave - 1);

        /// <summary>Vang se nhan neu bam bay gio — de hien len ruong.</summary>
        public static int PendingGold(WaveTuning tuning) =>
            tuning == null ? 0 : Mathf.FloorToInt(Hours * GoldPerHourOfWaveReward * tuning.WaveGold(BestWave));

        /// <summary>
        /// Nhan thuong va dat lai moc ve bay gio. Manh / trang bi ghi thang vao save; VANG tra ve de nguoi goi cong qua
        /// `Economy` (cho HUD nghe `Changed`).
        /// </summary>
        public static Reward Claim(WaveTuning tuning, EquipmentConfig equipment)
        {
            float hours = Hours;
            var reward = new Reward { gold = PendingGold(tuning) };

            var data = SaveSystem.Data;
            int shardRolls = Mathf.FloorToInt(hours / ShardEveryHours);
            for (int i = 0; i < shardRolls && data.owned.Count > 0; i++)
            {
                if (Random.value >= ShardChance) continue;
                data.owned[Random.Range(0, data.owned.Count)].shards++;
                reward.shards++;
            }

            int dropRolls = Mathf.FloorToInt(hours / DropEveryHours);
            for (int i = 0; i < dropRolls && reward.drop == null; i++)
                reward.drop = Armory.RollDrop(equipment, BestWave);

            data.afkSince = Now;
            QuestLog.CountAfkClaim();   // nhiem vu "Claim the AFK Chest"
            SaveSystem.Save();
            return reward;
        }

        /// <summary>Admin (F1): lui moc AFK <paramref name="hours"/> gio de thu ruong ma khong phai cho.</summary>
        public static void DevAddHours(float hours)
        {
            _ = Hours;   // save chua co moc thi dat moc truoc khi lui
            SaveSystem.Data.afkSince = Math.Max(1, SaveSystem.Data.afkSince - (long)(hours * 3600f));
            SaveSystem.Save();
        }
    }
}
