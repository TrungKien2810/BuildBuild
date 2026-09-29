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
    /// LUAT CHAN (xem `Blocker`): moi unit phe ta giu toi da `Blocker.MaxHeld` quai (3, Chainwarden 4). Quai
    /// nhan mot CHO canh unit roi di toi dung cho do danh (khong don khit vao mot diem); unit da het cho thi
    /// quai bo qua unit do va di tiep. Giu cho toi khi unit chet hoac chay qua xa. Quai bi giu chet thi
    /// `Blocker` goi con gan nhat vao the cho (`HoldBy`).
    ///
    /// Quai BAN XA (tam > `RangedReach`) khong bi giu va khong chiem cho: no chon unit gan nhat trong tam nhin
    /// va dung xa ban — de no chiem mot trong 3 cho thi con can chien phia sau lot qua het.
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

        /// <summary>Mau phu luc mang Xich (Chainwarden) — tim nhat: nguoi choi doc ra con nao dang an them sat thuong.</summary>
        static readonly Color ChainedTint = new Color(0.85f, 0.68f, 1f);

        [SerializeField] SpriteAnimator animator;

        EnemyDefinition _definition;
        Health _health;
        Health _target;
        Transform _targetTransform;
        SpriteRenderer _renderer;

        float _damage;
        float _aweWeaken, _aweVulnerability, _aweUntil;
        float _armorBreak, _armorBrokenUntil;
        float _attackCooldown;
        float _frozenUntil;
        float _stunnedUntil;
        bool _frozenVisual;
        bool _dying;
        UnityEngine.Object _markedBy;
        float _markedUntil;
        GameObject _markFx;
        GameObject _rootFx;
        SpriteRenderer _rootRenderer;
        GameObject _chainIcon;
        SpriteRenderer _chainIconRenderer;

        /// <summary>O khoa tren dau quai mang Xich — `Warden` gan khi Chainwarden ra san; null = khong ve.</summary>
        public static Sprite[] ChainIconFrames;
        static readonly Vector3 ChainIconOffset = new Vector3(0.2f, 0.65f, 0f);   // lech phai de khong chong sao choang
        Health _pendingHit;
        SpriteAnimationClip _cast;
        SpriteRenderer _markRenderer;
        GameObject _stunFx;
        SpriteRenderer _stunRenderer;
        float _bodyRadius;
        float _bodyHeight;
        Hero _engaged;
        Blocker _blocker;   // unit dang GIU quai nay (quai ban xa danh ma khong bi giu: _engaged co, _blocker null)
        int _slot = -1;
        float _slotSide;
        float _rootedUntil;
        float _chainedUntil;
        float _chainBonus;
        float _pullUntil;
        Vector3 _pullTo;
        float _launchStart, _launchUntil, _launchHeight;
        Vector3 _launchGround;
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

        /// <summary>Tam danh tu muc nay tro len la quai ban xa: khong bi giu, khong chiem cho (cung nguong can chien cua hero).</summary>
        const float RangedReach = 1.5f;

        /// <summary>Cach cho vay trong khoang nay (tren mat dat) thi coi nhu da dung vao cho.</summary>
        const float SlotTolerance = 0.1f;

        /// <summary>Hero dang vay chay ra xa hon aggro x he so nay thi moi bo — tranh bo cho chi vi hero lui mot nhip.</summary>
        const float StickyAggro = 1.5f;

        /// <summary>Ban ra khi quai chet han (da chay xong animation death).</summary>
        public event Action<Enemy> Died;

        /// <summary>Ban ra NGAY LUC quai het mau (truoc animation death), cho moi quai — `Necromancer` nghe de dung linh xuong.</summary>
        public static event Action<Enemy> Killed;

        public EnemyDefinition Definition => _definition;
        public Health Health => _health;
        public bool IsDying => _dying;
        public bool IsFrozen => Time.time < _frozenUntil;
        bool IsStunned => Time.time < _stunnedUntil;
        public bool IsBoss => _definition != null && _definition.kind == EnemyKind.Boss;
        public bool IsElite => _definition != null && _definition.kind == EnemyKind.TinhAnh;

        /// <summary>Bi troi (Quat Xich): dung im, van danh.</summary>
        public bool IsRooted => Time.time < _rootedUntil;

        /// <summary>
        /// Phan sat thuong nhan them do Xich (Chainwarden, GDD muc 3): dang bi mot unit co `heldDamageBonus` giu,
        /// hoac con han Xich tu Nem Moc — lay muc cao hon, khong cong don. 0 = khong bi Xich.
        /// </summary>
        public float ChainBonus
        {
            get
            {
                var holder = Holder;
                float held = holder != null ? holder.HeldDamageBonus : 0f;
                float hooked = Time.time < _chainedUntil ? _chainBonus : 0f;
                return Mathf.Max(held, hooked);
            }
        }

        public bool IsChained => ChainBonus > 0f;

        /// <summary>Nua be ngang than tren mat dat (world) — tam can chien va vung no do toi MEP than, khong toi chan.</summary>
        public float BodyRadius => _bodyRadius;

        /// <summary>Chieu cao than (world) — cho dat thanh mau tren dau.</summary>
        public float BodyHeight => _bodyHeight;

        /// <summary>Sat thuong moi don thuong (da nhan he so wave, tru Long Uy cua Wyrmspear neu dang dung trong tam).</summary>
        public float Damage => _damage * (Time.time < _aweUntil ? 1f - _aweWeaken : 1f);

        /// <summary>Dang chay clip than cua mot chieu (`PlayCast`) — dung di, dung danh toi het clip.</summary>
        public bool IsCasting => _cast != null && animator != null && animator.CurrentClip == _cast.name && !animator.IsFinished;

        /// <summary>Dang vung don thuong hoac dang cast — chieu moi phai doi, cat ngang la nuot mat don.</summary>
        public bool IsBusy => IsCasting || (animator != null && animator.CurrentClip == "attack" && !animator.IsFinished);

        /// <summary>Giua than (world) — cho dan nham vao.</summary>
        public Vector3 BodyCenter => transform.position + new Vector3(0f, _bodyHeight * 0.5f, 0f);

        /// <summary>Unit phe ta quai nay dang danh (bi giu, hoac quai ban xa dang nham); null = di ve lau dai.</summary>
        public Hero EngagedHero => _engaged;

        /// <summary>Unit dang GIU quai nay (con song, con giu no); null = khong bi ai giu.</summary>
        public Blocker Holder => _blocker != null && !_blocker.IsDown && _blocker.Holds(this) ? _blocker : null;

        /// <summary>Quai can chien moi bi giu; quai ban xa dung xa danh, khong chiem cho cua unit.</summary>
        public bool CanBeHeld => _definition != null && _definition.attackRange <= RangedReach;

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _health = GetComponent<Health>();
            _health.Damaged += OnDamaged;
            gameObject.AddComponent<CombatFeedback>().Init(ally: false, () => _bodyHeight);
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
            _cast = null;
            Disengage();
            _rootedUntil = 0f;
            _chainedUntil = 0f;
            _aweUntil = 0f;
            _armorBrokenUntil = 0f;
            _pullUntil = 0f;
            _launchUntil = 0f;
            _health.Vulnerability = 0f;
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

        // ---------------------------------------------------------------- he so toc (nhanh Tru, plan muc B1.4)

        float _slowUntil;
        float _slowPercent;
        float _hasteUntil;
        float _hastePercent;

        /// <summary>Dang bi lam cham (va khong mien).</summary>
        public bool IsSlowed => Time.time < _slowUntil && !SlowImmune;

        /// <summary>Mien moi lam cham (boss Noi Gian). Bat len thi slow dang chay cung mat tac dung.</summary>
        public bool SlowImmune { get; set; }

        /// <summary>He so toc DANH (boss Noi Gian 1,5). Doc moi lan vung don.</summary>
        public float AttackRateMultiplier { get; set; } = 1f;

        /// <summary>Toc chay hien tai: goc x (1 - cham) x (1 + tang toc). Moi buoc di doc so nay.</summary>
        float MoveSpeed
        {
            get
            {
                float speed = _definition.moveSpeed;
                if (IsSlowed) speed *= 1f - _slowPercent;
                if (Time.time < _hasteUntil) speed *= 1f + _hastePercent;
                return speed;
            }
        }

        /// <summary>
        /// Lam cham <paramref name="percent"/> (0..1) trong <paramref name="seconds"/> giay (phao Bang). Hai nguon chong
        /// nhau thi lay muc MANH hon va thoi han DAI hon — khong cong don.
        /// </summary>
        public void ApplySlow(float percent, float seconds)
        {
            if (_dying || SlowImmune || percent <= 0f || seconds <= 0f) return;
            bool active = Time.time < _slowUntil;
            _slowPercent = active ? Mathf.Max(_slowPercent, Mathf.Clamp01(percent)) : Mathf.Clamp01(percent);
            _slowUntil = Mathf.Max(_slowUntil, Time.time + seconds);
        }

        /// <summary>Tang toc chay <paramref name="percent"/> trong <paramref name="seconds"/> giay (boss Tieng Thet). Khong cong don.</summary>
        public void ApplyHaste(float percent, float seconds)
        {
            if (_dying || percent <= 0f || seconds <= 0f) return;
            _hastePercent = Mathf.Max(Time.time < _hasteUntil ? _hastePercent : 0f, percent);
            _hasteUntil = Mathf.Max(_hasteUntil, Time.time + seconds);
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

        /// <summary>Troi <paramref name="seconds"/> giay (Quat Xich): khong di, khong bi day, van danh. Chong nhau lay han dai hon.</summary>
        public void ApplyRoot(float seconds, Sprite[] frames = null)
        {
            if (_dying || seconds <= 0f) return;
            if (Time.time + seconds <= _rootedUntil) return;
            _rootedUntil = Time.time + seconds;
            // Vong xich quanh chan, lap toi het troi; troi moi dai hon thi dung lai vong cho khop han.
            if (_rootFx != null) Destroy(_rootFx);
            _rootFx = OneShotFx.SpawnLoop(transform, Vector3.zero, frames, seconds, SortOrder.ForUnit(this) + 1);
            _rootRenderer = _rootFx != null ? _rootFx.GetComponent<SpriteRenderer>() : null;
        }

        /// <summary>O khoa tren dau hien/tat theo `IsChained` (Xich doi theo unit giu nen soat moi frame).</summary>
        void UpdateChainIcon()
        {
            bool show = IsChained && ChainIconFrames != null && ChainIconFrames.Length > 0;
            if (show == (_chainIcon != null)) return;
            if (!show)
            {
                Destroy(_chainIcon);
                _chainIcon = null;
                _chainIconRenderer = null;
                return;
            }
            _chainIcon = OneShotFx.SpawnLoop(transform, ChainIconOffset, ChainIconFrames, float.PositiveInfinity,
                SortOrder.ForUnit(this) + 1);
            _chainIconRenderer = _chainIcon != null ? _chainIcon.GetComponent<SpriteRenderer>() : null;
        }

        /// <summary>
        /// Long Uy (Wyrmspear): dang trong tam thi giam cong <paramref name="weaken"/> va nhan them <paramref name="vulnerability"/>
        /// sat thuong. Nguon goi MOI frame, het han ngay sau (0,2s) — ra khoi tam la mat. Hai nguon lay muc cao hon.
        /// </summary>
        public void ApplyAwe(float weaken, float vulnerability)
        {
            if (_dying) return;
            bool active = Time.time < _aweUntil;
            _aweWeaken = active ? Mathf.Max(_aweWeaken, weaken) : weaken;
            _aweVulnerability = active ? Mathf.Max(_aweVulnerability, vulnerability) : vulnerability;
            _aweUntil = Time.time + 0.2f;
        }

        /// <summary>Pha giap (Dia Long Chan): nhan them <paramref name="bonus"/> sat thuong tu moi nguon. Khong cong don.</summary>
        public void ApplyArmorBreak(float bonus, float seconds)
        {
            if (_dying || bonus <= 0f || seconds <= 0f) return;
            _armorBreak = Time.time < _armorBrokenUntil ? Mathf.Max(_armorBreak, bonus) : bonus;
            _armorBrokenUntil = Mathf.Max(_armorBrokenUntil, Time.time + seconds);
        }

        /// <summary>Xich co thoi han (Nem Moc): nhan them <paramref name="bonus"/> sat thuong ke ca khi khong bi giu.</summary>
        public void ApplyChain(float bonus, float seconds)
        {
            if (_dying || bonus <= 0f || seconds <= 0f) return;
            bool active = Time.time < _chainedUntil;
            _chainBonus = active ? Mathf.Max(_chainBonus, bonus) : bonus;
            _chainedUntil = Mathf.Max(_chainedUntil, Time.time + seconds);
        }

        /// <summary>
        /// Bi KEO ve <paramref name="destination"/> trong <paramref name="seconds"/> giay (Nem Moc). Bo cho vay dang giu:
        /// toi noi, quai tu nhan cho moi o unit gan nhat nhu binh thuong.
        /// </summary>
        public void PullTo(Vector3 destination, float seconds)
        {
            if (_dying) return;
            Disengage();
            _knockback.Reset();
            _pullTo = new Vector3(destination.x, destination.y, transform.position.z);
            _pullUntil = Time.time + Mathf.Max(0.01f, seconds);
        }

        /// <summary>Dang bi hat tung (cot da Bach Dia Long): khong di, khong danh, ve o lop Air.</summary>
        public bool IsLaunched => _launchUntil > 0f;

        /// <summary>
        /// Hat tung cao <paramref name="height"/> (world) trong <paramref name="seconds"/> giay roi roi ve dung cho cu:
        /// bo cho vay, khong di khong danh. Boss khong bi nhac len. Dang bay thi khong hat lai.
        /// </summary>
        public void KnockUp(float height, float seconds)
        {
            if (_dying || IsBoss || IsLaunched || seconds <= 0f) return;
            Disengage();
            _knockback.Reset();
            _pullUntil = 0f;
            _launchGround = transform.position;
            _launchHeight = height;
            _launchStart = Time.time;
            _launchUntil = Time.time + seconds;
        }

        /// <summary>Dang bay thi dat do cao theo parabol va tra ve true; cham dat thi ve cho cu.</summary>
        bool UpdateLaunch()
        {
            if (!IsLaunched) return false;
            float u = (Time.time - _launchStart) / (_launchUntil - _launchStart);
            if (u >= 1f)
            {
                Land();
                return false;
            }
            transform.position = _launchGround + new Vector3(0f, 4f * _launchHeight * u * (1f - u), 0f);
            return true;
        }

        void Land()
        {
            transform.position = _launchGround;
            _launchUntil = 0f;
        }

        /// <summary>Dang bi keo thi truot mot nhip ve dich va tra ve true — frame nay khong di, khong danh.</summary>
        bool UpdatePull()
        {
            if (_pullUntil <= 0f) return false;
            float left = _pullUntil - Time.time;
            if (left <= Time.deltaTime)
            {
                transform.position = _pullTo;
                _pullUntil = 0f;
                return false;
            }
            transform.position += (_pullTo - transform.position) * (Time.deltaTime / left);
            return true;
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

            // Xich doi theo unit dang giu (con song, con giu) — dat lai moi frame, ke ca luc dang bi khoa.
            _health.Vulnerability = ChainBonus
                + (Time.time < _armorBrokenUntil ? _armorBreak : 0f)
                + (Time.time < _aweUntil ? _aweVulnerability : 0f);
            UpdateChainIcon();

            // Bi moc keo: truot ve phia Chainwarden, khong di khong danh (van ve mau/choang dung).
            if (UpdateLaunch() || UpdatePull())
            {
                UpdateFrozen();
                return;
            }

            // Dat TRUOC khi tru cooldown: quai dong bang giu nguyen nhip danh dang do, tan bang la
            // danh tiep tu dung cho cu — khong bi "an" mat mot don vi co.
            if (UpdateFrozen()) return;

            _attackCooldown -= Time.deltaTime;

            // Dang cast (boss dap dat / gam): dung yen toi het clip than.
            if (IsCasting) return;

            // Bi day lui: frame nay chi lui, khong di khong danh (cooldown van chay tiep o tren). Bi troi thi dung chan.
            bool rooted = IsRooted;
            if (!rooted && _knockback.Step(transform)) return;

            // Dang vung don (clip "attack" chua het) thi dung khoa nguyen cho — ke ca luc doi hinh day
            // tach nhau: tach giua chung la nuot mat don, va la nguon lam quai "vua di vua danh" truoc day.
            bool swinging = animator != null && animator.CurrentClip == "attack" && !animator.IsFinished;

            // Ca doi hinh khong don khit vao mot diem truoc hero hay truoc tuong.
            if (!swinging && !rooted)
                transform.position += Battlefield.Separation(this, Alive) * Mathf.Min(1f, SeparationRate * Time.deltaTime);

            var hero = EngageHero();
            if (hero != null)
            {
                Fight(hero, swinging);
                return;
            }

            if (_target == null || _target.IsDead)
            {
                Walk();
                return;
            }
            if (swinging) return;

            // Tuong truoc lau dai la mot duong thang dung: quai o y nao cung chi can MEP THAN du gan theo x.
            float distance = Mathf.Abs(_targetTransform.position.x - transform.position.x);
            if (distance > _definition.attackRange + _bodyRadius) Walk();
            else
            {
                if (_renderer != null) _renderer.flipX = false;
                Attack(_target);
            }
        }

        // Order theo chan sau khi moi dich chuyen trong frame da xong; dau an ky ve ngay tren than.
        void LateUpdate()
        {
            // Dang bay: khong kep vao dai dat (do cao la toa do y) va ve de len moi unit.
            if (!_dying && !IsLaunched) transform.position = Battlefield.Clamp(transform.position, _bodyRadius, clampRight: false);
            if (_renderer == null) return;
            _renderer.sortingOrder = IsLaunched ? SortOrder.Air : SortOrder.ForUnit(this);
            if (_markRenderer != null) _markRenderer.sortingOrder = _renderer.sortingOrder + 1;
            if (_stunRenderer != null) _stunRenderer.sortingOrder = _renderer.sortingOrder + 1;
            if (_rootRenderer != null) _rootRenderer.sortingOrder = _renderer.sortingOrder + 1;
            if (_chainIconRenderer != null) _chainIconRenderer.sortingOrder = _renderer.sortingOrder + 1;
        }

        /// <summary>
        /// Unit dang danh (con song, con giu quai nay, chua chay qua xa) thi giu; khong thi nha cho va tim unit
        /// gan nhat trong `heroAggroRange` CON CHO TRONG (quai ban xa: unit gan nhat, khong can cho). Khong co thi
        /// null — quai di tiep toi lau dai.
        /// </summary>
        Hero EngageHero()
        {
            // Khieu Khich (Cragfist): dung trong vong cua ong va ong con cho thi bo unit dang danh, sang ong.
            var taunter = Hero.Taunter(this);
            if (taunter != null && taunter.Unit != _engaged)
            {
                if (!CanBeHeld)
                {
                    Disengage();
                    _engaged = taunter.Unit;
                    return _engaged;
                }
                if (HoldBy(taunter)) return _engaged;
            }

            bool stillOn = _engaged != null && !_engaged.IsDying && (!CanBeHeld || Holder != null);
            if (stillOn)
            {
                float sticky = _definition.heroAggroRange * StickyAggro;
                if ((_engaged.transform.position - transform.position).sqrMagnitude <= sticky * sticky) return _engaged;
            }
            Disengage();

            Blocker best = null;
            float bestSqr = _definition.heroAggroRange * _definition.heroAggroRange;
            foreach (var blocker in Blocker.Active)
            {
                if (blocker == null || blocker.IsDown) continue;
                if (CanBeHeld && !blocker.HasFreeSlot(this)) continue;
                float sqr = (blocker.transform.position - transform.position).sqrMagnitude;
                if (sqr > bestSqr) continue;
                bestSqr = sqr;
                best = blocker;
            }
            if (best == null) return null;

            if (!CanBeHeld)
            {
                _engaged = best.Unit;
                return _engaged;
            }
            return HoldBy(best) ? _engaged : null;
        }

        /// <summary>
        /// Vao cho cua <paramref name="blocker"/> (bo unit dang danh neu co). `Blocker` goi thang ham nay khi mot quai
        /// no dang giu chet — con gan nhat vao the cho. Tra ve false neu unit het cho / quai ban xa.
        /// </summary>
        public bool HoldBy(Blocker blocker)
        {
            if (_dying || _definition == null || !CanBeHeld || blocker == null || blocker.IsDown) return false;
            if (_blocker != blocker) Disengage();

            // Phia cua cho vay chot luc nhan cho: tinh lai moi frame thi quai dung ngay tren unit se lat qua lai.
            float side = blocker.transform.position.x <= transform.position.x ? 1f : -1f;
            int slot = blocker.ClaimSlot(this, side, _definition.attackRange);
            if (slot < 0) return false;

            _blocker = blocker;
            _engaged = blocker.Unit;
            _slot = slot;
            _slotSide = side;
            return true;
        }

        void Disengage()
        {
            if (_blocker != null) _blocker.Release(this);
            _blocker = null;
            _engaged = null;
            _slot = -1;
        }

        /// <summary>
        /// Vao tam (chan quai toi MEP than unit, do tren mat dat) va het cooldown thi danh ngay; giua hai don thi
        /// di ve cho vay cua minh (quai ban xa: buoc thang toi khi vao tam). Dang vung don thi dung yen: di giua
        /// chung la cat clip va nuot mat don.
        /// </summary>
        void Fight(Hero hero, bool swinging)
        {
            var heroPosition = hero.transform.position;
            if (swinging) return;

            // Quai ban xa con ngoai mep phai man thi chua tinh la vao tam: dung ban tu ngoai man thi hero (bi kep trong
            // mep phai) khong bao gio toi duoc.
            bool inReach = Battlefield.GroundDistance(heroPosition, transform.position) - hero.BodyRadius <= _definition.attackRange
                && Battlefield.InsideRight(transform.position, _bodyRadius);
            if (inReach && _attackCooldown <= 0f)
            {
                Face(heroPosition.x);
                Attack(hero.Health);
                return;
            }

            var slot = _blocker != null && _slot >= 0
                ? _blocker.SlotPosition(_slot, _slotSide, _definition.attackRange)
                : inReach ? transform.position : heroPosition;

            // Bi troi thi dung tai cho nhin unit, van danh neu dang trong tam (nhanh tren).
            if (!IsRooted && Battlefield.GroundDistance(slot, transform.position) > SlotTolerance)
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
            var tint = IsFrozen ? FrozenTint : IsStunned ? StunnedTint : IsChained ? ChainedTint : Color.white;
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

        /// <summary>Di ve lau dai — tru khi dang bi troi: dung idle tai cho.</summary>
        void Walk()
        {
            if (!IsRooted)
            {
                March();
                return;
            }
            if (animator != null && animator.CurrentClip == "walk") animator.Play("idle");
        }

        void March()
        {
            // Quy uoc du an: sprite ve QUAY TRAI, va quai di sang trai — nen khong lat flipX.
            if (_renderer != null) _renderer.flipX = false;
            transform.position += Vector3.left * (MoveSpeed * Time.deltaTime);
            if (animator != null && animator.CurrentClip != "hurt") animator.Play("walk");
        }

        /// <summary>Buoc theo mot huong bat ky (dung khi re sang hero dung lech duong di).</summary>
        void Advance(Vector3 direction)
        {
            if (Mathf.Abs(direction.x) > 0.01f) Face(transform.position.x + direction.x);
            // Khong buoc qua dich: cho vay o gan hon mot buoc thi dung dung vao cho.
            float step = Mathf.Min(MoveSpeed * Time.deltaTime, direction.magnitude);
            transform.position += direction.normalized * step;
            if (animator != null && animator.CurrentClip != "hurt") animator.Play("walk");
        }

        void Attack(Health target)
        {
            if (_attackCooldown > 0f) return;
            if (_definition.attackRate <= 0f) return;

            _attackCooldown = 1f / (_definition.attackRate * AttackRateMultiplier);
            // Sat thuong ra o frame cham don cua clip (`hitFrame`), khong phai luc vua gio vu khi. Clip khong
            // khai hitFrame (hoac khong co animator) thi danh ngay nhu cu.
            _pendingHit = target;
            var clip = animator != null && _definition.animations != null ? _definition.animations.attack : null;
            if (animator != null) animator.Play("attack", restart: true);
            if (clip == null || !clip.IsValid || clip.hitFrame <= 0) LandHit();
        }

        /// <summary>
        /// Chay clip than cua mot chieu (vd. boss dap dat). Trong luc clip chay quai dung yen, khong danh, khong giat
        /// hurt. `HitFrame` / `ClipFinished` ban ra voi ten clip — nguoi goi nghe de ra don dung frame.
        /// </summary>
        public void PlayCast(SpriteAnimationClip clip)
        {
            if (_dying || animator == null || clip == null || !clip.IsValid) return;
            _cast = clip;
            animator.Play(clip, restart: true);
        }

        void LandHit()
        {
            if (_pendingHit != null && !_pendingHit.IsDead) _pendingHit.TakeDamage(Damage, _health);
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
            if (IsBusy) return;
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
            if (IsLaunched) Land();     // chet giua khong trung -> clip death nam tren dat
            Killed?.Invoke(this);   // doc an ky TRUOC khi go FX; an ky dua tren thoi gian, khong tren FX
            if (_markFx != null) Destroy(_markFx);
            if (_rootFx != null) Destroy(_rootFx);
            if (_chainIcon != null) Destroy(_chainIcon);
            _chainIcon = null;
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
            Died?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
