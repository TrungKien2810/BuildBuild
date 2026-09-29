using System.Collections.Generic;
using System.Linq;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Meta
{
    public struct GachaResult
    {
        public UnitDefinition unit;
        public bool isNew;
        public int shardsGained;
        /// <summary>Du mang de bam nang sao (khong tu len — `Progression.TryStarUp`).</summary>
        public bool canStarUp;
    }

    /// <summary>
    /// Luat quay theo GAME_DESIGN.md muc 6. KHONG tru vang — nguoi goi tru qua `Economy` truoc, de chi
    /// mot noi quyet dinh "du tien hay khong".
    ///
    /// Do hiem gan co dinh vao unit. Quay ra mot bac chua co unit nao (roster hien tai chi co N + mot
    /// SR) thi ha dan xuong bac thap hon con unit — thu cung loai truoc, roi cac loai kia.
    ///
    /// Pity dem theo bac QUAY RA, khong theo unit nhan duoc: dem theo unit thi khi roster chua co SSR,
    /// bo dem SSR khong bao gio ve 0 va moi luot sau luot 80 deu bi ep SSR.
    /// </summary>
    public class GachaSystem
    {
        readonly GachaConfig _config;
        readonly SaveData _save;

        public GachaSystem(GachaConfig config, SaveData save)
        {
            _config = config;
            _save = save;
        }

        /// <summary>
        /// <paramref name="only"/> = null: banner tong hop (chia loai theo `heroShare`/`soldierShare`). Co gia tri: banner
        /// rieng mot loai — chi ra unit loai do, bac trong thi ha bac trong CUNG loai. Pity dung chung moi banner.
        /// </summary>
        public List<GachaResult> Roll(int count, UnitCategory? only = null)
        {
            var results = new List<GachaResult>(count);
            for (int i = 0; i < count; i++)
            {
                var unit = PickUnit(RollRarityWithPity(), only ?? RollCategory(), fallbackOtherCategories: only == null);
                if (unit == null)
                {
                    Debug.LogError($"[Gacha] GachaConfig.pool chua co unit nao{(only != null ? $" loai {only}" : "")}.");
                    break;
                }
                results.Add(Grant(unit));
            }
            SaveSystem.Save();
            return results;
        }

        Rarity RollRarityWithPity()
        {
            var rarity = _config.RollRarity(Random.value);
            if (_save.rollsSinceSSR >= _config.hardPity) rarity = Rarity.SSR;
            else if (_save.rollsSinceSR >= _config.softPity && rarity < Rarity.SR) rarity = Rarity.SR;

            _save.rollsSinceSSR = rarity == Rarity.SSR ? 0 : _save.rollsSinceSSR + 1;
            _save.rollsSinceSR = rarity >= Rarity.SR ? 0 : _save.rollsSinceSR + 1;
            return rarity;
        }

        /// <summary>Hero `heroShare`, linh `soldierShare`, con lai la phao.</summary>
        UnitCategory RollCategory()
        {
            float x = Random.value;
            if (x < _config.heroShare) return UnitCategory.Hero;
            if (x < _config.heroShare + _config.soldierShare) return UnitCategory.Soldier;
            return UnitCategory.Cannon;
        }

        static readonly UnitCategory[] Categories = { UnitCategory.Hero, UnitCategory.Cannon, UnitCategory.Soldier };

        /// <summary>Moi bac: loai da quay truoc, roi (banner tong hop) cac loai con lai; bac trong het thi ha xuong bac duoi.</summary>
        UnitDefinition PickUnit(Rarity rarity, UnitCategory category, bool fallbackOtherCategories)
        {
            for (int tier = (int)rarity; tier >= 0; tier--)
            {
                var unit = RandomUnit((Rarity)tier, category);
                if (fallbackOtherCategories)
                    foreach (var other in Categories)
                        if (unit == null && other != category) unit = RandomUnit((Rarity)tier, other);
                if (unit != null) return unit;
            }
            return null;
        }

        UnitDefinition RandomUnit(Rarity rarity, UnitCategory category)
        {
            var matches = _config.pool.Where(u => u != null && u.rarity == rarity && u.category == category).ToList();
            return matches.Count == 0 ? null : matches[Random.Range(0, matches.Count)];
        }

        GachaResult Grant(UnitDefinition unit)
        {
            var result = new GachaResult { unit = unit };
            var owned = _save.Find(unit.id);
            if (owned == null)
            {
                _save.owned.Add(new OwnedUnit { id = unit.id });
                result.isNew = true;
                return result;
            }

            owned.shards += _config.shardsPerDuplicate;
            result.shardsGained = _config.shardsPerDuplicate;
            // Nang sao la nut bam, khong tu len khi quay (chot 2026-09-28) — mang du thi chi bao.
            result.canStarUp = Progression.CanStarUp(owned, _config.shardsPerStar);
            return result;
        }
    }
}
