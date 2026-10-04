using BuildBuild.Core;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Chay cua Phap Su Lua Tap Su (GAME_DESIGN.md muc 3): `dps` sat thuong/giay trong `seconds` giay, trung lai thi LAM MOI
    /// han (khong cong don). Noi tai Ben Lua (`spreadRadius` &gt; 0): quai dang Chay ma chet thi lua nhay sang quai gan nhat
    /// trong ban kinh do (Chay moi, day chuyen duoc). Gan thang vao unit bi dot (component tu them lan dau).
    /// </summary>
    public class Burn : MonoBehaviour
    {
        const float TickInterval = 0.5f;

        Unit _unit;
        bool _burning;
        float _dps;
        float _seconds;
        float _until;
        float _nextTick;
        float _spreadRadius;
        Faction _side;
        Health _source;
        Sprite[] _frames;
        GameObject _fx;
        SpriteRenderer _fxRenderer;

        /// <param name="side">Phe cua nguoi dot — lua chi lan sang doi thu cua phe nay.</param>
        public static void Apply(Unit target, float dps, float seconds, Health source, Sprite[] frames, Faction side,
            float spreadRadius)
        {
            if (target == null || target.IsDying || seconds <= 0f || dps <= 0f) return;
            var burn = target.GetComponent<Burn>();
            if (burn == null) burn = target.gameObject.AddComponent<Burn>();
            burn.Ignite(target, dps, seconds, source, frames, side, spreadRadius);
        }

        void Ignite(Unit unit, float dps, float seconds, Health source, Sprite[] frames, Faction side, float spreadRadius)
        {
            if (!_burning) _nextTick = Time.time + TickInterval;
            _burning = true;
            _unit = unit;
            _dps = dps;
            _seconds = seconds;
            _until = Time.time + seconds;
            _source = source;
            _frames = frames;
            _side = side;
            _spreadRadius = spreadRadius;
            if (_fx != null) Destroy(_fx);
            _fx = OneShotFx.SpawnLoop(unit.transform, Vector3.zero, frames, seconds, SortOrder.ForUnit(unit) + 1);
            _fxRenderer = _fx != null ? _fx.GetComponent<SpriteRenderer>() : null;
        }

        void Update()
        {
            if (!_burning) return;
            if (_unit == null || _unit.IsDying)
            {
                if (_unit != null && _spreadRadius > 0f) Spread();
                Clear();
                return;
            }
            if (Time.time >= _until)
            {
                Clear();
                return;
            }
            if (_fxRenderer != null) _fxRenderer.sortingOrder = SortOrder.ForUnit(_unit) + 1;
            if (Time.time < _nextTick) return;
            _nextTick += TickInterval;
            _unit.Health.TakeDamage(_dps * TickInterval, _source != null ? _source : null, kind: DamageKind.Periodic);
        }

        /// <summary>Ben Lua: Chay moi cho quai gan xac nhat trong `_spreadRadius`.</summary>
        void Spread()
        {
            var at = _unit.transform.position;
            Unit best = null;
            float bestDistance = float.PositiveInfinity;
            foreach (var other in Unit.Hostiles(_side))
            {
                if (other == null || other == _unit || other.IsDying) continue;
                float distance = Battlefield.GroundDistance(other.AimPoint(at), at) - other.BodyRadius;
                if (distance > _spreadRadius || distance >= bestDistance) continue;
                bestDistance = distance;
                best = other;
            }
            if (best != null) Apply(best, _dps, _seconds, _source, _frames, _side, _spreadRadius);
        }

        void Clear()
        {
            _burning = false;
            if (_fx != null) Destroy(_fx);
            _fx = null;
            _fxRenderer = null;
        }
    }
}
