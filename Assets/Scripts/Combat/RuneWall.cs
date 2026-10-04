using System.Collections.Generic;
using BuildBuild.Core;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Tuong Rune cua Nguoi Lun Khien Rune (GAME_DESIGN.md muc 3): mot duong thang DOC (theo truc y cua san) dat truoc mat
    /// hero, dai `halfLength` moi phia, ton tai `duration` giay, khong co mau. Quai phe kia cham tuong thi dung lai nhu gap
    /// hero (`Stops` / `Clamp`, goi tu `Enemy`); dan quai bay qua tuong thi vo (`Intercepts`, goi tu `EnemyShot`). Quai bay
    /// cao khong bi chan. Hinh: cac cot rune xep doc theo tuong (moc -&gt; lap -&gt; tan).
    /// </summary>
    public class RuneWall : MonoBehaviour
    {
        static readonly List<RuneWall> Active = new List<RuneWall>();

        /// <summary>Khoang cach giua hai cot rune (don vi the gioi) — cot rong ~0,4 nen xep sat thanh mot buc tuong.</summary>
        const float PillarSpacing = 0.45f;

        Faction _side;
        float _direction;   // +1: phia doi thu la x lon hon tuong
        float _halfLength;
        float _until;
        Sprite[] _outro;
        Sprite[] _breakFrames;
        readonly List<Transform> _pillars = new List<Transform>();

        /// <param name="direction">+1 = doi thu den tu ben phai tuong (hero phe ta nhin sang phai).</param>
        /// <param name="breakFrames">Tia dan vo tren tuong.</param>
        public static void Spawn(Vector3 position, float direction, float halfLength, float duration, Faction side,
            Sprite[] intro, Sprite[] loop, Sprite[] outro, Sprite[] breakFrames)
        {
            if (duration <= 0f) return;
            var go = new GameObject("RuneWall");
            go.transform.position = new Vector3(position.x, position.y, 0f);
            var wall = go.AddComponent<RuneWall>();
            wall._side = side;
            wall._direction = direction >= 0f ? 1f : -1f;
            wall._halfLength = halfLength;
            wall._until = Time.time + duration;
            wall._outro = outro;
            wall._breakFrames = breakFrames;

            int count = Mathf.Max(1, Mathf.RoundToInt(2f * halfLength / PillarSpacing) + 1);
            for (int i = 0; i < count; i++)
            {
                float y = count == 1 ? position.y : position.y - halfLength + i * (2f * halfLength / (count - 1));
                var at = new Vector3(position.x, y, 0f);
                if (!Battlefield.OnField(at)) continue;
                var pillar = new GameObject("RunePillar").transform;
                pillar.SetParent(go.transform, false);
                pillar.position = at;
                OneShotFx.SpawnLoop(pillar, Vector3.zero, loop, duration, SortOrder.ForUnit(pillar), intro, Battlefield.UnitScale);
                wall._pillars.Add(pillar);
            }
        }

        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);

        void Update()
        {
            if (Time.time < _until) return;
            foreach (var pillar in _pillars)
                if (pillar != null)
                    OneShotFx.Spawn(pillar.position, _outro, false, SortOrder.ForUnit(pillar), Battlefield.UnitScale);
            Destroy(gameObject);
        }

        bool Covers(Unit unit) =>
            unit != null && unit.Side != _side && !unit.FliesHigh &&
            Mathf.Abs(unit.transform.position.y - transform.position.y) <= _halfLength;

        /// <summary><paramref name="unit"/> dang ap sat mot tuong phe kia va buoc theo <paramref name="moveX"/> se dam vao no.</summary>
        public static bool Stops(Unit unit, float moveX)
        {
            foreach (var wall in Active)
            {
                if (!wall.Covers(unit) || moveX * wall._direction >= 0f) continue;
                float gap = wall._direction * (unit.transform.position.x - wall.transform.position.x);
                if (gap > -unit.BodyRadius && gap <= unit.BodyRadius + 0.05f) return true;
            }
            return false;
        }

        /// <summary>Day <paramref name="position"/> ra phia doi thu neu than unit dang lan vao tuong (unit da qua han tuong thi thoi).</summary>
        public static Vector3 Clamp(Unit unit, Vector3 position)
        {
            foreach (var wall in Active)
            {
                if (!wall.Covers(unit)) continue;
                float x = wall.transform.position.x;
                float gap = wall._direction * (position.x - x);
                if (gap > -unit.BodyRadius && gap < unit.BodyRadius) position.x = x + wall._direction * unit.BodyRadius;
            }
            return position;
        }

        /// <summary>Dan cua <paramref name="shooter"/> bay <paramref name="from"/> -> <paramref name="to"/> cat qua tuong phe kia: vo tai tuong.</summary>
        public static bool Intercepts(Faction shooter, Vector3 from, Vector3 to)
        {
            foreach (var wall in Active)
            {
                if (wall._side == shooter) continue;
                float x = wall.transform.position.x;
                float before = wall._direction * (from.x - x);
                float after = wall._direction * (to.x - x);
                if (before <= 0f || after > 0f) continue;
                var at = Vector3.Lerp(from, to, before / (before - after));
                if (Mathf.Abs(at.y - wall.transform.position.y) > wall._halfLength) continue;
                OneShotFx.Spawn(at, wall._breakFrames, false, SortOrder.Air, Battlefield.UnitScale);
                return true;
            }
            return false;
        }
    }
}
