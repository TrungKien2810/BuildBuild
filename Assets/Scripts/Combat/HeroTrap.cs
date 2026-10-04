using BuildBuild.Core;
using UnityEngine;

namespace BuildBuild.Combat
{
    public class HeroTrap : MonoBehaviour
    {
        Hero _owner;
        Faction _side;
        public static HeroTrap Spawn(Hero owner, Vector3 position)
        {
            var go = new GameObject("KoboldTrap");
            go.transform.position = position;
            var trap = go.AddComponent<HeroTrap>();
            trap._owner = owner; trap._side = owner.Side;
            OneShotFx.SpawnLoop(go.transform, Vector3.zero, owner.Definition.passiveFrames, 3600f,
                SortOrder.GroundDecal + 1, scale: Battlefield.UnitScale);
            return trap;
        }
        void Update()
        {
            if (_owner == null || _owner.IsDying || !WaveManager.Fighting) { Destroy(gameObject); return; }
            foreach (var enemy in Unit.Hostiles(_side).ToArray())
            {
                if (enemy == null || enemy.IsDying || Battlefield.GroundDistance(enemy.transform.position, transform.position) > 0.3f + enemy.BodyRadius) continue;
                enemy.Health.TakeDamage(_owner.AttackDamage, _owner.Health);
                enemy.ApplyRoot(enemy.IsElite ? 0.75f : 1.5f, _owner.Definition.skillIntroFrames);
                OneShotFx.Spawn(transform.position, _owner.Definition.skillIntroFrames, false, SortOrder.GroundDecal + 1, Battlefield.UnitScale);
                Destroy(gameObject);
                return;
            }
        }
        public void Detonate()
        {
            foreach (var enemy in Unit.Hostiles(_side).ToArray())
                if (enemy != null && !enemy.IsDying && Battlefield.GroundDistance(enemy.transform.position, transform.position) <= 0.8f + enemy.BodyRadius)
                {
                    enemy.Health.TakeDamage(_owner.AttackDamage * 3f, _owner.Health);
                    enemy.ApplyRoot(enemy.IsElite ? 0.5f : 1f, _owner.Definition.skillIntroFrames);
                }
            OneShotFx.Spawn(transform.position, _owner.Definition.skillFrames, false, SortOrder.Air, Battlefield.UnitScale);
            Destroy(gameObject);
        }
    }
}
