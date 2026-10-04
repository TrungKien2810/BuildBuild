using System;
using BuildBuild.Core;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// FX roi tu tren xuong mot diem tren san (cau lua Mua Tan Lua): cho `delay` giay, bay thang `from` -> `to` trong
    /// `duration` giay, cham dat thi goi `landed`. Chi la hinh — nguoi goi tu tinh sat thuong trong `landed`.
    /// </summary>
    public class FallingFx : MonoBehaviour
    {
        Vector3 _from;
        Vector3 _to;
        float _time;
        float _duration;
        Sprite[] _frames;
        float _scale;
        bool _shown;
        Action _landed;

        public static void Spawn(Vector3 from, Vector3 to, float delay, float duration, Sprite[] frames, float scale,
            Action landed)
        {
            var go = new GameObject("FallingFx");
            go.transform.position = from;
            var fx = go.AddComponent<FallingFx>();
            fx._from = from;
            fx._to = to;
            fx._time = -delay;
            fx._duration = Mathf.Max(0.05f, duration);
            fx._frames = frames;
            fx._scale = scale;
            fx._landed = landed;
        }

        void Update()
        {
            _time += Time.deltaTime;
            if (_time < 0f) return;
            if (!_shown)
            {
                _shown = true;
                OneShotFx.SpawnLoop(transform, Vector3.zero, _frames, _duration + 1f, SortOrder.Air, null, _scale);
            }
            float u = Mathf.Clamp01(_time / _duration);
            transform.position = Vector3.Lerp(_from, _to, u);
            if (u < 1f) return;
            _landed?.Invoke();
            Destroy(gameObject);
        }
    }
}
