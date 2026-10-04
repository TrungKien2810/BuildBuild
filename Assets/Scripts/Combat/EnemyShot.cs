using BuildBuild.Core;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Vien da cua quai ban xa (Goblin Na): bay bam theo muc tieu roi vo, `landed` (do `EnemyExtras` gan) tru sat thuong
    /// luc cham. Muc tieu mat giua duong thi vo tai cho cu.
    ///
    /// `arcHeight` &gt; 0 (binh doc): NEM CONG — dich chot tai cho muc tieu dung luc nem (khong bam theo), bay theo parabol,
    /// cham dat thi goi `landed` (de vung doc).
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
        float _arcHeight;
        Vector3 _start;
        Vector3 _ground;    // vi tri tren duong thang start -> dich (chua cong do cao cung)
        System.Action<Vector3> _landed;
        Faction _shooter;

        /// <param name="aimHeight">Nham cao hon chan muc tieu bao nhieu (giua than hero). null = ban tuong: giu nguyen y luc ban.</param>
        public static void Launch(Vector3 from, Transform target, float? aimHeight, float speed,
            Sprite[] frames, Sprite[] impactFrames, float scale, float arcHeight = 0f, System.Action<Vector3> landed = null,
            Faction shooter = Faction.Npc)
        {
            if (target == null || frames == null || frames.Length == 0) return;
            Sfx.Play("enemy-shot");
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
            shot._arcHeight = arcHeight;
            shot._start = from;
            shot._ground = from;
            shot._landed = landed;
            shot._shooter = shooter;
            if (arcHeight > 0f)
            {
                // Nem cong roi xuong CHAN muc tieu (vung doc nam tren dat), khong vao giua than; tuong thi giu hang y.
                var feet = shot._destination;
                if (aimHeight.HasValue) feet.y -= aimHeight.Value;
                shot._destination = feet;
                shot._target = null;
            }
            shot.SetFrames(frames);
        }

        void Update()
        {
            Animate();
            if (_impacting) return;

            if (_target != null) _destination = Aim();

            var toDestination = _destination - _ground;
            toDestination.z = 0f;
            float step = _speed * Time.deltaTime;
            // Tuong Rune phe kia: dan vo tai tuong, khong tru ai.
            var next = toDestination.magnitude <= step ? _destination : _ground + toDestination.normalized * step;
            if (RuneWall.Intercepts(_shooter, _ground, next))
            {
                Destroy(gameObject);
                return;
            }
            if (toDestination.magnitude <= step)
            {
                transform.position = _ground = _destination;
                Impact();
                _landed?.Invoke(_destination);
                return;
            }
            _ground += toDestination.normalized * step;
            transform.position = _ground + new Vector3(0f, ArcOffset(), 0f);
            _renderer.flipX = toDestination.x > 0f;     // art quay trai: bay sang phai thi lat
        }

        /// <summary>Do cao parabol theo quang duong da bay: 0 o hai dau, `_arcHeight` o giua.</summary>
        float ArcOffset()
        {
            if (_arcHeight <= 0f) return 0f;
            float total = Vector3.Distance(_start, _destination);
            if (total < 0.01f) return 0f;
            float u = Mathf.Clamp01(Vector3.Distance(_start, _ground) / total);
            return 4f * _arcHeight * u * (1f - u);
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
