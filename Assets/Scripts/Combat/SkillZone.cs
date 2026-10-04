using System;
using BuildBuild.Core;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Vung chieu dat xuong san cua hero N (Vac Suong Doc, Xoay Nuoc, nam No Nam): ton tai `duration` giay, moi frame goi
    /// `everyFrame` va moi giay goi `everySecond` cho tung doi thu cua phe dat vung co than cham vong `radius` (do tren mat
    /// dat), het gio goi `onExpire` va chay FX tan. Vung KHONG bam theo nguoi dat — hero chet thi vung van chay het.
    /// </summary>
    public class SkillZone : MonoBehaviour
    {
        float _radius;
        float _until;
        float _nextTick;
        Faction _side;
        Action<Unit> _everyFrame;
        Action<Unit> _everySecond;
        Action _onExpire;
        Sprite[] _outro;
        float _fxScale;

        /// <param name="loop">FX lap suot vung (trong = khong ve gi).</param>
        /// <param name="intro">FX chay MOT lan truoc phan lap (nam moc len).</param>
        /// <param name="outro">FX chay MOT lan luc vung het (xoay tan).</param>
        public static void Spawn(Vector3 position, float radius, float duration, Faction side, Sprite[] loop,
            float fxScale, Action<Unit> everyFrame = null, Action<Unit> everySecond = null, Action onExpire = null,
            Sprite[] intro = null, Sprite[] outro = null)
        {
            if (duration <= 0f) return;
            var go = new GameObject("SkillZone");
            go.transform.position = new Vector3(position.x, position.y, 0f);
            var zone = go.AddComponent<SkillZone>();
            zone._radius = radius;
            zone._until = Time.time + duration;
            zone._nextTick = Time.time + 1f;
            zone._side = side;
            zone._everyFrame = everyFrame;
            zone._everySecond = everySecond;
            zone._onExpire = onExpire;
            zone._outro = outro;
            zone._fxScale = fxScale;
            OneShotFx.SpawnLoop(go.transform, Vector3.zero, loop, duration, SortOrder.GroundDecal + 1, intro, fxScale);
        }

        void Update()
        {
            if (Time.time >= _until)
            {
                _onExpire?.Invoke();
                OneShotFx.Spawn(transform.position, _outro, false, SortOrder.GroundDecal + 1, _fxScale);
                Destroy(gameObject);
                return;
            }

            bool tick = Time.time >= _nextTick;
            if (tick) _nextTick += 1f;
            if (_everyFrame == null && !(tick && _everySecond != null)) return;

            var at = transform.position;
            // Chep ra mang: sat thuong moi giay co the giet quai va lam doi danh sach.
            foreach (var unit in Unit.Hostiles(_side).ToArray())
            {
                if (unit == null || unit.IsDying) continue;
                if (Battlefield.GroundDistance(unit.AimPoint(at), at) > _radius + unit.BodyRadius) continue;
                _everyFrame?.Invoke(unit);
                if (tick) _everySecond?.Invoke(unit);
            }
        }
    }
}
