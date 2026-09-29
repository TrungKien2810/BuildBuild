using System.Collections.Generic;
using BuildBuild.Core;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Phan cua `Hero` do nhanh Tru them (plan 2 thanh vien, muc B2/B3), tach file de khong dung vao luat di chuyen
    /// ma linh trieu hoi dung chung:
    ///   - Choang (`ApplyStun`) — boss Chan Dia choang hero / linh 2s;
    ///   - Hero HOI MAU (`HeroDefinition.healsAllies`, Vespera): nham dong minh thap mau nhat, khong ban quai;
    ///   - Den Canh (`SkillKind.Lantern`, Vespera): vong hoi mau + khien ao quanh dong minh thap mau nhat;
    ///   - FX buff Ban Nhanh Lien Hoan tren dau Hawkfletch thay cho to cam;
    ///   - Buff tu Banneret (`ApplyBuff`, `ApplyGuard`, dau co `ShowRally`) + di theo dong minh chung luat voi hero hoi mau.
    /// </summary>
    public partial class Hero
    {
        /// <summary>Cung mau vang voi quai bi choang — nguoi choi doc ra cung mot trang thai.</summary>
        static readonly Color StunnedTint = new Color(1f, 0.85f, 0.35f);

        /// <summary>Scale LOCAL cua FX buff (art 64 px, than hero da nhan UnitScale 2) -> rong ~ bang dau hero.</summary>
        const float BuffFxScale = 0.5f;

        /// <summary>FX buff dat tren thanh mana (StatBar dat mana o dinh dau + 0,09 local).</summary>
        const float BuffFxGap = 0.28f;

        /// <summary>Be rong elip trong art `dencanh-ring` (151 px, PPU 100) — vong ve dung 2 x `skillRadius` world.</summary>
        const float RingArtDiameter = 1.51f;

        /// <summary>Chieu cao vo khien trong art (74 px, PPU 100) va ti le so voi than: khien cao hon dau mot chut.</summary>
        const float ShieldArtHeight = 0.74f;
        const float ShieldPerBody = 1.35f;

        float _stunnedUntil;
        bool _stunVisual;
        GameObject _buffFx;
        float _shieldUntil;
        GameObject _shieldFx;
        SpriteRenderer _shieldRenderer;
        Sprite[] _shieldBreakFrames;
        readonly List<TimedBuff> _buffs = new List<TimedBuff>();
        float _guard;
        float _guardUntil;
        GameObject _rallyFx;

        struct TimedBuff
        {
            public string id;
            public float damage, rate, until;
        }

        public bool IsStunned => Time.time < _stunnedUntil;

        bool HasBuffFx => _definition != null && _definition.buffFrames != null && _definition.buffFrames.Length > 0;

        /// <summary>Choang <paramref name="seconds"/> giay: khong di, khong danh, animation dung hinh. Hoa Da (mien sat thuong) thi mien luon choang.</summary>
        public void ApplyStun(float seconds)
        {
            if (_dying || seconds <= 0f || _health.Invulnerable) return;
            if (_dragonLink != null && _dragonLink.Unshakable(this)) return;   // Long Giap mien choang
            _stunnedUntil = Mathf.Max(_stunnedUntil, Time.time + seconds);
        }

        void InitialiseSupport() => ClearSupport();

        void ClearSupport()
        {
            _stunnedUntil = 0f;
            _stunVisual = false;
            if (animator != null) animator.Paused = false;
            if (_buffFx != null) Destroy(_buffFx);
            _buffFx = null;
            EndShield();
            _buffs.Clear();
            _guardUntil = 0f;
            _health.Guard = 0f;
            if (_rallyFx != null) Destroy(_rallyFx);
            _rallyFx = null;
        }

        /// <summary>Tra ve true khi dang choang — Update bo qua moi hanh dong frame nay. Chi cham animator khi trang thai DOI.</summary>
        bool UpdateStunned()
        {
            bool stunned = IsStunned;
            if (stunned != _stunVisual)
            {
                _stunVisual = stunned;
                if (animator != null) animator.Paused = stunned;
                if (!stunned) _renderer.color = Color.white;
            }
            if (stunned) _renderer.color = StunnedTint;
            return stunned;
        }

        void SpawnBuffFx(float seconds)
        {
            if (!HasBuffFx) return;
            if (_buffFx != null) Destroy(_buffFx);
            // Order Air: tren moi unit du hero di len xuong — FX nho, khong can sort theo chan.
            _buffFx = OneShotFx.SpawnLoop(transform, BuffFxOffset, _definition.buffFrames, seconds, SortOrder.Air, scale: BuffFxScale);
        }

        Vector3 BuffFxOffset => new Vector3(0f, _bodyHeight / Mathf.Max(0.01f, transform.lossyScale.y) + BuffFxGap, 0f);

        // ---------------------------------------------------------------- buff tu Banneret

        /// <summary>
        /// Buff +sat thuong / +toc danh (0,25 = +25%) trong <paramref name="seconds"/> giay. Cung <paramref name="id"/> thi
        /// lam moi (lay so lon hon), khac id thi nhan chong len nhau.
        /// </summary>
        public void ApplyBuff(string id, float damage, float rate, float seconds)
        {
            if (_dying || seconds <= 0f) return;
            float until = Time.time + seconds;
            for (int i = 0; i < _buffs.Count; i++)
            {
                if (_buffs[i].id != id) continue;
                var buff = _buffs[i];
                buff.damage = Mathf.Max(buff.damage, damage);
                buff.rate = Mathf.Max(buff.rate, rate);
                buff.until = Mathf.Max(buff.until, until);
                _buffs[i] = buff;
                return;
            }
            _buffs.Add(new TimedBuff { id = id, damage = damage, rate = rate, until = until });
        }

        void BuffMultipliers(ref float damage, ref float rate)
        {
            for (int i = _buffs.Count - 1; i >= 0; i--)
            {
                if (Time.time >= _buffs[i].until)
                {
                    _buffs.RemoveAt(i);
                    continue;
                }
                damage *= 1f + _buffs[i].damage;
                rate *= 1f + _buffs[i].rate;
            }
        }

        /// <summary>Giam <paramref name="reduction"/> sat thuong nhan (sau giap) trong <paramref name="seconds"/> giay — vung Cam Co lam moi moi frame.</summary>
        public void ApplyGuard(float reduction, float seconds)
        {
            if (_dying) return;
            _guard = reduction;
            _guardUntil = Time.time + seconds;
        }

        void UpdateGuard() => _health.Guard = Time.time < _guardUntil ? _guard : 0f;

        /// <summary>Dau co vang tren dau trong luc co buff Banneret; buff moi thi dat lai tu dau.</summary>
        public void ShowRally(Sprite[] frames, float seconds)
        {
            if (_dying) return;
            if (_rallyFx != null) Destroy(_rallyFx);
            // Art rally-fx ve san co dau hero (13 px) — scale local 1.
            _rallyFx = OneShotFx.SpawnLoop(transform, BuffFxOffset, frames, seconds, SortOrder.Air);
        }

        // ---------------------------------------------------------------- hero ho tro (hoi mau, Banneret)

        /// <summary>
        /// Giong luat hero tam xa, nhung muc tieu la DONG MINH <paramref name="ally"/> (hoi mau: thap mau nhat bi thuong;
        /// Banneret: dang danh gan nhat) ma cho dung toi duoc trong day xich. null thi ve o dung cho.
        /// </summary>
        void UpdateSupport(Hero ally)
        {
            if (ally == null)
            {
                ReturnHome();
                return;
            }

            if (!InHealRange(ally))
            {
                if (CanReach(ally.transform.position)) MoveToward(ally.transform.position);
                else ReturnHome();
                return;
            }

            Face(ally.transform.position.x);
            if (_definition.HasSkill && SkillReady) BeginCast(skill: true);
            else if (_cooldown <= 0f) BeginCast(skill: false);
            else PlayIdle();
        }

        /// <summary>Goi o frame cham cua clip attack: chon lai nguoi thap mau nhat trong tam luc do (nguoi cu co the da day / da chet).</summary>
        void HealAlly()
        {
            var ally = WoundedAlly(inRangeOnly: true);
            if (ally == null) return;
            AuraMultipliers(out float multiplier, out _);
            ally.Health.Heal(BaseDamage * multiplier * RescueMultiplier(ally));
            var at = ally.transform.position + new Vector3(0f, ally.BodyHeight * 0.5f, 0f);
            OneShotFx.Spawn(at, _definition.healFrames, false, SortOrder.Air);
            OnHitsLanded(1);    // healer khong danh trung quai: moi lan hoi tinh nhu mot don trung de nap mana
        }

        /// <summary>
        /// Den Canh: vong tren dat quanh dong minh thap mau nhat trong tam (khong ai bi thuong thi quanh chinh minh).
        /// Do trong vong bang `GroundDistance` — cung ti le bep 0,5 voi art elip, nguoi choi thay ai dung trong vong.
        /// </summary>
        void CastLantern()
        {
            var centre = WoundedAlly(inRangeOnly: true) ?? this;
            var at = centre.transform.position;
            float radius = _definition.skillRadius;
            OneShotFx.Spawn(at, _definition.skillFrames, false, SortOrder.GroundDecal, radius * 2f / RingArtDiameter);

            AuraMultipliers(out float multiplier, out _);
            float heal = BaseDamage * _definition.skillPower * multiplier;
            foreach (var ally in Alive)
            {
                if (ally == null || ally.IsDying || ally._definition == null) continue;
                if (Battlefield.GroundDistance(ally.transform.position, at) > radius) continue;
                ally.Health.Heal(heal * RescueMultiplier(ally));
                var fxAt = ally.transform.position + new Vector3(0f, ally.BodyHeight * 0.5f, 0f);
                OneShotFx.Spawn(fxAt, _definition.healFrames, false, SortOrder.Air);
                ally.ApplyShield(_definition.shieldAmount, _definition.skillDuration,
                    _definition.shieldFrames, _definition.shieldBreakFrames);
            }
        }

        // ---------------------------------------------------------------- khien ao

        /// <summary>Scale LOCAL cua vo khien: cao ~ `ShieldPerBody` lan than hero (cha da nhan UnitScale).</summary>
        float ShieldLocalScale => _bodyHeight * ShieldPerBody / (ShieldArtHeight * Mathf.Max(0.01f, transform.lossyScale.y));

        /// <summary>Khien ao `amount` sat thuong trong `seconds` giay; dat lai thi lam moi (khong cong don).</summary>
        public void ApplyShield(float amount, float seconds, Sprite[] loop, Sprite[] broken)
        {
            if (_dying || amount <= 0f || seconds <= 0f) return;
            _health.AddShield(amount);
            _shieldUntil = Time.time + seconds;
            _shieldBreakFrames = broken;
            if (_shieldFx != null) Destroy(_shieldFx);
            // Pivot art = tam vong day khien -> dat dung chan hero.
            _shieldFx = OneShotFx.SpawnLoop(transform, Vector3.zero, loop, seconds, SortOrder.ForUnit(this) + 1,
                scale: ShieldLocalScale);
            _shieldRenderer = _shieldFx != null ? _shieldFx.GetComponent<SpriteRenderer>() : null;
        }

        void UpdateShield()
        {
            if (_shieldUntil > 0f && Time.time >= _shieldUntil) EndShield();
        }

        void EndShield()
        {
            _shieldUntil = 0f;
            if (_health != null) _health.ClearShield();
            if (_shieldFx != null) Destroy(_shieldFx);
            _shieldFx = null;
            _shieldRenderer = null;
        }

        /// <summary>Bi danh het khien: bo vo lap, chay FX vo mot lan tai cho (khong bam theo hero — chi ~0,4 s).</summary>
        void OnShieldBroken()
        {
            float worldScale = ShieldLocalScale * transform.lossyScale.y;
            EndShield();
            OneShotFx.Spawn(transform.position, _shieldBreakFrames, false, SortOrder.ForUnit(this) + 1, worldScale);
        }

        Hero WoundedAlly(bool inRangeOnly)
        {
            Hero best = null;
            float bestFraction = 1f;
            foreach (var ally in Alive)
            {
                if (ally == null || ally.IsDying || ally._definition == null) continue;
                float fraction = ally.Health.Fraction;
                if (fraction >= bestFraction) continue;
                if (inRangeOnly ? !InHealRange(ally) : !CanReach(ally.transform.position)) continue;
                bestFraction = fraction;
                best = ally;
            }
            return best;
        }

        bool InHealRange(Hero ally) =>
            ally == this || (ally.transform.position - transform.position).sqrMagnitude <= _definition.range * _definition.range;
    }
}
