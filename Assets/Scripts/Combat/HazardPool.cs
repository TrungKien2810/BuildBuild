using BuildBuild.Core;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Vung doc cua Goblin Nem Binh Doc (GAME_DESIGN.md muc 5): binh vo tai cho roi -> vung ton tai `duration` giay,
    /// moi `TickInterval` giay moi hero / linh phe ta dung trong vung (do tren mat dat toi MEP than) mat mau.
    /// Muc dich la EP DOI hero giua wave — dung yen trong vung thi mat mau deu.
    /// </summary>
    public class PoisonPool : MonoBehaviour
    {
        const float TickInterval = 0.5f;

        float _radius;
        float _damagePerTick;
        float _until;
        float _nextTick;
        float _scorch;

        /// <param name="scorch">&gt; 0: moi lan tick, hero trong vung bi Thieu Hon (khong hoi mau/mana) chung nay giay — dat chay Dreadwyrm.</param>
        public static void Spawn(Vector3 position, Sprite[] frames, float duration, float radius, float damagePerSecond,
            float scorch = 0f, float scale = Battlefield.UnitScale)
        {
            if (duration <= 0f || radius <= 0f) return;
            var go = new GameObject("PoisonPool");
            go.transform.position = new Vector3(position.x, position.y, 0f);
            var pool = go.AddComponent<PoisonPool>();
            pool._radius = radius;
            pool._damagePerTick = damagePerSecond * TickInterval;
            pool._until = Time.time + duration;
            pool._nextTick = Time.time + TickInterval;
            pool._scorch = scorch;
            // Art ve dung co man hinh o luoi unit (o 128) -> scale UnitScale nhu vet no.
            OneShotFx.SpawnLoop(go.transform, Vector3.zero, frames, duration, SortOrder.GroundDecal + 1,
                scale: scale);
        }

        void Update()
        {
            if (Time.time >= _until)
            {
                Destroy(gameObject);
                return;
            }
            if (Time.time < _nextTick) return;
            _nextTick += TickInterval;

            foreach (var hero in Hero.Alive.ToArray())
            {
                if (hero == null || hero.IsDying || hero.Side == Faction.Npc) continue;   // vung cua quai: khong dot phe NPC
                if (Battlefield.GroundDistance(hero.transform.position, transform.position) - hero.BodyRadius > _radius) continue;
                hero.Health.TakeDamage(_damagePerTick, kind: DamageKind.Periodic);
                if (_scorch > 0f) hero.ApplyScorch(_scorch);
            }
        }
    }
}
