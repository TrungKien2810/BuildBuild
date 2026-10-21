using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Bo Ngu But Su (`InkScribeKit`), gan vao `Hero` khi `HeroDefinition.inkScribe` co:
    ///   - Mac Diem (don thuong, dan muc): don truc tiep cua chinh minh dat 1 dau But (toi da `sealMax`, ton tai
    ///     `sealSeconds`); du dau thi no them `sealPopPower` (moi dich 1 dau moi `sealMinInterval`).
    ///   - Hoanh But (minorStar): ve net dai `strokeLength` cat ngang huong tien cua dich (dat truoc
    ///     dich `strokeAhead`), ton tai `strokeDuration`; dich dat buoc qua an 1 hit (moi dich 1 hit/net, toi da
    ///     `strokeMax` net). Net phu HET be sau san (moi hang deu dinh, `Battlefield.InLaneBand`). Dich bay mien.
    ///   - Tran Son An (secondMinorStar): trien xuong cum dich dat dong nhat: bao truoc 0,4s roi gay sat thuong ban kinh
    ///     `sealRadius` + troi `sealStun` (tinh anh mot nua, boss mien troi).
    ///   - Hoa Long Diem Tinh (chieu xin, mana): mac long quet dai `dragonLength` phu het be sau gay 3x roi no cuoi
    ///     duong ban kinh `dragonBlast` gay 2x; danh ca dat/bay. Moi dich toi da 1 hit quet + 1 hit no.
    /// Net la vung phep, khong can duong; dich dung yen trong net luc hien nhan 1 hit ngay. So CHUA can.
    /// </summary>
    [RequireComponent(typeof(Hero))]
    public class InkScribe : MonoBehaviour
    {
        class Stroke
        {
            public Vector3 at;
            public Vector3 dir;
            public float until;
            public GameObject[] fx;      // mot tam moi hang hero (net phu het be sau san)
            public readonly HashSet<Unit> hit = new HashSet<Unit>();
        }

        /// <summary>
        /// Phong art Tran Son An so voi `UnitScale` (nguoi dung 2026-10-10: an "nho qua"). O ve 1x an rong 80 art px
        /// = 1,6 world, chi bang chieu ngang mot quai; x1,5 -> 2,4 world, khop `sealRadius` 1,2 trong kit.
        /// </summary>
        const float SealFxScale = 1.5f;

        Hero _hero;
        InkScribeKit _kit;
        float _strokeReady, _sealReady;
        readonly Dictionary<Unit, (int stacks, float until, float nextMark)> _seals =
            new Dictionary<Unit, (int, float, float)>();
        readonly List<Stroke> _strokes = new List<Stroke>();

        public void Initialise(Hero hero, InkScribeKit kit)
        {
            _hero = hero;
            _kit = kit;
            _strokeReady = _sealReady = 0f;
            _seals.Clear();
            foreach (var s in _strokes) KillStroke(s);
            _strokes.Clear();
        }

        /// <summary>Xoa cac tam FX cua mot net (mot tam moi hang hero).</summary>
        static void KillStroke(Stroke stroke)
        {
            if (stroke.fx == null) return;
            foreach (var fx in stroke.fx) if (fx != null) Destroy(fx);
            stroke.fx = null;
        }

        void OnDisable()
        {
            foreach (var s in _strokes) KillStroke(s);
            _strokes.Clear();
            _seals.Clear();
        }

        void Update()
        {
            if (_kit == null || _hero == null || _hero.IsDying || !WaveManager.Fighting) return;
            TickStrokes();
            if (_hero.Casting || _hero.IsStunned || _hero.IsDisarmed) return;
            if (_hero.SecondMinorUnlocked && Time.time >= _sealReady && DensestGround(_hero.Definition.range) != null
                && _hero.PlayAction(_kit.sealCast))
            {
                _sealReady = Time.time + _kit.sealCooldown;
                StartCoroutine(DropSeal());
                return;
            }
            if (_hero.MinorUnlocked && Time.time >= _strokeReady)
            {
                var target = _hero.AttackTargetInRange();
                if (target != null && _hero.PlayAction(_kit.horizontalCast))
                {
                    _strokeReady = Time.time + _kit.strokeCooldown;
                    StartCoroutine(DrawStroke(target));
                }
            }
        }

        /// <summary>Don truc tiep cua chinh minh vua trung: dat dau But.</summary>
        public void OnBasicHit(Unit enemy)
        {
            if (_kit == null || !_hero.PassiveUnlocked || enemy == null || enemy.IsDying) return;
            if (!_seals.TryGetValue(enemy, out var s) || Time.time >= s.until) s = (0, 0f, 0f);
            if (Time.time < s.nextMark) return;
            s.nextMark = Time.time + _kit.sealMinInterval;
            s.until = Time.time + _kit.sealSeconds;
            s.stacks++;
            OneShotFx.Spawn(enemy.BodyCenter, _kit.brushMarks, false, SortOrder.Air, Battlefield.UnitScale);
            if (s.stacks >= _kit.sealMax)
            {
                enemy.Health.TakeDamage(_hero.AttackDamage * _kit.sealPopPower, _hero.Health);
                _seals.Remove(enemy);
            }
            else _seals[enemy] = s;
        }

        IEnumerator DrawStroke(Unit target)
        {
            yield return new WaitForSeconds(_hero.HitDelay(_kit.horizontalCast));
            if (_hero.IsDying || target == null) yield break;
            CombatFeedback.Cue("spell");
            // Huong tien cua dich: quai phe dich di ve lau dai phe ta theo -x (`Enemy`: `Vector3.left`), nen net dat
            // PHIA -x cua muc tieu. Ban cu de +1 -> net nam sau lung quai, nguoi choi thay chieu "di nguoc".
            var dir = new Vector3(_hero.Side == Faction.Player ? -1f : 1f, 0f, 0f);
            var at = target.transform.position + dir * _kit.strokeAhead;
            at = Battlefield.Clamp(at, 0.2f, clampRight: true);
            while (_strokes.Count >= _kit.strokeMax)
            {
                var old = _strokes[0];
                _strokes.RemoveAt(0);
                KillStroke(old);
            }
            // Offset FX duoi day = do day art so voi tam o (pivot center) quy ra world, de net nam tren mat dat.
            // Net cat ca be sau san (dich chieu nao di qua cung dinh) -> ve mot tam o MOI hang hero. KHONG keo
            // gian art theo Y: o FX vuong 1,28 world, keo cho phu 11 don vi be sau la 8,6x -> thay bang vet nhoe.
            var fx = LaneFx.SpawnRows(null, at.x, _kit.horizontalStroke, _kit.strokeDuration,
                SortOrder.GroundDecal + 1, scale: Battlefield.UnitScale, yOffset: 0.22f * Battlefield.UnitScale);
            var stroke = new Stroke { at = at, dir = dir, until = Time.time + _kit.strokeDuration, fx = fx };
            _strokes.Add(stroke);
            StrikeStroke(stroke, instant: true);
        }

        void TickStrokes()
        {
            for (int i = _strokes.Count - 1; i >= 0; i--)
            {
                var s = _strokes[i];
                if (Time.time >= s.until)
                {
                    KillStroke(s);
                    _strokes.RemoveAt(i);
                    continue;
                }
                StrikeStroke(s, instant: false);
            }
            // Don dau het han / dich chet.
            var dead = _seals.Where(kv => kv.Key == null || kv.Key.IsDying || Time.time >= kv.Value.until)
                .Select(kv => kv.Key).ToArray();
            foreach (var u in dead) _seals.Remove(u);
        }

        void StrikeStroke(Stroke s, bool instant)
        {
            float half = _kit.strokeLength * 0.5f;
            // Net keo ngang ca be sau: doan quet la [at - nua dai .. at + nua dai] theo X, chieu sau phu het san.
            var from = s.at - s.dir * half;
            var to = s.at + s.dir * half;
            foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
            {
                if (enemy == null || enemy.IsDying || enemy.FliesHigh || s.hit.Contains(enemy)) continue;
                if (!Battlefield.InLaneBand(enemy, from, to)) continue;
                s.hit.Add(enemy);
                enemy.Health.TakeDamage(_hero.AttackDamage * 1.5f, _hero.Health);
            }
            if (instant) _hero.OnHitsLanded(1);
        }

        IEnumerator DropSeal()
        {
            yield return new WaitForSeconds(_hero.HitDelay(_kit.sealCast));
            if (_hero.IsDying) yield break;
            var centre = DensestGround(_hero.Definition.range);
            if (centre == null) yield break;
            var at = centre.Value;
            // Day art an o duoi tam o 41 px (zone) / 22 px (burst) -> nang theo dung scale ve de day cham mat dat.
            OneShotFx.Spawn(at + new Vector3(0f, 0.84f * SealFxScale, 0f), _kit.sealZone, false, SortOrder.GroundDecal + 1,
                Battlefield.UnitScale * SealFxScale);
            yield return new WaitForSeconds(0.4f);
            if (_hero.IsDying) yield break;
            OneShotFx.Spawn(at + new Vector3(0f, 0.43f * SealFxScale, 0f), _kit.sealBurst, false, SortOrder.Air,
                Battlefield.UnitScale * SealFxScale);
            CombatFeedback.Cue("skill-stone");
            bool landed = false;
            foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
            {
                if (enemy == null || enemy.IsDying || enemy.FliesHigh) continue;
                if (Battlefield.GroundDistance(enemy.AimPoint(at), at) > _kit.sealRadius + enemy.BodyRadius) continue;
                float before = enemy.Health.Current;
                enemy.Health.TakeDamage(_hero.AttackDamage * 1.5f, _hero.Health);
                landed |= enemy.Health.Current < before;
                if (!enemy.IsBoss)
                    enemy.ApplyStun(enemy.IsElite ? _kit.sealStun * 0.5f : _kit.sealStun, _hero.Definition.stunFrames);
            }
            if (landed) CombatFeedback.HeavyHit(Vector2.down, 0.03f);
            _hero.OnHitsLanded(1);
        }

        Vector3? DensestGround(float range)
        {
            Unit best = null;
            int bestCount = 0;
            var origin = transform.position;
            foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
            {
                if (enemy == null || enemy.IsDying || enemy.FliesHigh) continue;
                if (Battlefield.GroundDistance(enemy.transform.position, origin) - enemy.BodyRadius > range) continue;
                int count = 0;
                foreach (var other in Unit.Hostiles(_hero.Side).ToArray())
                {
                    if (other == null || other.IsDying || other.FliesHigh) continue;
                    if (Battlefield.GroundDistance(other.transform.position, enemy.transform.position)
                        - other.BodyRadius <= _kit.sealRadius * 2f) count++;
                }
                if (count > bestCount) { bestCount = count; best = enemy; }
            }
            return best != null ? best.transform.position : (Vector3?)null;
        }

        /// <summary>Hoa Long Diem Tinh, goi o frame cham cua clip cast.</summary>
        public void CastUltimate()
        {
            float facing = _hero.FacesRight ? 1f : -1f;
            var origin = transform.position;
            var end = origin + new Vector3(facing * _kit.dragonLength, 0f, 0f);
            // Vet mac duoi dat chinh la VUNG TRUNG (mac long quet het be sau san), khong con la duong bao:
            // mot tam o MOI hang hero. KHONG keo gian art theo Y (o 2,56 world keo 4,3x = vet nhoe doc).
            // Art ve MUI TEN + vach gach chi sang TRAI (prompt-used: "directional ground strip facing LEFT"), nen hero
            // nhin PHAI phai lat; ban cu truyen `!FacesRight` -> vet chi ve nguoc ve lai phia thanh (nguoi dung 2026-10-21).
            LaneFx.SpawnRowsOnce((origin + end).x * 0.5f, _kit.dragonPath, _hero.FacesRight,
                SortOrder.GroundDecal + 1, Battlefield.UnitScale);
            // Mac long bay o tam cao than doc dai quet.
            var flight = new Vector3(0f, _hero.BodyHeight * 0.5f, 0f);
            TravellingHeroFx.Spawn(origin + flight, end + flight, _kit.inkDragon, 0.6f);
            OneShotFx.Spawn(origin, _kit.dragonPainting, !_hero.FacesRight, SortOrder.Air, Battlefield.UnitScale);
            var swept = new HashSet<Unit>();
            bool landed = false;
            foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
            {
                if (enemy == null || enemy.IsDying) continue;
                if (!Battlefield.InLaneBand(enemy, origin, end)) continue;
                swept.Add(enemy);
                float before = enemy.Health.Current;
                enemy.Health.TakeDamage(_hero.AttackDamage * 3f, _hero.Health);
                landed |= enemy.Health.Current < before;
            }
            OneShotFx.Spawn(end + new Vector3(0f, 0.86f, 0f), _kit.dragonImpact, false, SortOrder.Air, Battlefield.UnitScale);
            foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
            {
                if (enemy == null || enemy.IsDying || swept.Contains(enemy)) continue;
                if (Battlefield.GroundDistance(enemy.AimPoint(end), end) > _kit.dragonBlast + enemy.BodyRadius) continue;
                float before = enemy.Health.Current;
                enemy.Health.TakeDamage(_hero.AttackDamage * 2f, _hero.Health);
                landed |= enemy.Health.Current < before;
            }
            // Dich trong ca hai vung an 2 hit (quet + no) theo kit.
            foreach (var enemy in swept)
            {
                if (enemy == null || enemy.IsDying) continue;
                if (Battlefield.GroundDistance(enemy.AimPoint(end), end) <= _kit.dragonBlast + enemy.BodyRadius)
                {
                    float before = enemy.Health.Current;
                    enemy.Health.TakeDamage(_hero.AttackDamage * 2f, _hero.Health);
                    landed |= enemy.Health.Current < before;
                }
            }
            if (landed) CombatFeedback.HeavyHit(new Vector2(facing, 0f), 0.055f, "explode");
            _hero.OnHitsLanded(1);
        }
    }
}
