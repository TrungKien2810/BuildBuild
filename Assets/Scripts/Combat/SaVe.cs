using System.Collections;
using System.Linq;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Bo Sa Ve (`SaVeKit`), gan vao `Hero` khi `HeroDefinition.saVe` co:
    ///   - Thach Quyen (don thuong can chien): cham, vua.
    ///   - Tu Sa (passiveStar): chiu don TRUC TIEP tich 1 tang (toi da `sandMax`, don cach nhau toi thieu
    ///     `sandMinInterval`); moi tang giam `sandResistPerStack` sat thuong nhan (DoT khong gop tang).
    ///   - Tram Sa Quyen (minorStar): quat dat ngan phia truoc (nay do X toi `SlamReach`, chieu sau phu het be sau san)
    ///     + cham `slamSlow` `slamSlowSeconds` giay.
    ///   - Boi Thach (secondMinorStar): du 2 tang va dang thuong thi tieu `healCost` tang hoi `healPct` mau.
    ///   - Sa Thanh Tran Tuyen (chieu xin, mana): 1 nhip sat thuong quanh minh + vung cat `fortDuration` giay ban kinh
    ///     `fortRadius`: dich cham, ban than trong vung giam `fortSelfResist` moi sat thuong. Vung phu het be sau san
    ///     (`fullLane`): quai di hang nao cung dinh, khong con chi an hang cua ban than.
    ///     CHUA XONG: dong minh trong vung bot sat thuong dan xa (`fortAllyRangedResist`) — can moc vao pipeline nhan
    ///     dan cua dong minh, de danh follow-up (khong cat chuc nang hien co).
    /// Khong tang so quai chan. So CHUA can.
    /// </summary>
    [RequireComponent(typeof(Hero))]
    public class SaVe : MonoBehaviour
    {
        /// <summary>
        /// Quat dat dai bao xa truoc mat (don vi mat dat). Truoc day chi 1,2 nen nhat quet mong hon ca be sau san
        /// (11 don vi): quai di o hang khac trong cung khoang do khong bao gio dinh.
        /// </summary>
        const float SlamReach = 2.4f;

        Hero _hero;
        SaVeKit _kit;
        int _sand;
        float _sandAt, _slamReady, _healReady;
        Vector3 _fortAt;
        float _fortUntil;

        public void Initialise(Hero hero, SaVeKit kit)
        {
            _hero = hero;
            _kit = kit;
            _sand = 0;
            _sandAt = _slamReady = _healReady = _fortUntil = 0f;
            _hero.Health.AdjustDirectHit = AdjustSand;
        }

        void OnDisable()
        {
            if (_hero != null && _hero.Health != null && _hero.Health.AdjustDirectHit == AdjustSand)
                _hero.Health.AdjustDirectHit = null;
        }

        float AdjustSand(float amount, Health source)
        {
            if (_kit == null || !_hero.PassiveUnlocked || source == null || source == _hero.Health) return amount;
            if (Time.time - _sandAt >= _kit.sandMinInterval && _sand < _kit.sandMax)
            {
                _sand++;
                _sandAt = Time.time;
                OneShotFx.Spawn(transform.position, _kit.sandCore, false, SortOrder.Air, Battlefield.UnitScale);
            }
            float resist = _sand * _kit.sandResistPerStack;
            if (Time.time < _fortUntil
                && Battlefield.GroundDistance(transform.position, _fortAt) <= _kit.fortRadius)
                resist += _kit.fortSelfResist;
            return amount * Mathf.Max(0f, 1f - resist);
        }

        void Update()
        {
            if (_kit == null || _hero == null || _hero.IsDying || !WaveManager.Fighting) return;
            if (_hero.Casting || _hero.IsStunned || _hero.IsDisarmed) return;
            var target = _hero.AttackTargetInRange();
            if (_hero.SecondMinorUnlocked && Time.time >= _healReady && _sand >= _kit.healCost
                && _hero.Health.Fraction < 1f && _hero.PlayAction(_kit.healCast))
            {
                _healReady = Time.time + _kit.healCooldown;
                _sand -= _kit.healCost;
                _hero.Health.Heal(_hero.Health.Max * _kit.healPct, _hero.Health);
                // Quang hoi la art MAT DAT (day o tam o) -> dat o chan, khong o tam than.
                OneShotFx.Spawn(transform.position, _kit.heal, false, SortOrder.Air, Battlefield.UnitScale);
                return;
            }
            if (_hero.MinorUnlocked && target != null && Time.time >= _slamReady && _hero.PlayAction(_kit.slamCast))
            {
                _slamReady = Time.time + _kit.slamCooldown;
                StartCoroutine(Slam());
            }
        }

        /// <summary>Don thuong: dam truc tiep muc tieu.</summary>
        public void Strike(Unit target, float damage)
        {
            target.Health.TakeDamage(damage, _hero.Health);
            OneShotFx.Spawn(target.BodyCenter, _hero.Definition.impactFrames, !_hero.FacesRight,
                SortOrder.Air, Battlefield.UnitScale);
            _hero.OnHitsLanded(1);
        }

        IEnumerator Slam()
        {
            yield return new WaitForSeconds(_hero.HitDelay(_kit.slamCast));
            if (_hero.IsDying) yield break;
            float facing = _hero.FacesRight ? 1f : -1f;
            var origin = transform.position;
            OneShotFx.Spawn(origin + new Vector3(facing * 0.7f, 0f, 0f), _kit.quake,
                !_hero.FacesRight, SortOrder.GroundDecal + 1, Battlefield.UnitScale);
            foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
            {
                if (enemy == null || enemy.IsDying) continue;
                if (!Battlefield.InLaneBand(enemy, origin, origin + new Vector3(facing * SlamReach, 0f, 0f))) continue;
                enemy.Health.TakeDamage(_hero.AttackDamage * _kit.slamPower, _hero.Health);
                if (!enemy.IsBoss) enemy.ApplySlow(_kit.slamSlow, _kit.slamSlowSeconds);
            }
            _hero.OnHitsLanded(1);
        }

        /// <summary>Sa Thanh Tran Tuyen, goi o frame cham cua clip cast.</summary>
        public void CastUltimate()
        {
            var at = transform.position;
            foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
            {
                if (enemy == null || enemy.IsDying) continue;
                // Cung luat vung cat ben duoi (`fullLane`): do X, phu het be sau — quai hang khac dinh cham ma khong
                // an nhip no la hai vung lech nhau.
                if (!Battlefield.InLaneColumn(enemy, at.x, 1.2f)) continue;
                enemy.Health.TakeDamage(_hero.AttackDamage * _kit.fortPower, _hero.Health);
            }
            _fortAt = at;
            _fortUntil = Time.time + _kit.fortDuration;
            OneShotFx.Spawn(at, _kit.fortTelegraph, false, SortOrder.GroundDecal + 1, Battlefield.UnitScale);
            SkillZone.Spawn(at, _kit.fortRadius, _kit.fortDuration, _hero.Side, _kit.fortLoop, Battlefield.UnitScale,
                everySecond: enemy =>
                {
                    if (!enemy.IsBoss) enemy.ApplySlow(0.3f, 1.1f);
                },
                outro: _kit.fortFade, fullLane: true);
            _hero.OnHitsLanded(1);
        }
    }
}
