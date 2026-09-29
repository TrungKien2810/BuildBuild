using System.Collections.Generic;
using System.Linq;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Meta
{
    /// <summary>
    /// Kho trang bi trong save: mac / thao / nang cap / roi khi thang wave. Moi mon mac cho DUNG MOT hero (`heroId` = unitId,
    /// rong = nam trong tui), moi hero toi da mot mon moi o. So nam o `EquipmentConfig`.
    /// </summary>
    public static class Armory
    {
        static List<OwnedEquipment> Items => SaveSystem.Data.equipment;

        public static OwnedEquipment Equipped(string heroId, EquipmentSlot slot) =>
            Items.FirstOrDefault(e => e.heroId == heroId && e.slot == slot);

        /// <summary>Mon trong tui cua mot o, do hiem cao + level cao truoc.</summary>
        public static List<OwnedEquipment> Bag(EquipmentSlot slot) =>
            Items.Where(e => string.IsNullOrEmpty(e.heroId) && e.slot == slot)
                .OrderByDescending(e => e.rarity).ThenByDescending(e => e.level).ToList();

        /// <summary>Mac `item` cho hero; mon dang o o do (neu co) ve tui.</summary>
        public static void Equip(OwnedEquipment item, string heroId)
        {
            var current = Equipped(heroId, item.slot);
            if (current != null) current.heroId = "";
            item.heroId = heroId;
            SaveSystem.Save();
        }

        public static void Unequip(OwnedEquipment item)
        {
            item.heroId = "";
            SaveSystem.Save();
        }

        /// <summary>Tong % cong them tu do hero dang mac, theo tung chi so.</summary>
        public static void Bonuses(string heroId, EquipmentConfig config, out float health, out float damage, out float attackRate)
        {
            health = damage = attackRate = 0f;
            if (config == null || string.IsNullOrEmpty(heroId)) return;
            foreach (var item in Items)
            {
                if (item.heroId != heroId) continue;
                float bonus = config.Bonus(item.slot, item.rarity, item.level);
                switch (item.slot)
                {
                    case EquipmentSlot.Weapon: damage += bonus; break;
                    case EquipmentSlot.Ring: attackRate += bonus; break;
                    default: health += bonus; break;
                }
            }
        }

        public static OwnedEquipment Add(EquipmentSlot slot, Rarity rarity)
        {
            var data = SaveSystem.Data;
            var item = new OwnedEquipment { uid = ++data.nextEquipmentUid, slot = slot, rarity = rarity };
            data.equipment.Add(item);
            SaveSystem.Save();
            return item;
        }

        /// <summary>
        /// Luot roi sau khi thang `wave`: wave boss chac chan roi (it nhat R), wave thuong theo `dropChance`. Do hiem noi suy
        /// ti trong tu wave 1 toi `lateWave`. Tra null neu khong roi.
        /// </summary>
        public static OwnedEquipment RollDrop(EquipmentConfig config, int wave)
        {
            if (config == null) return null;
            bool guaranteed = config.guaranteedEvery > 0 && wave % config.guaranteedEvery == 0;
            if (!guaranteed && Random.value >= config.dropChance) return null;

            float t = Mathf.Clamp01((wave - 1f) / Mathf.Max(1, config.lateWave - 1));
            var weights = new float[4];
            for (int i = 0; i < weights.Length; i++)
                weights[i] = Mathf.Lerp(config.earlyWeights[i], config.lateWeights[i], t);
            if (guaranteed) weights[(int)Rarity.N] = 0f;

            float x = Random.value * weights.Sum();
            var rarity = Rarity.SSR;
            for (int i = 0; i < weights.Length; i++)
                if ((x -= weights[i]) < 0f) { rarity = (Rarity)i; break; }
            return Add((EquipmentSlot)Random.Range(0, 4), rarity);
        }
    }
}
