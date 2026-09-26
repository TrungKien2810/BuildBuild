using BuildBuild.Core;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Vien da cua quai ban xa (Goblin Na) — CHI de nhin: sat thuong do `Enemy` tru o frame roi tay (luat danh cua A giu
    /// nguyen), vien da bay nhanh toi muc tieu roi vo. Bay bam theo muc tieu; muc tieu mat giua duong thi vo tai cho cu.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class EnemyShot : MonoBehaviour
    {
        const float FramesPerSecond = 9f;

        SpriteRenderer _renderer;
        Transform _target;
        float _aimHeight;
        float? _fixedY;     // ban tuong: tuong la duong thang dung, bay ngang toi dung hang y cua quai
        Vector3 _destination;
        float _speed;
        Sprite[] _frames;
        Sprite[] _impactFrames;
        bool _impacting;
        float _timer;
        int _frame;

        /// <param name="aimHeight">Nham cao hon chan muc tieu bao nhieu (giua than hero). null = ban tuong: giu nguyen y luc ban.</param>
        public static void Launch(Vector3 from, Transform target, float? aimHeight, float speed,
            Sprite[] frames, Sprite[] impactFrames, float scale)
        {
            if (target == null || frames == null || frames.Length == 0) return;
            var go = new GameObject("EnemyShot");
            go.transform.position = from;
            go.transform.localScale = Vector3.one * (scale * Battlefield.UnitScale);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = SortOrder.Air;
            var shot = go.AddComponent<EnemyShot>();
            shot._renderer = renderer;
            shot._target = target;
            shot._aimHeight = aimHeight ?? 0f;
            shot._fixedY = aimHeight.HasValue ? (float?)null : from.y;
            shot._destination = shot.Aim();
            shot._speed = Mathf.Max(0.1f, speed);
            shot._impactFrames = impactFrames;
            shot.SetFrames(frames);
        }

        void Update()
        {
            Animate();
            if (_impacting) return;

            if (_target != null) _destination = Aim();

            var toDestination = _destination - transform.position;
            toDestination.z = 0f;
            float step = _speed * Time.deltaTime;
            if (toDestination.magnitude <= step)
            {
                transform.position = _destination;
                Impact();
                return;
            }
            transform.position += toDestination.normalized * step;
            _renderer.flipX = toDestination.x > 0f;     // art quay trai: bay sang phai thi lat
        }

        Vector3 Aim()
        {
            var position = _target.position;
            if (_fixedY.HasValue) position.y = _fixedY.Value;
            else position.y += _aimHeight;
            position.z = 0f;
            return position;
        }

        void Impact()
        {
            _impacting = true;
            transform.localScale = Vector3.one * Battlefield.UnitScale;
            if (_impactFrames == null || _impactFrames.Length == 0) Destroy(gameObject);
            else SetFrames(_impactFrames);
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
            float frameDuration = 1f / FramesPerSecond;
            while (_timer >= frameDuration)
            {
                _timer -= frameDuration;
                _frame++;
                if (_frame >= _frames.Length)
                {
                    if (_impacting) { Destroy(gameObject); return; }
                    _frame = 0;
                }
                _renderer.sprite = _frames[_frame];
            }
        }
    }
}
