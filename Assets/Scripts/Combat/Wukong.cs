using System.Collections.Generic;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Bo chieu Ton Ngo Khong (GAME_DESIGN.md muc 3, `WukongDefinition`), gan vao `Hero` khi `HeroDefinition.wukong` co:
    ///   - Noi tai (passiveStar): The Than — lan dau het mau moi wave, con phan than thi mot con thanh ban the (40% mau),
    ///     khong con thi chet nhu thuong (`Hero.OnDied` hoi `TrySubstitute`). Long Ve Than — moi phan than tan hoi 8% mau;
    ///     hut mau 10% tren sat thuong gay ra.
    ///   - Nho Long Phan Than (minorStar): 2 phan than song 8s, 40% cong/mau, tu danh, moi con chan 3 quai (la `Hero`
    ///     nhu linh xuong, co `Blocker` rieng).
    ///   - Toan Phong Con (secondMinorStar): xoay 3s — nhip 0,5s danh quanh minh, hoi 3% mau / quai trung (tran 25%);
    ///     dan cua quai ban xa bi chan va bat nguoc vao quai ban (`Health.Deflects`). Phan than dang song xoay cung luc
    ///     (danh quanh chinh no bang cong cua no, khong hoi mau, khong bat dan).
    ///   - Nhu Y Thien Tru (chieu xin, mana): gay khong lo dap nam ngang xuong cum quai dong nhat — sat thuong theo vet dai,
    ///     choang, vet nut lam cham; moi phan than dap gay nho 30%.
    /// </summary>
    [RequireComponent(typeof(Hero))]
    public class Wukong : MonoBehaviour
    {
        const float TickInterval = 0.5f;
        const float FramesPerSecond = 9f;   // cung nhip 110 ms/frame voi SpriteAnimationSet
        /// <summary>Gay khong lo cham dat o frame 3 cua FX (o 1-2 dang roi).</summary>
        const int StaffImpactFrame = 3;
        /// <summary>
        /// FX gay (o 320, pivot TAM o) ve diem cham dat / tam vet nut ~20 art-px DUOI tam o (do `aligned_bbox` o 3-6):
        /// nang FX len chung do (world = px * 0,01 * scale) de vet nut nam dung tam cum quai, khong lot xuong duoi.
        /// </summary>
        const float StaffGroundBelowCentre = 0.20f;

        Hero _hero;
        WukongDefinition _definition;
        static readonly List<Hero> AllClones = new List<Hero>();
        readonly List<Hero> _clones = new List<Hero>();
        float _cloneCooldown;
        float _cloneAt = -1f;
        float _spinCooldown;
        float _spinUntil;
        float _nextSpinTick;
        float _spinHealed;
        GameObject _spinRing;
        /// <summary>Phan than xoay cung dot nay + vong FX cua tung con (cung chi so).</summary>
        readonly List<Hero> _spinClones = new List<Hero>();
        readonly List<GameObject> _cloneRings = new List<GameObject>();
        SpriteAnimationClip _spinLoop;
        bool _substituteUsed;

        /// <summary>Moc "het wave": xoa moi phan than con song (giong `Necromancer.DespawnAll`).</summary>
        public static void DespawnAll()
        {
            foreach (var clone in AllClones)
                if (clone != null) Destroy(clone.gameObject);
            AllClones.Clear();
        }

        public void Initialise(Hero hero, WukongDefinition definition)
        {
            _hero = hero;
            _definition = definition;
            _cloneCooldown = definition.cloneCooldown;
            _spinCooldown = definition.spinCooldown;
            _cloneAt = -1f;
            _spinUntil = 0f;
            _substituteUsed = false;
            _clones.Clear();
            // Vong xoay lap o 3-6 cua clip xoay (o 1-2 rut gay chi chay mot lan dau).
            var frames = definition.spinBody.frames;
            _spinLoop = new SpriteAnimationClip
            {
                name = "skill",
                loop = false,
                frames = frames != null && frames.Length > 2 ? frames[2..] : frames,
            };
            _hero.Health.DamageDealt -= OnDamageDealt;
            _hero.Health.DamageDealt += OnDamageDealt;
            _hero.Health.Deflects = Deflects;
        }

        void OnDestroy()
        {
            if (_hero != null && _hero.Health != null) _hero.Health.DamageDealt -= OnDamageDealt;
        }

        void Update()
        {
            if (_definition == null || _hero == null || _hero.IsDying)
            {
                EndSpin();
                return;
            }
            _clones.RemoveAll(c => c == null || c.IsDying);

            UpdateSpin();
            if (_hero.SecondMinorUnlocked && Spinning == false)
            {
                _spinCooldown -= Time.deltaTime;
                if (_spinCooldown <= 0f && AnyEnemyNear(_definition.spinRadius) && _hero.PlayAction(_definition.spinBody))
                    BeginSpin();
            }

            if (_cloneAt > 0f && Time.time >= _cloneAt)
            {
                _cloneAt = -1f;
                SpawnClones();
            }
            if (!_hero.MinorUnlocked || Spinning) return;
            _cloneCooldown -= Time.deltaTime;
            if (_cloneCooldown > 0f || _cloneAt > 0f || !AnyEnemyNear(_hero.CurrentAttackRange + 3f)) return;
            if (!_hero.PlayAction(_definition.cloneBody)) return;   // dang ban clip khac: thu lai frame sau
            _cloneCooldown = _definition.cloneCooldown;
            _cloneAt = Time.time + Mathf.Max(0, _definition.cloneBody.hitFrame - 1) / FramesPerSecond;
        }

        bool AnyEnemyNear(float reach)
        {
            foreach (var enemy in Unit.Hostiles(_hero.Side))
                if (enemy != null && !enemy.IsDying &&
                    Battlefield.GroundDistance(enemy.AimPoint(transform.position), transform.position) - enemy.BodyRadius <= reach)
                    return true;
            return false;
        }

        // ---------------------------------------------------------------- noi tai

        void OnDamageDealt(float amount)
        {
            if (!_hero.PassiveUnlocked || amount <= 0f) return;
            Heal(amount * _definition.lifesteal, fx: false);
        }

        void Heal(float amount, bool fx = true)
        {
            if (amount <= 0f || _hero.IsDying) return;
            _hero.Health.Heal(amount, _hero.Health);
            if (fx) OneShotFx.Spawn(transform.position, _definition.healFrames, false, SortOrder.Air, Battlefield.UnitScale);
        }

        /// <summary>
        /// The Than: goi tu `Hero.OnDied`. Con phan than va chua dung trong wave nay thi ban the lay cho con do voi
        /// `substituteHealth` mau, con do tan — tra ve true (khong chet). Khong thi false.
        /// </summary>
        public bool TrySubstitute()
        {
            if (_substituteUsed || !_hero.PassiveUnlocked) return false;
            _clones.RemoveAll(c => c == null || c.IsDying);
            if (_clones.Count == 0) return false;
            _substituteUsed = true;
            var clone = _clones[0];
            _clones.RemoveAt(0);
            OneShotFx.Spawn(transform.position, _definition.poofFrames, false, SortOrder.Air, Battlefield.UnitScale);
            transform.position = clone.transform.position;
            Destroy(clone.gameObject);
            OneShotFx.Spawn(transform.position, _definition.poofFrames, false, SortOrder.Air, Battlefield.UnitScale);
            _hero.Health.ResetTo(_hero.MaxHealth * _definition.substituteHealth);
            return true;
        }

        // ---------------------------------------------------------------- Nho Long Phan Than

        void SpawnClones()
        {
            if (_hero.IsDying || _definition.cloneDefinition == null || _definition.unitPrefab == null) return;
            float side = _hero.FacesRight ? 1f : -1f;
            for (int i = 0; i < _definition.cloneCount; i++)
            {
                // Hai ben truoc mat, lech tren / duoi lane de khong chong len ban the.
                float dy = (i % 2 == 0 ? 1f : -1f) * 0.35f * (1 + i / 2);
                var at = transform.position + new Vector3(side * 0.5f, dy, 0f);
                var clone = Instantiate(_definition.unitPrefab, Battlefield.Clamp(at, _hero.BodyRadius, clampRight: true),
                    Quaternion.identity);
                clone.Initialise(_definition.cloneDefinition);
                clone.InheritProgression(_hero, withGear: true);   // "40% cong/mau ban the" = ca trang bi
                clone.MakeSummon(null, _definition.cloneLifetime);
                clone.SetBaseTint(_definition.cloneTint);
                clone.Health.Died += OnCloneGone;
                _clones.Add(clone);
                AllClones.Add(clone);
                OneShotFx.Spawn(clone.transform.position, _definition.poofFrames, false, SortOrder.Air, Battlefield.UnitScale);
            }
        }

        // Het gio cung la chet (`Hero.Update` tru het mau khi het `lifetime`) nen chi can nghe Died.
        void OnCloneGone()
        {
            _clones.RemoveAll(c => c == null || c.IsDying);
            if (_hero == null || _hero.IsDying || !_hero.PassiveUnlocked) return;
            Heal(ConquestMatch.PercentBase(_hero.MaxHealth) * _definition.cloneReturnHeal);
        }

        // ---------------------------------------------------------------- Toan Phong Con

        bool Spinning => Time.time < _spinUntil;

        void BeginSpin()
        {
            _spinCooldown = _definition.spinCooldown;
            _spinUntil = Time.time + _definition.spinDuration;
            _nextSpinTick = Time.time + TickInterval;
            _spinHealed = 0f;
            if (_spinRing != null) Destroy(_spinRing);
            // Pivot vong o TAM o: dat vao giua than (local cua cha = world / UnitScale), khong vao chan.
            var bodyCentre = new Vector3(0f, _hero.BodyHeight * 0.5f / Battlefield.UnitScale, 0f);
            _spinRing = OneShotFx.SpawnLoop(transform, bodyCentre, _definition.spinRingFrames, _definition.spinDuration,
                SortOrder.Air);

            _spinClones.Clear();
            foreach (var clone in _clones)
            {
                if (clone == null || clone.IsDying || !clone.PlayAction(_definition.spinBody)) continue;
                _spinClones.Add(clone);
                var centre = new Vector3(0f, clone.BodyHeight * 0.5f / Battlefield.UnitScale, 0f);
                _cloneRings.Add(OneShotFx.SpawnLoop(clone.transform, centre, _definition.spinRingFrames,
                    _definition.spinDuration, SortOrder.Air));
            }
        }

        void UpdateSpin()
        {
            if (!Spinning)
            {
                EndSpin();
                return;
            }
            // Clip xoay het thi noi vong lap o 3-6 toi het thoi gian.
            if (!_hero.Casting) _hero.PlayAction(_spinLoop);
            _spinClones.RemoveAll(c => c == null || c.IsDying);
            foreach (var clone in _spinClones)
                if (!clone.Casting) clone.PlayAction(_spinLoop);
            if (Time.time < _nextSpinTick) return;
            _nextSpinTick += TickInterval;

            SpinTick(_hero, heal: true);
            foreach (var clone in _spinClones) SpinTick(clone, heal: false);
        }

        void SpinTick(Hero spinner, bool heal)
        {
            float damage = spinner.AttackDamage * _definition.spinPower;
            var at = spinner.transform.position;
            foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
            {
                if (enemy == null || enemy.IsDying) continue;
                if (Battlefield.GroundDistance(enemy.AimPoint(at), at) - enemy.BodyRadius > _definition.spinRadius)
                    continue;
                enemy.Health.TakeDamage(damage, spinner.Health);
                if (heal) SpinHeal();
            }
        }

        void SpinHeal()
        {
            float max = ConquestMatch.PercentBase(_hero.MaxHealth);
            float cap = max * _definition.spinHealCap;
            float amount = Mathf.Min(max * _definition.spinHealPerHit, cap - _spinHealed);
            if (amount <= 0f) return;
            _spinHealed += amount;
            Heal(amount, fx: false);
        }

        void EndSpin()
        {
            _spinUntil = 0f;
            if (_spinRing != null) Destroy(_spinRing);
            _spinRing = null;
            foreach (var ring in _cloneRings)
                if (ring != null) Destroy(ring);
            _cloneRings.Clear();
            _spinClones.Clear();
        }

        /// <summary>
        /// Dang xoay thi chan don tu quai BAN XA (dan bay toi): bat nguoc — quai ban an lai dung luong do, ban the hoi nhu
        /// trung mot quai. Tra ve true = don bi chan.
        /// </summary>
        bool Deflects(float amount, Health source)
        {
            if (!Spinning || source == null) return false;
            var shooter = source.GetComponent<Unit>();
            if (shooter == null || shooter.IsDying || !shooter.IsHostileTo(_hero.Side)) return false;
            bool ranged = shooter is Enemy enemy ? enemy.Definition != null && enemy.Definition.IsRanged
                : shooter is Hero hero && hero.IsRangedUnit;
            if (!ranged) return false;
            var chest = transform.position + new Vector3(0f, _hero.BodyHeight * 0.5f, 0f);
            OneShotFx.Spawn(chest, _definition.deflectFrames, false, SortOrder.Air, Battlefield.UnitScale);
            shooter.Health.TakeDamage(amount, _hero.Health, kind: DamageKind.Reflected);
            SpinHeal();
            return true;
        }

        // ---------------------------------------------------------------- Nhu Y Thien Tru

        /// <summary>Chieu xin, goi tu `Hero.FinishCast`: dap gay khong lo vao cum quai dong nhat trong tam.</summary>
        public void CastUltimate()
        {
            if (_hero == null || _hero.IsDying) return;
            var definition = _hero.Definition;
            var centre = _hero.DensestCluster(_definition.ultimateRange, definition.skillRadius * 0.5f);
            if (!centre.HasValue) return;
            var at = centre.Value;
            float damage = _hero.AttackDamage * definition.skillPower;

            // Gay roi ~2 frame roi moi cham dat: sat thuong ra dung luc do. O cuoi (vet nut) giu toi het lam cham.
            var fx = new GameObject("GiantStaff");
            fx.transform.position = at + new Vector3(0f, StaffGroundBelowCentre * Battlefield.UnitScale, 0f);
            var frames = _definition.giantStaffFrames;
            if (frames != null && frames.Length > 0)
                OneShotFx.SpawnLoop(fx.transform, Vector3.zero, new[] { frames[frames.Length - 1] }, _definition.crackDuration,
                    SortOrder.GroundDecal + 1, frames, Battlefield.UnitScale);
            Destroy(fx, _definition.crackDuration + (frames != null ? frames.Length : 0) / FramesPerSecond);
            StartCoroutine(Smash(at, definition.skillRadius * 0.5f, _definition.staffHalfWidth, damage,
                definition.skillDuration, (StaffImpactFrame - 1) / FramesPerSecond, crack: true));

            // Moi phan than dap gay nho truoc mat no.
            foreach (var clone in _clones)
            {
                if (clone == null || clone.IsDying) continue;
                float side = clone.FacesRight ? 1f : -1f;
                var point = clone.transform.position + new Vector3(side * 0.6f, 0f, 0f);
                if (frames != null && frames.Length > 0)
                    OneShotFx.Spawn(point + new Vector3(0f, StaffGroundBelowCentre * Battlefield.UnitScale * 0.3f, 0f), frames,
                        !clone.FacesRight, SortOrder.Air, Battlefield.UnitScale * 0.3f);
                StartCoroutine(Smash(point, 0.8f, _definition.staffHalfWidth, damage * _definition.cloneSmashPower,
                    definition.skillDuration, (StaffImpactFrame - 1) / FramesPerSecond, crack: false));
            }
        }

        System.Collections.IEnumerator Smash(Vector3 at, float halfLength, float halfWidth, float damage, float stun,
            float delay, bool crack)
        {
            yield return new WaitForSeconds(delay);
            var stunFrames = _hero != null ? _hero.Definition.stunFrames : null;
            foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
            {
                if (enemy == null || enemy.IsDying) continue;
                var offset = enemy.AimPoint(at) - at;
                if (Mathf.Abs(offset.x) > halfLength + enemy.BodyRadius) continue;
                if (Mathf.Abs(offset.y) / Battlefield.DepthSquash > halfWidth) continue;
                enemy.ApplyStun(enemy.IsBoss ? stun * 0.5f : stun, stunFrames);
                if (crack) enemy.ApplySlow(_definition.crackSlow, _definition.crackDuration);
                enemy.Health.TakeDamage(damage, _hero != null ? _hero.Health : null);
            }
        }
    }
}
