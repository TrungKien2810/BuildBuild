using System.Collections;
using System.Linq;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Bo Nguoi Soi SR (`WerewolfKit`), gan vao `Hero` khi `HeroDefinition.werewolf` co:
    ///   - Quyen Kich (don nguoi): ×`humanPower`, toc danh = attackRate cua definition (dat nhanh san), moi don trung
    ///     +`humanManaPerHit` mana de hoa soi som wave dau. Don gian hoa co ghi nhan: toc danh 2 dang dung mot so
    ///     (khong tach nhanh/chuan), bu lai bang chi so soi.
    ///   - Huyết Thuc (passiveStar): hoi `leechHuman` (nguoi) / `leechWolf` (soi) sat thuong truc tiep that su vao HP
    ///     dich; phan tran thanh khien Mau Du (tran `shieldCapPct` mau toi da, tan sau `shieldFadeSeconds` khong danh trung).
    ///     Khong hut tu DoT/sao Soulbinder/phan khien do.
    ///   - Tru Nguyet (minorStar, chi soi): tu dung khi dang chan ≥2 dich: dich trong `howlRadius` bi Khiep So
    ///     `howlSeconds` (gay it hon `howlFear` sat thuong; boss chi -10%), hero +25% toc danh 4s.
    ///   - Ngoam Ha (secondMinorStar, chi soi): tu dung khi co dich can chien duoi `biteThreshold` mau: can ×`bitePower`;
    ///     ha duoc hoi 10% mau (tran vao Mau Du) va giam nua hoi chieu con lai. Boss: van can, khong ket lieu.
    ///   - Nguyet Bien (chieu xin, mana, dang nguoi): 1 LAN moi tran: clip bien hinh (bat tu trong clip), hoi
    ///     `transformHealPct` mau, VINH VIEN trong tran: mau toi da +`hpBonus` (mau hien tai cong cung luong),
    ///     cong +`atkBonus` (qua `Multipliers`), doi art sang `wolfSet`, mo phu (neu du 3 sao). Go thanh mana sau
    ///     bien hinh (ngung tich). Giu dang qua wave, tran moi lai dang nguoi.
    /// So CHUA can.
    /// </summary>
    [RequireComponent(typeof(Hero))]
    public class Werewolf : MonoBehaviour
    {
        Hero _hero;
        WerewolfKit _kit;
        bool _wolf;
        int _sweepCount;
        float _howlReady, _biteReady, _hasteUntil, _shieldAt;
        bool _transforming;

        /// <summary>Da hoa soi (thanh mana da go).</summary>
        public bool Transformed => _wolf;

        public void Initialise(Hero hero, WerewolfKit kit)
        {
            _hero = hero;
            _kit = kit;
            _wolf = _transforming = false;
            _sweepCount = 0;
            _howlReady = _biteReady = _hasteUntil = _shieldAt = 0f;
            _hero.Health.DamageDealt -= OnDamageDealt;
            _hero.Health.DamageDealt += OnDamageDealt;
        }

        void OnDisable()
        {
            if (_hero != null && _hero.Health != null) _hero.Health.DamageDealt -= OnDamageDealt;
        }

        void OnDamageDealt(float amount)
        {
            if (_kit == null || !_hero.PassiveUnlocked || amount <= 0f || _hero.IsDying) return;
            float heal = amount * (_wolf ? _kit.leechWolf : _kit.leechHuman);
            float missing = _hero.Health.Max - _hero.Health.Current;
            float overflow = Mathf.Max(0f, heal - Mathf.Max(0f, missing));
            _hero.Health.Heal(heal, _hero.Health);
            if (overflow > 0f)
            {
                float cap = _hero.Health.Max * _kit.shieldCapPct;
                float add = Mathf.Min(overflow, Mathf.Max(0f, cap - _hero.Health.Shield));
                if (add > 0f) _hero.Health.AddShield(add, _hero.Health);
            }
            _shieldAt = Time.time;
        }

        void Update()
        {
            if (_kit == null || _hero == null || _hero.IsDying || !WaveManager.Fighting) return;
            // Mau Du sinh ra ca o dang nguoi (Huyet Thuc hut ca hai dang) nen dong ho tan phai chay truoc khi loc dang soi.
            if (_hero.Health.Shield > 0f && Time.time - _shieldAt >= _kit.shieldFadeSeconds)
                _hero.Health.ClearShield();
            if (!_wolf) return;
            if (_hero.Casting || _hero.IsStunned || _hero.IsDisarmed) return;
            if (_hero.SecondMinorUnlocked && Time.time >= _biteReady && FindWeakPrey(out var prey)
                && _hero.PlayAction(_kit.biteClip))
            {
                _biteReady = Time.time + _kit.biteCooldown;
                StartCoroutine(Bite(prey));
                return;
            }
            if (_hero.MinorUnlocked && Time.time >= _howlReady && CountHeld() >= 2 && _hero.PlayAction(_kit.howlClip))
            {
                _howlReady = Time.time + _kit.howlCooldown;
                StartCoroutine(Howl());
            }
        }

        int CountHeld()
        {
            int n = 0;
            foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
            {
                if (enemy == null || enemy.IsDying) continue;
                if (Battlefield.GroundDistance(enemy.transform.position, transform.position) - enemy.BodyRadius
                    <= _kit.howlRadius + 1f) n++;
            }
            return n;
        }

        bool FindWeakPrey(out Unit prey)
        {
            prey = null;
            float best = float.PositiveInfinity;
            foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
            {
                if (enemy == null || enemy.IsDying) continue;
                if (Battlefield.GroundDistance(enemy.transform.position, transform.position) - enemy.BodyRadius > 1.2f) continue;
                float frac = enemy.Health.Current / Mathf.Max(1f, enemy.Health.Max);
                if (frac >= _kit.biteThreshold || frac >= best) continue;
                best = frac;
                prey = enemy;
            }
            return prey != null;
        }

        IEnumerator Howl()
        {
            yield return new WaitForSeconds(_hero.HitDelay(_kit.howlClip));
            if (_hero.IsDying || !_wolf) yield break;
            var at = transform.position;
            OneShotFx.Spawn(at, _kit.howlWave, false, SortOrder.Air, Battlefield.UnitScale);
            foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
            {
                if (enemy == null || enemy.IsDying) continue;
                if (Battlefield.GroundDistance(enemy.AimPoint(at), at) > _kit.howlRadius + enemy.BodyRadius) continue;
                enemy.ApplyAwe(enemy.IsBoss ? 0.1f : _kit.howlFear, _kit.howlSeconds);
            }
            _hasteUntil = Time.time + _kit.howlSeconds;
            _hero.OnHitsLanded(1);
        }

        IEnumerator Bite(Unit prey)
        {
            yield return new WaitForSeconds(_hero.HitDelay(_kit.biteClip));
            if (_hero.IsDying || !_wolf || prey == null || prey.IsDying) yield break;
            OneShotFx.Spawn(prey.BodyCenter, _kit.biteImpact, false, SortOrder.Air, Battlefield.UnitScale);
            prey.Health.TakeDamage(_hero.AttackDamage * _kit.bitePower, _hero.Health);
            if (prey.Health.IsDead || prey.IsDying)
            {
                _hero.Health.Heal(_hero.Health.Max * 0.1f, _hero.Health);
                OnDamageDealt(0.01f);  // nap lai dong ho tan khien Mau Du
                _biteReady = Time.time + (_biteReady - Time.time) * 0.5f;
            }
            _hero.OnHitsLanded(1);
        }

        /// <summary>He so cong (soi +`atkBonus`) + toc danh (Tru Nguyet +25%). Goi tu `Hero.AuraMultipliers`.</summary>
        public void Multipliers(ref float damage, ref float rate)
        {
            if (_kit == null) return;
            if (_wolf) damage *= 1f + _kit.atkBonus;
            if (Time.time < _hasteUntil) rate *= 1.25f;
        }

        /// <summary>Don thuong: nguoi dam nhanh yeu (+mana), soi cao (moi don thu 3 quet toi da 3 dich dang chan).</summary>
        public void Strike(Unit target, float damage)
        {
            if (!_wolf)
            {
                target.Health.TakeDamage(damage * _kit.humanPower, _hero.Health);
                OneShotFx.Spawn(target.BodyCenter, _kit.punchImpact, !_hero.FacesRight, SortOrder.Air, Battlefield.UnitScale);
                _hero.OnHitsLanded(1);
                _hero.AddMana(_kit.humanManaPerHit);
                return;
            }
            _sweepCount++;
            if (_sweepCount % Mathf.Max(1, _kit.sweepEvery) == 0)
            {
                // Day art vuot quet thap hon tam o 16 px -> nang 0.32 world cho vet vuot nam tren mat dat.
                OneShotFx.Spawn(transform.position + new Vector3(0f, 0.32f, 0f), _kit.clawSweep, !_hero.FacesRight, SortOrder.Air, Battlefield.UnitScale);
                int hits = 0;
                foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
                {
                    if (enemy == null || enemy.IsDying || hits >= 3) continue;
                    if (Battlefield.GroundDistance(enemy.transform.position, transform.position) - enemy.BodyRadius > 1.2f) continue;
                    bool main = enemy == target;
                    enemy.Health.TakeDamage(damage * _kit.wolfPower * (main ? 1f : _kit.sweepPower), _hero.Health);
                    hits++;
                }
                _hero.OnHitsLanded(hits);
            }
            else
            {
                target.Health.TakeDamage(damage * _kit.wolfPower, _hero.Health);
                OneShotFx.Spawn(target.BodyCenter, _kit.clawSlash, !_hero.FacesRight, SortOrder.Air, Battlefield.UnitScale);
                _hero.OnHitsLanded(1);
            }
        }

        /// <summary>Nguyet Bien, goi o frame cham cua clip cast (chi 1 lan/tran).</summary>
        public void CastUltimate()
        {
            if (_wolf || _transforming) return;
            _transforming = true;
            StartCoroutine(Transform());
        }

        IEnumerator Transform()
        {
            _hero.Health.Invulnerable = true;
            // Clip bien hinh DANG chay: no la clip `skill` cua hero, `CastUltimate` goi o frame cham — khong phat lai
            // (phat lai thi clip tua ve frame 1, bien hinh hai lan). Chi cho phan con lai cua clip.
            OneShotFx.Spawn(transform.position, _kit.transformBurst, false, SortOrder.Air, Battlefield.UnitScale);
            var clip = _kit.transformClip;
            float fps = _hero.Definition.animations != null ? _hero.Definition.animations.framesPerSecond : 0f;
            float seconds = clip.IsValid && fps > 0f ? Mathf.Max(0f, clip.frames.Length / fps - _hero.HitDelay(clip)) : 0.8f;
            yield return new WaitForSeconds(seconds);
            _hero.Health.Invulnerable = false;
            _transforming = false;
            if (_hero.IsDying) yield break;
            _wolf = true;
            _hero.SwapAnimations(_kit.wolfSet);
            _hero.Health.SetMax(_hero.Health.Max * (1f + _kit.hpBonus));
            _hero.Health.Heal(_hero.Health.Max * _kit.transformHealPct, _hero.Health);
            OneShotFx.Spawn(transform.position, _kit.howlWave, false, SortOrder.Air, Battlefield.UnitScale);
            _hero.ReleaseHeldClip();
        }
    }
}
