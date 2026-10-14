using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Bo Dao Si Ho Lo (`GourdTaoistKit`), gan vao `Hero` khi `HeroDefinition.gourdTaoist` co:
    ///   - Linh Khi Dan (don thuong, dan chung): moi don trung +1 Khi (tran `qiMax`).
    ///   - Thanh Khi Ho Than (passiveStar): chinh minh go hieu ung cho dong minh → khien `shieldPct` mau nguoi nhan
    ///     `shieldDuration` giay (moi dong minh 1 lan moi `shieldPerAllyCooldown`).
    ///   - Thu Ta (minorStar): tu dung khi co dong minh bi Doc/Chay/Cham trong `suctionRadius`: hut 1 loai khoi toi da
    ///     `suctionMaxAllies` nguoi, moi loai go duoc +1 Khi.
    ///   - Luyen Khi Pha (secondMinorStar): co ≥ `compressedCost` Khi thi tieu dung 2, ban vien nen no `compressedRadius`
    ///     + ap 1 tang Doc 4s (luat Doc chung).
    ///   - Can Khon Khai Ho (chieu xin, mana): hut sach Doc/Chay/Cham toi da `ultCleanseMax` dong minh (moi loai +1 Khi
    ///     toi tran) roi tieu toan bo Khi phun non `ultDuration` giay: `ultTicks` nhip, moi nhip ×(0,5 + 0,1 × Khi).
    ///     Non phu HET be sau san (quai spawn o do cao bat ky trong dai).
    /// So CHUA can. Khi giu qua wave cung tran, reset tran moi (`Initialise`).
    /// </summary>
    [RequireComponent(typeof(Hero))]
    public class GourdTaoist : MonoBehaviour
    {
        static readonly StatusId[] Cleansable = { StatusId.Poison, StatusId.Burn, StatusId.Slow };

        Hero _hero;
        GourdTaoistKit _kit;
        int _qi;
        float _suctionReady, _compressedReady;
        readonly Dictionary<Hero, float> _wardReady = new Dictionary<Hero, float>();

        public void Initialise(Hero hero, GourdTaoistKit kit)
        {
            _hero = hero;
            _kit = kit;
            _qi = 0;
            _suctionReady = _compressedReady = 0f;
            _wardReady.Clear();
        }

        void Update()
        {
            if (_kit == null || _hero == null || _hero.IsDying || !WaveManager.Fighting) return;
            if (_hero.Casting || _hero.IsStunned || _hero.IsDisarmed) return;
            if (_hero.SecondMinorUnlocked && Time.time >= _compressedReady && _qi >= _kit.compressedCost
                && _hero.AttackTargetInRange() != null && _hero.PlayAction(_kit.compressedCast))
            {
                _compressedReady = Time.time + _kit.compressedCooldown;
                _qi -= _kit.compressedCost;
                StartCoroutine(FireCompressed());
                return;
            }
            if (_hero.MinorUnlocked && Time.time >= _suctionReady
                && AfflictedAllies(_kit.suctionRadius, _kit.suctionMaxAllies).Count > 0
                && _hero.PlayAction(_kit.suctionCast))
            {
                if (TrySuction()) _suctionReady = Time.time + _kit.suctionCooldown;
            }
        }

        /// <summary>Don thuong (dan chung) vua trung: +1 Khi toi tran.</summary>
        public void OnBasicHit(Unit enemy)
        {
            if (_kit == null || enemy == null) return;
            _qi = Mathf.Min(_kit.qiMax, _qi + 1);
        }

        bool TrySuction()
        {
            var allies = AfflictedAllies(_kit.suctionRadius, _kit.suctionMaxAllies);
            if (allies.Count == 0) return false;
            foreach (var ally in allies)
            {
                foreach (var id in Cleansable)
                {
                    if (!ally.Status.Has(id)) continue;
                    ally.Status.Remove(id);
                    _qi = Mathf.Min(_kit.qiMax, _qi + 1);
                    Ward(ally);
                    OneShotFx.Spawn(ally.BodyCenter, _kit.suction, false, SortOrder.Air, Battlefield.UnitScale);
                    break;  // 1 loai = 1 Khi
                }
            }
            return true;
        }

        List<Hero> AfflictedAllies(float radius, int max)
        {
            var at = transform.position;
            return Hero.Alive
                .Where(a => a != null && !a.IsDying && a.Side == _hero.Side
                    && Battlefield.GroundDistance(a.transform.position, at) - a.BodyRadius <= radius
                    && Cleansable.Any(id => a.Status.Has(id)))
                .OrderByDescending(a => Cleansable.Count(id => a.Status.Has(id)))
                .ThenBy(a => a.Health.Fraction)
                .Take(Mathf.Max(1, max)).ToList();
        }

        void Ward(Hero ally)
        {
            if (!_hero.PassiveUnlocked) return;
            if (_wardReady.TryGetValue(ally, out float ready) && Time.time < ready) return;
            _wardReady[ally] = Time.time + _kit.shieldPerAllyCooldown;
            // Don gian hoa co ghi nhan: khien ton tai toi khi bi danh het (khong tu tan sau `shieldDuration`) —
            // can moc het han tren tung dong minh, de danh can bang sau.
            ally.Health.AddShield(ally.Health.Max * _kit.shieldPct, _hero.Health);
            OneShotFx.Spawn(ally.BodyCenter, _kit.shield, false, SortOrder.Air, Battlefield.UnitScale);
        }

        IEnumerator FireCompressed()
        {
            yield return new WaitForSeconds(_hero.HitDelay(_kit.compressedCast));
            if (_hero.IsDying) yield break;
            var target = _hero.AttackTargetInRange();
            if (target == null) yield break;
            var at = target.transform.position;
            TravellingHeroFx.Spawn(_hero.BodyCenter, target.BodyCenter, _kit.compressedBolt, 0.25f);
            yield return new WaitForSeconds(0.25f);
            if (_hero.IsDying) yield break;
            OneShotFx.Spawn(at, _kit.compressedBlast, false, SortOrder.Air, Battlefield.UnitScale);
            foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
            {
                if (enemy == null || enemy.IsDying) continue;
                if (Battlefield.GroundDistance(enemy.AimPoint(at), at) > _kit.compressedRadius + enemy.BodyRadius) continue;
                enemy.Health.TakeDamage(_hero.AttackDamage * 2f, _hero.Health);
                if (!enemy.IsDying)
                    enemy.Status.Apply(StatusId.Poison, 2f, 4f, _hero.Health, _hero.Side, 1, 5, fx: null);
            }
            _hero.OnHitsLanded(1);
        }

        /// <summary>Can Khon Khai Ho, goi o frame cham cua clip cast.</summary>
        public void CastUltimate()
        {
            var cleaned = AfflictedAllies(_kit.ultCleanseRadius, _kit.ultCleanseMax);
            foreach (var ally in cleaned)
            {
                int kinds = 0;
                foreach (var id in Cleansable)
                    if (ally.Status.Has(id)) { ally.Status.Remove(id); kinds++; }
                for (int i = 0; i < kinds; i++) _qi = Mathf.Min(_kit.qiMax, _qi + 1);
                Ward(ally);
            }
            int spent = _qi;
            _qi = 0;
            StartCoroutine(BreatheCone(spent));
        }

        IEnumerator BreatheCone(int spent)
        {
            float facing = _hero.FacesRight ? 1f : -1f;
            var origin = transform.position;
            var end = origin + new Vector3(facing * _kit.ultConeLength, 0f, 0f);
            float perTick = _hero.AttackDamage * (0.5f + 0.1f * spent);
            for (int i = 0; i < _kit.ultTicks; i++)
            {
                if (_hero.IsDying) yield break;
                // Non phun la vung NAM NGANG dat tren lan: do X toi `ultConeLength` truoc mat, chieu sau phu HET san
                // (quai spawn o do cao bat ky); be day cu 0,6 don vi chi cham ~1/6 so quai di qua.
                // Hinh: MOT non o co goc — KHONG keo gian theo Y (o non 2,56 world, keo 4,3x = non nhoe doc).
                LaneFx.SpawnLane(origin + ConeOffset(facing), _kit.cone, _hero.FacesRight, SortOrder.Air,
                    Battlefield.UnitScale);
                foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
                {
                    if (enemy == null || enemy.IsDying) continue;
                    if (!Battlefield.InLaneBand(enemy, origin, end)) continue;
                    enemy.Health.TakeDamage(perTick, _hero.Health);
                }
                yield return new WaitForSeconds(_kit.ultDuration / Mathf.Max(1, _kit.ultTicks));
            }
            _hero.ReleaseHeldClip();
        }

        /// <summary>
        /// Chỗ đặt nón phun so với chân hero: **mép MIỆNG của mây nằm ngay trước mặt hero**, phần loe ra chạy về
        /// phía trước. Art vẽ miệng ở mép PHẢI ô (cột phải chỉ cao 31–41 px, cột trái 145–166 px) rồi loe dần về
        /// trái → `OneShotFx` lật lại khi hero quay phải, nên mép gần là mép trái của sprite.
        ///
        /// Ô art rộng 256 px = 5,12 world ở `UnitScale`; mép ô lệch mép mây 23 art-px đệm trong suốt
        /// (nội dung dừng ở px 233) = 0,46 world — trừ ra, nếu không mây hở một khoảng trước mặt hero.
        /// Lấy nửa ô từ chính sprite chứ không ghi số cứng: art đổi cỡ thì chỗ đặt tự đi theo.
        /// </summary>
        Vector3 ConeOffset(float facing)
        {
            // Thieu art non: `cone[0]` nem loi giua coroutine -> khong toi `ReleaseHeldClip`, hero ket clip cast.
            if (_kit.cone == null || _kit.cone.Length == 0) return new Vector3(0f, ConeY, 0f);
            float half = _kit.cone[0].bounds.extents.x * Battlefield.UnitScale - ConeMouthPadding;
            return new Vector3(facing * half, ConeY, 0f);
        }

        /// <summary>Đệm trong suốt mép miệng của clip nón (world, đã nhân `UnitScale`).</summary>
        const float ConeMouthPadding = 0.46f;
        /// <summary>
        /// Độ cao tâm ô art — mây phủ ngang tầm thân hero, không cần hạ xuống chạm đất. Từ lúc nón được kéo phủ hết
        /// bề sau sân (11 đơn vị) thì độ cao này chỉ còn là sai lệch nhỏ, hình vẫn trùm cả dải.
        /// </summary>
        const float ConeY = 0.4f;
    }
}
