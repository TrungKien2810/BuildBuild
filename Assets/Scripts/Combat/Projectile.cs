using BuildBuild.Core;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Dan cua hero: bay vao GIUA THAN quai (hitbox do tu sprite, `Enemy.BodyCenter`), trung thi NO — sat
    /// thuong moi quai co than cham vong `splashRadius` quanh chan muc tieu, do tren mat dat (vai tro sat
    /// thuong dien rong, GAME_DESIGN.md muc 3), phat impact mot lan roi tu huy.
    ///
    /// Art frost-bolt ve mui chi sang TRAI: bay sang phai thi lat flipX, roi nghieng theo goc bay.
    /// Khong xoay 180 do de doi huong — xoay thi hat pixel bi lon nguoc tren-duoi.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class Projectile : MonoBehaviour
    {
        const float FramesPerSecond = 9f;               // cung nhip 110ms/frame voi animation da duyet

        SpriteRenderer _renderer;
        Enemy _target;
        Vector3 _destination;
        Vector3 _groundPoint;   // chan muc tieu — tam vung no tren mat dat
        float _damage;
        float _splashRadius;
        float _impactScale = 1f;
        float _pushDirection;   // 0 = don khong day lui
        float _speed;
        Sprite[] _frames;
        Sprite[] _impactFrames;
        Hero _owner;
        bool _impacting;
        float _timer;
        int _frame;

        public void Launch(Enemy target, float damage, float splashRadius, float speed, Sprite[] frames,
            Sprite[] impactFrames, Hero owner)
        {
            _owner = owner;
            // Chot luc ban: hero co the chet/bi go truoc khi dan trung.
            _impactScale = owner != null ? owner.Definition.impactScale : 1f;
            _pushDirection = owner != null && owner.Definition.knockbackOnHit
                ? (target.transform.position.x >= owner.transform.position.x ? 1f : -1f) : 0f;
            _renderer = GetComponent<SpriteRenderer>();
            _target = target;
            _destination = target.BodyCenter;
            _groundPoint = target.transform.position;
            _damage = damage;
            _splashRadius = splashRadius;
            _speed = speed;
            _impactFrames = impactFrames;
            SetFrames(frames);
        }

        void Update()
        {
            Animate();
            if (_impacting || _renderer == null) return;

            if (_target != null && !_target.IsDying)
            {
                _destination = _target.BodyCenter;
                _groundPoint = _target.transform.position;
            }

            Vector3 toDestination = _destination - transform.position;
            float step = _speed * Time.deltaTime;
            if (toDestination.magnitude <= step)
            {
                transform.position = _destination;
                Impact();
                return;
            }

            Vector3 direction = toDestination.normalized;
            transform.position += direction * step;

            bool right = direction.x > 0f;
            _renderer.flipX = right;
            float angle = Mathf.Atan2(direction.y, Mathf.Abs(direction.x)) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, right ? angle : -angle);
        }

        void Impact()
        {
            _impacting = true;
            transform.rotation = Quaternion.identity;
            // `projectileScale` chi cho luc bay; frame no = co art x he so than chung x `impactScale` cua hero
            transform.localScale = Vector3.one * (Battlefield.UnitScale * _impactScale);
            _renderer.flipX = false;

            // Chep ra mang: quai chet co the phat su kien lam doi danh sach giua vong lap.
            int hits = 0;
            foreach (var enemy in Enemy.Alive.ToArray())
            {
                if (enemy == null || enemy.IsDying) continue;
                if (Battlefield.GroundDistance(enemy.transform.position, _groundPoint) > _splashRadius + enemy.BodyRadius) continue;
                enemy.Health.TakeDamage(_damage);
                if (_pushDirection != 0f) enemy.PushBack(_pushDirection);
                hits++;
            }

            // Hero co the da bi go khoi o giua luc dan bay — Unity null-check bat ca truong hop da Destroy.
            if (_owner != null) _owner.OnHitsLanded(hits);

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

        // Luc bay: lap vong. Luc no: chay mot lan roi tu huy.
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
