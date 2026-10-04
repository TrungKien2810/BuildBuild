using BuildBuild.Core;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Doc cong tang cua Phu Thuy Dam Lay (GAME_DESIGN.md muc 3, noi tai Ngam Doc): moi tang gay `dps` sat thuong/giay,
    /// toi da `maxStacks` tang; trung them thi +tang VA dem lai han. Gan thang vao unit bi doc (component tu them lan dau),
    /// tu go khi het han hoac unit chet. Bong bong doc lap o chan, dam dan theo so tang.
    /// </summary>
    public class Venom : MonoBehaviour
    {
        const float TickInterval = 0.5f;

        Unit _unit;
        int _stacks;
        float _dps;
        float _until;
        float _nextTick;
        Health _source;
        GameObject _fx;
        SpriteRenderer _fxRenderer;

        public int Stacks => _stacks;

        public static void Apply(Unit target, int stacks, int maxStacks, float dpsPerStack, float seconds, Health source,
            Sprite[] frames)
        {
            if (target == null || target.IsDying || stacks <= 0 || seconds <= 0f || dpsPerStack <= 0f) return;
            var venom = target.GetComponent<Venom>();
            if (venom == null) venom = target.gameObject.AddComponent<Venom>();
            venom.Add(target, stacks, Mathf.Max(1, maxStacks), dpsPerStack, seconds, source, frames);
        }

        void Add(Unit unit, int stacks, int maxStacks, float dps, float seconds, Health source, Sprite[] frames)
        {
            bool fresh = _stacks == 0;
            _unit = unit;
            _stacks = Mathf.Min(maxStacks, _stacks + stacks);
            _dps = dps;
            _source = source;
            _until = Time.time + seconds;
            if (fresh) _nextTick = Time.time + TickInterval;
            // FX lap toi het han hien tai; trung them thi dung lai cho khop han moi.
            if (_fx != null) Destroy(_fx);
            _fx = OneShotFx.SpawnLoop(unit.transform, Vector3.zero, frames, seconds, SortOrder.ForUnit(unit) + 1);
            _fxRenderer = _fx != null ? _fx.GetComponent<SpriteRenderer>() : null;
            if (_fxRenderer != null) _fxRenderer.color = new Color(1f, 1f, 1f, 0.4f + 0.12f * _stacks);
        }

        void Update()
        {
            if (_stacks == 0) return;
            if (_unit == null || _unit.IsDying || Time.time >= _until)
            {
                Clear();
                return;
            }
            if (_fxRenderer != null) _fxRenderer.sortingOrder = SortOrder.ForUnit(_unit) + 1;
            if (Time.time < _nextTick) return;
            _nextTick += TickInterval;
            _unit.Health.TakeDamage(_stacks * _dps * TickInterval, _source != null ? _source : null, kind: DamageKind.Periodic);
        }

        void Clear()
        {
            _stacks = 0;
            if (_fx != null) Destroy(_fx);
            _fx = null;
            _fxRenderer = null;
        }
    }
}
