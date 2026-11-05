using BuildBuild.Core;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Vung nguy hiem tren mat dat: vung doc Goblin Nem Binh Doc, dat chay sau Hoi Tho cua Rong, va dat chay cua phao
    /// Dau Rong Da (Long Tức). Vung ton tai `duration` giay, moi `TickInterval` giay GAN trang thai (`StatusEffects`:
    /// Doc / Chay) cho moi unit KHAC PHE dat vung dung trong vung (do tren mat dat toi MEP than) — sat thuong do trang
    /// thai tick, nen buoc ra van con dinh `statusSeconds` giay, va giai debuff go duoc. Muc dich la EP DOI hero giua
    /// wave (vung cua quai) hoac don quai dung yen (vung cua phao, `side: Faction.Player`).
    /// </summary>
    public class HazardPool : MonoBehaviour
    {
        const float TickInterval = 0.5f;

        float _radius;
        StatusId _status;
        float _damagePerSecond;
        float _statusSeconds;
        float _until;
        float _nextTick;
        float _noHeal;
        Faction _side;

        /// <param name="status">Trang thai gan cho unit trong vung (Doc / Chay).</param>
        /// <param name="statusSeconds">Trang thai con bam bao lau sau lan tick cuoi (buoc ra khoi vung).</param>
        /// <param name="noHeal">&gt; 0: moi lan tick them Cam hoi mau chung nay giay — dat chay Dreadwyrm.</param>
        /// <param name="side">Phe DAT vung: chi dot unit KHAC phe (mac dinh Npc = vung cua quai, dot phe ta).
        /// Dau Rong Da dat `Player` de dot quai.</param>
        public static void Spawn(Vector3 position, Sprite[] frames, float duration, float radius, StatusId status,
            float damagePerSecond, float statusSeconds, float noHeal = 0f, float scale = Battlefield.UnitScale,
            Faction side = Faction.Npc)
        {
            if (duration <= 0f || radius <= 0f) return;
            var go = new GameObject("HazardPool");
            go.transform.position = new Vector3(position.x, position.y, 0f);
            var pool = go.AddComponent<HazardPool>();
            pool._radius = radius;
            pool._status = status;
            pool._damagePerSecond = damagePerSecond;
            pool._statusSeconds = statusSeconds;
            pool._until = Time.time + duration;
            pool._nextTick = Time.time + TickInterval;
            pool._noHeal = noHeal;
            pool._side = side;
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

            foreach (var unit in Unit.Hostiles(_side).ToArray())
            {
                if (unit == null || unit.IsDying || unit is Castle) continue;   // vung dat khong dot thanh
                if (Battlefield.GroundDistance(unit.transform.position, transform.position) - unit.BodyRadius > _radius) continue;
                unit.Status.Apply(_status, _damagePerSecond, _statusSeconds, side: _side);
                if (_noHeal > 0f && unit is Hero hero) hero.ApplyScorch(_noHeal);
            }
        }
    }
}
