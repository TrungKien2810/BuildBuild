using BuildBuild.Core;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Phan cua `Hero` do nhanh Tru them (plan 2 thanh vien, muc B2/B3), tach file de khong dung vao luat di chuyen
    /// ma linh trieu hoi dung chung:
    ///   - Choang (`ApplyStun`) — boss Chan Dia choang hero / linh 2s;
    ///   - Hero HOI MAU (`HeroDefinition.healsAllies`, Vespera): nham dong minh thap mau nhat, khong ban quai;
    ///   - FX buff Ban Nhanh Lien Hoan tren dau Hawkfletch thay cho to cam.
    /// </summary>
    public partial class Hero
    {
        /// <summary>Cung mau vang voi quai bi choang — nguoi choi doc ra cung mot trang thai.</summary>
        static readonly Color StunnedTint = new Color(1f, 0.85f, 0.35f);

        /// <summary>Scale LOCAL cua FX buff (art 64 px, than hero da nhan UnitScale 2) -> rong ~ bang dau hero.</summary>
        const float BuffFxScale = 0.5f;

        /// <summary>FX buff dat tren thanh mana (StatBar dat mana o dinh dau + 0,09 local).</summary>
        const float BuffFxGap = 0.28f;

        float _stunnedUntil;
        bool _stunVisual;
        GameObject _buffFx;

        public bool IsStunned => Time.time < _stunnedUntil;

        bool HasBuffFx => _definition != null && _definition.buffFrames != null && _definition.buffFrames.Length > 0;

        /// <summary>Choang <paramref name="seconds"/> giay: khong di, khong danh, animation dung hinh. Hoa Da (mien sat thuong) thi mien luon choang.</summary>
        public void ApplyStun(float seconds)
        {
            if (_dying || seconds <= 0f || _health.Invulnerable) return;
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
            var offset = new Vector3(0f, _bodyHeight / Mathf.Max(0.01f, transform.lossyScale.y) + BuffFxGap, 0f);
            // Order Air: tren moi unit du hero di len xuong — FX nho, khong can sort theo chan.
            _buffFx = OneShotFx.SpawnLoop(transform, offset, _definition.buffFrames, seconds, SortOrder.Air, scale: BuffFxScale);
        }

        // ---------------------------------------------------------------- hero hoi mau

        /// <summary>
        /// Giong luat hero tam xa, nhung muc tieu la dong minh BI THUONG thap mau nhat (theo ti le) ma cho dung hoi toi duoc
        /// trong day xich. Khong ai bi thuong thi ve o dung cho.
        /// </summary>
        void UpdateHealer()
        {
            var ally = WoundedAlly(inRangeOnly: false);
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
            if (_cooldown <= 0f) BeginCast(skill: false);
            else PlayIdle();
        }

        /// <summary>Goi o frame cham cua clip attack: chon lai nguoi thap mau nhat trong tam luc do (nguoi cu co the da day / da chet).</summary>
        void HealAlly()
        {
            var ally = WoundedAlly(inRangeOnly: true);
            if (ally == null) return;
            AuraMultipliers(out float multiplier, out _);
            ally.Health.Heal(_definition.damage * multiplier);
            var at = ally.transform.position + new Vector3(0f, ally.BodyHeight * 0.5f, 0f);
            OneShotFx.Spawn(at, _definition.healFrames, false, SortOrder.Air);
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
