using System.Collections.Generic;
using BuildBuild.Core;
using BuildBuild.Data;
using BuildBuild.Meta;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Hero tren san. O dat hero (`HeroSlot`) CHI la diem xuat hien dau wave — khong phai mot buc
    /// tuong mien sat thuong: hero co `Health` nhu quai va lau dai, quai thay hero gan duong di thi
    /// re sang danh (xem `Enemy`), het mau thi hero chet va bien mat cho toi dau wave sau.
    ///
    /// Hero TU DI TIM quai gan nhat tren ca san (nguoi dung chot 2026-09-15), khong dung o cho doi:
    ///   - quai trong `range`       -> dung tai cho danh;
    ///   - CAN CHIEN (range <= 1,5) -> duoi toi, khong day xich;
    ///   - TAM XA                   -> chi di toi khi vao tam, va chi khi cho dung ban do nam trong
    ///     `leashRange` tinh tu o (`_anchor`). Quai xa hon the -> di ve o. Het quai cung ve o.
    /// "Vao tam duoc trong day xich" do bang khoang cach O->quai <= leash + range, khong do tu cho
    /// hero dang dung: do tu cho dang dung thi hero cham mep day se tien-lui lien tuc.
    ///
    /// Linh trieu hoi cua Bonecaller (linh xuong, ky si tu than) CUNG la `Hero`: cung mau, animation,
    /// duoi quai va bi quai danh. Khac o `MakeSummon`: co the het han (`lifetime`), nhan hao quang
    /// cua ky si, va `_anchor` la cho trieu hoi chu khong phai o.
    ///
    /// Hero co `HasSkill` con dung them mot don ky nang, loai theo `SkillKind`:
    ///   - FrostZone   (Frostweaver) : vung bang, xem `FrostZone`;
    ///   - Petrify     (Cragfist)    : Hoa Da — bat dong `skillDuration` giay, mien sat thuong, phan
    ///     `skillPower` sat thuong nhan ve ke danh;
    ///   - ConeSweep   (Ironblade)   : chem quat truoc mat, dai `skillRadius`, sat thuong damage x `skillPower`;
    ///   - RapidFire   (Hawkfletch)  : `skillDuration` giay danh nhanh gap `skillPower` lan;
    ///   - SummonKnight(Bonecaller)  : goi ky si tu than (hoac hoi day mau neu no con song), xem `Necromancer`;
    ///   - Charge      (Ky si tu than): phi `skillRadius` unit VE PHIA CUM QUAI DONG NHAT (ke ca huong cheo), `skillPower`
    ///     sat thuong + choang `skillDuration` giay moi quai tren duong, roi danh tiep tai cho (khong ep ve cho trieu hoi).
    ///   - HookThrow   (Chainwarden) : Nem Moc keo quai ve roi bien hinh Cuong No, xem `Warden`.
    ///   - Rally       (Banneret)    : Hieu Lenh Xung Phong ca doi +sat thuong, xem `Banneret`.
    /// Ky nang chia HAI TANG (`SkillTier`):
    ///   - Chieu LON: THANH MANA tich theo thoi gian VA theo so quai an don thuong, day thi tu cast
    ///     roi ve 0. Danh dong quai thi day nhanh hon — co chu dich: don khong che phai den dung luc
    ///     dang bi vay. Co thanh tren dau nen nguoi choi doan truoc duoc.
    ///   - Chieu NHO: cooldown AN, khong thanh, khong an mana — chay ngam nhu mot nhip phu.
    /// Mot hero hien chi mang duoc MOT ky nang qua `SkillKind`; bo chieu rieng cua Bonecaller (noi tai +
    /// chieu thuong) nam o `Necromancer`, xem AD-17.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public partial class Hero : Unit
    {
        /// <summary>Moi hero dang song tren san — quai quet danh sach nay de tim muc tieu gan.</summary>
        public static readonly List<Hero> Alive = new List<Hero>();

        // Diem dan xuat hien: dau gay, tinh tu chan (pivot), o co art 1x — dung thi nhan `Battlefield.UnitScale`.
        static readonly Vector3 MuzzleOffset = new Vector3(0.15f, 0.3f, 0f) * Battlefield.UnitScale;

        /// <summary>Tam danh tu muc nay tro xuong la can chien: duoi quai khong day xich.</summary>
        const float MeleeRange = 1.5f;

        /// <summary>Toi day thi coi nhu da ve o — tranh buoc qua buoc lai quanh diem anchor.</summary>
        const float HomeTolerance = 0.05f;

        /// <summary>Mau phu luc Ban Nhanh Lien Hoan dang chay — chua co FX buff tren dau (GDD muc 9).</summary>
        static readonly Color RapidFireTint = new Color(1f, 0.72f, 0.55f);

        /// <summary>Mau phu linh xuong dang dung trong hao quang ky si — cho nguoi choi thay buff dang chay.</summary>
        static readonly Color BoneAuraTint = new Color(0.7f, 1f, 0.75f);

        /// <summary>Toc phi cua Charge (unit/giay) va nua be ngang duong phi theo truc y (GDD muc 3: ±0,6).</summary>
        const float ChargeSpeed = 5f;
        const float ChargeHalfWidth = 0.6f;

        /// <summary>Vo da Hoa Da: FX pivot giua canvas, day phien da thap hon tam 0,33 unit — nang len cho chan da trung chan hero.</summary>
        static readonly Vector3 PetrifyFxOffset = new Vector3(0f, 0.32f, 0f);

        /// <summary>Toc tach hai unit phe ta dang dung chong len nhau (xem `Battlefield.Separation`).</summary>
        const float SeparationRate = 6f;

        [SerializeField] SpriteAnimator animator;

        HeroDefinition _definition;
        Health _health;
        SpriteRenderer _renderer;
        Necromancer _necromancer;
        Necromancer _summoner;
        /// <summary>Unit an tien trien cua hero khac (`InheritProgression`) — ghi cong ve hero do.</summary>
        Hero _progressionOwner;
        Warden _warden;
        Banneret _banneret;
        Wukong _wukong;
        Stormcaller _stormcaller;
        Galemonk _galemonk;
        Bloodswordsman _blood;
        Soulbinder _soulbinder;
        GourdTaoist _gourd;
        InkScribe _ink;
        HongLien _hong;
        SaVe _save;
        KhoiLoiSu _khoi;
        Werewolf _werewolf;
        HauNghe _hauNghe;
        BachLocTien _bachLoc;
        /// <summary>Mau nen thay cho trang (phan than Wukong to nhat) — cac mau trang thai van de len.</summary>
        Color _baseTint = Color.white;
        Color _painted = Color.white;
        /// <summary>Level + sao + trang bi tu save (`Progression.Multipliers`), doc lai moi lan `Initialise`.</summary>
        float _healthMultiplier = 1f, _damageMultiplier = 1f, _rateMultiplier = 1f;
        int _stars;
        /// <summary>Wyrmspear: component cua CHINH no. Bach Dia Long: component cua Wyrmspear da goi no. Con lai null.</summary>
        DragonRider _dragonLink;
        Vector3 _anchor;
        float _cooldown;
        float _skillCooldown;
        float _mana;
        bool _casting;
        bool _castingSkill;
        bool _released;
        bool _dying;
        bool _petrified;
        float _petrifyUntil;
        float _rapidFireUntil;
        float _expiresAt;
        bool _charging;
        float _chargeLeft;
        Vector3 _chargeDirection;
        readonly HashSet<Unit> _chargeHits = new HashSet<Unit>();
        readonly Knockback _knockback = new Knockback();
        Blocker _blocker;
        SpriteRenderer _petrifyRenderer;
        float _bodyRadius;
        float _bodyHeight;

        public HeroDefinition Definition => _definition;
        public override Health Health => _health;
        public Hero ContributionOwner => _summoner != null ? _summoner.GetComponent<Hero>()
            : _dragonLink != null ? _dragonLink.Rider
            : _progressionOwner != null ? _progressionOwner : this;

        /// <summary>Nua be ngang than tren mat dat (world) — quai do tam toi MEP than hero.</summary>
        public override float BodyRadius => _bodyRadius;

        /// <summary>Dinh dau tinh tu chan (world) — cho dat thanh mau.</summary>
        public override float BodyHeight => _bodyHeight;

        /// <summary>Luat chan quai cua unit nay (giu toi da `HeroDefinition.maxHeld` quai) — xem `Blocker`.</summary>
        public Blocker Blocker => _blocker;

        public override bool IsDying => _dying;

        /// <summary>
        /// Chu dong tien danh (Chinh Phat, ca hai phe): tam xa bo day xich ve o, het doi thu gan thi di toi doi thu gan nhat
        /// ke ca thanh dich. Tran thuong de false. Dat SAU `Initialise`; linh trieu hoi an theo chu.
        /// </summary>
        public bool Aggressive { get; set; }

        /// <summary>Long Giap (Wyrmspear) mien troi / keo / hat tung nhu mien choang.</summary>
        protected override bool Unmovable => _dragonLink != null && _dragonLink.Unshakable(this);

        /// <summary>Bi keo / hat tung: bo day lui va dang phi.</summary>
        protected override void OnDisplaced()
        {
            _knockback.Reset();
            _charging = false;
        }

        /// <summary>0..1 cho thanh mana ve tren dau (`StatBar`).</summary>
        public float ManaFraction =>
            _definition == null || _definition.manaMax <= 0f ? 0f : Mathf.Clamp01(_mana / _definition.manaMax);

        /// <summary>Chi chieu LON moi tich mana va moi co thanh tren dau.</summary>
        bool UsesMana => _definition.HasSkill && _definition.skillTier == SkillTier.Ultimate;

        /// <summary>Chieu lon doi thanh mana day; chieu nho doi cooldown an chay het.</summary>
        bool SkillReady => _definition.skillTier == SkillTier.Ultimate
            ? _mana >= _definition.manaMax
            : _skillCooldown <= 0f;

        bool IsRanged => _definition.range > MeleeRange;

        /// <summary>Hero / linh ban xa — moi danh trung quai bay cao (`Enemy.FliesHigh`).</summary>
        public bool IsRangedUnit => _definition != null && IsRanged;
        bool RapidFire => Time.time < _rapidFireUntil;
        bool Raging => _warden != null && _warden.Raging;

        /// <summary>Tam danh hien tai: Chainwarden Cuong No dung `rageRange` (hai xich dai), Long Nhan dung `dragonmanRange`.</summary>
        float AttackRange => Raging ? _warden.RageRange
            : _dragonLink != null ? _dragonLink.AttackRange(this, _definition.range) : _definition.range;

        public float CurrentAttackRange => AttackRange;
        public bool FacesRight => FacingRight;

        /// <summary>Sat thuong goc da cong level + sao + trang bi; chua nhan hao quang / Cuong No.</summary>
        float BaseDamage => _definition.damage * _damageMultiplier;

        /// <summary>Mau toi da da cong level + sao + trang bi.</summary>
        // Chinh Phat: moi unit hai phe them mau (`ConquestMatch.HealthScale`) cho tran keo dai — doi xung nen khong lech can bang.
        public float MaxHealth => _definition.maxHealth * _healthMultiplier * (ConquestMatch.Current != null ? ConquestMatch.HealthScale : 1f);

        /// <summary>Noi tai / chieu phu mo theo sao; danh thuong + chieu cuoi luon mo.</summary>
        public bool PassiveUnlocked => _stars >= _definition.passiveStar;
        public bool MinorUnlocked => _stars >= _definition.minorStar;
        public bool SecondMinorUnlocked => _stars >= _definition.secondMinorStar;
        /// <summary>Chieu phu thu ba — chi bo SSR 3 chieu phu (Hau Nghe, Bach Loc Tien) co.</summary>
        public bool ThirdMinorUnlocked => _stars >= _definition.thirdMinorStar;

        /// <summary>Sat thuong mot don thuong luc nay (da nhan hao quang / Cuong No).</summary>
        public float AttackDamage
        {
            get
            {
                AuraMultipliers(out float damage, out _);
                return BaseDamage * damage * (1f - AweWeaken);
            }
        }

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _health = GetComponent<Health>();
            _health.Damaged += OnDamaged;
            gameObject.AddComponent<CombatFeedback>().Init(ally: true, () => _bodyHeight);
            _health.Blocked += OnBlocked;
            _health.ShieldBroken += OnShieldBroken;
            _health.Died += OnDied;
            _health.CheatDeath = CheatDeath;
        }

        // Chan cai chet o `Health` (truoc `KilledBy`/`Died`) thay vi trong `OnDied`.
        bool CheatDeath()
        {
            if (_dying) return false;
            // The Than (Wukong): mot phan than len lam ban the thay vi chet.
            if (_wukong != null && _wukong.TrySubstitute()) return true;
            // Dong Sinh Cong Tu (Luu Bi): anh em ket nghia nga lan dau moi wave duoc keo day.
            return DaoVienBrother.TryRevive(this);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Alive.Add(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Alive.Remove(this);
            Unit.Killed -= OnEnemyKilled;     // Vo Bang — Hero.Passive.cs
            Health.AnyDamaged -= OnAnyDamaged;  // Bao Tu — Hero.Passive.cs
            ClearHeroN();         // Hero.HeroN.cs
        }

        /// <param name="equipment">Chi hero dat tu doi hinh (`HeroBoard`) truyen vao; null = khong tinh trang bi.</param>
        /// <param name="side">`Npc` = hero cua lanh chua (Chinh Phat): nhin sang trai, nham phe Player.</param>
        public void Initialise(HeroDefinition definition, EquipmentConfig equipment = null, Faction side = Faction.Player)
        {
            _definition = definition;
            Side = side;
            Aggressive = false;
            Progression.Multipliers(definition.unitId, equipment, out _healthMultiplier, out _damageMultiplier, out _rateMultiplier);
            _stars = Progression.Stars(definition.unitId);
            // Chinh Phat: hero / linh NPC lay suc manh cua thanh da chon tren ban do thay save cua minh.
            var rival = side == Faction.Npc ? ConquestMatch.Current?.Target : null;
            if (rival != null)
            {
                _healthMultiplier = _damageMultiplier = rival.strength;
                _rateMultiplier = 1f;
                _stars = rival.stars;
            }
            // Vi tri luc Initialise la o dat hero — moi khoang cach ve sau deu do tu day.
            _anchor = transform.position;
            _dying = false;
            _casting = false;
            _castingSkill = false;
            _petrified = false;
            _charging = false;
            _rapidFireUntil = 0f;
            _expiresAt = 0f;
            _summoner = null;
            _progressionOwner = null;
            _dragonLink = null;
            _health.Invulnerable = false;
            _mana = 0f;
            _health.ResetTo(MaxHealth);
            _baseTint = Color.white;
            _renderer.color = Color.white;
            _knockback.Reset();
            ResetStatus();
            if (_blocker == null) _blocker = GetComponent<Blocker>();
            if (_blocker == null) _blocker = gameObject.AddComponent<Blocker>();
            // Cai Nguc (Chainwarden) la noi tai: chua mo thi giu 3 quai nhu moi unit (GDD muc 2) va khong Xich.
            _blocker.Initialise(PassiveUnlocked ? definition.maxHeld : Mathf.Min(definition.maxHeld, 3),
                PassiveUnlocked ? definition.heldDamageBonus : 0f);
            InitialisePassive();    // Hero.Passive.cs
            InitialiseHeroN();    // Hero.HeroN.cs

            transform.localScale = Vector3.one * Battlefield.UnitScale;
            Battlefield.MeasureBody(definition.animations, out _bodyRadius, out _bodyHeight);
            Paint(_baseTint);

            // Hero nhin ve phia doi thu: phe ta sang phai, phe NPC sang trai.
            FacingRight = Side == Faction.Player;

            if (animator != null)
            {
                animator.Speed = 1f;
                animator.Bind(definition.animations);
                animator.ClipFinished -= OnClipFinished;    // tranh dang ky hai lan neu Initialise goi lai
                animator.ClipFinished += OnClipFinished;
                animator.HitFrame -= OnHitFrame;
                animator.HitFrame += OnHitFrame;
            }

            // Chainwarden: Quat Xich (cooldown an) + so cua Nem Moc — xem `Warden`.
            if (definition.warden != null)
            {
                _warden = GetComponent<Warden>();
                if (_warden == null) _warden = gameObject.AddComponent<Warden>();
                _warden.Initialise(this, definition.warden);
            }

            // Banneret: don thuong buff dong minh, Quan Ky, Cam Co, Hieu Lenh — xem `Banneret`.
            if (definition.banner != null)
            {
                _banneret = GetComponent<Banneret>();
                if (_banneret == null) _banneret = gameObject.AddComponent<Banneret>();
                _banneret.Initialise(this, definition.banner);
            }

            // Wukong: The Than, Nho Long, Toan Phong Con, Nhu Y Thien Tru — xem `Wukong`.
            if (definition.wukong != null)
            {
                _wukong = GetComponent<Wukong>();
                if (_wukong == null) _wukong = gameObject.AddComponent<Wukong>();
                _wukong.Initialise(this, definition.wukong);
            }

            // Stormcaller: Xich Loi, Qua Tai, Cot Thu Loi, Loi Bo, Loi Bao — xem `Stormcaller`.
            if (definition.stormcaller != null)
            {
                _stormcaller = GetComponent<Stormcaller>();
                if (_stormcaller == null) _stormcaller = gameObject.AddComponent<Stormcaller>();
                _stormcaller.Initialise(this, definition.stormcaller);
            }

            // Galemonk: Chuong Phong, Thuan Phong, Phong Tao, Long Quyen — xem `Galemonk`.
            if (definition.galemonk != null)
            {
                _galemonk = GetComponent<Galemonk>();
                if (_galemonk == null) _galemonk = gameObject.AddComponent<Galemonk>();
                _galemonk.Initialise(this, definition.galemonk);
            }

            // Huyet Kiem Khach: Huyet Y, Hien Huyet, Phan Huyet, Toai Kiem Quy Huyet — xem `Bloodswordsman`.
            if (definition.bloodswordsman != null)
            {
                _blood = GetComponent<Bloodswordsman>();
                if (_blood == null) _blood = gameObject.AddComponent<Bloodswordsman>();
                _blood.Initialise(this, definition.bloodswordsman);
            }

            // Thuat Si Ket Hon: Chuyen Ket, Tam Hon Ket, Giat Hon, Dai Hon Khe — xem `Soulbinder`.
            if (definition.soulbinder != null)
            {
                _soulbinder = GetComponent<Soulbinder>();
                if (_soulbinder == null) _soulbinder = gameObject.AddComponent<Soulbinder>();
                _soulbinder.Initialise(this, definition.soulbinder);
            }

            // Dao Si Ho Lo: Khi, Thu Ta, Luyen Khi Pha, Can Khon Khai Ho — xem `GourdTaoist`.
            if (definition.gourdTaoist != null)
            {
                _gourd = GetComponent<GourdTaoist>();
                if (_gourd == null) _gourd = gameObject.AddComponent<GourdTaoist>();
                _gourd.Initialise(this, definition.gourdTaoist);
            }

            // Ngu But Su: Tam But, Hoanh But, Tran Son An, Hoa Long Diem Tinh — xem `InkScribe`.
            if (definition.inkScribe != null)
            {
                _ink = GetComponent<InkScribe>();
                if (_ink == null) _ink = gameObject.AddComponent<InkScribe>();
                _ink.Initialise(this, definition.inkScribe);
            }

            // Vu Nu Hong Lien: Gieo Sen, Song Phien Vu, Lien Tam Dan, Hong Lien Dai Vu — xem `HongLien`.
            if (definition.hongLien != null)
            {
                _hong = GetComponent<HongLien>();
                if (_hong == null) _hong = gameObject.AddComponent<HongLien>();
                _hong.Initialise(this, definition.hongLien);
            }

            // Sa Ve: Tu Sa, Tram Sa Quyen, Boi Thach, Sa Thanh Tran Tuyen — xem `SaVe`.
            if (definition.saVe != null)
            {
                _save = GetComponent<SaVe>();
                if (_save == null) _save = gameObject.AddComponent<SaVe>();
                _save.Initialise(this, definition.saVe);
            }

            // Khoi Loi Su: roi MocKhoi, Ho Chu, Khoi Vuong Giang Tran — xem `KhoiLoiSu`.
            if (definition.khoiLoiSu != null)
            {
                _khoi = GetComponent<KhoiLoiSu>();
                if (_khoi == null) _khoi = gameObject.AddComponent<KhoiLoiSu>();
                _khoi.Initialise(this, definition.khoiLoiSu);
            }

            // Nguoi Soi: Huyet Thuc, Tru Nguyet, Ngoam Ha, Nguyet Bien — xem `Werewolf`.
            if (definition.werewolf != null)
            {
                _werewolf = GetComponent<Werewolf>();
                if (_werewolf == null) _werewolf = gameObject.AddComponent<Werewolf>();
                _werewolf.Initialise(this, definition.werewolf);
            }

            // Hau Nghe: Nhat An, Nhat Luan Tran Tuyen, Cuu Tien Quy Nhat, Nhat Boc Lien Hoan, Xa Nhat — xem `HauNghe`.
            if (definition.hauNghe != null)
            {
                _hauNghe = GetComponent<HauNghe>();
                if (_hauNghe == null) _hauNghe = gameObject.AddComponent<HauNghe>();
                _hauNghe.Initialise(this, definition.hauNghe);
            }

            // Bach Loc Tien: Sinh Khi, Tam Mam, Hoan Diep, Loc Anh, Bach Loc Hoi Xuan — xem `BachLocTien`.
            if (definition.bachLocTien != null)
            {
                _bachLoc = GetComponent<BachLocTien>();
                if (_bachLoc == null) _bachLoc = gameObject.AddComponent<BachLocTien>();
                _bachLoc.Initialise(this, definition.bachLocTien);
            }

            // Dao Vien: Ket Nghia + bo chieu Luu Bi / Quan Vu / Truong Phi — Hero.DaoVien.cs.
            InitialiseBrother();

            // Wyrmspear: Nghich Lan, chieu thuong, bien hinh Long Nhan + Bach Dia Long — xem `DragonRider`.
            if (definition.dragonRider != null)
            {
                _dragonLink = GetComponent<DragonRider>();
                if (_dragonLink == null) _dragonLink = gameObject.AddComponent<DragonRider>();
                _dragonLink.Initialise(this, definition.dragonRider);
            }

            if (definition.summoner != null)
            {
                _necromancer = GetComponent<Necromancer>();
                if (_necromancer == null) _necromancer = gameObject.AddComponent<Necromancer>();
                _necromancer.Initialise(this, definition.summoner);
            }

            InitialiseSupport();    // choang, hoi mau, FX buff — Hero.Support.cs

            StatBar.AttachHealth(this);
            // Chieu nho co cooldown AN — dung nghia den la khong ve gi len man hinh.
            if (UsesMana) StatBar.AttachMana(this);
        }

        /// <summary>
        /// Danh dau day la linh trieu hoi cua <paramref name="owner"/>. Goi SAU `Initialise`.
        /// <paramref name="lifetime"/> &lt;= 0 = song toi khi bi giet hoac het wave.
        /// </summary>
        public void MakeSummon(Necromancer owner, float lifetime)
        {
            _summoner = owner;
            _expiresAt = lifetime > 0f ? Time.time + lifetime : 0f;
        }

        /// <summary>
        /// Dau phe NPC: phu do len than. Moi mau buff/trang thai deu NHAN voi mau nay (qua `Paint`) chu khong
        /// thay the no — buff doi mau khong lam hai phe trong nhu nhau.
        /// </summary>
        static readonly Color NpcTint = new Color(1f, 0.55f, 0.55f);

        void Paint(Color color)
        {
            _painted = color;
            _renderer.color = BodyColor(Side == Faction.Npc ? color * NpcTint : color);
        }

        /// <summary>Ve lai mau than dang dung voi anh sang moi (xem `Unit.RepaintBody`) — khong doi mau trang thai.</summary>
        public override void RepaintBody() => Paint(_painted);

        /// <summary>Doi mau nen cua unit (phan than Wukong). Goi SAU `Initialise`.</summary>
        public void SetBaseTint(Color tint)
        {
            _baseTint = tint;
            Paint(tint);
        }

        /// <summary>
        /// Unit trieu hoi (linh xuong, ky si tu than, Bach Dia Long) khong co id rieng: an level + sao cua
        /// <paramref name="owner"/> (khong an trang bi, tru khi <paramref name="withGear"/> — phan than Wukong). Sat thuong /
        /// do don cua unit ghi cong ve <paramref name="owner"/>. Goi SAU `Initialise`, day lai mau.
        /// </summary>
        public void InheritProgression(Hero owner, bool withGear = false)
        {
            _progressionOwner = owner;
            Side = owner.Side;
            Aggressive = owner.Aggressive;
            Paint(_baseTint);
            FacingRight = Side == Faction.Player;
            if (withGear)
            {
                _healthMultiplier = owner._healthMultiplier;
                _damageMultiplier = owner._damageMultiplier;
                _rateMultiplier = owner._rateMultiplier;
            }
            else if (owner.Side == Faction.Npc) _healthMultiplier = _damageMultiplier = owner._damageMultiplier;   // hero NPC: `strength` cua thanh
            else _healthMultiplier = _damageMultiplier = Progression.StatMultiplier(owner._definition.unitId);
            _health.ResetTo(MaxHealth);
        }

        /// <summary>Danh dau day la Bach Dia Long cua <paramref name="rider"/>; <paramref name="anchor"/> = cho ve khi het quai. Goi SAU `Initialise`.</summary>
        public void MakeDragonMount(DragonRider rider, Vector3 anchor)
        {
            _dragonLink = rider;
            _anchor = anchor;
        }

        void Update()
        {
            if (_definition == null || _dying) return;

            // Linh noi tai het 10s: chet binh thuong (chay death) de nguoi choi thay no tan.
            if (_expiresAt > 0f && Time.time >= _expiresAt)
            {
                _health.Invulnerable = false;
                _health.TakeDamage(_health.Current, _health);
                return;
            }

            _cooldown -= Time.deltaTime;
            _skillCooldown -= Time.deltaTime;
            // Chua bam bat dau wave thi thanh dung yen: mana theo thoi gian chi chay trong luc danh.
            // Dang Ban Nhanh thi khoa thanh: 3x don danh ma van tich mana thi chieu gan nhu lien hoan (2026-09-15).
            // Cuong No van tich: bien hinh vinh vien, khoa thanh la mat chieu lon ca tran (nguoi dung 2026-09-29).
            // Nguoi Soi da hoa soi: go thanh mana, khong tich tiep (kit 2026-10-04).
            if (UsesMana && WaveManager.Fighting && !RapidFire && !_health.HealBlocked
                && (_werewolf == null || !_werewolf.Transformed))
                _mana = Mathf.Min(_definition.manaMax, _mana + _definition.manaPerSecond * ManaRate * Time.deltaTime);

            UpdateShield();     // khien ao Den Canh het han — Hero.Support.cs
            UpdateGuard();      // vung Cam Co cua Banneret — Hero.Support.cs
            UpdateHeroN();      // bay Kobold, an Yeu — Hero.HeroN.cs
            UpdatePurify();     // Thanh Tay cua Chuong Bac — Hero.LowTrio.cs
            UpdateNewSR();      // Qua Den Xe Giap cua Tho San — Hero.NewSR.cs

            // Hoa Da: bat dong la cai GIA cua mien sat thuong — khong di, khong danh (GDD muc 3).
            if (_petrified)
            {
                if (Time.time < _petrifyUntil) return;
                EndPetrify();
            }

            // Tuong Rune: dung yen sau khien toi khi tuong tan — Hero.HeroN.cs.
            if (_bracedUntil > 0f)
            {
                if (Braced) return;
                _bracedUntil = 0f;
                if (animator != null) animator.Play("idle");
            }

            // Bi hat lui (Vo Canh Cuong Phong cua Dreadwyrm): truot het quang roi moi lam gi khac — Hero.Support.cs.
            if (UpdateShove()) return;

            // Chieu cua hero phe kia: Xich co han / pha giap / Long Uy dat lai moi frame; bi hat tung / keo thi khong di khong danh.
            _health.Vulnerability = TimedChainBonus + StatusVulnerability;
            if (UpdateLaunch() || UpdatePull()) return;

            // Choang (boss Chan Dia): dung han, animation dung hinh — Hero.Support.cs.
            if (UpdateStunned()) return;
            if (_blood != null && _blood.Busy) return;
            if (Time.time < _seedRainUntil) return;
            if (_bladeStorming) return;     // Bao Dao (Song Dao) — Hero.LowTrio.cs

            bool rapid = RapidFire;
            AuraMultipliers(out float auraDamage, out _, buffs: false);
            Paint(Raging ? _warden.RageTint
                : rapid && !HasBuffFx ? RapidFireTint : auraDamage > 1f ? BoneAuraTint : _baseTint);
            if (animator != null) animator.Speed = rapid ? _definition.skillPower : 1f;

            if (_charging)
            {
                UpdateCharge();
                return;
            }

            // Bi day lui: frame nay chi lui (cooldown van chay o tren).
            if (!IsRooted && _knockback.Step(transform)) return;

            // Hai unit phe ta khong dung chong khit mot diem (hai hero can chien cung duoi mot con quai).
            transform.position += Battlefield.Separation(this, Alive) * Mathf.Min(1f, SeparationRate * Time.deltaTime);

            if (_casting) return;

            // Hero hoi mau (Vespera) nham dong minh, khong nham quai.
            if (_definition.healsAllies)
            {
                UpdateSupport(WoundedAlly(inRangeOnly: false));
                return;
            }

            // Banneret buff dong minh, khong nham quai.
            if (_banneret != null)
            {
                UpdateSupport(_banneret.FollowTarget());
                return;
            }

            // Phi xong KHONG ep di bo ve cho trieu hoi: ky si can chien quay lung di ve thi bi quai danh ma khong danh
            // lai (nguoi dung bat 2026-09-15). Het quai thi nhanh `target == null` ben duoi tu dua ve.
            var target = FindAttackTarget();
            if (target == null)
            {
                ReturnHome();
                return;
            }

            // Ngoai tam ban: buoc toi (tam xa: chi khi vao tam duoc trong day xich). Khong vua di vua
            // danh — moi frame dung mot hanh dong, de chuoi animation doc ra dung viec hero dang lam.
            if (!InRange(target))
            {
                if (CanReach(target)) MoveToward(target.AimPoint(transform.position), leashed: !Aggressive && !FightingAlly(target));
                else ReturnHome();
                return;
            }

            Face(target.AimPoint(transform.position).x);

            // Bi cuon trong Xoay Nuoc cua Ngu Nhan phe kia (Chinh Phat): dung yen, khong ra don.
            if (IsDisarmed)
            {
                PlayIdle();
                return;
            }

            // Ky nang di truoc don thuong khi ca hai cung san sang: chu ky ky nang dai hon nhieu,
            // nhuong nhip cho mot phat dan le thi phi ca chu ky.
            // Khoi Loi Su: chieu cuoi can roi song + co dich trong tam roi — chua du thi giu mana, danh thuong tiep.
            // Roi Go (attackRate 0) khong tu danh: chi dam theo lenh chu (`KhoiLoiSu.Strike`), khong thi moi lan
            // trien khai roi tu dam them mot hit (dong ho don bat dau o 0).
            if (_definition.HasSkill && SkillReady && (_khoi == null || _khoi.CanUltimate)) BeginCast(skill: true);
            else if (_cooldown <= 0f && _definition.attackRate > 0f) BeginCast(skill: false);
            else PlayIdle();
        }

        /// <summary>
        /// Can chien do tren MAT DAT (`Battlefield.GroundDistance`, chieu sau nhan doi): hai than phai dung cung hang
        /// sau moi cham nhau, va `range` la tam voi tinh tu chan hero toi MEP THAN quai (hitbox), khong toi chan
        /// quai — Ashfang be ngang gap ruoi Goblin. Tam xa van do thang tren man — dan bay qua khong khi.
        /// </summary>
        bool InRange(Unit target)
        {
            var position = target.AimPoint(transform.position);
            // Tuong thanh: than hero bi kep cach vach dung `_bodyRadius` (LateUpdate), tinh tu chan thi con to hon tam
            // can chien dung sat tuong ma khong bao gio "vao tam".
            float reach = target is Castle ? AttackRange + _bodyRadius : AttackRange;
            return IsRanged
                ? (position - transform.position).sqrMagnitude <= AttackRange * AttackRange
                : Battlefield.GroundDistance(position, transform.position) - target.BodyRadius <= reach;
        }

        // Order theo chan sau khi moi dich chuyen trong frame da xong; vo da ve ngay tren than.
        void LateUpdate()
        {
            // Duoi quai / bi day lui / tach doi hinh / phi: khong buoc nao duoc xuyen qua thanh hay ra khoi san.
            // Long Nhan dang bay (Thien Dia Long Giang) thi o tren khong: khong kep vao san, ve de len moi unit.
            bool airborne = IsLaunched || (_dragonLink != null && _dragonLink.Airborne(this));
            if (!_dying && !airborne) transform.position = Battlefield.Clamp(transform.position, _bodyRadius, clampRight: true);
            _renderer.sortingOrder = airborne ? SortOrder.Air : SortOrder.ForUnit(this);
            if (_petrifyRenderer != null) _petrifyRenderer.sortingOrder = _renderer.sortingOrder + 1;
            if (_shieldRenderer != null) _shieldRenderer.sortingOrder = _renderer.sortingOrder + 1;
            SortStatusFx(_renderer.sortingOrder);
        }

        /// <summary>Can chien luon duoi; tam xa chi di khi cho dung ban duoc nam trong day xich tinh tu o.</summary>
        bool CanReach(Vector3 destination)
        {
            if (!IsRanged || Aggressive) return true;
            float reach = _definition.leashRange + _definition.range;
            var fromAnchor = destination - _anchor;
            fromAnchor.z = 0f;
            return fromAnchor.sqrMagnitude <= reach * reach;
        }

        /// <summary>
        /// Quai dang danh nhau voi unit phe ta khac (bi giu, hoac dang nham unit khac): can chien dang tank thi tam xa
        /// phai theo len xa sat thuong, khong dung o o vi day xich (nguoi dung 2026-09-29).
        /// </summary>
        bool FightingAlly(Unit enemy) =>
            enemy.Holder != null || (enemy.EngagedHero != null && enemy.EngagedHero != this);

        bool CanReach(Unit enemy) => FightingAlly(enemy) || CanReach(enemy.AimPoint(transform.position));

        /// <summary>
        /// Can chien: quai gan nhat. Tam xa: con gan nhat DANG trong tam truoc (dung tai cho ban), khong co thi con gan
        /// nhat toi duoc — con gan nhat ngoai day xich khong duoc keo hero ve o trong khi con khac ban duoc.
        /// </summary>
        Unit FindAttackTarget()
        {
            if (!IsRanged) return FindPriorityTarget(float.PositiveInfinity);
            Unit inRange = null, reachable = null;
            float reachableSqr = float.PositiveInfinity;
            float inRangeScore = float.NegativeInfinity;
            foreach (var enemy in Unit.Hostiles(Side))
            {
                if (enemy == null || enemy.IsDying || !enemy.TargetableBy(this)) continue;
                float sqr = (enemy.AimPoint(transform.position) - transform.position).sqrMagnitude;
                if (InRange(enemy))
                {
                    // Nut "uu tien muc tieu" (GDD 10.1) doi cach CHON trong so da trong tam; mac dinh Nearest
                    // cho ra dung ket qua cu (diem = -khoang cach, lon hon = gan hon).
                    float score = Targeting.Score(enemy, sqr);
                    if (score > inRangeScore) { inRangeScore = score; inRange = enemy; }
                }
                else if (sqr < reachableSqr && CanReach(enemy)) { reachableSqr = sqr; reachable = enemy; }
            }
            return inRange != null ? inRange : reachable;
        }

        /// <summary>
        /// Chon muc tieu danh thuong cho hero CAN CHIEN theo `Targeting.Current` trong tam `maxRange`
        /// (truoc day luon la con gan nhat — xem `FindTarget`).
        /// </summary>
        Unit FindPriorityTarget(float maxRange)
        {
            Unit best = null;
            float bestSqr = maxRange * maxRange;
            float bestScore = float.NegativeInfinity;
            foreach (var enemy in Unit.Hostiles(Side))
            {
                if (enemy == null || enemy.IsDying || !enemy.TargetableBy(this)) continue;
                float sqr = (enemy.AimPoint(transform.position) - transform.position).sqrMagnitude;
                if (sqr > bestSqr) continue;
                float score = Targeting.Score(enemy, sqr);
                if (score > bestScore) { bestScore = score; best = enemy; }
            }
            return best;
        }

        /// <summary>
        /// Buoc ve phia <paramref name="destination"/>; tam xa <paramref name="leashed"/> khong buoc ra ngoai `leashRange`
        /// tinh tu o — cham mep thi truot theo mep (truoc: dung im giua duong vi buoc thang toi quai bi chan).
        /// </summary>
        void MoveToward(Vector3 destination, bool leashed = true)
        {
            var direction = destination - transform.position;
            direction.z = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;

            Face(destination.x);
            // Bi troi: dung tai cho nhin muc tieu.
            if (IsRooted)
            {
                PlayIdle();
                return;
            }
            var next = transform.position + direction.normalized * (_definition.moveSpeed * SpeedFactor * Time.deltaTime);
            if (IsRanged && leashed)
            {
                var fromAnchor = next - _anchor;
                fromAnchor.z = 0f;
                if (fromAnchor.sqrMagnitude > _definition.leashRange * _definition.leashRange)
                    next = _anchor + fromAnchor.normalized * _definition.leashRange;
            }

            transform.position = next;
            if (animator != null) animator.Play("walk");
        }

        /// <summary>Di ve o; toi noi thi dung idle nhin sang phai (phia quai den). Tra ve true khi da o o.</summary>
        bool ReturnHome()
        {
            if (IsRooted)
            {
                PlayIdle();
                return false;
            }
            var toHome = _anchor - transform.position;
            toHome.z = 0f;
            float step = _definition.moveSpeed * SpeedFactor * Time.deltaTime;
            if (toHome.magnitude <= Mathf.Max(step, HomeTolerance))
            {
                transform.position = _anchor;
                FacingRight = Side == Faction.Player;
                PlayIdle();
                return true;
            }

            Face(_anchor.x);
            transform.position += toHome.normalized * step;
            if (animator != null) animator.Play("walk");
            return false;
        }

        // Chi doi walk -> idle; khong cat ngang hurt/attack dang chay.
        void PlayIdle()
        {
            if (animator != null && animator.CurrentClip == "walk") animator.Play("idle");
        }

        void BeginCast(bool skill)
        {
            AuraMultipliers(out _, out float rateMultiplier);

            // Tru o DAU cu cast: thanh voi di ngay luc cay gay vung len, khop voi cai nguoi choi thay.
            // Dat ca hai dong ho — tang nao khong dung thi dong ho cua no khong ai doc.
            if (skill)
            {
                _mana = 0f;
                _skillCooldown = _definition.skillCooldown;
            }
            // Cham (Coc Suong) cham ca toc danh, khong chi buoc chay.
            else _cooldown = 1f / Mathf.Max(0.01f,
                _definition.attackRate * _rateMultiplier * (RapidFire ? _definition.skillPower : 1f) * rateMultiplier
                * (IsSlowed ? 1f - Status.Power(StatusId.Slow) : 1f));

            if (skill && _definition.skillKind == SkillKind.Petrify)
            {
                BeginPetrify();
                return;
            }
            if (skill && _definition.skillKind == SkillKind.Charge)
            {
                BeginCharge();
                return;
            }

            // Thien Dia Long Giang: chon muc tieu + cat canh ngay tu o 1 cua clip (don ra o o cham dat).
            if (skill && _definition.skillKind == SkillKind.DragonRider && _dragonLink != null) _dragonLink.BeginUltimate();

            _casting = true;
            _released = false;
            _castingSkill = skill;
            // Clip "skill" roi ve "attack" neu unit khong co clip rieng (Frostweaver dung chung folder cast).
            if (animator != null) animator.Play(skill ? "skill" : "attack", restart: true);
            else
            {
                FinishCast();
                _casting = false;
            }
        }

        // Mien sat thuong tinh tu DAU cu hoa da, khong doi het clip: nguoi choi thay than hoa da la da an toan.
        void BeginPetrify()
        {
            _petrified = true;
            _petrifyUntil = Time.time + _definition.skillDuration;
            _health.Invulnerable = true;
            // Vo da moc len (skillIntroFrames) roi lap (skillFrames) dung `skillDuration` — tu huy dung luc het hoa da.
            var shell = OneShotFx.SpawnLoop(transform, PetrifyFxOffset, _definition.skillFrames, _definition.skillDuration,
                SortOrder.ForUnit(this) + 1, _definition.skillIntroFrames);
            _petrifyRenderer = shell != null ? shell.GetComponent<SpriteRenderer>() : null;
            if (animator != null) animator.Play("skill", restart: true);   // het clip giu frame cuoi (dong da)
        }

        void EndPetrify()
        {
            _petrified = false;
            _health.Invulnerable = false;
            if (animator != null) animator.Play("idle", restart: true);
        }

        // Phan don khi dang hoa da: tinh tren sat thuong GOC ke danh gay ra (hero khong mat mau).
        void OnBlocked(float amount, Health source)
        {
            if (!_petrified || source == null || source.IsDead) return;
            source.TakeDamage(amount * _definition.skillPower, _health, kind: DamageKind.Reflected);
        }

        void BeginCharge()
        {
            _charging = true;
            _chargeLeft = _definition.skillRadius;
            // Ngam cum quai dong nhat trong tam phi (nguoi dung 2026-09-15: quai o huong cheo thi phi cheo), khong co
            // thi phi thang theo huong dang nhin.
            var facing = new Vector3(FacingRight ? 1f : -1f, 0f, 0f);
            var aim = DensestCluster(_definition.skillRadius, ChargeHalfWidth * 2f);
            var direction = aim.HasValue ? aim.Value - transform.position : facing;
            direction.z = 0f;
            _chargeDirection = direction.sqrMagnitude < 0.0001f ? facing : direction.normalized;
            Face(transform.position.x + _chargeDirection.x);
            _chargeHits.Clear();
            // Bui tung o chan (Warhorn) — art ve theo huong art goc nen lat theo renderer. Bui ve chung khung 128px voi than
            // (day bui sat chan) nhung pivot tam o: nang len bang khoang tam o -> pivot chan cua than, khong thi lot duoi dat.
            // Lay pivot tu clip skill (charge) chu khong phai sprite dang hien: walk Warhorn la canvas 256, pivot khac.
            var dust = _definition.skillFrames;
            var body = _definition.animations != null && _definition.animations.skill.IsValid
                ? _definition.animations.skill.frames[0] : _renderer.sprite;
            float dustLift = dust != null && dust.Length > 0 && body != null
                ? (dust[0].rect.height * 0.5f - body.pivot.y) / body.pixelsPerUnit * Battlefield.UnitScale
                : 0f;
            OneShotFx.Spawn(transform.position + new Vector3(0f, dustLift, 0f), dust, _renderer.flipX, SortOrder.ForUnit(this) + 1,
                Battlefield.UnitScale);
            if (animator != null) animator.Play("skill", restart: true);
        }

        /// <summary>
        /// Phi theo `_chargeDirection`. Moi quai cham phai (doc duong phi &lt;= hai nua than, ngang duong phi &lt;= 0,6) an don MOT lan:
        /// `skillPower` sat thuong + choang `skillDuration` giay, boss chi bi choang mot nua.
        /// </summary>
        void UpdateCharge()
        {
            float step = Mathf.Min(ChargeSpeed * Time.deltaTime, _chargeLeft);
            transform.position += _chargeDirection * step;
            _chargeLeft -= step;

            foreach (var enemy in Unit.Hostiles(Side).ToArray())
            {
                if (enemy == null || enemy.IsDying || _chargeHits.Contains(enemy)) continue;
                var offset = enemy.AimPoint(transform.position) - transform.position;
                float along = offset.x * _chargeDirection.x + offset.y * _chargeDirection.y;
                float across = offset.x * _chargeDirection.y - offset.y * _chargeDirection.x;
                if (Mathf.Abs(along) > _bodyRadius + enemy.BodyRadius || Mathf.Abs(across) > ChargeHalfWidth) continue;
                _chargeHits.Add(enemy);
                enemy.ApplyStun(enemy.IsBoss ? _definition.skillDuration * 0.5f : _definition.skillDuration, _definition.stunFrames);
                enemy.Health.TakeDamage(_definition.skillPower * BaneMultiplier(enemy), _health);
                if (_definition.knockbackOnHit) enemy.PushBack(_chargeDirection.x >= 0f ? 1f : -1f);
            }

            if (_chargeLeft > 0f) return;
            _charging = false;
            // Clip phi giu frame cuoi; ve idle de nhanh danh/duoi ben tren tu chon clip tiep.
            if (animator != null) animator.Play("idle", restart: true);
        }

        /// <summary>
        /// Huong dang nhin. Art mac dinh ve QUAY TRAI (flipX = nhin phai); 3 linh trieu hoi ve QUAY PHAI
        /// (`artFacesRight`) nen flipX nguoc lai — moi cho doc/ghi huong di qua day, dung dung `flipX`.
        /// </summary>
        bool FacingRight
        {
            get => _renderer.flipX != _definition.artFacesRight;
            set => _renderer.flipX = value != _definition.artFacesRight;
        }

        // Quai lot qua sau lung thi hero quay lai.
        // Trung x (healer tu hoi minh, Banneret buff chinh minh) thi giu huong cu — `>=` tung lat hero NPC quay phai.
        void Face(float targetX)
        {
            if (!Mathf.Approximately(targetX, transform.position.x)) FacingRight = targetX > transform.position.x;
        }

        /// <summary>Dang chay mot clip danh / chieu / `PlayAction` chua het.</summary>
        public bool Casting => _casting;

        /// <summary>Doi bo animation (Chainwarden Cuong No); null = ve bo cua `HeroDefinition`.</summary>
        public void SwapAnimations(SpriteAnimationSet set)
        {
            if (animator != null) animator.Swap(set != null ? set : _definition.animations);
        }

        /// <summary>
        /// Phat mot clip THAN khong ra don (bien hinh, Loc Xich): khoa danh + hurt toi het clip nhu mot lan cast.
        /// Clip phai ten "skill" hoac "attack" de `OnClipFinished` mo khoa. Dang ban viec khac thi bo qua, tra false.
        /// </summary>
        /// <summary>Muc tieu don thuong neu dang trong tam, khong thi null — cho component chieu phu chon luc ra chieu.</summary>
        public Unit AttackTargetInRange()
        {
            var target = FindAttackTarget();
            return target != null && InRange(target) ? target : null;
        }

        public bool InAttackRange(Unit target) => InRange(target);

        /// <summary>Giay tu luc `PlayAction` toi frame cham (`hitFrame`) cua <paramref name="clip"/>.</summary>
        public float HitDelay(SpriteAnimationClip clip)
        {
            if (clip.frameSeconds > 0f) return Mathf.Max(0, clip.hitFrame - 1) * clip.frameSeconds;
            float fps = _definition.animations != null ? _definition.animations.framesPerSecond : 0f;
            return fps > 0f ? Mathf.Max(0, clip.hitFrame - 1) / fps : 0f;
        }

        /// <summary>Chieu giu frame cuoi clip (manh kiem dang cam, mua hat) xong: clip da het thi ve idle.</summary>
        public void ReleaseHeldClip()
        {
            if (_dying || animator == null || !animator.IsFinished) return;
            _casting = false;
            animator.Play("idle");
        }

        public bool PlayAction(SpriteAnimationClip clip)
        {
            if (animator == null || clip == null || !clip.IsValid) return false;
            if (_dying || _petrified || _charging || IsStunned) return false;
            if (_casting && !_released) return false;   // don dang vung chua ra: dung cat ngang
            _casting = true;
            _released = true;
            _castingSkill = false;
            animator.Play(clip, restart: true);
            return true;
        }

        /// <summary>He so sat thuong / toc danh tu hao quang ky si tu than (chi linh xuong nhan).</summary>
        /// <param name="buffs">false = bo buff Banneret (don thuong, Hieu Lenh, Quan Ky) — cho to mau hao quang linh xuong.</param>
        void AuraMultipliers(out float damage, out float rate, bool buffs = true)
        {
            damage = 1f;
            rate = 1f;
            if (_summoner != null) _summoner.BoneAura(this, ref damage, ref rate);
            if (_warden != null) _warden.RageMultipliers(ref damage, ref rate);
            if (_dragonLink != null) _dragonLink.Multipliers(this, ref damage);
            if (!buffs) return;
            BuffMultipliers(ref damage, ref rate);
            if (_brother != null) _brother.Multipliers(ref damage);
            if (_blood != null) _blood.Multipliers(ref damage);
            if (_werewolf != null) _werewolf.Multipliers(ref damage, ref rate);
            Banneret.AuraRate(this, ref rate);
        }

        // Dan bay ra o frame cham don cua clip (`hitFrame`; clip khong khai thi o cuoi clip) — khop voi luc vu khi
        // vuon ra, va muc tieu duoc chon lai tai thoi diem ban (con cu co the da chet trong luc cast).
        // `_casting` van giu toi het clip: phan thu ve cua animation khong bi buoc di cat ngang.
        void FinishCast()
        {
            _released = true;
            if (_castingSkill)
            {
                _castingSkill = false;
                Sfx.Play(_definition.skillSfx);
                switch (_definition.skillKind)
                {
                    case SkillKind.HookThrow: if (_warden != null) _warden.CastUltimate(); break;
                    case SkillKind.DragonRider: if (_dragonLink != null) _dragonLink.CastUltimate(); break;
                    case SkillKind.FrostZone: CastFrostZone(); break;
                    case SkillKind.ConeSweep: CastConeSweep(); break;
                    case SkillKind.RapidFire:
                        _rapidFireUntil = Time.time + _definition.skillDuration;
                        SpawnBuffFx(_definition.skillDuration);
                        break;
                    case SkillKind.SummonKnight: if (_necromancer != null) _necromancer.SummonKnight(); break;
                    case SkillKind.Lantern: CastLantern(); break;
                    case SkillKind.Rally: if (_banneret != null) _banneret.CastUltimate(); break;
                    case SkillKind.GiantStaff: if (_wukong != null) _wukong.CastUltimate(); break;
                    case SkillKind.ThunderStorm: if (_stormcaller != null) _stormcaller.CastUltimate(); break;
                    case SkillKind.Whirlwind: if (_galemonk != null) _galemonk.CastUltimate(); break;
                    case SkillKind.DaoVien: if (_brother != null) _brother.CastUltimate(); break;
                    case SkillKind.ToxicMist: CastToxicMist(); break;           // Hero.HeroN.cs
                    case SkillKind.MushroomBloom: CastMushroomBloom(); break;
                    case SkillKind.Whirlpool: CastWhirlpool(); break;
                    case SkillKind.EmberRain: CastEmberRain(); break;
                    case SkillKind.RuneWall: CastRuneWall(); break;
                    case SkillKind.Puppet: CastPuppet(); break;
                    case SkillKind.BloodShatter: if (_blood != null) _blood.CastUltimate(); break;
                    case SkillKind.SoulPact: if (_soulbinder != null) _soulbinder.CastUltimate(); break;
                    case SkillKind.SeedRain: CastSeedRain(); break;           // Hero.HeroN.cs
                    case SkillKind.TrapDetonation: CastTrapDetonation(); break;
                    case SkillKind.SigilBombs: CastSigilBombs(); break;
                    case SkillKind.HornSlam: CastHornSlam(); break;         // Hero.LowTrio.cs
                    case SkillKind.BladeStorm: CastBladeStorm(); break;
                    case SkillKind.BellToll: CastBellToll(); break;
                    case SkillKind.RingReturn: CastRingReturn(); break;       // Hero.NewSR.cs
                    case SkillKind.SigilJudgment: CastSigilJudgment(); break;
                    case SkillKind.GourdUnleash: if (_gourd != null) _gourd.CastUltimate(); break;
                    case SkillKind.InkDragon: if (_ink != null) _ink.CastUltimate(); break;
                    case SkillKind.LotusBloom: if (_hong != null) _hong.CastUltimate(); break;
                    case SkillKind.SandFort: if (_save != null) _save.CastUltimate(); break;
                    case SkillKind.GrandPuppet: if (_khoi != null) _khoi.CastUltimate(); break;
                    case SkillKind.MoonTransform: if (_werewolf != null) _werewolf.CastUltimate(); break;
                    case SkillKind.SolarShot: if (_hauNghe != null) _hauNghe.CastUltimate(); break;
                    case SkillKind.DeerRevival: if (_bachLoc != null) _bachLoc.CastUltimate(); break;
                }
                return;
            }

            if (_definition.healsAllies)
            {
                HealAlly();
                return;
            }
            if (_banneret != null)
            {
                _banneret.Strike();
                return;
            }

            var target = FindAttackTarget();
            if (target == null || !InRange(target)) return;
            Sfx.Play(_definition.fireSfx);

            AuraMultipliers(out float damageMultiplier, out _);
            damageMultiplier *= PassiveDamageMultiplier(target);    // Truy Kich / Nham Yeu Diem — Hero.Passive.cs
            // Huyet Kiem Khach chem truc tiep (Hien Huyet them mot dich) — xem `Bloodswordsman`.
            if (_blood != null)
            {
                _blood.Strike(target, BaseDamage * damageMultiplier);
                return;
            }
            // Wyrmspear dam truc tiep (giao dai, khong co dan) — Nghich Lan can biet muc tieu.
            if (_dragonLink != null && _dragonLink.Rider == this)
            {
                _dragonLink.Strike(target, BaseDamage * damageMultiplier);
                return;
            }
            // Stormcaller: tia set tuc thi nay chuoi, khong co dan bay.
            if (_stormcaller != null)
            {
                _stormcaller.Strike(target, BaseDamage * damageMultiplier);
                return;
            }
            // Quan Vu: chem quat nhieu quai (Vo Thanh), khong qua `Projectile`.
            if (_brother != null && _brother.Strike(target, BaseDamage * damageMultiplier)) return;
            // Galemonk: dan gio day lui (khong qua `Projectile`).
            if (_galemonk != null)
            {
                _galemonk.Strike(target, BaseDamage * damageMultiplier);
                return;
            }
            // Sa Ve: dam truc tiep can chien — xem `SaVe`.
            if (_save != null)
            {
                _save.Strike(target, BaseDamage * damageMultiplier);
                return;
            }
            // Nguoi Soi: Quyen Kich / Vuot Soi — xem `Werewolf`.
            if (_werewolf != null)
            {
                _werewolf.Strike(target, BaseDamage * damageMultiplier);
                return;
            }
            // Khoi Loi Su: chu khong tu danh, lenh roi dam — xem `KhoiLoiSu`.
            if (_khoi != null)
            {
                _khoi.Strike();
                return;
            }
            var go = new GameObject("Projectile");
            // Dau gay nam o phia hero dang nhin.
            go.transform.position = transform.position +
                new Vector3(FacingRight ? MuzzleOffset.x : -MuzzleOffset.x, MuzzleOffset.y, 0f);
            go.transform.localScale = Vector3.one * (_definition.projectileScale * Battlefield.UnitScale);
            go.AddComponent<SpriteRenderer>().sortingOrder = SortOrder.Air;
            go.AddComponent<Projectile>().Launch(target, BaseDamage * damageMultiplier, _definition.splashRadius,
                _definition.projectileSpeed, _definition.projectileFrames, _definition.impactFrames, this);
            if (Raging) _warden.OffhandStrike(target, BaseDamage * damageMultiplier);
        }

        /// <summary>Admin panel: day thanh mana ngay (chieu nho khong co thanh thi het cooldown).</summary>
        public void DevFillMana()
        {
            if (_definition == null) return;
            _mana = _definition.manaMax;
            _skillCooldown = 0f;
        }

        /// <summary>Hoan mana (Thien Dia Long Giang ha duoc muc tieu).</summary>
        public void AddMana(float amount)
        {
            if (_definition == null || !UsesMana || amount <= 0f || _health.HealBlocked) return;
            _mana = Mathf.Min(_definition.manaMax, _mana + amount);
        }

        /// <summary>`Projectile` bao ve so quai vua an don thuong — nguon mana thu hai ben canh thoi gian.</summary>
        public void OnHitsLanded(int count)
        {
            if (_definition == null || !UsesMana || count <= 0 || RapidFire || _health.HealBlocked) return;
            if (_werewolf != null && _werewolf.Transformed) return;   // hoa soi roi: khong tich mana nua
            _mana = Mathf.Min(_definition.manaMax, _mana + count * _definition.manaPerHit * ManaRate);
        }

        // Vung bang roi vao CUM quai dong nhat trong tam, khong phai con gan nhat: don khong che an
        // tien theo so muc tieu dinh duoc, ma con gan nhat thuong la con di le dau doi hinh.
        void CastFrostZone()
        {
            var centre = DensestCluster(_definition.range, _definition.skillRadius);
            if (centre == null) return;
            FrostZone.Spawn(centre.Value, _definition.skillRadius, _definition.skillDuration,
                _definition.skillFrames, Side);
        }

        /// <summary>
        /// Chem quat TRUOC MAT: moi quai co x nam trong [0, skillRadius] theo huong dang nhin va |dy| trong
        /// nua chieu dai do (mat dat bep doi nhu `FrostZone`). Khong ngam cum — can chien dung sat quai roi.
        /// </summary>
        void CastConeSweep()
        {
            float facing = FacingRight ? 1f : -1f;
            float length = _definition.skillRadius;
            float damage = BaseDamage * _definition.skillPower;

            foreach (var enemy in Unit.Hostiles(Side).ToArray())
            {
                if (enemy == null || enemy.IsDying) continue;
                var offset = enemy.AimPoint(transform.position) - transform.position;
                float forward = offset.x * facing;
                if (forward < 0f || forward > length + enemy.BodyRadius || Mathf.Abs(offset.y) > length * 0.5f) continue;
                enemy.Health.TakeDamage(damage, _health);
            }

            var fxPosition = transform.position + new Vector3(facing * length * 0.5f, MuzzleOffset.y, 0f);
            OneShotFx.Spawn(fxPosition, _definition.skillFrames, FacingRight, SortOrder.Air, Battlefield.UnitScale);
        }

        /// <summary>
        /// Tam cum quai dong nhat: xet moi quai trong <paramref name="searchRange"/> quanh hero lam tam, dem so
        /// quai trong <paramref name="radius"/> quanh no. Quet doi O(n^2), nhung n la so quai con song trong
        /// wave (5 + wave/2, toi da ~20) va chi chay mot lan moi lan cast.
        /// </summary>
        public Vector3? DensestCluster(float searchRange, float radius)
        {
            float rangeSqr = searchRange * searchRange;
            float radiusSqr = radius * radius;
            Vector3? best = null;
            int bestCount = 0;

            foreach (var candidate in Unit.Hostiles(Side))
            {
                if (candidate == null || candidate.IsDying) continue;
                if ((candidate.transform.position - transform.position).sqrMagnitude > rangeSqr) continue;

                int count = 0;
                foreach (var other in Unit.Hostiles(Side))
                {
                    if (other == null || other.IsDying) continue;
                    if ((other.transform.position - candidate.transform.position).sqrMagnitude <= radiusSqr) count++;
                }

                if (count <= bestCount) continue;
                bestCount = count;
                best = candidate.transform.position;
            }
            return best;
        }

        // Hurt KHONG duoc cat ngang cast: dan bay ra o frame cham don (hoac cuoi) clip "attack", nen doi clip
        // giua chung la nuot mat phat dan va tru cooldown vo ich.
        void OnDamaged(float _)
        {
            if (_dying || _casting || _petrified || _charging || Braced || IsStunned || animator == null) return;
            animator.Play("hurt", restart: true);
            // Lui NGUOC huong dang nhin. Long Giap mien hat van.
            if (_dragonLink == null || !_dragonLink.Unshakable(this)) _knockback.Trigger(FacingRight ? -1f : 1f);
        }

        void OnDied()
        {
            if (_dying) return;
            _dying = true;
            Sfx.Play("hero-die");
            if (IsLaunched) Land();     // chet giua khong trung -> clip death nam tren dat
            RaiseKilled(this);
            ClearStatusFx();
            _casting = false;
            _petrified = false;
            _charging = false;
            ClearSupport();     // animator dang dung hinh vi choang thi death khong bao gio chay het
            Paint(Color.white);
            if (animator != null && animator.CurrentClip != null)
            {
                animator.Speed = 1f;
                animator.Play("death", restart: true);
                return;     // doi ClipFinished moi xoa
            }
            Destroy(gameObject);
        }

        void OnClipFinished(string clipName)
        {
            if (clipName == "death")
            {
                Destroy(gameObject);
                return;
            }
            // Hoa da / dang phi: giu frame cuoi cho toi khi het gio / het duong.
            if (_dying || _petrified || _charging) return;
            if (_blood != null && _blood.Busy) return;
            if (Time.time < _seedRainUntil) return;
            if (_bladeStorming) return;
            // Tuong Rune: het clip cast thi giu frame cuoi (khien cam dat) toi khi tuong tan.
            if (Braced && clipName == "skill")
            {
                _casting = false;
                return;
            }

            if (clipName == "attack" || clipName == "skill")
            {
                if (_casting && !_released) FinishCast();   // clip khong khai hitFrame: ra don o cuoi nhu cu
                _casting = false;
            }
            animator.Play("idle");
        }

        void OnHitFrame(string clipName)
        {
            if (_dying || !_casting || _released) return;
            if (clipName == "attack" || clipName == "skill") FinishCast();
        }
    }
}
