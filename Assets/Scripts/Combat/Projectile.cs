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
        Unit _target;
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
        Faction _side;   // phe ban — dan chi trung doi thu cua phe nay; phao (khong chu) la Player
        BuildBuild.Data.CannonDefinition _cannon;
        bool _singleTarget;     // phao Lien Thanh / Bang: chi con bi nham an don
        float _slowPercent;
        float _slowDuration;
        bool _impacting;
        string _impactSfx;
        // Lao xuyen (No Xuyen): bay thang theo `_flight`, moi quai tren duong an don mot lan, het luot/ra khoi tam thi tan.
        int _pierceLeft;
        Vector3 _flight;
        float _flightLeft;
        readonly System.Collections.Generic.HashSet<Unit> _pierced = new System.Collections.Generic.HashSet<Unit>();
        float _timer;
        int _frame;

        /// <summary>
        /// Dan phao (`Cannon`): khong co hero chu. `aoeRadius` 0 = DON muc tieu (chi con bi nham, ke ca khi quai khac
        /// dung chong len no); &gt; 0 = no nhu dan hero. Moi con trung bi lam cham neu phao co `slowPercent`.
        /// </summary>
        public void LaunchCannon(Unit target, float damage, BuildBuild.Data.CannonDefinition cannon, Faction side)
        {
            Launch(target, damage, cannon.aoeRadius, cannon.projectileSpeed, cannon.projectileFrames, cannon.impactFrames, null);
            _side = side;   // phao thanh NPC (Chinh Phat) ban phe ta
            _singleTarget = cannon.aoeRadius <= 0f;
            _impactScale = cannon.impactScale;
            _slowPercent = cannon.slowPercent;
            _slowDuration = cannon.slowDuration;
            _cannon = cannon;
            _impactSfx = cannon.impactSfx;
            if (cannon.pierceCount > 0)
            {
                _pierceLeft = cannon.pierceCount;
                _flight = (target.BodyCenter - transform.position).normalized;
                // Bay qua muc tieu them mot tam ban: con o cuoi hang van an lao.
                _flightLeft = (target.BodyCenter - transform.position).magnitude + cannon.range;
                _pierced.Clear();
            }
        }

        public void Launch(Unit target, float damage, float splashRadius, float speed, Sprite[] frames,
            Sprite[] impactFrames, Hero owner)
        {
            _owner = owner;
            _side = owner != null ? owner.Side : Faction.Player;
            // Chot luc ban: hero co the chet/bi go truoc khi dan trung.
            _impactScale = owner != null ? owner.Definition.impactScale : 1f;
            _impactSfx = owner != null ? owner.Definition.impactSfx : null;
            _pushDirection = owner != null && owner.Definition.knockbackOnHit
                ? (target.transform.position.x >= owner.transform.position.x ? 1f : -1f) : 0f;
            _renderer = GetComponent<SpriteRenderer>();
            _target = target;
            // Diem nham tren mat dat (lau dai: mat tuong cung hang voi CHAN nguoi ban), giua than tinh tu do.
            _groundPoint = target.AimPoint(owner != null ? owner.transform.position : transform.position);
            _destination = _groundPoint + new Vector3(0f, target.BodyHeight * 0.5f, 0f);
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
            if (_pierceLeft > 0)
            {
                Pierce();
                return;
            }

            if (_target != null && !_target.IsDying)
            {
                // Hoi tu diem nham cu, KHONG tu dan: dan dang o tren khong, lau dai giu y dau vao nen dich se troi len mai.
                _groundPoint = _target.AimPoint(_groundPoint);
                _destination = _groundPoint + new Vector3(0f, _target.BodyHeight * 0.5f, 0f);
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
            Sfx.Play(_impactSfx);
            transform.rotation = Quaternion.identity;
            // `projectileScale` chi cho luc bay; frame no = co art x he so than chung x `impactScale` cua hero
            transform.localScale = Vector3.one * (Battlefield.UnitScale * _impactScale);
            _renderer.flipX = false;

            // Chep ra mang: quai chet co the phat su kien lam doi danh sach giua vong lap.
            int hits = 0;
            foreach (var enemy in Unit.Hostiles(_side).ToArray())
            {
                if (enemy == null || enemy.IsDying) continue;
                if (_singleTarget ? enemy != _target
                    : Battlefield.GroundDistance(enemy.AimPoint(_groundPoint), _groundPoint) > _splashRadius + enemy.BodyRadius) continue;
                float before = enemy.Health.Current;
                enemy.Health.TakeDamage(_damage, _owner != null ? _owner.Health : null, side: _side);
                if (_cannon != null) WaveStats.RecordCannonDamage(_cannon, _side, before - enemy.Health.Current);
                if (_pushDirection != 0f) enemy.PushBack(_pushDirection);
                if (_slowPercent > 0f) enemy.ApplySlow(_slowPercent, _slowDuration);
                if (_owner != null) _owner.OnProjectileHit(enemy, enemy == _target, _damage);   // Ngam Doc / Thoc Hang
                hits++;
            }

            // Hero co the da bi go khoi o giua luc dan bay — Unity null-check bat ca truong hop da Destroy.
            if (_owner != null) _owner.OnHitsLanded(hits);

            if (_impactFrames == null || _impactFrames.Length == 0) Destroy(gameObject);
            else SetFrames(_impactFrames);
        }

        /// <summary>
        /// Bay thang khong doi huong (khong bam muc tieu): trung quai nao co than chua diem lao (ngang: ban kinh than,
        /// doc: tu chan toi dinh dau) thi quai do an don + vet no tai cho, lao bay tiep.
        /// </summary>
        void Pierce()
        {
            float step = _speed * Time.deltaTime;
            transform.position += _flight * step;
            _flightLeft -= step;
            bool right = _flight.x > 0f;
            _renderer.flipX = right;
            float angle = Mathf.Atan2(_flight.y, Mathf.Abs(_flight.x)) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, right ? angle : -angle);

            var at = transform.position;
            foreach (var enemy in Unit.Hostiles(_side).ToArray())
            {
                if (enemy == null || enemy.IsDying || _pierced.Contains(enemy)) continue;
                var foot = enemy.transform.position;
                if (Mathf.Abs(at.x - foot.x) > enemy.BodyRadius * 0.5f) continue;
                if (at.y < foot.y || at.y > foot.y + enemy.BodyHeight) continue;
                _pierced.Add(enemy);
                float before = enemy.Health.Current;
                enemy.Health.TakeDamage(_damage, side: _side);
                if (_cannon != null) WaveStats.RecordCannonDamage(_cannon, _side, before - enemy.Health.Current);
                OneShotFx.Spawn(enemy.BodyCenter, _impactFrames, false, SortOrder.Air, Battlefield.UnitScale * _impactScale);
                Sfx.Play(_impactSfx);
                if (--_pierceLeft <= 0) break;
            }
            if (_pierceLeft <= 0 || _flightLeft <= 0f) Destroy(gameObject);
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
