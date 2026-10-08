using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Ho den cua Phao Hac Tinh (tempart/CannonVoid/mechanics.md, so TAM): mo tai dat noi loi roi, `vortexSeconds` giay
    /// hut moi quai trong ban kinh `aoeRadius` ve tam; moi quai chet trong ho (bat ky ai ket lieu) +1 cong don, toi da
    /// `vortexMaxStacks`. Het gio no: sat thuong x (1 + `vortexStackBonus` x cong don) len moi quai trong ban kinh.
    /// Vong lap + vu no doi hinh theo cong don (0-1 / 2-3 / 4-5).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class VoidVortex : MonoBehaviour
    {
        const float FramesPerSecond = 9f;
        const float PullStep = 0.25f;   // giay moi lan keo — quai bi keo dan ve tam chu khong dich tuc thi

        CannonDefinition _cannon;
        Faction _side;
        float _damage;
        float _until;
        float _nextPull;
        int _stacks;
        SpriteRenderer _renderer;
        Sprite[] _frames;
        bool _opening;
        float _timer;
        int _frame;

        public static void Open(Vector3 ground, float damage, CannonDefinition cannon, Faction side)
        {
            var go = new GameObject("VoidVortex");
            go.transform.position = ground;
            go.transform.localScale = Vector3.one * Battlefield.UnitScale;
            var vortex = go.AddComponent<VoidVortex>();
            vortex._cannon = cannon;
            vortex._side = side;
            vortex._damage = damage;
            vortex._until = Time.time + cannon.vortexSeconds;
            vortex._renderer = go.GetComponent<SpriteRenderer>();
            vortex._renderer.sortingOrder = SortOrder.GroundDecal;
            vortex._opening = cannon.vortexOpenFrames != null && cannon.vortexOpenFrames.Length > 0;
            vortex.SetFrames(vortex._opening ? cannon.vortexOpenFrames : vortex.Loop);
            Unit.Killed += vortex.OnKilled;
        }

        void OnDestroy() => Unit.Killed -= OnKilled;

        Sprite[] Loop => _stacks >= 4 ? _cannon.vortexLoop2 : _stacks >= 2 ? _cannon.vortexLoop1 : _cannon.vortexLoop0;

        void OnKilled(Unit unit)
        {
            if (unit == null || unit.Side == _side || _stacks >= _cannon.vortexMaxStacks) return;
            if (Battlefield.GroundDistance(unit.transform.position, transform.position) > _cannon.aoeRadius + unit.BodyRadius) return;
            var before = Loop;
            _stacks++;
            OneShotFx.Spawn(unit.BodyCenter, _cannon.absorbFrames, false, SortOrder.Air, Battlefield.UnitScale);
            if (!_opening && Loop != before) SetFrames(Loop);
        }

        void Update()
        {
            Animate();
            if (Time.time >= _nextPull)
            {
                _nextPull = Time.time + PullStep;
                foreach (var enemy in Unit.Hostiles(_side))
                {
                    if (enemy == null || enemy.IsDying || enemy.IsBoss) continue;
                    var foot = enemy.transform.position;
                    float distance = Battlefield.GroundDistance(foot, transform.position);
                    if (distance > _cannon.aoeRadius + enemy.BodyRadius || distance < 0.15f) continue;
                    enemy.PullTo(Vector3.Lerp(foot, transform.position, 0.35f), PullStep);
                }
            }
            if (Time.time >= _until) Explode();
        }

        void Explode()
        {
            float damage = _damage * (1f + _cannon.vortexStackBonus * _stacks);
            foreach (var enemy in Unit.Hostiles(_side).ToArray())
            {
                if (enemy == null || enemy.IsDying) continue;
                if (Battlefield.GroundDistance(enemy.AimPoint(transform.position), transform.position) > _cannon.aoeRadius + enemy.BodyRadius) continue;
                float before = enemy.Health.Current;
                enemy.Health.TakeDamage(damage * Cannon.BigTargetFactor(_cannon, enemy), side: _side);
                WaveStats.RecordCannonDamage(_cannon, _side, before - enemy.Health.Current);
            }
            var blast = _stacks >= 4 ? _cannon.impactFrames2 : _stacks >= 2 ? _cannon.impactFrames1 : _cannon.impactFrames;
            OneShotFx.Spawn(transform.position, blast, false, SortOrder.Air, Battlefield.UnitScale * _cannon.impactScale);
            Sfx.Play(_cannon.impactSfx);
            Destroy(gameObject);
        }

        void SetFrames(Sprite[] frames)
        {
            _frames = frames;
            _frame = 0;
            _timer = 0f;
            if (_frames != null && _frames.Length > 0) _renderer.sprite = _frames[0];
        }

        void Animate()
        {
            if (_frames == null || _frames.Length == 0) return;
            _timer += Time.deltaTime;
            while (_timer >= 1f / FramesPerSecond)
            {
                _timer -= 1f / FramesPerSecond;
                if (++_frame >= _frames.Length)
                {
                    if (_opening)
                    {
                        _opening = false;
                        SetFrames(Loop);
                        return;
                    }
                    _frame = 0;
                }
                _renderer.sprite = _frames[_frame];
            }
        }
    }
}
