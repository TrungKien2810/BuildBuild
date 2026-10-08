using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>Loai hieu ung dang dinh tren unit — `HeroInfoPanel` tra icon theo chi so enum nay.</summary>
    public enum EffectKind { Stun, Slow, Haste, Root, Mark, ArmorBreak, Awe, Chain, Shield, Guard, NoHeal, DamageBuff, RateBuff, Skill, Passive, Burn, Poison, Freeze }

    /// <summary>
    /// Mot dong hieu ung de hien len bang thong tin. <see cref="icon"/> != null thi dung icon do (icon chieu / noi tai
    /// cua hero), con lai UI tra theo <see cref="kind"/>. <see cref="stacks"/> 0 = khong co tang; <see cref="secondsLeft"/>
    /// 0 = khong han (vd Cuong No, tang noi tai).
    /// </summary>
    public struct UnitEffect
    {
        public EffectKind kind;
        public string label;
        public Sprite icon;
        public int stacks;
        public float secondsLeft;

        public UnitEffect(EffectKind kind, string label, float until = 0f, int stacks = 0, Sprite icon = null)
        {
            this.kind = kind;
            this.label = label;
            this.icon = icon;
            this.stacks = stacks;
            secondsLeft = until > 0f ? Mathf.Max(0f, until - Time.time) : 0f;
        }
    }
}
