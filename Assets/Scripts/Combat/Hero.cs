using System.Collections.Generic;
using BuildBuild.Core;
using BuildBuild.Data;
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
    /// Ky nang chia HAI TANG (`SkillTier`):
    ///   - Chieu LON: THANH MANA tich theo thoi gian VA theo so quai an don thuong, day thi tu cast
    ///     roi ve 0. Danh dong quai thi day nhanh hon — co chu dich: don khong che phai den dung luc
    ///     dang bi vay. Co thanh tren dau nen nguoi choi doan truoc duoc.
    ///   - Chieu NHO: cooldown AN, khong thanh, khong an mana — chay ngam nhu mot nhip phu.
    /// Mot hero hien chi mang duoc MOT ky nang qua `SkillKind`; bo chieu rieng cua Bonecaller (noi tai +
    /// chieu thuong) nam o `Necromancer`, xem AD-17.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public partial class Hero : MonoBehaviour
    {
        /// <summary>Moi hero dang song tren san — quai quet danh sach nay de tim muc tieu gan.</summary>
        public static readonly List<Hero> Alive = new List<Hero>();

        // Diem dan xuat hien: dau gay, tinh tu chan (pivot), o co art 1x — dung thi nhan `Battlefield.UnitScale`.
        static readonly Vector3 MuzzleOffset = new Vector3(0.15f, 0.3f, 0f) * Battlefield.UnitScale;

        /// <summary>So quai toi da vay danh mot hero cung luc (xem `Enemy.EngageHero`).</summary>
        public const int EngageSlots = 3;

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
        readonly HashSet<Enemy> _chargeHits = new HashSet<Enemy>();
        readonly Enemy[] _engagers = new Enemy[EngageSlots];
        readonly Knockback _knockback = new Knockback();
        SpriteRenderer _petrifyRenderer;
        float _bodyRadius;
        float _bodyHeight;

        public HeroDefinition Definition => _definition;
        public Health Health => _health;

        /// <summary>Nua be ngang than tren mat dat (world) — quai do tam toi MEP than hero.</summary>
        public float BodyRadius => _bodyRadius;

        /// <summary>Dinh dau tinh tu chan (world) — cho dat thanh mau.</summary>
        public float BodyHeight => _bodyHeight;

        /// <summary>Con cho vay cho <paramref name="enemy"/> (hoac no dang giu san mot cho).</summary>
        public bool HasFreeSlot(Enemy enemy)
        {
            for (int i = 0; i < EngageSlots; i++)
                if (_engagers[i] == enemy || SlotFree(i)) return true;
            return false;
        }

        /// <summary>Giu mot cho vay cho <paramref name="enemy"/>; tra ve chi so cho, -1 neu het cho.</summary>
        public int ClaimSlot(Enemy enemy)
        {
            for (int i = 0; i < EngageSlots; i++)
                if (_engagers[i] == enemy) return i;
            for (int i = 0; i < EngageSlots; i++)
            {
                if (!SlotFree(i)) continue;
                _engagers[i] = enemy;
                return i;
            }
            return -1;
        }

        // Cho tu nha khi quai chet/bi xoa hoac da bo sang muc tieu khac — khong can quai bao lai.
        bool SlotFree(int i) =>
            _engagers[i] == null || _engagers[i].IsDying || _engagers[i].EngagedHero != this;
        public bool IsDying => _dying;

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
        bool RapidFire => Time.time < _rapidFireUntil;

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _health = GetComponent<Health>();
            _health.Damaged += OnDamaged;
            _health.Blocked += OnBlocked;
            _health.Died += OnDied;
        }

        void OnEnable() => Alive.Add(this);

        void OnDisable() => Alive.Remove(this);

        public void Initialise(HeroDefinition definition)
        {
            _definition = definition;
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
            _health.Invulnerable = false;
            _mana = 0f;
            _health.ResetTo(definition.maxHealth);
            _renderer.color = Color.white;
            _knockback.Reset();
            System.Array.Clear(_engagers, 0, EngageSlots);

            transform.localScale = Vector3.one * Battlefield.UnitScale;
            Battlefield.MeasureBody(definition.animations, out _bodyRadius, out _bodyHeight);

            // Art ve QUAY TRAI; hero nhin ve phia quai (ben phai) nen lat.
            _renderer.flipX = true;

            if (animator != null)
            {
                animator.Speed = 1f;
                animator.Bind(definition.animations);
                animator.ClipFinished -= OnClipFinished;    // tranh dang ky hai lan neu Initialise goi lai
                animator.ClipFinished += OnClipFinished;
                animator.HitFrame -= OnHitFrame;
                animator.HitFrame += OnHitFrame;
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

        void Update()
        {
            if (_definition == null || _dying) return;

            // Linh noi tai het 10s: chet binh thuong (chay death) de nguoi choi thay no tan.
            if (_expiresAt > 0f && Time.time >= _expiresAt)
            {
                _health.Invulnerable = false;
                _health.TakeDamage(_health.Current);
                return;
            }

            _cooldown -= Time.deltaTime;
            _skillCooldown -= Time.deltaTime;
            // Chua bam bat dau wave thi thanh dung yen: mana theo thoi gian chi chay trong luc danh.
            // Dang Ban Nhanh thi khoa thanh: 3x don danh ma van tich mana thi chieu gan nhu lien hoan (2026-09-15).
            if (UsesMana && WaveManager.Fighting && !RapidFire)
                _mana = Mathf.Min(_definition.manaMax, _mana + _definition.manaPerSecond * Time.deltaTime);

            // Hoa Da: bat dong la cai GIA cua mien sat thuong — khong di, khong danh (GDD muc 3).
            if (_petrified)
            {
                if (Time.time < _petrifyUntil) return;
                EndPetrify();
            }

            // Choang (boss Chan Dia): dung han, animation dung hinh — Hero.Support.cs.
            if (UpdateStunned()) return;

            bool rapid = RapidFire;
            AuraMultipliers(out float auraDamage, out _);
            _renderer.color = rapid && !HasBuffFx ? RapidFireTint : auraDamage > 1f ? BoneAuraTint : Color.white;
            if (animator != null) animator.Speed = rapid ? _definition.skillPower : 1f;

            if (_charging)
            {
                UpdateCharge();
                return;
            }

            // Bi day lui: frame nay chi lui (cooldown van chay o tren).
            if (_knockback.Step(transform)) return;

            // Hai unit phe ta khong dung chong khit mot diem (hai hero can chien cung duoi mot con quai).
            transform.position += Battlefield.Separation(this, Alive) * Mathf.Min(1f, SeparationRate * Time.deltaTime);

            if (_casting) return;

            // Hero hoi mau (Vespera) nham dong minh, khong nham quai.
            if (_definition.healsAllies)
            {
                UpdateHealer();
                return;
            }

            // Phi xong KHONG ep di bo ve cho trieu hoi: ky si can chien quay lung di ve thi bi quai danh ma khong danh
            // lai (nguoi dung bat 2026-09-15). Het quai thi nhanh `target == null` ben duoi tu dua ve.
            var target = FindTarget(float.PositiveInfinity);
            if (target == null)
            {
                ReturnHome();
                return;
            }

            // Ngoai tam ban: buoc toi (tam xa: chi khi vao tam duoc trong day xich). Khong vua di vua
            // danh — moi frame dung mot hanh dong, de chuoi animation doc ra dung viec hero dang lam.
            if (!InRange(target))
            {
                if (CanReach(target.transform.position)) MoveToward(target.transform.position);
                else ReturnHome();
                return;
            }

            Face(target.transform.position.x);

            // Ky nang di truoc don thuong khi ca hai cung san sang: chu ky ky nang dai hon nhieu,
            // nhuong nhip cho mot phat dan le thi phi ca chu ky.
            if (_definition.HasSkill && SkillReady) BeginCast(skill: true);
            else if (_cooldown <= 0f) BeginCast(skill: false);
            else PlayIdle();
        }

        /// <summary>
        /// Can chien do tren MAT DAT (`Battlefield.GroundDistance`, chieu sau nhan doi): hai than phai dung cung hang
        /// sau moi cham nhau, va `range` la tam voi tinh tu chan hero toi MEP THAN quai (hitbox), khong toi chan
        /// quai — Ashfang be ngang gap ruoi Goblin. Tam xa van do thang tren man — dan bay qua khong khi.
        /// </summary>
        bool InRange(Enemy target)
        {
            var position = target.transform.position;
            return IsRanged
                ? (position - transform.position).sqrMagnitude <= _definition.range * _definition.range
                : Battlefield.GroundDistance(position, transform.position) - target.BodyRadius <= _definition.range;
        }

        // Order theo chan sau khi moi dich chuyen trong frame da xong; vo da ve ngay tren than.
        void LateUpdate()
        {
            // Duoi quai / bi day lui / tach doi hinh / phi: khong buoc nao duoc xuyen qua thanh hay ra khoi san.
            if (!_dying) transform.position = Battlefield.Clamp(transform.position, _bodyRadius, clampRight: true);
            _renderer.sortingOrder = SortOrder.ForUnit(this);
            if (_petrifyRenderer != null) _petrifyRenderer.sortingOrder = _renderer.sortingOrder + 1;
        }

        /// <summary>Can chien luon duoi; tam xa chi di khi cho dung ban duoc nam trong day xich tinh tu o.</summary>
        bool CanReach(Vector3 destination)
        {
            if (!IsRanged) return true;
            float reach = _definition.leashRange + _definition.range;
            var fromAnchor = destination - _anchor;
            fromAnchor.z = 0f;
            return fromAnchor.sqrMagnitude <= reach * reach;
        }

        /// <summary>Buoc ve phia <paramref name="destination"/>; tam xa khong buoc ra ngoai `leashRange` tinh tu o.</summary>
        void MoveToward(Vector3 destination)
        {
            var direction = destination - transform.position;
            direction.z = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;

            Face(destination.x);
            var next = transform.position + direction.normalized * (_definition.moveSpeed * Time.deltaTime);
            if (IsRanged && (next - _anchor).sqrMagnitude > _definition.leashRange * _definition.leashRange) return;

            transform.position = next;
            if (animator != null) animator.Play("walk");
        }

        /// <summary>Di ve o; toi noi thi dung idle nhin sang phai (phia quai den). Tra ve true khi da o o.</summary>
        bool ReturnHome()
        {
            var toHome = _anchor - transform.position;
            toHome.z = 0f;
            float step = _definition.moveSpeed * Time.deltaTime;
            if (toHome.magnitude <= Mathf.Max(step, HomeTolerance))
            {
                transform.position = _anchor;
                _renderer.flipX = true;
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
            else _cooldown = 1f / Mathf.Max(0.01f,
                _definition.attackRate * (RapidFire ? _definition.skillPower : 1f) * rateMultiplier);

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
            source.TakeDamage(amount * _definition.skillPower);
        }

        void BeginCharge()
        {
            _charging = true;
            _chargeLeft = _definition.skillRadius;
            // Ngam cum quai dong nhat trong tam phi (nguoi dung 2026-09-15: quai o huong cheo thi phi cheo), khong co
            // thi phi thang theo huong dang nhin.
            var facing = new Vector3(_renderer.flipX ? 1f : -1f, 0f, 0f);
            var aim = DensestCluster(_definition.skillRadius, ChargeHalfWidth * 2f);
            var direction = aim.HasValue ? aim.Value - transform.position : facing;
            direction.z = 0f;
            _chargeDirection = direction.sqrMagnitude < 0.0001f ? facing : direction.normalized;
            Face(transform.position.x + _chargeDirection.x);
            _chargeHits.Clear();
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

            foreach (var enemy in Enemy.Alive.ToArray())
            {
                if (enemy == null || enemy.IsDying || _chargeHits.Contains(enemy)) continue;
                var offset = enemy.transform.position - transform.position;
                float along = offset.x * _chargeDirection.x + offset.y * _chargeDirection.y;
                float across = offset.x * _chargeDirection.y - offset.y * _chargeDirection.x;
                if (Mathf.Abs(along) > _bodyRadius + enemy.BodyRadius || Mathf.Abs(across) > ChargeHalfWidth) continue;
                _chargeHits.Add(enemy);
                enemy.ApplyStun(enemy.IsBoss ? _definition.skillDuration * 0.5f : _definition.skillDuration, _definition.stunFrames);
                enemy.Health.TakeDamage(_definition.skillPower, _health);
            }

            if (_chargeLeft > 0f) return;
            _charging = false;
            // Clip phi giu frame cuoi; ve idle de nhanh danh/duoi ben tren tu chon clip tiep.
            if (animator != null) animator.Play("idle", restart: true);
        }

        Enemy FindTarget(float maxRange)
        {
            Enemy best = null;
            float bestSqr = maxRange * maxRange;
            foreach (var enemy in Enemy.Alive)
            {
                if (enemy == null || enemy.IsDying) continue;
                float sqr = (enemy.transform.position - transform.position).sqrMagnitude;
                if (sqr > bestSqr) continue;
                bestSqr = sqr;
                best = enemy;
            }
            return best;
        }

        // Art ve QUAY TRAI: flipX = true la nhin sang PHAI. Quai lot qua sau lung thi hero quay lai.
        void Face(float targetX) => _renderer.flipX = targetX >= transform.position.x;

        /// <summary>He so sat thuong / toc danh tu hao quang ky si tu than (chi linh xuong nhan).</summary>
        void AuraMultipliers(out float damage, out float rate)
        {
            damage = 1f;
            rate = 1f;
            if (_summoner != null) _summoner.BoneAura(this, ref damage, ref rate);
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
                switch (_definition.skillKind)
                {
                    case SkillKind.FrostZone: CastFrostZone(); break;
                    case SkillKind.ConeSweep: CastConeSweep(); break;
                    case SkillKind.RapidFire:
                        _rapidFireUntil = Time.time + _definition.skillDuration;
                        SpawnBuffFx(_definition.skillDuration);
                        break;
                    case SkillKind.SummonKnight: if (_necromancer != null) _necromancer.SummonKnight(); break;
                }
                return;
            }

            if (_definition.healsAllies)
            {
                HealAlly();
                return;
            }

            var target = FindTarget(float.PositiveInfinity);
            if (target == null || !InRange(target)) return;

            AuraMultipliers(out float damageMultiplier, out _);
            var go = new GameObject("Projectile");
            // Art quay trai, flipX = nhin phai: dau gay nam o phia hero dang nhin.
            go.transform.position = transform.position +
                new Vector3(_renderer.flipX ? MuzzleOffset.x : -MuzzleOffset.x, MuzzleOffset.y, 0f);
            go.transform.localScale = Vector3.one * (_definition.projectileScale * Battlefield.UnitScale);
            go.AddComponent<SpriteRenderer>().sortingOrder = SortOrder.Air;
            go.AddComponent<Projectile>().Launch(target, _definition.damage * damageMultiplier, _definition.splashRadius,
                _definition.projectileSpeed, _definition.projectileFrames, _definition.impactFrames, this);
        }

        /// <summary>Admin panel: day thanh mana ngay (chieu nho khong co thanh thi het cooldown).</summary>
        public void DevFillMana()
        {
            if (_definition == null) return;
            _mana = _definition.manaMax;
            _skillCooldown = 0f;
        }

        /// <summary>`Projectile` bao ve so quai vua an don thuong — nguon mana thu hai ben canh thoi gian.</summary>
        public void OnHitsLanded(int count)
        {
            if (_definition == null || !UsesMana || count <= 0 || RapidFire) return;
            _mana = Mathf.Min(_definition.manaMax, _mana + count * _definition.manaPerHit);
        }

        // Vung bang roi vao CUM quai dong nhat trong tam, khong phai con gan nhat: don khong che an
        // tien theo so muc tieu dinh duoc, ma con gan nhat thuong la con di le dau doi hinh.
        void CastFrostZone()
        {
            var centre = DensestCluster(_definition.range, _definition.skillRadius);
            if (centre == null) return;
            FrostZone.Spawn(centre.Value, _definition.skillRadius, _definition.skillDuration,
                _definition.skillFrames);
        }

        /// <summary>
        /// Chem quat TRUOC MAT: moi quai co x nam trong [0, skillRadius] theo huong dang nhin va |dy| trong
        /// nua chieu dai do (mat dat bep doi nhu `FrostZone`). Khong ngam cum — can chien dung sat quai roi.
        /// </summary>
        void CastConeSweep()
        {
            float facing = _renderer.flipX ? 1f : -1f;
            float length = _definition.skillRadius;
            float damage = _definition.damage * _definition.skillPower;

            foreach (var enemy in Enemy.Alive.ToArray())
            {
                if (enemy == null || enemy.IsDying) continue;
                var offset = enemy.transform.position - transform.position;
                float forward = offset.x * facing;
                if (forward < 0f || forward > length + enemy.BodyRadius || Mathf.Abs(offset.y) > length * 0.5f) continue;
                enemy.Health.TakeDamage(damage);
            }

            var fxPosition = transform.position + new Vector3(facing * length * 0.5f, MuzzleOffset.y, 0f);
            OneShotFx.Spawn(fxPosition, _definition.skillFrames, _renderer.flipX, SortOrder.Air, Battlefield.UnitScale);
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

            foreach (var candidate in Enemy.Alive)
            {
                if (candidate == null || candidate.IsDying) continue;
                if ((candidate.transform.position - transform.position).sqrMagnitude > rangeSqr) continue;

                int count = 0;
                foreach (var other in Enemy.Alive)
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
            if (_dying || _casting || _petrified || _charging || IsStunned || animator == null) return;
            animator.Play("hurt", restart: true);
            // Lui NGUOC huong dang nhin (flipX = nhin phai -> lui sang trai).
            _knockback.Trigger(_renderer.flipX ? -1f : 1f);
        }

        void OnDied()
        {
            if (_dying) return;
            _dying = true;
            _casting = false;
            _petrified = false;
            _charging = false;
            ClearSupport();     // animator dang dung hinh vi choang thi death khong bao gio chay het
            _renderer.color = Color.white;
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
