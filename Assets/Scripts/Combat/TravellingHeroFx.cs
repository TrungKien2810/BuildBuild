using UnityEngine;
using BuildBuild.Core;

namespace BuildBuild.Combat
{
    public class TravellingHeroFx : MonoBehaviour
    {
        Vector3 _from, _to;
        float _started, _duration;
        Sprite[] _frames;
        SpriteRenderer _renderer;
        public static void Spawn(Vector3 from, Vector3 to, Sprite[] frames, float duration)
        {
            if (frames == null || frames.Length == 0) return;
            var go = new GameObject("TravellingHeroFx");
            var fx = go.AddComponent<TravellingHeroFx>();
            fx._renderer = go.AddComponent<SpriteRenderer>();
            fx._renderer.sortingOrder = SortOrder.Air;
            fx._frames = frames;
            fx._from = from; fx._to = to; fx._duration = duration; fx._started = Time.time;
            go.transform.position = from;
            go.transform.localScale = Vector3.one * Battlefield.UnitScale;
            fx._renderer.sprite = frames[0];
        }
        void Update()
        {
            float t = Mathf.Clamp01((Time.time - _started) / _duration);
            transform.position = Vector3.Lerp(_from, _to, t);
            _renderer.sprite = _frames[Mathf.Min(_frames.Length - 1, Mathf.FloorToInt(t * _frames.Length))];
            _renderer.flipX = _to.x > _from.x;
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
