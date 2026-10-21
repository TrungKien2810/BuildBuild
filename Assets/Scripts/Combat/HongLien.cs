using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Bo Vu Nu Hong Lien (`HongLienKit`), gan vao `Hero` khi `HeroDefinition.hongLien` co:
    ///   - Phien Hoa (don thuong, dan lua): don le, khong xuyen.
    ///   - Gieo Hong Lien (passiveStar): moi don thu 3 TRUNG dich dat 1 nu sen tai mat dat duoi dich luc trung
    ///     (toi da `budMax`, ton tai `budSeconds`; thua thi nu cu nhat tan, khong no). Nu khong bam dich.
    ///   - Song Phien Vu (minorStar): 2 lan lua quat hep ve phia muc tieu, khong tang dem gieo.
    ///   - Lien Tam Dan (secondMinorStar): kich no nu gan muc tieu hien tai nhat (vung `detonateRadius`, ×`detonatePower`);
    ///     khong co nu hop le thi giu chieu.
    ///   - Hong Lien Dai Vu (chieu xin, mana): tung nu lon tai muc tieu roi no nu lon (×`grandPower`, ban kinh
    ///     `grandRadius`) + toan bo nu thuong; cung 1 dich chiu duoc nhieu nu chong vung.
    /// So CHUA can.
    /// </summary>
    [RequireComponent(typeof(Hero))]
    public class HongLien : MonoBehaviour
    {
        class Bud
        {
            public Vector3 at;
            public float until;
            public GameObject fx;
        }

        Hero _hero;
        HongLienKit _kit;
        int _hitCount;
        float _fanReady, _detonateReady;
        readonly List<Bud> _buds = new List<Bud>();

        public void Initialise(Hero hero, HongLienKit kit)
        {
            _hero = hero;
            _kit = kit;
            _hitCount = 0;
            _fanReady = _detonateReady = 0f;
            ClearBuds();
        }

        void OnDisable() => ClearBuds();

        void ClearBuds()
        {
            foreach (var b in _buds) if (b.fx != null) Destroy(b.fx);
            _buds.Clear();
        }

        void Update()
        {
            if (_kit == null || _hero == null || _hero.IsDying || !WaveManager.Fighting) return;
            TickBuds();
            if (_hero.Casting || _hero.IsStunned || _hero.IsDisarmed) return;
            var target = _hero.AttackTargetInRange();
            if (_hero.SecondMinorUnlocked && target != null && Time.time >= _detonateReady && NearestBud(target) != null
                && _hero.PlayAction(_kit.detonateCast))
            {
                _detonateReady = Time.time + _kit.detonateCooldown;
                StartCoroutine(Detonate(target));
                return;
            }
            if (_hero.MinorUnlocked && target != null && Time.time >= _fanReady && _hero.PlayAction(_kit.fanCast))
            {
                _fanReady = Time.time + _kit.fanCooldown;
                StartCoroutine(Fan(target));
            }
        }

        /// <summary>Don thuong vua trung: dem gieo sen (chi don thu 3).</summary>
        public void OnBasicHit(Unit enemy)
        {
            if (_kit == null || !_hero.PassiveUnlocked || enemy == null || enemy.IsDying) return;
            if (++_hitCount % Mathf.Max(1, _kit.seedEvery) != 0) return;
            PlantBud(enemy.transform.position, _kit.lotusBud);
        }

        void PlantBud(Vector3 at, Sprite[] loopFx)
        {
            while (_buds.Count >= _kit.budMax)
            {
                var old = _buds[0];
                _buds.RemoveAt(0);
                if (old.fx != null) Destroy(old.fx);
            }
            var fx = OneShotFx.SpawnLoop(null, at, loopFx, _kit.budSeconds, SortOrder.GroundDecal + 1,
                scale: Battlefield.UnitScale);
            _buds.Add(new Bud { at = at, until = Time.time + _kit.budSeconds, fx = fx });
        }

        void TickBuds()
        {
            for (int i = _buds.Count - 1; i >= 0; i--)
            {
                var b = _buds[i];
                if (Time.time < b.until) continue;
                _buds.RemoveAt(i);
                OneShotFx.Spawn(b.at, _kit.lotusFade, false, SortOrder.GroundDecal + 1, Battlefield.UnitScale);
                if (b.fx != null) Destroy(b.fx);
            }
        }

        /// <summary>
        /// Nu gan muc tieu nhat MA cot no cua no (`detonateRadius`) phu toi muc tieu — khong co thi null (giu chieu).
        /// Ban cu khong gioi han khoang cach: no ca nu o dau kia san, khong trung ai.
        /// </summary>
        Bud NearestBud(Unit target)
        {
            var at = target.transform.position;
            Bud best = null;
            float bestDistance = float.PositiveInfinity;
            foreach (var b in _buds)
            {
                if (!Battlefield.InLaneColumn(target, b.at.x, _kit.detonateRadius)) continue;
                float d = Mathf.Abs(b.at.x - at.x);
                if (d < bestDistance) { bestDistance = d; best = b; }
            }
            return best;
        }

        bool PopBud(Bud bud, Sprite[] bloomFx, float power, float radius)
        {
            _buds.Remove(bud);
            if (bud.fx != null) Destroy(bud.fx);
            // Nu no theo COT phu het be sau san (khong phai vong tron): quai di hang nao cung phai an,
            // vong tron ban kinh 0,8-1,0 chi cham ~1/6 so quai (do 2026-10-10). Hinh: mot tam moi hang hero.
            LaneFx.SpawnRowsOnce(bud.at.x, bloomFx, false, SortOrder.Air, Battlefield.UnitScale);
            bool landed = false;
            foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
            {
                if (enemy == null || enemy.IsDying) continue;
                if (!Battlefield.InLaneColumn(enemy, bud.at.x, radius)) continue;
                float before = enemy.Health.Current;
                enemy.Health.TakeDamage(_hero.AttackDamage * power, _hero.Health);
                landed |= enemy.Health.Current < before;
            }
            CombatFeedback.Cue("explode");
            return landed;
        }

        IEnumerator Fan(Unit target)
        {
            yield return new WaitForSeconds(_hero.HitDelay(_kit.fanCast));
            if (_hero.IsDying || target == null) yield break;
            float facing = _hero.FacesRight ? 1f : -1f;
            var origin = transform.position;
            var reach = origin + new Vector3(facing * _kit.fanLength, 0f, 0f);
            // Luoi lua quat phu het be sau san -> mot tam o MOI hang hero. KHONG keo gian theo Y: o FX vuong
            // 1,28 world, keo cho phu 11 don vi be sau la 8,6x -> nguoi dung thay mot vet nhoe doc (2026-10-10).
            LaneFx.SpawnRowsOnce(origin.x, _kit.fanFlame, !_hero.FacesRight, SortOrder.Air, Battlefield.UnitScale);
            foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
            {
                if (enemy == null || enemy.IsDying) continue;
                var d = enemy.transform.position - origin;
                if (d.x * facing < -enemy.BodyRadius) continue;
                if (!Battlefield.InLaneBand(enemy, origin, reach)) continue;
                enemy.Health.TakeDamage(_hero.AttackDamage * _kit.fanPower, _hero.Health);
            }
            _hero.OnHitsLanded(1);
        }

        IEnumerator Detonate(Unit target)
        {
            yield return new WaitForSeconds(_hero.HitDelay(_kit.detonateCast));
            if (_hero.IsDying || target == null || target.IsDying) yield break;
            var bud = NearestBud(target);
            if (bud == null) yield break;
            if (PopBud(bud, _kit.lotusBloom, _kit.detonatePower, _kit.detonateRadius))
                CombatFeedback.HeavyHit(Vector2.up, 0.03f);
            _hero.OnHitsLanded(1);
        }

        /// <summary>Hong Lien Dai Vu, goi o frame cham cua clip cast.</summary>
        public void CastUltimate()
        {
            var target = _hero.AttackTargetInRange();
            var at = target != null ? target.transform.position : transform.position + new Vector3(_hero.FacesRight ? 2f : -2f, 0f, 0f);
            // No het nu thuong TRUOC roi moi trong nu lon: trong truoc thi luc da du `budMax`, `PlantBud` xoa nu cu
            // nhat ma khong no (mat sat thuong cua nu do).
            bool landed = false;
            foreach (var bud in _buds.ToArray())
                landed |= PopBud(bud, _kit.lotusBloom, _kit.grandPower, _kit.grandRadius);
            PlantBud(at, _kit.greatBud);
            landed |= PopBud(_buds[_buds.Count - 1], _kit.greatBloom, _kit.grandPower, _kit.grandRadius);
            if (landed) CombatFeedback.HeavyHit(Vector2.up, 0.065f);
            _hero.OnHitsLanded(1);
        }
    }
}
