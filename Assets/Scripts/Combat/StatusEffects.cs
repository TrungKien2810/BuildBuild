using System;
using System.Collections.Generic;
using BuildBuild.Core;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Ma hieu ung trang thai dung chung moi nguon. Them hieu ung moi: them ma o day + luat o `StatusEffects.Apply`
    /// (+ FX trong `StatusLibrary` neu co). Moi ma hien tai deu la debuff — `Cleanse` go het.
    /// </summary>
    public enum StatusId { Burn, Poison, Slow, Freeze, Stun, ArmorBreak, NoHeal }

    /// <summary>
    /// Kho hieu ung trang thai cua MOT unit (`Unit.Status`, tu gan lan dau). Moi nguon — Phap Su Lua, Ky Nhong, dat chay
    /// cua Rong, Phu Thuy Dam Lay, vung doc Goblin, phao Bang... — goi chung `Apply`, nen cung mot luat cong don, mot FX,
    /// mot icon, va `Cleanse` (An Duc, giai debuff ve sau) go duoc moi thu ma khong can biet nguon.
    ///
    /// Luat cong don:
    ///   - Poison: cong tang toi tran (tran lon nhat tung gap), sat thuong moi tang lay muc cao hon, han dai hon;
    ///   - con lai: KHONG cong don — lay muc MANH hon va han DAI hon (Chay giu nguon cua lan manh nhat, de lan khi chet).
    /// DoT (Burn, Poison) tick moi 0,5s; NoHeal ghi xuyen sang `Health.NoHealUntil` (Health chan hoi mau).
    /// </summary>
    public class StatusEffects : MonoBehaviour
    {
        const float TickInterval = 0.5f;

        class Entry
        {
            public float until;
            public float power;
            public float seconds;       // han cua lan ap manh nhat — Chay lan sang con khac dung lai han nay
            public int stacks;
            public int maxStacks;
            public float spread;
            public float nextTick;
            public Health source;
            public Faction side;
            public Sprite[] fxFrames;
            public GameObject fx;
            public SpriteRenderer fxRenderer;
        }

        static readonly int Count = Enum.GetValues(typeof(StatusId)).Length;

        readonly Entry[] _entries = new Entry[Count];
        Unit _unit;
        bool _any;

        public void Bind(Unit unit)
        {
            _unit = unit;
            for (int i = 0; i < Count; i++)
                if (_entries[i] == null) _entries[i] = new Entry();
        }

        public bool Has(StatusId id) => Time.time < _entries[(int)id].until;

        /// <summary>Muc cua hieu ung (Chay/Doc: sat thuong/giay moi tang; Cham: phan giam toc; Pha giap: phan nhan them). 0 neu khong co.</summary>
        public float Power(StatusId id) => Has(id) ? _entries[(int)id].power : 0f;

        public int Stacks(StatusId id) => Has(id) ? _entries[(int)id].stacks : 0;

        public float Until(StatusId id) => _entries[(int)id].until;

        /// <param name="power">Xem `Power`. Choang / Dong bang / Cam hoi mau: bo qua.</param>
        /// <param name="side">Phe cua NGUOI gay — Chay chi lan sang doi thu cua phe nay.</param>
        /// <param name="maxStacks">Tran tang Doc.</param>
        /// <param name="spread">Chay: &gt; 0 thi unit chet luc dang chay lam lua nhay sang doi thu gan nhat trong ban kinh nay.</param>
        /// <param name="fx">FX bam than RIENG cua nguon (dac trung tung unit). Null = FX chung trong `StatusLibrary`.
        /// Hieu ung van la MOT trang thai — hinh hien theo lan ap dang manh nhat.</param>
        public void Apply(StatusId id, float power, float seconds, Health source = null, Faction side = Faction.Player,
            int stacks = 1, int maxStacks = 1, float spread = 0f, Sprite[] fx = null)
        {
            if (_unit == null || _unit.IsDying || seconds <= 0f) return;
            if (IsDot(id) && power <= 0f) return;
            var e = _entries[(int)id];
            bool active = Has(id);
            float now = Time.time;

            if (id == StatusId.Poison)
            {
                if (stacks <= 0) return;
                e.maxStacks = active ? Mathf.Max(e.maxStacks, maxStacks) : Mathf.Max(1, maxStacks);
                e.stacks = Mathf.Min(e.maxStacks, (active ? e.stacks : 0) + stacks);
            }
            if (!active || power >= e.power)
            {
                e.power = power;
                e.seconds = seconds;
                e.source = source;
                e.side = side;
                var frames = fx != null && fx.Length > 0 ? fx : StatusLibrary.FxOf(id);
                if (frames != e.fxFrames)
                {
                    if (e.fx != null) Destroy(e.fx);
                    e.fx = null;
                    e.fxFrames = frames;
                }
            }
            e.spread = active ? Mathf.Max(e.spread, spread) : spread;
            if (!active) e.nextTick = now + TickInterval;
            e.until = Mathf.Max(active ? e.until : 0f, now + seconds);
            _any = true;

            if (id == StatusId.NoHeal) _unit.Health.NoHealUntil = e.until;
            if (e.fx == null)
            {
                e.fx = OneShotFx.SpawnLoop(_unit.transform, Vector3.zero, e.fxFrames, float.PositiveInfinity, SortOrder.ForUnit(_unit) + 1);
                e.fxRenderer = e.fx != null ? e.fx.GetComponent<SpriteRenderer>() : null;
            }
            // Bong bong Doc dam dan theo so tang.
            if (id == StatusId.Poison && e.fxRenderer != null) e.fxRenderer.color = new Color(1f, 1f, 1f, 0.4f + 0.12f * e.stacks);
        }

        public void Remove(StatusId id)
        {
            var e = _entries[(int)id];
            e.until = 0f;
            e.stacks = 0;
            e.source = null;
            e.fxFrames = null;
            if (e.fx != null) Destroy(e.fx);
            e.fx = null;
            e.fxRenderer = null;
            if (id == StatusId.NoHeal && _unit != null) _unit.Health.NoHealUntil = 0f;
        }

        /// <summary>Go moi debuff (An Duc, hero giai debuff).</summary>
        public void Cleanse() => Clear();

        /// <summary>Xoa sach (unit dung lai / chet).</summary>
        public void Clear()
        {
            for (int i = 0; i < Count; i++) Remove((StatusId)i);
            _any = false;
        }

        /// <summary>Dong hien len bang thong tin hero.</summary>
        public void Collect(List<UnitEffect> into)
        {
            if (!_any) return;
            if (Has(StatusId.Stun)) into.Add(new UnitEffect(EffectKind.Stun, "Stun", Until(StatusId.Stun)));
            if (Has(StatusId.Freeze)) into.Add(new UnitEffect(EffectKind.Freeze, "Freeze", Until(StatusId.Freeze)));
            if (_unit.IsSlowed)
                into.Add(new UnitEffect(EffectKind.Slow, $"Slow {Percent(Power(StatusId.Slow))}", Until(StatusId.Slow)));
            if (Has(StatusId.ArmorBreak))
                into.Add(new UnitEffect(EffectKind.ArmorBreak, $"Armor Break +{Percent(Power(StatusId.ArmorBreak))}", Until(StatusId.ArmorBreak)));
            if (Has(StatusId.Burn)) into.Add(new UnitEffect(EffectKind.Burn, "Burn", Until(StatusId.Burn)));
            if (Has(StatusId.Poison))
                into.Add(new UnitEffect(EffectKind.Poison, "Poison", Until(StatusId.Poison), stacks: Stacks(StatusId.Poison)));
            if (Has(StatusId.NoHeal)) into.Add(new UnitEffect(EffectKind.NoHeal, "Heal Block", Until(StatusId.NoHeal)));
        }

        static string Percent(float fraction) => Mathf.RoundToInt(fraction * 100f) + "%";

        static bool IsDot(StatusId id) => id == StatusId.Burn || id == StatusId.Poison;

        void Update()
        {
            if (!_any || _unit == null) return;
            if (_unit.IsDying)
            {
                var burn = _entries[(int)StatusId.Burn];
                if (Has(StatusId.Burn) && burn.spread > 0f) SpreadBurn(burn);
                Clear();
                return;
            }

            float now = Time.time;
            int order = SortOrder.ForUnit(_unit) + 1;
            bool any = false;
            for (int i = 0; i < Count; i++)
            {
                var e = _entries[i];
                if (e.until <= 0f) continue;
                if (now >= e.until)
                {
                    Remove((StatusId)i);
                    continue;
                }
                any = true;
                if (e.fxRenderer != null) e.fxRenderer.sortingOrder = order;
                if (!IsDot((StatusId)i) || now < e.nextTick) continue;
                e.nextTick += TickInterval;
                float perTick = e.power * TickInterval * (i == (int)StatusId.Poison ? e.stacks : 1);
                _unit.Health.TakeDamage(perTick, e.source != null ? e.source : null, kind: DamageKind.Periodic);
            }
            _any = any;
        }

        /// <summary>Ben Lua: Chay moi cho doi thu gan xac nhat trong `spread`.</summary>
        void SpreadBurn(Entry burn)
        {
            var at = _unit.transform.position;
            Unit best = null;
            float bestDistance = float.PositiveInfinity;
            foreach (var other in Unit.Hostiles(burn.side))
            {
                if (other == null || other == _unit || other.IsDying) continue;
                float distance = Battlefield.GroundDistance(other.AimPoint(at), at) - other.BodyRadius;
                if (distance > burn.spread || distance >= bestDistance) continue;
                bestDistance = distance;
                best = other;
            }
            if (best != null)
                best.Status.Apply(StatusId.Burn, burn.power, burn.seconds, burn.source, burn.side, spread: burn.spread,
                    fx: burn.fxFrames);
        }
    }
}
