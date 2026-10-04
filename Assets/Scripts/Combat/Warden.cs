using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Bo chieu rieng cua Chainwarden (Cai nguc moc xich, SR — GAME_DESIGN.md muc 3, chot 2026-09-24) — `Hero` tu gan
    /// component nay khi `HeroDefinition.warden` co gia tri, giong `Necromancer` cua Bonecaller.
    ///   - NOI TAI Cai Nguc: KHONG o day — `Blocker` giu 4 quai (`maxHeld`) va quai dang bi giu mang Xich
    ///     (`heldDamageBonus`, nhan them sat thuong tu MOI nguon qua `Health.Vulnerability`);
    ///   - CHIEU THUONG Quat Xich (cooldown AN): co quai trong `sweepRadius` thi quay xich — sat thuong thap + TROI
    ///     (dung im, van danh). Boss troi mot nua. Khong co quai thi giu chieu san sang, khong phi;
    ///   - CHIEU LON (thanh mana cua `Hero`, `SkillKind.HookThrow`): mo dau bang Nem Moc — keo quai ban xa (uu tien),
    ///     khong co thi con xa nhat chua bi minh giu, trong `hookRange`, ve sat minh + choang ngan + Xich — roi BIEN HINH
    ///     Cuong No VINH VIEN toi khi chet (nguoi dung chot 2026-09-26; bo gioi han 8s 2026-09-29): xich thanh hai xich dai hai ben, tam danh
    ///     `rageRange` + moi don trung them mot con ben kia, tang cong/toc danh, giam sat thuong nhan vao, hut mau theo
    ///     sat thuong gay ra; Quat Xich doi thanh Loc Xich (quay 360 do, ban kinh rong, cooldown ngan).
    ///   - CHIEU PHU 2 Giat Xich (cooldown an, chot 2026-09-29): dang giu >= `yankMinHeld` quai thi giat manh — moi quai
    ///     dang giu (Cuong No: them moi quai trong `rageRange`, keo ve sat minh) an don thuong x (yankPower +
    ///     yankPowerPerHeld x so quai) + choang. Sat thuong ra o hitFrame cua clip than `yankBody`.
    /// Nem Moc: than chay clip `skill` (moc bay ra o hitFrame), hinh moc + xich la `HookChain`; Cuong No doi clip than
    /// chay het moi bat dau (khong cat ngang pha giu / giat xich). Cuong No doi han
    /// bo animation sang `rageAnimations` (bien hinh `rageTransform`, Loc Xich = clip than `skill`); bo trong thi phong
    /// to + nhuom sprite thuong (`rageScale`, `rageTint`).
    /// </summary>
    [RequireComponent(typeof(Hero))]
    public class Warden : MonoBehaviour
    {
        Hero _hero;
        WardenDefinition _definition;
        float _sweepCooldown;
        float _yankCooldown;
        float _yankAt = -1f;     // > 0: thoi diem cu giat trung (hitFrame cua clip than)
        float _armorBeforeRage;
        bool _rageQueued;

        public bool Raging { get; private set; }
        public float RageRange => _definition.rageRange;
        public Color RageTint => HasRageArt ? Color.white : _definition.rageTint;

        bool HasRageArt => _definition.rageAnimations != null;

        public void Initialise(Hero hero, WardenDefinition definition)
        {
            _hero = hero;
            _definition = definition;
            _sweepCooldown = definition.sweepCooldown;
            _yankCooldown = definition.yankCooldown;
            _hero.Health.DamageDealt -= OnDamageDealt;
            _hero.Health.DamageDealt += OnDamageDealt;
            Enemy.ChainIconFrames = definition.chainIconFrames;
        }

        void Update()
        {
            if (_definition == null || _hero == null) return;
            if (Raging && _hero.IsDying) EndRage();
            if (_hero.IsDying) return;
            if (_rageQueued && !_hero.Casting)
            {
                _rageQueued = false;
                BeginRage();
            }

            UpdateYank();
            if (!_hero.MinorUnlocked) return;    // Quat Xich / Loc Xich la chieu phu — mo theo sao
            _sweepCooldown -= Time.deltaTime;
            if (_sweepCooldown > 0f || !AnyInSweep()) return;
            // Loc Xich la clip than: doi bien hinh / don danh dang chay xong roi moi quay, khong cat ngang.
            if (Raging && HasRageArt && _hero.Casting) return;
            _sweepCooldown = Raging ? _definition.stormCooldown : _definition.sweepCooldown;
            Sweep();
        }

        float SweepRadius => Raging ? _definition.stormRadius : _definition.sweepRadius;

        bool InSweep(Unit enemy) =>
            Battlefield.GroundDistance(enemy.AimPoint(transform.position), transform.position) - enemy.BodyRadius <= SweepRadius;

        bool AnyInSweep()
        {
            foreach (var enemy in Unit.Hostiles(_hero.Side))
                if (enemy != null && !enemy.IsDying && !enemy.FliesHigh && InSweep(enemy)) return true;
            return false;
        }

        /// <summary>
        /// Quat Xich, hoac Loc Xich khi Cuong No. Troi TRUOC khi tru mau: con chet vi don nay khong can troi, con song
        /// thi dung im ngay frame sau.
        /// </summary>
        void Sweep()
        {
            float root = Raging ? _definition.stormRootDuration : _definition.rootDuration;
            float damage = Raging ? _hero.AttackDamage * _definition.stormPower : _definition.sweepDamage;
            foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
            {
                if (enemy == null || enemy.IsDying || !InSweep(enemy)) continue;
                enemy.ApplyRoot(enemy.IsBoss ? root * 0.5f : root, _definition.rootFrames);
                enemy.Health.TakeDamage(damage, _hero.Health);
            }
            // Loc Xich co clip than (hai xich quay trong chinh sprite) thi khong chong them FX.
            if (Raging && HasRageArt && _hero.PlayAction(_definition.rageAnimations.skill)) return;
            // Dang thuong: than quay xich (neu dang danh do thi bo qua clip) + vong xich FX quanh chan.
            if (!Raging && _definition.sweepBody != null && _definition.sweepBody.IsValid) _hero.PlayAction(_definition.sweepBody);
            var frames = Raging && _definition.stormFrames != null && _definition.stormFrames.Length > 0
                ? _definition.stormFrames
                : _definition.sweepFrames;
            OneShotFx.Spawn(transform.position, frames, false, SortOrder.Air, Battlefield.UnitScale);
        }

        void UpdateYank()
        {
            if (_yankAt > 0f)
            {
                if (Time.time < _yankAt) return;
                _yankAt = -1f;
                Yank();
                return;
            }
            if (!_hero.SecondMinorUnlocked) return;
            _yankCooldown -= Time.deltaTime;
            if (_yankCooldown > 0f || HeldCount() < _definition.yankMinHeld) return;

            var body = _definition.yankBody;
            if (Raging && HasRageArt && body != null && body.IsValid)
            {
                if (!_hero.PlayAction(body)) return;     // dang ban clip khac: thu lai frame sau, khong phi cooldown
                _yankAt = Time.time + Mathf.Max(0, body.hitFrame - 1) / YankFramesPerSecond;
            }
            else _yankAt = Time.time;
            _yankCooldown = _definition.yankCooldown;
        }

        const float YankFramesPerSecond = 9f;   // cung nhip 110ms/frame voi SpriteAnimationSet

        int HeldCount()
        {
            var own = _hero.Blocker;
            return own == null ? 0 : own.HeldCount;
        }

        /// <summary>
        /// Giat Xich: nan nhan = quai dang bi minh giu; Cuong No them quai chua bi giu trong `rageRange` (keo ve sat minh).
        /// Choang TRUOC khi tru mau nhu Quat Xich.
        /// </summary>
        void Yank()
        {
            if (_hero.IsDying) return;
            var own = _hero.Blocker;
            var victims = new System.Collections.Generic.List<Unit>();
            foreach (var enemy in Unit.Hostiles(_hero.Side))
            {
                if (enemy == null || enemy.IsDying) continue;
                if (own != null && own.Holds(enemy)) { victims.Add(enemy); continue; }
                if (!Raging) continue;
                float reach = Battlefield.GroundDistance(enemy.AimPoint(transform.position), transform.position) - enemy.BodyRadius;
                if (reach > _definition.rageRange) continue;
                float side = enemy.transform.position.x >= transform.position.x ? 1f : -1f;
                var destination = transform.position +
                    new Vector3(side * (_hero.BodyRadius + enemy.BodyRadius + 0.05f), 0f, 0f);
                enemy.PullTo(Battlefield.Clamp(destination, enemy.BodyRadius, clampRight: false), _definition.yankPullTime);
                victims.Add(enemy);
            }
            if (victims.Count == 0) return;

            float damage = _hero.AttackDamage * (_definition.yankPower + _definition.yankPowerPerHeld * victims.Count);
            foreach (var enemy in victims)
            {
                bool tough = enemy.IsBoss || enemy.IsElite;
                enemy.ApplyStun(tough ? _definition.yankStun * 0.5f : _definition.yankStun, _definition.stunFrames);
                enemy.Health.TakeDamage(damage, _hero.Health);
                OneShotFx.Spawn(enemy.transform.position, _definition.yankFrames, false, SortOrder.Air,
                    Battlefield.UnitScale);
            }
        }

        /// <summary>Chieu lon, goi tu `Hero.FinishCast` khi thanh mana day: Nem Moc (neu co quai de moc) roi Cuong No.</summary>
        public void CastUltimate()
        {
            if (_definition == null || _hero == null || _hero.IsDying) return;
            ThrowHook();
            _rageQueued = true;     // Update mo Cuong No khi clip Nem Moc chay xong
        }

        /// <summary>Cuong No: he so cong/toc danh — `Hero.AuraMultipliers` nhan them.</summary>
        public void RageMultipliers(ref float damage, ref float rate)
        {
            if (!Raging) return;
            damage *= _definition.rageDamageMultiplier;
            rate *= _definition.rageRateMultiplier;
        }

        /// <summary>
        /// Xich thu hai: moi don thuong luc Cuong No trung them con GAN NHAT o phia ben kia <paramref name="main"/>
        /// (khong co thi con gan nhat cung phia) trong `rageRange`.
        /// </summary>
        public void OffhandStrike(Unit main, float damage)
        {
            if (!Raging || main == null) return;
            float mainSide = Mathf.Sign(main.transform.position.x - transform.position.x);
            Unit opposite = null, sameSide = null;
            float oppositeBest = float.PositiveInfinity, sameBest = float.PositiveInfinity;
            foreach (var enemy in Unit.Hostiles(_hero.Side))
            {
                if (enemy == null || enemy.IsDying || enemy == main || enemy.FliesHigh) continue;
                float reach = Battlefield.GroundDistance(enemy.AimPoint(transform.position), transform.position) - enemy.BodyRadius;
                if (reach > _definition.rageRange) continue;
                bool isOpposite = Mathf.Sign(enemy.transform.position.x - transform.position.x) != mainSide;
                if (isOpposite && reach < oppositeBest) { oppositeBest = reach; opposite = enemy; }
                else if (!isOpposite && reach < sameBest) { sameBest = reach; sameSide = enemy; }
            }
            var second = opposite != null ? opposite : sameSide;
            if (second != null) second.Health.TakeDamage(damage, _hero.Health);
        }

        void BeginRage()
        {
            if (!Raging)
            {
                _armorBeforeRage = _hero.Health.Armor;
                _hero.Health.Armor = Mathf.Max(_armorBeforeRage, _definition.rageDamageReduction);
                if (HasRageArt)
                {
                    _hero.SwapAnimations(_definition.rageAnimations);
                    _hero.PlayAction(_definition.rageTransform);
                }
                else transform.localScale = Vector3.one * (Battlefield.UnitScale * _definition.rageScale);
            }
            Raging = true;
            _sweepCooldown = 0f;    // vao Cuong No la quay Loc Xich ngay neu co quai sat ben
        }

        void EndRage()
        {
            Raging = false;
            _hero.Health.Armor = _armorBeforeRage;
            if (HasRageArt) _hero.SwapAnimations(null);
            else transform.localScale = Vector3.one * Battlefield.UnitScale;
            _sweepCooldown = Mathf.Max(_sweepCooldown, _definition.stormCooldown);
        }

        void OnDamageDealt(float amount)
        {
            if (Raging && amount > 0f) _hero.Health.Heal(amount * _definition.rageLifesteal, _hero.Health);
        }

        void OnDestroy()
        {
            if (_hero != null) _hero.Health.DamageDealt -= OnDamageDealt;
        }

        /// <summary>Keo quai ve sat minh. Tra ve false neu khong co quai nao de moc.</summary>
        bool ThrowHook()
        {
            if (_definition == null || _hero == null || _hero.IsDying) return false;
            var target = PickHookTarget();
            if (target == null) return false;

            // Dich: sat mep than Chainwarden, phia quai dang dung, cung hang y — quai can chien toi noi la vao cho giu.
            float side = target.transform.position.x >= transform.position.x ? 1f : -1f;
            var destination = transform.position +
                new Vector3(side * (_hero.BodyRadius + target.BodyRadius + 0.05f), 0f, 0f);
            destination = Battlefield.Clamp(destination, target.BodyRadius, clampRight: false);

            // Quay ve phia con bi moc (art quay trai: flipX = nhin phai) — moc bay ra tu tay phia do.
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.flipX = target.transform.position.x > transform.position.x;

            // Co art thi keo khi moc TOI noi; khong co art thi keo ngay nhu cu.
            if (_definition.hookFrames != null && _definition.hookFrames.Length > 0)
                HookChain.Launch(_hero, _definition.hookHand, target, _definition.hookFrames, _definition.hookBite,
                    _definition.chainLink, _definition.hookPullTime, latched => Latch(latched, destination));
            else Latch(target, destination);
            return true;
        }

        void Latch(Unit target, Vector3 destination)
        {
            float stun = target.IsBoss ? _definition.hookStun * 0.5f : _definition.hookStun;
            target.PullTo(destination, _definition.hookPullTime);
            target.ApplyStun(_definition.hookPullTime + stun, _definition.stunFrames);
            target.ApplyChain(_definition.hookChainBonus, _definition.hookChainDuration);
        }

        /// <summary>
        /// Quai BAN XA gan nhat trong tam (GDD: uu tien Goblin Na); khong co thi con XA nhat chua bi Chainwarden giu —
        /// keo con dung sat roi thi phi chieu.
        /// </summary>
        Unit PickHookTarget()
        {
            float rangeSqr = _definition.hookRange * _definition.hookRange;
            Unit ranged = null, far = null;
            float rangedSqr = float.PositiveInfinity, farSqr = -1f;
            var own = _hero.Blocker;

            foreach (var enemy in Unit.Hostiles(_hero.Side))
            {
                if (enemy == null || enemy.IsDying || enemy.FliesHigh) continue;
                float sqr = (enemy.AimPoint(transform.position) - transform.position).sqrMagnitude;
                if (sqr > rangeSqr) continue;

                if (!enemy.CanBeHeld)
                {
                    if (sqr < rangedSqr) { rangedSqr = sqr; ranged = enemy; }
                    continue;
                }
                if (own != null && own.Holds(enemy) && enemy.Holder == own) continue;
                if (sqr > farSqr) { farSqr = sqr; far = enemy; }
            }
            return ranged != null ? ranged : far;
        }
    }
}
