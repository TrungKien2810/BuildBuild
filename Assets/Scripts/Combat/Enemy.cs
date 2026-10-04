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
    ///
    /// Quai BAY CAO (`EnemyDefinition.flyHigh`, Gloomwing): khong bi giu, bay thang ve lau dai bo qua moi unit; chi hero /
    /// linh ban xa va phao danh trung (don can chien bi `Health.IgnoresHitFrom` nuot im). Bi hero / linh ban xa danh thi
    /// quay sang phan don ke do toi khi no chet; bi phao ban thi khong phan ung. Quai bay THAP (Ma Hoa, Rong) la quai dat.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class Enemy : Unit
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
        float _attackCooldown;
        float _frozenUntil;
        float _stunnedUntil;
        bool _frozenVisual;
        bool _dying;
        GameObject _chainIcon;
        SpriteRenderer _chainIconRenderer;

        /// <summary>O khoa tren dau quai mang Xich — `Warden` gan khi Chainwarden ra san; null = khong ve.</summary>
        public static Sprite[] ChainIconFrames;
        static readonly Vector3 ChainIconOffset = new Vector3(0.2f, 0.65f, 0f);   // lech phai de khong chong sao choang
        Health _pendingHit;
        SpriteAnimationClip _cast;
        GameObject _stunFx;
        SpriteRenderer _stunRenderer;
        float _bodyRadius;
        float _bodyHeight;
        Hero _engaged;
        Hero _avenging;     // bay cao: hero / linh ban xa da danh trung no — phan don ke nay
        Blocker _blocker;   // unit dang GIU quai nay (quai ban xa danh ma khong bi giu: _engaged co, _blocker null)
        int _slot = -1;
        float _slotSide;
        readonly Knockback _knockback = new Knockback();

        /// <summary>Toc tach hai quai dang dung chong len nhau (xem `Battlefield.Separation`).</summary>
        const float SeparationRate = 6f;

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

        public EnemyDefinition Definition => _definition;
        public override Health Health => _health;
        public override bool IsDying => _dying;
        public bool IsFrozen => Time.time < _frozenUntil;
        public override bool IsStunned => Time.time < _stunnedUntil;
        public override bool IsBoss => _definition != null && _definition.kind == EnemyKind.Boss;
        public override bool IsElite => _definition != null && _definition.kind == EnemyKind.TinhAnh;

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
                return Mathf.Max(held, TimedChainBonus);
            }
        }

        public bool IsChained => ChainBonus > 0f;

        /// <summary>Nua be ngang than tren mat dat (world) — tam can chien va vung no do toi MEP than, khong toi chan.</summary>
        public override float BodyRadius => _bodyRadius;

        /// <summary>Chieu cao than (world) — cho dat thanh mau tren dau.</summary>
        public override float BodyHeight => _bodyHeight;

        /// <summary>Sat thuong moi don thuong (da nhan he so wave, tru Long Uy cua Wyrmspear neu dang dung trong tam).</summary>
        public float Damage => _damage * (1f + DamageBonus) * (1f - AweWeaken);

        /// <summary>Cong them vao sat thuong don thuong (Goi Hon Lua cua Dreadwyrm 0,25 = +25%).</summary>
        public float DamageBonus { get; set; }

        /// <summary>Dang chay clip than cua mot chieu (`PlayCast`) — dung di, dung danh toi het clip.</summary>
        public bool IsCasting => _cast != null && animator != null && animator.CurrentClip == _cast.name && !animator.IsFinished;

        /// <summary>Dang vung don thuong hoac dang cast — chieu moi phai doi, cat ngang la nuot mat don.</summary>
        public bool IsBusy => IsCasting || (animator != null && animator.CurrentClip == "attack" && !animator.IsFinished);

        /// <summary>Unit phe ta quai nay dang danh (bi giu, hoac quai ban xa dang nham); null = di ve lau dai.</summary>
        public override Hero EngagedHero => _engaged;

        /// <summary>Unit dang GIU quai nay (con song, con giu no); null = khong bi ai giu.</summary>
        public override Blocker Holder => _blocker != null && !_blocker.IsDown && _blocker.Holds(this) ? _blocker : null;

        /// <summary>Quai can chien moi bi giu; quai ban xa dung xa danh, khong chiem cho cua unit. Bay cao khong ai giu.</summary>
        public override bool CanBeHeld => _definition != null && _definition.attackRange <= RangedReach && !FliesHigh;

        /// <summary>Bay cao (Gloomwing): chi hero / linh ban xa va phao danh trung.</summary>
        public override bool FliesHigh => _definition != null && _definition.flyHigh;

        /// <summary><paramref name="hero"/> co nham / danh trung duoc quai nay khong (can chien khong voi toi quai bay cao).</summary>
        public override bool TargetableBy(Hero hero) => !FliesHigh || (hero != null && hero.IsRangedUnit);

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _health = GetComponent<Health>();
            _health.Damaged += OnDamaged;
            gameObject.AddComponent<CombatFeedback>().Init(ally: false, () => _bodyHeight);
            _health.Died += OnDied;
        }

        /// <summary>Bay cao: khong bi troi / keo / hat tung.</summary>
        protected override bool Unmovable => FliesHigh;

        /// <summary>Bi keo / hat tung: nha cho vay, dung day lui dang chay.</summary>
        protected override void OnDisplaced()
        {
            Disengage();
            _knockback.Reset();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Side = Faction.Npc;
            Alive.Add(this);
            Health.AnyDamaged += OnAnyDamaged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Alive.Remove(this);
            Health.AnyDamaged -= OnAnyDamaged;
        }

        /// <summary>Don can chien vao quai bay cao: nguon la Health cua hero / linh khong ban xa. Phao (nguon null) trung.</summary>
        bool IgnoresHit(Health source)
        {
            if (source == null) return false;
            var hero = source.GetComponent<Hero>();
            return hero != null && !hero.IsRangedUnit;
        }

        /// <summary>Bay cao bi hero / linh ban xa danh trung thi nho ke do de phan don; phao ban thi bo qua.</summary>
        void OnAnyDamaged(Health victim, Health source, float dealt, float shield, Health shieldCaster)
        {
            if (victim != _health || _dying || !FliesHigh || source == null) return;
            var hero = source.GetComponent<Hero>();
            if (hero != null && hero.IsRangedUnit && !hero.IsDying) _avenging = hero;
        }

        /// <summary>
        /// Goi ngay sau khi spawn. Hai he so la muc len thang theo wave, do `WaveTuning` tinh.
        /// </summary>
        public void Initialise(EnemyDefinition definition, float healthMultiplier, float damageMultiplier)
        {
            _definition = definition;
            _damage = definition.baseDamage * damageMultiplier;
            _avenging = null;
            _health.IgnoresHitFrom = definition.flyHigh ? IgnoresHit : (System.Func<Health, bool>)null;
            _dying = false;
            ResetStatus();
            _cast = null;
            Disengage();
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
        public override void ApplyFreeze(float seconds)
        {
            if (_dying || seconds <= 0f) return;
            _frozenUntil = Mathf.Max(_frozenUntil, Time.time + seconds);
        }

        /// <summary>Choang: khoa y nhu dong bang (khong di, khong danh, dung animation), phu mau vang + sao choang tren dau.</summary>
        public override void ApplyStun(float seconds, Sprite[] frames = null)
        {
            // Moi nguon choang hien co deu la can chien (phi, dap dat, xich) — khong voi toi quai bay cao.
            if (_dying || seconds <= 0f || FliesHigh) return;
            bool alreadyStunned = IsStunned;
            _stunnedUntil = Mathf.Max(_stunnedUntil, Time.time + seconds);
            if (alreadyStunned || frames == null || frames.Length == 0) return;
            if (_stunFx != null) Destroy(_stunFx);
            _stunFx = OneShotFx.SpawnLoop(transform, MarkOffset, frames, seconds, SortOrder.ForUnit(this) + 1,
                scale: StunFxScale);
            _stunRenderer = _stunFx != null ? _stunFx.GetComponent<SpriteRenderer>() : null;
        }

        // ---------------------------------------------------------------- he so toc (nhanh Tru, plan muc B1.4)

        /// <summary>He so toc DANH (boss Noi Gian 1,5). Doc moi lan vung don.</summary>
        public float AttackRateMultiplier { get; set; } = 1f;

        /// <summary>Toc chay hien tai: goc x (1 - cham) x (1 + tang toc). Moi buoc di doc so nay.</summary>
        float MoveSpeed => _definition.moveSpeed * SpeedFactor;

        /// <summary>
        /// Day lui do don danh co `knockbackOnHit` (ky si tu than). Khac `OnDamaged`: day ca khi quai dang vung attack —
        /// quai vay danh ky si thi luc nao cung dang vung, bo qua thi khong bao gio thay lui.
        /// </summary>
        public override void PushBack(float direction)
        {
            if (_dying) return;
            _knockback.Trigger(direction);
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
            _health.Vulnerability = ChainBonus + StatusVulnerability;
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

            // Bi Mua Roi (Tien Det To): quay sang danh phe cu toi het gio.
            if (UpdatePuppet(swinging)) return;

            // Ca doi hinh khong don khit vao mot diem truoc hero hay truoc tuong.
            if (!swinging && !rooted && !FliesHigh)
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
            if (!_dying && !IsLaunched)
            {
                transform.position = RuneWall.Clamp(this, transform.position);   // Tuong Rune chan nhu gap hero
                transform.position = Battlefield.Clamp(transform.position, _bodyRadius, clampRight: false);
            }
            if (_renderer == null) return;
            _renderer.sortingOrder = IsLaunched || FliesHigh ? SortOrder.Air : SortOrder.ForUnit(this);
            SortStatusFx(_renderer.sortingOrder);
            if (_stunRenderer != null) _stunRenderer.sortingOrder = _renderer.sortingOrder + 1;
            if (_chainIconRenderer != null) _chainIconRenderer.sortingOrder = _renderer.sortingOrder + 1;
            if (_puppetRenderer != null) _puppetRenderer.sortingOrder = _renderer.sortingOrder + 1;
        }

        Faction _homeSide;
        float _puppetUntil;
        GameObject _puppetFx;
        SpriteRenderer _puppetRenderer;
        Sprite[] _puppetSnap;

        /// <summary>Dang bi Mua Roi: tam thoi thuoc phe nguoi giat to.</summary>
        public bool IsPuppet => _puppetUntil > 0f;

        /// <summary>
        /// Mua Roi (Tien Det To): `seconds` giay quai nay sang phe <paramref name="side"/> va danh quai phe cu gan nhat; het
        /// gio tro lai phe cu, dung nguyen cho. Boss mien. <paramref name="strings"/> lap tren nguoi, <paramref name="snap"/>
        /// chay mot lan luc dut to.
        /// </summary>
        public void ApplyPuppet(Faction side, float seconds, Sprite[] strings, Sprite[] snap)
        {
            if (_dying || IsBoss || seconds <= 0f) return;
            if (!IsPuppet) _homeSide = Side;
            Disengage();
            Side = side;
            _puppetUntil = Time.time + seconds;
            _puppetSnap = snap;
            if (_puppetFx != null) Destroy(_puppetFx);
            _puppetFx = OneShotFx.SpawnLoop(transform, Vector3.zero, strings, seconds, SortOrder.ForUnit(this) + 1);
            _puppetRenderer = _puppetFx != null ? _puppetFx.GetComponent<SpriteRenderer>() : null;
        }

        /// <summary>Tra ve true neu frame nay da xu ly xong theo Mua Roi (di toi / danh quai phe cu).</summary>
        bool UpdatePuppet(bool swinging)
        {
            if (!IsPuppet) return false;
            if (Time.time >= _puppetUntil)
            {
                EndPuppet(snap: true);
                return false;
            }
            if (swinging) return true;

            Unit best = null;
            float bestDistance = float.PositiveInfinity;
            foreach (var unit in Unit.Hostiles(Side))
            {
                if (unit == null || unit == this || unit.IsDying || unit is Castle) continue;
                if (unit.FliesHigh && !_definition.IsRanged) continue;
                float distance = Battlefield.GroundDistance(unit.AimPoint(transform.position), transform.position) - unit.BodyRadius;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = unit;
            }
            if (best == null)
            {
                if (animator != null && animator.CurrentClip == "walk") animator.Play("idle");
                return true;
            }
            if (bestDistance <= _definition.attackRange)
            {
                Face(best.transform.position.x);
                if (_attackCooldown <= 0f) Attack(best.Health);
                else if (animator != null && animator.CurrentClip == "walk") animator.Play("idle");
                return true;
            }
            if (!IsRooted)
            {
                var to = best.transform.position - transform.position;
                to.z = 0f;
                Advance(to);
            }
            return true;
        }

        void EndPuppet(bool snap)
        {
            Side = _homeSide;
            _puppetUntil = 0f;
            if (_puppetFx != null) Destroy(_puppetFx);
            _puppetFx = null;
            _puppetRenderer = null;
            if (snap)
                OneShotFx.Spawn(transform.position, _puppetSnap, false, SortOrder.ForUnit(this) + 1, Battlefield.UnitScale);
        }

        /// <summary>
        /// Unit dang danh (con song, con giu quai nay, chua chay qua xa) thi giu; khong thi nha cho va tim unit
        /// gan nhat trong `heroAggroRange` CON CHO TRONG (quai ban xa: unit gan nhat, khong can cho). Khong co thi
        /// null — quai di tiep toi lau dai.
        /// </summary>
        Hero EngageHero()
        {
            // Bay cao: khong khieu khich, khong aggro — chi phan don hero / linh ban xa da danh trung no.
            if (FliesHigh)
            {
                if (_avenging != null && _avenging.IsDying) _avenging = null;
                _engaged = _avenging;
                return _engaged;
            }

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
                if (blocker == null || blocker.IsDown || !blocker.Unit.IsHostileTo(Side)) continue;
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
            if (RuneWall.Stops(this, -1f))
            {
                if (animator != null && animator.CurrentClip == "walk") animator.Play("idle");
                return;
            }
            transform.position += Vector3.left * (MoveSpeed * Time.deltaTime);
            if (animator != null && animator.CurrentClip != "hurt") animator.Play("walk");
        }

        /// <summary>Buoc theo mot huong bat ky (dung khi re sang hero dung lech duong di).</summary>
        void Advance(Vector3 direction)
        {
            if (Mathf.Abs(direction.x) > 0.01f) Face(transform.position.x + direction.x);
            if (RuneWall.Stops(this, direction.x))
            {
                if (animator != null && animator.CurrentClip == "walk") animator.Play("idle");
                return;
            }
            // Khong buoc qua dich: cho vay o gan hon mot buoc thi dung dung vao cho.
            float step = Mathf.Min(MoveSpeed * Time.deltaTime, direction.magnitude);
            transform.position += direction.normalized * step;
            if (animator != null && animator.CurrentClip != "hurt") animator.Play("walk");
        }

        void Attack(Health target)
        {
            if (_attackCooldown > 0f) return;
            if (_definition.attackRate <= 0f) return;
            if (IsDisarmed) return;     // bi cuon trong Xoay Nuoc: dung yen, het cuon thi danh ngay

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

        /// <summary>
        /// Quai ban xa (`EnemyExtras`) gan vao day: frame ra tay chi THA vien dan, sat thuong tru luc vien dan cham muc tieu.
        /// null = can chien, tru mau ngay o frame cham.
        /// </summary>
        public System.Action<Health, float> FireShot;

        void LandHit()
        {
            // Day Roi: don truot (ca quai ban xa — khong tha dan).
            if (_pendingHit != null && !_pendingHit.IsDead && !RollMiss())
            {
                if (FireShot != null) FireShot(_pendingHit, Damage);
                else _pendingHit.TakeDamage(Damage, _health);
            }
            _pendingHit = null;
        }

        void OnHitFrame(string clipName)
        {
            if (!_dying && clipName == "attack") LandHit();
        }

        void OnDamaged(float _)
        {
            if (_dying || animator == null || IsFrozen) return;
            // Boss khong giat hurt / khong bi day lui: bi ca doi hinh danh don dap thi hurt + lui moi 0,6s cat
            // nhip danh lien tuc, boss gan nhu khong ra don. Choang / dong bang van khoa boss nhu thuong.
            if (IsBoss) return;
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
            Sfx.Play("enemy-die");
            if (IsPuppet) EndPuppet(snap: false);   // chet luc bi giat to: tinh la quai phe cu
            if (IsLaunched) Land();     // chet giua khong trung -> clip death nam tren dat
            RaiseKilled(this);   // doc an ky TRUOC khi go FX; an ky dua tren thoi gian, khong tren FX
            ClearStatusFx();
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
