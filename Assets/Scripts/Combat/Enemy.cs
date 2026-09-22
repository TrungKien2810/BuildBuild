using System;
using System.Collections.Generic;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Quai: di tu mep phai sang trai ve phia lau dai, gap tuong thi dung lai danh.
    ///
    /// Hero con song trong `heroAggroRange` di TRUOC lau dai. Hero khong con la vat trang tri mien sat
    /// thuong — dat hero chan duong la mot lua chon that, va mot hero yeu dat sai cho se chet. Khong con
    /// hero nao gan thi quai quay ve muc tieu mac dinh (`SetTarget`, hien la lau dai).
    ///
    /// VAY DANH co cho: moi hero chi co `Hero.EngageSlots` (3) cho dung phia quai den — chinh dien, cheo
    /// tren, cheo duoi. Quai giu cho de di toi dung vi tri do danh (khong don khit vao mot diem); hero da
    /// het cho thi quai bo qua hero do va di tiep. Giu hero da vay cho toi khi hero chet hoac chay qua xa.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class Enemy : MonoBehaviour
    {
        /// <summary>Moi con quai dang song trong scene — hero va dan quet danh sach nay de tim muc tieu.</summary>
        public static readonly List<Enemy> Alive = new List<Enemy>();

        /// <summary>Mau phu luc dang bi dong bang — de nguoi choi doc ra con nao dang dinh.</summary>
        static readonly Color FrozenTint = new Color(0.62f, 0.82f, 1f);

        /// <summary>Mau phu luc dang bi choang (vd. phi cua Ky si tu than) — vang, phan biet voi dong bang.</summary>
        static readonly Color StunnedTint = new Color(1f, 0.85f, 0.35f);

        [SerializeField] SpriteAnimator animator;

        EnemyDefinition _definition;
        Health _health;
        Health _target;
        Transform _targetTransform;
        SpriteRenderer _renderer;

        float _damage;
        float _attackCooldown;
        float _frozenUntil;
        float _stunnedUntil;
        bool _frozenVisual;
        bool _dying;
        UnityEngine.Object _markedBy;
        float _markedUntil;
        GameObject _markFx;
        Health _pendingHit;
        SpriteRenderer _markRenderer;
        GameObject _stunFx;
        SpriteRenderer _stunRenderer;
        float _bodyRadius;
        float _bodyHeight;
        Hero _engaged;
        int _slot;
        float _slotSide;
        readonly Knockback _knockback = new Knockback();

        /// <summary>Toc tach hai quai dang dung chong len nhau (xem `Battlefield.Separation`).</summary>
        const float SeparationRate = 6f;

        /// <summary>Tren dau quai (toa do LOCAL, da nhan scale than) — cho dat FX an ky.</summary>
        static readonly Vector3 MarkOffset = new Vector3(0f, 0.65f, 0f);

        /// <summary>
        /// Scale LOCAL cua FX choang. Art ve tren canvas 128px @100 PPU = 1,28 unit, ma FX gan vao than quai
        /// (transform da nhan `Battlefield.UnitScale` = 2) nen de 1 thi sao choang rong ~2,1 unit — to hon ca
        /// con quai. 0,3 cho no rong ~0,64 unit, vua bang be ngang dau.
        /// </summary>
        const float StunFxScale = 0.3f;

        /// <summary>
        /// Goc tren MAT DAT cua tung cho vay, tinh tu phia quai den: chinh dien, cheo tren, cheo duoi. 70 do chu
        /// khong 45: hai cho canh nhau phai cach nhau hon `Battlefield.PersonalSpace`, khong thi luc tach day quai
        /// ra khoi cho lien tuc.
        /// </summary>
        static readonly float[] SlotAngles = { 0f, 70f, -70f };

        /// <summary>Cach cho vay trong khoang nay (tren mat dat) thi coi nhu da dung vao cho.</summary>
        const float SlotTolerance = 0.1f;

        /// <summary>Hero dang vay chay ra xa hon aggro x he so nay thi moi bo — tranh bo cho chi vi hero lui mot nhip.</summary>
        const float StickyAggro = 1.5f;

        /// <summary>Ban ra khi quai chet han (da chay xong animation death). Tham so la vang thuong.</summary>
        public event Action<Enemy, int> Died;

        /// <summary>Ban ra NGAY LUC quai het mau (truoc animation death), cho moi quai — `Necromancer` nghe de dung linh xuong.</summary>
        public static event Action<Enemy> Killed;

        public EnemyDefinition Definition => _definition;
        public Health Health => _health;
        public bool IsDying => _dying;
        public bool IsFrozen => Time.time < _frozenUntil;
        bool IsStunned => Time.time < _stunnedUntil;
        public bool IsBoss => _definition != null && _definition.kind == EnemyKind.Boss;

        /// <summary>Nua be ngang than tren mat dat (world) — tam can chien va vung no do toi MEP than, khong toi chan.</summary>
        public float BodyRadius => _bodyRadius;

        /// <summary>Giua than (world) — cho dan nham vao.</summary>
        public Vector3 BodyCenter => transform.position + new Vector3(0f, _bodyHeight * 0.5f, 0f);

        /// <summary>Hero ma quai nay dang giu cho vay; null = khong vay ai.</summary>
        public Hero EngagedHero => _engaged;

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _health = GetComponent<Health>();
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
        }

        void OnEnable() => Alive.Add(this);

        void OnDisable() => Alive.Remove(this);

        /// <summary>
        /// Goi ngay sau khi spawn. Hai he so la muc len thang theo wave, do `WaveTuning` tinh.
        /// </summary>
        public void Initialise(EnemyDefinition definition, float healthMultiplier, float damageMultiplier)
        {
            _definition = definition;
            _damage = definition.baseDamage * damageMultiplier;
            _dying = false;
            _markedBy = null;
            _markedUntil = 0f;
            _engaged = null;
            _knockback.Reset();
            Thaw();

            transform.localScale = Vector3.one * Battlefield.UnitScale;
            Battlefield.MeasureBody(definition.animations, out _bodyRadius, out _bodyHeight);
            if (_renderer != null) _renderer.flipX = false;

            _health.ResetTo(definition.baseHealth * healthMultiplier);

            if (animator != null)
            {
                animator.Bind(definition.animations);
                animator.ClipFinished -= OnClipFinished;   // tranh dang ky hai lan neu Initialise goi lai
                animator.ClipFinished += OnClipFinished;
                animator.HitFrame -= OnHitFrame;
                animator.HitFrame += OnHitFrame;
                animator.Play("walk");
            }
        }

        /// <summary>
        /// Dong bang them <paramref name="seconds"/> giay. Nguon dong bang (vd. `FrostZone`) goi lai
        /// moi frame voi thoi gian CON LAI cua no; `Max` nen hai nguon chong nhau tu lay cai dai hon
        /// ma khong ben nao phai dem so nguon.
        /// </summary>
        public void ApplyFreeze(float seconds)
        {
            if (_dying || seconds <= 0f) return;
            _frozenUntil = Mathf.Max(_frozenUntil, Time.time + seconds);
        }

        /// <summary>Choang: khoa y nhu dong bang (khong di, khong danh, dung animation), phu mau vang + sao choang tren dau.</summary>
        public void ApplyStun(float seconds, Sprite[] frames)
        {
            if (_dying || seconds <= 0f) return;
            bool alreadyStunned = IsStunned;
            _stunnedUntil = Mathf.Max(_stunnedUntil, Time.time + seconds);
            if (alreadyStunned || frames == null || frames.Length == 0) return;
            if (_stunFx != null) Destroy(_stunFx);
            _stunFx = OneShotFx.SpawnLoop(transform, MarkOffset, frames, seconds, SortOrder.ForUnit(this) + 1,
                scale: StunFxScale);
            _stunRenderer = _stunFx != null ? _stunFx.GetComponent<SpriteRenderer>() : null;
        }

        /// <summary>
        /// Day lui do don danh co `knockbackOnHit` (ky si tu than). Khac `OnDamaged`: day ca khi quai dang vung attack —
        /// quai vay danh ky si thi luc nao cung dang vung, bo qua thi khong bao gio thay lui.
        /// </summary>
        public void PushBack(float direction)
        {
            if (_dying) return;
            _knockback.Trigger(direction);
        }

        /// <summary>An ky cua <paramref name="marker"/> trong <paramref name="seconds"/> giay; an lai thi dem lai tu dau.</summary>
        public void ApplyMark(UnityEngine.Object marker, float seconds, Sprite[] frames)
        {
            if (_dying || seconds <= 0f) return;
            _markedBy = marker;
            _markedUntil = Time.time + seconds;
            if (_markFx != null) Destroy(_markFx);
            _markFx = OneShotFx.SpawnLoop(transform, MarkOffset, frames, seconds, SortOrder.ForUnit(this) + 1);
            _markRenderer = _markFx != null ? _markFx.GetComponent<SpriteRenderer>() : null;
        }

        public bool IsMarkedBy(UnityEngine.Object marker) =>
            marker != null && _markedBy == marker && Time.time < _markedUntil;

        /// <summary>Muc tieu MAC DINH khi khong co hero nao gan — `WaveManager` dat lau dai vao day.</summary>
        public void SetTarget(Health target)
        {
            _target = target;
            _targetTransform = target != null ? target.transform : null;
        }

        void Update()
        {
            if (_dying || _definition == null) return;

            // Dat TRUOC khi tru cooldown: quai dong bang giu nguyen nhip danh dang do, tan bang la
            // danh tiep tu dung cho cu — khong bi "an" mat mot don vi co.
            if (UpdateFrozen()) return;

            _attackCooldown -= Time.deltaTime;

            // Bi day lui: frame nay chi lui, khong di khong danh (cooldown van chay tiep o tren).
            if (_knockback.Step(transform)) return;

            // Dang vung don (clip "attack" chua het) thi dung khoa nguyen cho — ke ca luc doi hinh day
            // tach nhau: tach giua chung la nuot mat don, va la nguon lam quai "vua di vua danh" truoc day.
            bool swinging = animator != null && animator.CurrentClip == "attack" && !animator.IsFinished;

            // Ca doi hinh khong don khit vao mot diem truoc hero hay truoc tuong.
            if (!swinging)
                transform.position += Battlefield.Separation(this, Alive) * Mathf.Min(1f, SeparationRate * Time.deltaTime);

            var hero = EngageHero();
            if (hero != null)
            {
                Fight(hero, swinging);
                return;
            }

            if (_target == null || _target.IsDead)
            {
                March();
                return;
            }
            if (swinging) return;

            // Tuong truoc lau dai la mot duong thang dung: quai o y nao cung chi can MEP THAN du gan theo x.
            float distance = Mathf.Abs(_targetTransform.position.x - transform.position.x);
            if (distance > _definition.attackRange + _bodyRadius) March();
            else
            {
                if (_renderer != null) _renderer.flipX = false;
                Attack(_target);
            }
        }

        // Order theo chan sau khi moi dich chuyen trong frame da xong; dau an ky ve ngay tren than.
        void LateUpdate()
        {
            if (!_dying) transform.position = Battlefield.Clamp(transform.position, _bodyRadius, clampRight: false);
            if (_renderer == null) return;
            _renderer.sortingOrder = SortOrder.ForUnit(this);
            if (_markRenderer != null) _markRenderer.sortingOrder = _renderer.sortingOrder + 1;
            if (_stunRenderer != null) _stunRenderer.sortingOrder = _renderer.sortingOrder + 1;
        }

        /// <summary>
        /// Hero dang vay (con song, chua chay qua xa) thi giu; khong thi nha cho va tim hero con song gan nhat
        /// trong `heroAggroRange` CON CHO TRONG. Khong co thi null — quai di tiep toi lau dai.
        /// </summary>
        Hero EngageHero()
        {
            if (_engaged != null && !_engaged.IsDying)
            {
                float sticky = _definition.heroAggroRange * StickyAggro;
                if ((_engaged.transform.position - transform.position).sqrMagnitude <= sticky * sticky) return _engaged;
            }
            _engaged = null;

            Hero best = null;
            float bestSqr = _definition.heroAggroRange * _definition.heroAggroRange;
            foreach (var hero in Hero.Alive)
            {
                if (hero == null || hero.IsDying || !hero.HasFreeSlot(this)) continue;
                float sqr = (hero.transform.position - transform.position).sqrMagnitude;
                if (sqr > bestSqr) continue;
                bestSqr = sqr;
                best = hero;
            }
            if (best == null) return null;

            _slot = best.ClaimSlot(this);
            _engaged = best;
            // Phia cua cho vay chot luc nhan cho: tinh lai moi frame thi quai dung ngay tren hero se lat qua lai.
            _slotSide = best.transform.position.x <= transform.position.x ? 1f : -1f;
            return best;
        }

        /// <summary>
        /// Vao tam (chan quai toi MEP than hero, do tren mat dat) va het cooldown thi danh ngay; giua hai don thi
        /// di ve cho vay cua minh. Dang vung don thi dung yen: di giua chung la cat clip va nuot mat don.
        /// </summary>
        void Fight(Hero hero, bool swinging)
        {
            var heroPosition = hero.transform.position;
            if (swinging) return;

            bool inReach = Battlefield.GroundDistance(heroPosition, transform.position) - hero.BodyRadius <= _definition.attackRange;
            if (inReach && _attackCooldown <= 0f)
            {
                Face(heroPosition.x);
                Attack(hero.Health);
                return;
            }

            float angle = SlotAngles[_slot] * Mathf.Deg2Rad;
            float ring = hero.BodyRadius + _definition.attackRange * 0.8f;
            var slot = heroPosition + new Vector3(_slotSide * Mathf.Cos(angle) * ring,
                Mathf.Sin(angle) * ring * Battlefield.DepthSquash, 0f);

            if (Battlefield.GroundDistance(slot, transform.position) > SlotTolerance)
            {
                var to = slot - transform.position;
                to.z = 0f;
                Advance(to);
                return;
            }

            Face(heroPosition.x);
            if (animator != null && animator.CurrentClip == "walk") animator.Play("idle");
        }

        // Art ve QUAY TRAI: flipX = true la nhin sang PHAI.
        void Face(float targetX)
        {
            if (_renderer != null) _renderer.flipX = targetX > transform.position.x;
        }

        /// <summary>
        /// Tra ve true neu dang bi dong bang hoac choang. Dung luon animation va doi mau: neu chi chan
        /// dung ma chan van buoc thi doc ra nhu game dung hinh, khong phai bi khoa.
        /// Chi cham renderer khi trang thai DOI, khong phai moi frame.
        /// </summary>
        bool UpdateFrozen()
        {
            bool locked = IsFrozen || IsStunned;
            var tint = IsFrozen ? FrozenTint : IsStunned ? StunnedTint : Color.white;
            if (locked != _frozenVisual || (_renderer != null && _renderer.color != tint))
            {
                _frozenVisual = locked;
                if (animator != null) animator.Paused = locked;
                if (_renderer != null) _renderer.color = tint;
            }
            return locked;
        }

        void Thaw()
        {
            _frozenUntil = 0f;
            _stunnedUntil = 0f;
            _frozenVisual = false;
            if (animator != null) animator.Paused = false;
            if (_renderer != null) _renderer.color = Color.white;
            if (_stunFx != null) Destroy(_stunFx);
            _stunFx = null;
            _stunRenderer = null;
        }

        void March()
        {
            // Quy uoc du an: sprite ve QUAY TRAI, va quai di sang trai — nen khong lat flipX.
            if (_renderer != null) _renderer.flipX = false;
            transform.position += Vector3.left * (_definition.moveSpeed * Time.deltaTime);
            if (animator != null && animator.CurrentClip != "hurt") animator.Play("walk");
        }

        /// <summary>Buoc theo mot huong bat ky (dung khi re sang hero dung lech duong di).</summary>
        void Advance(Vector3 direction)
        {
            if (Mathf.Abs(direction.x) > 0.01f) Face(transform.position.x + direction.x);
            // Khong buoc qua dich: cho vay o gan hon mot buoc thi dung dung vao cho.
            float step = Mathf.Min(_definition.moveSpeed * Time.deltaTime, direction.magnitude);
            transform.position += direction.normalized * step;
            if (animator != null && animator.CurrentClip != "hurt") animator.Play("walk");
        }

        void Attack(Health target)
        {
            if (_attackCooldown > 0f) return;
            if (_definition.attackRate <= 0f) return;

            _attackCooldown = 1f / _definition.attackRate;
            // Sat thuong ra o frame cham don cua clip (`hitFrame`), khong phai luc vua gio vu khi. Clip khong
            // khai hitFrame (hoac khong co animator) thi danh ngay nhu cu.
            _pendingHit = target;
            var clip = animator != null && _definition.animations != null ? _definition.animations.attack : null;
            if (animator != null) animator.Play("attack", restart: true);
            if (clip == null || !clip.IsValid || clip.hitFrame <= 0) LandHit();
        }

        void LandHit()
        {
            if (_pendingHit != null && !_pendingHit.IsDead) _pendingHit.TakeDamage(_damage, _health);
            _pendingHit = null;
        }

        void OnHitFrame(string clipName)
        {
            if (!_dying && clipName == "attack") LandHit();
        }

        void OnDamaged(float _)
        {
            if (_dying || animator == null || IsFrozen) return;
            // Dang vung attack thi khong giat hurt: cat clip la nuot mat don chua toi frame cham.
            if (animator.CurrentClip == "attack" && !animator.IsFinished) return;
            // Hurt khong duoc cat ngang death; death moi la thu chot. Dang dong bang thi cung khong
            // giat hurt — giu nguyen dang cung do moi doc ra la dang bi khoa.
            animator.Play("hurt", restart: true);
            // Lui NGUOC huong dang nhin (art quay trai: flipX = nhin phai -> lui sang trai).
            _knockback.Trigger(_renderer != null && _renderer.flipX ? -1f : 1f);
        }

        void OnDied()
        {
            if (_dying) return;
            _dying = true;
            Killed?.Invoke(this);   // doc an ky TRUOC khi go FX; an ky dua tren thoi gian, khong tren FX
            if (_markFx != null) Destroy(_markFx);
            Thaw();     // chet trong luc dong bang van phai chay het animation death
            if (animator != null && animator.CurrentClip != null)
            {
                animator.Play("death", restart: true);
                return;     // doi ClipFinished moi bao chet
            }
            Finish();
        }

        void OnClipFinished(string clipName)
        {
            if (clipName == "death") Finish();
            else if (!_dying && animator != null) animator.Play("walk");
        }

        void Finish()
        {
            Died?.Invoke(this, _definition != null ? _definition.goldReward : 0);
            Destroy(gameObject);
        }
    }
}
