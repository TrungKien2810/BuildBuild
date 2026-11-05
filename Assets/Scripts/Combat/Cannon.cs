using BuildBuild.Core;
using BuildBuild.Data;
using BuildBuild.Meta;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Mot khau phao tu ban tren be phao dai (GAME_DESIGN.md muc 2 + 4). Khong co mau, khong bi danh, khong di chuyen.
    ///
    /// Nham quai TRONG TAM gan lau dai nhat (x nho nhat) — con sap cham tuong la con nguy hiem nhat, khong phai con
    /// dung gan nong phao nhat. Moi phat: chay clip "attack", dan ra o `hitFrame` (luc loe nong), chon lai muc tieu
    /// tai thoi diem do neu con cu da chet. Dan dung chung `Projectile` voi hero (`LaunchCannon`).
    ///
    /// Sprite ve QUAY TRAI nhu moi unit; quai den tu ben phai nen luon lat flipX.
    ///
    /// Bo phao 2026-10-05 them cach ra don theo `CannonDefinition.fire` (Chain / Cone / Vortex) va cac bien the cua dan
    /// thuong (vong cau, troi, nhan boss, nham quai nhieu mau, tang toc quay). So TAM, chua can.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(SpriteAnimator))]
    public class Cannon : MonoBehaviour
    {

        CannonDefinition _definition;
        SpriteRenderer _renderer;
        SpriteAnimator _animator;
        float _damage;
        float _cooldown;
        Unit _pending;
        bool _firing;
        Faction _side;
        float _spin;            // Gatling: 0..1, tang khi ban lien, giam khi nghi
        float _lastShot = -10f;
        int _shots;             // Cone: dem phat de ra xung kich
        string _clip = "attack";
        GameObject _aimMark;
        DragonBreath _dragon;

        public CannonDefinition Definition => _definition;
        public int Level { get; private set; }

        /// <summary>Sat thuong hien tai cua mot phat (Long Viêm: mot giay phun) — doc luc ban, khong cache ngoai.</summary>
        public float Damage => _damage;

        /// <summary>Phao nay co bo luat phun cua Dau Rong Da khong (xem `DragonBreath`).</summary>
        public bool UsesDragonBreath => _dragon != null;

        /// <summary>Diem dan ra trong the gioi (dau nong), da tinh lossyScale cua than lan be phao va huong lat.</summary>
        public Vector3 MuzzlePoint()
        {
            if (_definition == null) return transform.position;
            var muzzle = (Vector3)_definition.muzzle * transform.lossyScale.x;
            if (_renderer != null && _renderer.flipX) muzzle.x = -muzzle.x;
            return transform.position + muzzle;
        }

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<SpriteAnimator>();
        }

        /// <summary><paramref name="side"/> = Npc: phao tren thanh NPC (Chinh Phat) — quay trai, ban phe ta.</summary>
        public void Initialise(CannonDefinition definition, int level, Faction side = Faction.Player)
        {
            _definition = definition;
            _side = side;
            SetLevel(level);
            _cooldown = 0f;
            _pending = null;
            _firing = false;
            _spin = 0f;
            _shots = 0;

            transform.localScale = Vector3.one * definition.displayScale;
            _renderer.flipX = side == Faction.Player;
            _renderer.color = definition.tint;
            if (definition.slowIconFrames != null && definition.slowIconFrames.Length > 0)
                EnemyExtras.SlowIconFrames = definition.slowIconFrames;

            _animator.Bind(definition.animations);
            // Dau Rong Da: bo luat phun nam o `DragonBreath`, `Cannon` khong ban tung phat nua (`Update` thoat som).
            _dragon = null;
            if (definition.dragonKit != null)
            {
                _dragon = GetComponent<DragonBreath>();
                if (_dragon == null) _dragon = gameObject.AddComponent<DragonBreath>();
                _dragon.Initialise(this, definition, definition.dragonKit, side);
                return;
            }
            _animator.HitFrame -= OnHitFrame;
            _animator.HitFrame += OnHitFrame;
            _animator.ClipFinished -= OnClipFinished;
            _animator.ClipFinished += OnClipFinished;
        }

        /// <summary>Nang level giua wave thi phao dang lap an so moi ngay, khong can go ra lap lai.</summary>
        public void SetLevel(int level)
        {
            Level = _definition.ClampLevel(level);
            _damage = Progression.CannonDamage(_definition, Level);    // level + sao
        }

        void Update()
        {
            if (_dragon != null)
            {
                // Long Viêm: nhip phun/nghi + Long Nộ + Long Tức do `DragonBreath` quyet dinh, khong co cooldown o day.
                _dragon.Tick();
                return;
            }
            if (_definition == null || _definition.fireRate <= 0f) return;
            _cooldown -= Time.deltaTime;
            if (_definition.spinUpSeconds > 0f && Time.time - _lastShot > 0.35f)
                _spin = Mathf.Max(0f, _spin - 0.5f * Time.deltaTime);
            if (_firing || _cooldown > 0f) return;

            var target = FindTarget();
            if (target == null) return;

            if (_definition.spinUpSeconds > 0f)
            {
                // Gatling: khong cho clip — ban theo nhip quay, clip attack chi lap lam hinh.
                float gap = Time.time - _lastShot;
                if (gap <= 0.35f) _spin = Mathf.Min(1f, _spin + gap / _definition.spinUpSeconds);
                _lastShot = Time.time;
                _cooldown = 1f / (_definition.fireRate * Mathf.Lerp(1f, _definition.spinUpMultiplier, _spin));
                if (_animator.CurrentClip != "attack" || _animator.IsFinished) _animator.Play("attack", restart: true);
                _pending = target;
                Release();
                return;
            }

            _cooldown = 1f / _definition.fireRate;
            _pending = target;
            _firing = true;
            var anims = _definition.animations;
            _clip = NextIsBurst && anims != null && anims.skill.IsValid ? "skill" : "attack";
            var clip = anims != null ? anims.Get(_clip) : null;
            _animator.Play(_clip, restart: true);
            if (_definition.aimMarkFrames != null && _definition.aimMarkFrames.Length > 0)
            {
                float scale = Mathf.Max(0.01f, target.transform.lossyScale.y);
                _aimMark = OneShotFx.SpawnLoop(target.transform, new Vector3(0f, (target.BodyHeight + 0.2f) / scale, 0f),
                    _definition.aimMarkFrames, 3f, SortOrder.Air, scale: Battlefield.UnitScale / scale);
            }
            if (clip == null || !clip.IsValid || clip.hitFrame <= 0) Release();
        }

        bool NextIsBurst => _definition.fire == CannonFire.Cone && _definition.burstEvery > 0
            && _shots % _definition.burstEvery == _definition.burstEvery - 1;

        /// <summary>Doi thu con song trong tam, gan thanh minh nhat (phe ta: x nho nhat; thanh NPC ben phai: x lon nhat).</summary>
        Unit FindTarget()
        {
            Unit best = null;
            float bestX = float.MaxValue;
            float rangeSqr = _definition.range * _definition.range;
            foreach (var enemy in Unit.Hostiles(_side))
            {
                if (enemy == null || enemy.IsDying) continue;
                if ((enemy.AimPoint(transform.position) - transform.position).sqrMagnitude > rangeSqr) continue;
                float x = _side == Faction.Player ? enemy.transform.position.x : -enemy.transform.position.x;
                // Hoa Mai Thanh: mau hien tai cao nhat truoc; Hac Tinh: cho dong quai nhat truoc. Bang nhau moi xet gan thanh.
                if (_definition.targetHighestHealth) x -= enemy.Health.Current * 1000f;
                if (_definition.fire == CannonFire.Vortex) x -= Crowd(enemy) * 1000f;
                if (x >= bestX) continue;
                bestX = x;
                best = enemy;
            }
            return best;
        }

        /// <summary>Vortex: so quai dung quanh <paramref name="enemy"/> trong ban kinh ho — chon cho dong nhat.</summary>
        int Crowd(Unit enemy)
        {
            int count = 0;
            foreach (var other in Unit.Hostiles(_side))
                if (other != null && !other.IsDying
                    && Battlefield.GroundDistance(other.transform.position, enemy.transform.position) <= _definition.aoeRadius)
                    count++;
            return count;
        }

        void OnHitFrame(string clip)
        {
            if (clip == _clip) Release();
        }

        void OnClipFinished(string clip)
        {
            if (clip != _clip) return;
            if (_definition.spinUpSeconds > 0f)
            {
                // Gatling khong cho clip: het clip thi ve idle, phat ke tiep tu bat lai attack.
                _animator.Play("idle");
                return;
            }
            if (_pending != null) Release();    // clip ngan hon hitFrame da khai: van phai ra dan
            _firing = false;
            _animator.Play("idle");
        }

        void Release()
        {
            var target = _pending != null && !_pending.IsDying ? _pending : FindTarget();
            _pending = null;
            if (_aimMark != null) Destroy(_aimMark);
            if (_definition.animations == null || !_definition.animations.attack.IsValid) _firing = false;
            if (target == null) return;

            // lossyScale = displayScale x scale cua be (thanh tri 3 cap thu nho phao theo mat be).
            var muzzle = (Vector3)_definition.muzzle * transform.lossyScale.x;
            if (_renderer.flipX) muzzle.x = -muzzle.x;
            var origin = transform.position + muzzle;
            if (_definition.muzzleFrames != null && _definition.muzzleFrames.Length > 0)
                OneShotFx.Spawn(origin, _definition.muzzleFrames, _renderer.flipX, SortOrder.Air, Battlefield.UnitScale);

            if (_definition.fire == CannonFire.Chain || _definition.fire == CannonFire.Cone)
            {
                if (_definition.fire == CannonFire.Chain) FireChain(origin, target);
                else FireCone(origin, target);
                Sfx.Play(_definition.fireSfx);
                return;
            }

            var go = new GameObject("CannonShot");
            go.transform.position = transform.position + muzzle;
            go.transform.localScale = Vector3.one * (_definition.projectileScale * Battlefield.UnitScale);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = SortOrder.Air;
            renderer.color = _definition.tint;
            go.AddComponent<Projectile>().LaunchCannon(target, _damage, _definition, _side);
            Sfx.Play(_definition.fireSfx);
        }

        /// <summary>Don mot quai (tinh nhan boss) + ghi thong ke phao.</summary>
        void Hit(Unit enemy, float damage)
        {
            float before = enemy.Health.Current;
            enemy.Health.TakeDamage(damage * BigTargetFactor(_definition, enemy), side: _side);
            WaveStats.RecordCannonDamage(_definition, _side, before - enemy.Health.Current);
        }

        public static float BigTargetFactor(CannonDefinition cannon, Unit enemy) =>
            enemy != null && (enemy.IsBoss || enemy.IsElite) ? cannon.bigTargetMultiplier : 1f;

        /// <summary>Thap Set: tia nay tu nong sang muc tieu roi sang quai gan nhat chua trung, toi da `chainCount` con.</summary>
        void FireChain(Vector3 origin, Unit first)
        {
            var hit = new System.Collections.Generic.HashSet<Unit>();
            Vector3 from = origin;
            var current = first;
            int count = Mathf.Max(1, _definition.chainCount);
            while (current != null && hit.Count < count)
            {
                hit.Add(current);
                var to = current.BodyCenter;
                // Art tia chi phu x 16..112 cua o 128 -> noi dai hai dau 16/96 de tia cham dung nong va than quai.
                var pad = (to - from) * (16f / 96f);
                OneShotFx.SpawnBeam(from - pad, to + pad, _definition.beamFrames, SortOrder.Air, Battlefield.UnitScale);
                OneShotFx.Spawn(to, _definition.impactFrames, false, SortOrder.Air, Battlefield.UnitScale * _definition.impactScale);
                var source = current;
                Hit(current, _damage);
                from = to;
                Unit next = null;
                float best = _definition.chainRange;
                foreach (var enemy in Unit.Hostiles(_side))
                {
                    if (enemy == null || enemy.IsDying || hit.Contains(enemy)) continue;
                    float d = Battlefield.GroundDistance(enemy.transform.position, source.transform.position);
                    if (d > best) continue;
                    best = d;
                    next = enemy;
                }
                current = next;
            }
        }

        /// <summary>
        /// Phao Am Thanh: song hinh non tu nong ve phia muc tieu, moi quai trong tam + goc an don. Phat thu `burstEvery`
        /// la xung kich: sat thuong x `burstDamageMultiplier` + day lui (boss khong bi day — Enemy tu chan).
        /// </summary>
        void FireCone(Vector3 origin, Unit target)
        {
            bool burst = NextIsBurst;
            _shots++;
            var aim = target.BodyCenter - origin;
            aim.z = 0f;
            float half = _definition.coneAngle * 0.5f;
            float push = aim.x >= 0f ? 1f : -1f;
            foreach (var enemy in Unit.Hostiles(_side).ToArray())
            {
                if (enemy == null || enemy.IsDying) continue;
                var to = enemy.BodyCenter - origin;
                to.z = 0f;
                if (to.magnitude > _definition.range + enemy.BodyRadius || Vector3.Angle(aim, to) > half) continue;
                Hit(enemy, burst ? _damage * _definition.burstDamageMultiplier : _damage);
                OneShotFx.Spawn(enemy.BodyCenter, burst ? _definition.burstImpactFrames : _definition.impactFrames, false,
                    SortOrder.Air, Battlefield.UnitScale);
                if (burst) enemy.PushBack(push);
            }
            // Art song ve chay sang TRAI: dat giua tam, lat khi ban sang phai, nghieng theo huong ban.
            var wave = burst ? _definition.burstWaveFrames : _definition.waveFrames;
            float length = Mathf.Min(aim.magnitude, _definition.range) * 0.5f;
            float degrees = Mathf.Atan2(aim.y, Mathf.Abs(aim.x)) * Mathf.Rad2Deg * push;
            OneShotFx.Spawn(origin + aim.normalized * length, wave, push > 0f, SortOrder.Air, Battlefield.UnitScale, degrees: degrees);
        }
    }
}
