using System.Collections.Generic;
using BuildBuild.Core;
using UnityEngine;

namespace BuildBuild.Combat
{
    public class SeedMark : MonoBehaviour
    {
        Hero _owner;
        Unit _unit;
        int _stacks;
        float _until;
        readonly List<GameObject> _fx = new List<GameObject>();
        public static void Apply(Unit unit, Hero owner)
        {
            if (unit.IsDying || unit.Health.IsDead) return;
            SeedMark mark = null;
            foreach (var existing in unit.GetComponents<SeedMark>()) if (existing._owner == owner) mark = existing;
            if (mark == null) { mark = unit.gameObject.AddComponent<SeedMark>(); mark._owner = owner; mark._unit = unit; }
            mark._until = Time.time + 4f;
            mark._stacks++;
            mark.RefreshFx();
            if (mark._stacks < 3) return;
            unit.Health.TakeDamage(owner.AttackDamage * 2f, owner.Health);
            OneShotFx.Spawn(unit.BodyCenter, owner.Definition.impactFrames, false, SortOrder.Air, Battlefield.UnitScale);
            mark._stacks = 0;
            mark.RefreshFx();
        }
        void RefreshFx()
        {
            foreach (var fx in _fx) if (fx != null) Destroy(fx);
            _fx.Clear();
            for (int i = 0; i < _stacks; i++)
                _fx.Add(OneShotFx.SpawnLoop(transform,
                    new Vector3((i - 1) * 0.09f, _unit.BodyHeight * 0.5f / Battlefield.UnitScale),
                    _owner.Definition.passiveFrames, 4f, SortOrder.Air));
        }
        void Update() { if (_owner == null || _unit.IsDying || Time.time >= _until) Destroy(this); }
        void OnDestroy() { foreach (var fx in _fx) if (fx != null) Destroy(fx); }
    }
}
