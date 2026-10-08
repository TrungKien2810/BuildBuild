using System.Collections.Generic;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>Phan cua `Hero` doc ra cho bang thong tin (`HeroInfoPanel`): chi so luc nay + hieu ung dang dinh.</summary>
    public partial class Hero
    {
        /// <summary>Ten hien thi cua buff `ApplyBuff` theo id.</summary>
        static readonly Dictionary<string, string> BuffNames = new Dictionary<string, string>
        {
            { "banner-damage", "Cờ hiệu" },
            { "banner-rate", "Cờ hiệu" },
            { "banner-rally", "Hiệu lệnh" },
            { "dao-vien", "Đào Viên" },
        };

        public int Stars => _stars;

        /// <summary>So don thuong moi giay luc nay — cung cong thuc voi cooldown trong `BeginCast`.</summary>
        public float AttacksPerSecond
        {
            get
            {
                AuraMultipliers(out _, out float rate);
                return _definition.attackRate * _rateMultiplier * (RapidFire ? _definition.skillPower : 1f) * rate;
            }
        }

        public override void CollectEffects(List<UnitEffect> into)
        {
            base.CollectEffects(into);

            string ultimate = SkillName(_definition.ultimateText);
            if (_petrified) into.Add(new UnitEffect(EffectKind.Skill, ultimate, _petrifyUntil, icon: _definition.ultimateIcon));
            if (RapidFire) into.Add(new UnitEffect(EffectKind.Skill, ultimate, _rapidFireUntil, icon: _definition.ultimateIcon));
            if (Raging) into.Add(new UnitEffect(EffectKind.Skill, "Cuồng Nộ", icon: _definition.ultimateIcon));

            foreach (var buff in _buffs)
            {
                if (Time.time >= buff.until) continue;
                BuffNames.TryGetValue(buff.id, out var name);
                if (buff.damage > 0f) into.Add(new UnitEffect(EffectKind.DamageBuff, $"{name} +{Percent(buff.damage)} công", buff.until));
                if (buff.rate > 0f) into.Add(new UnitEffect(EffectKind.RateBuff, $"{name} +{Percent(buff.rate)} tốc đánh", buff.until));
            }
            if (_health.Shield > 0f)
                into.Add(new UnitEffect(EffectKind.Shield, $"Khiên {Mathf.RoundToInt(_health.Shield)}", _shieldUntil));
            if (_health.Guard > 0f) into.Add(new UnitEffect(EffectKind.Guard, $"Giảm ST {Percent(_health.Guard)}", _guardUntil));

            // Noi tai tich tang.
            string passive = SkillName(_definition.passiveText);
            if (HasPassive(PassiveKind.Crit))
                into.Add(new UnitEffect(EffectKind.Passive, $"{passive} ({_definition.passiveEvery})",
                    stacks: _shotCount % _definition.passiveEvery, icon: _definition.passiveIcon));
            if (_dragonLink != null && _dragonLink.Rider == this && _dragonLink.RageStacks > 0)
                into.Add(new UnitEffect(EffectKind.Passive, "Long Nộ", stacks: _dragonLink.RageStacks, icon: _definition.passiveIcon));
            if (_brother != null) _brother.CollectEffects(into);
        }

        /// <summary>"Ten: mo ta" -> "Ten".</summary>
        public static string SkillName(string text)
        {
            if (string.IsNullOrEmpty(text)) return "Kỹ năng";
            int colon = text.IndexOf(':');
            return (colon > 0 ? text.Substring(0, colon) : text).Trim();
        }
    }
}
