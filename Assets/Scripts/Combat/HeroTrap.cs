using BuildBuild.Core;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Bay cua Kobold Gai Bay: nam cho, ai giẫm thi dinh.
    ///
    /// Vung kich va vung no phu HET be sau cua san (chi do X): san sau 11 don vi mat dat va quai di hang
    /// nao cung phai giẫm duoc — ban kinh 0,3 cu chi cham ~1/6 so quai di qua (do 2026-10-10).
    /// </summary>
    public class HeroTrap : MonoBehaviour
    {
        /// <summary>Nua be ngang vung kich bay (world X) — be sau phu het san.</summary>
        const float TriggerHalfWidth = 0.45f;

        /// <summary>Nua be ngang vung no khi go bay (world X) — be sau phu het san.</summary>
        const float BlastHalfWidth = 1.1f;

        Hero _owner;
        Faction _side;
        public static HeroTrap Spawn(Hero owner, Vector3 position)
        {
            var go = new GameObject("KoboldTrap");
            go.transform.position = position;
            var trap = go.AddComponent<HeroTrap>();
            trap._owner = owner; trap._side = owner.Side;
            // Bay kich ca be sau san nen hinh ve o ca ba hang hero (khong phai mot tam o hang cua hero).
            LaneFx.SpawnRows(go.transform, position.x, owner.Definition.passiveFrames, 3600f,
                SortOrder.GroundDecal + 1, scale: Battlefield.UnitScale);
            return trap;
        }
        void Update()
        {
            if (_owner == null || _owner.IsDying || !WaveManager.Fighting) { Destroy(gameObject); return; }
            foreach (var enemy in Unit.Hostiles(_side).ToArray())
            {
                if (enemy == null || enemy.IsDying) continue;
                if (!Battlefield.InLaneColumn(enemy, transform.position.x, TriggerHalfWidth)) continue;
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
                if (enemy != null && !enemy.IsDying
                    && Battlefield.InLaneColumn(enemy, transform.position.x, BlastHalfWidth))
                {
                    enemy.Health.TakeDamage(_owner.AttackDamage * 3f, _owner.Health);
                    enemy.ApplyRoot(enemy.IsElite ? 0.5f : 1f, _owner.Definition.skillIntroFrames);
                }
            OneShotFx.Spawn(transform.position, _owner.Definition.skillFrames, false, SortOrder.Air, Battlefield.UnitScale);
            Destroy(gameObject);
        }
    }
}
