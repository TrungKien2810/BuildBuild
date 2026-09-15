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
        public bool starUp;
    }

    /// <summary>
    /// Luat quay theo GAME_DESIGN.md muc 6. KHONG tru vang — nguoi goi tru qua `Economy` truoc, de chi
    /// mot noi quyet dinh "du tien hay khong".
    ///
    /// Do hiem gan co dinh vao unit. Quay ra mot bac chua co unit nao (roster hien tai chi co N + mot
    /// SR) thi ha dan xuong bac thap hon con unit — thu cung loai truoc, roi loai kia.
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

        public List<GachaResult> Roll(int count)
        {
            var results = new List<GachaResult>(count);
            for (int i = 0; i < count; i++)
            {
                var category = Random.value < _config.heroShare ? UnitCategory.Hero : UnitCategory.Cannon;
                var unit = PickUnit(RollRarityWithPity(), category);
                if (unit == null)
                {
                    Debug.LogError("[Gacha] GachaConfig.pool chua co unit nao.");
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

        UnitDefinition PickUnit(Rarity rarity, UnitCategory category)
        {
            var other = category == UnitCategory.Hero ? UnitCategory.Cannon : UnitCategory.Hero;
            for (int tier = (int)rarity; tier >= 0; tier--)
            {
                var unit = RandomUnit((Rarity)tier, category) ?? RandomUnit((Rarity)tier, other);
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
            while (owned.shards >= _config.shardsPerStar)
            {
                owned.shards -= _config.shardsPerStar;
                owned.stars++;
                result.starUp = true;
            }
            return result;
        }
    }
}
