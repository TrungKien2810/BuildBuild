using System.Collections.Generic;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Bo chieu rieng cua Bonecaller (GAME_DESIGN.md muc 3) — `Hero` tu gan component nay khi
    /// `HeroDefinition.summoner` co gia tri. Chieu XIN (goi ky si) van chay qua thanh mana cua `Hero`
    /// (`SkillKind.SummonKnight`); lop nay giu phan con lai:
    ///   - NOI TAI: quai chet trong `raiseRadius` quanh phap su -> `raiseChance` dung linh xuong song
    ///     `passiveLifetime` giay;
    ///   - CHIEU THUONG (cooldown an): vong tren dat tai cum quai dong nhat, sat thuong thap + AN KY;
    ///     quai chet khi con an ky -> CHAC CHAN dung linh xuong, song toi khi bi giet / het wave;
    ///   - hai NGUON linh doc lap: moi nguon toi da `capPerSource` con song va bo dem rieng. Bo dem dem XAC
    ///     du dieu kien (ke ca khi nguon day), xac thu `bigEvery` la xuong lon va luon dung duoc;
    ///   - ky si tu than: moi luc mot con; mana day khi no con song -> hoi day mau cho no;
    ///   - hao quang: linh xuong trong `auraRadius` quanh ky si duoc buff (`BoneAura`).
    ///
    /// Linh trieu hoi nam trong danh sach TINH `Summons`, khong gan vao phap su: phap su chet giua wave
    /// thi linh van song toi khi bi giet (GDD). `HeroBoard` goi `DespawnAll` moi lan dung lai doi hinh
    /// (dau wave / thang wave) — "het wave thi don sach moi linh xuong".
    /// </summary>
    [RequireComponent(typeof(Hero))]
    public class Necromancer : MonoBehaviour
    {
        static readonly List<Hero> Summons = new List<Hero>();

        Hero _hero;
        SummonerDefinition _definition;
        readonly List<Hero> _passiveUnits = new List<Hero>();
        readonly List<Hero> _markUnits = new List<Hero>();
        int _passiveCounter;
        int _markCounter;
        Hero _knight;
        float _ringCooldown;

        public static void DespawnAll()
        {
            foreach (var unit in Summons.ToArray())
                if (unit != null) Destroy(unit.gameObject);
            Summons.Clear();
        }

        public void Initialise(Hero hero, SummonerDefinition definition)
        {
            _hero = hero;
            _definition = definition;
            _ringCooldown = definition.ringCooldown;
            Enemy.Killed -= OnEnemyKilled;
            Enemy.Killed += OnEnemyKilled;
        }

        void OnDestroy() => Enemy.Killed -= OnEnemyKilled;

        void Update()
        {
            if (_definition == null || _hero.IsDying) return;

            _ringCooldown -= Time.deltaTime;
            if (_ringCooldown > 0f) return;

            var centre = _hero.DensestCluster(_hero.Definition.range, _definition.ringRadius);
            if (centre == null) return;     // khong co quai trong tam: giu chieu san sang, khong phi
            _ringCooldown = _definition.ringCooldown;
            CastRing(centre.Value);
        }

        /// <summary>Vong bau duc 2:1 (cung khung `FrostZone`). An ky TRUOC khi tru mau: vong giet luon cung tinh.</summary>
        void CastRing(Vector3 centre)
        {
            // Lap vong `ringFxDuration` giay: 4 frame chay mot lan chi ~0,44 s, nguoi choi khong kip thay (2026-09-15).
            OneShotFx.SpawnLoop(null, centre, _definition.ringFrames, _definition.ringFxDuration, 5);
            foreach (var enemy in Enemy.Alive.ToArray())
            {
                if (enemy == null || enemy.IsDying) continue;
                float dx = (enemy.transform.position.x - centre.x) / _definition.ringRadius;
                float dy = (enemy.transform.position.y - centre.y) / (_definition.ringRadius * 0.5f);
                if (dx * dx + dy * dy > 1f) continue;
                enemy.ApplyMark(this, _definition.markDuration, _definition.markFrames);
                enemy.Health.TakeDamage(_definition.ringDamage);
            }
        }

        void OnEnemyKilled(Enemy enemy)
        {
            if (_definition == null || _hero == null || _hero.IsDying) return;
            Prune(_passiveUnits);
            Prune(_markUnits);

            var position = enemy.transform.position;
            // Vua an ky vua chet trong ban kinh noi tai -> tinh cho chieu thuong; chieu thuong da day
            // thi thu tiep ti le cua noi tai (GDD muc 3).
            if (enemy.IsMarkedBy(this) && Raise(position, _markUnits, ref _markCounter, 0f)) return;

            if ((position - _hero.transform.position).sqrMagnitude > _definition.raiseRadius * _definition.raiseRadius) return;
            if (Random.value >= _definition.raiseChance) return;
            Raise(position, _passiveUnits, ref _passiveCounter, _definition.passiveLifetime);
        }

        /// <summary>
        /// Bo dem tang theo MOI xac du dieu kien dung cua nguon, ke ca khi nguon da day (chot 2026-09-15): cu xac thu
        /// `bigEvery` la xuong lon, khong phu thuoc so xuong dang song. Xuong lon luon dung, vuot gioi han;
        /// xuong thuong gap nguon day thi bo. Tra ve true neu da dung.
        /// </summary>
        bool Raise(Vector3 position, List<Hero> source, ref int counter, float lifetime)
        {
            counter++;
            bool big = counter >= _definition.bigEvery;
            if (big) counter = 0;
            if (!big && source.Count >= _definition.capPerSource) return false;

            var unit = Spawn(big ? _definition.bigSkeleton : _definition.skeleton, position, lifetime);
            if (unit != null) source.Add(unit);
            return unit != null;
        }

        /// <summary>Goi tu `Hero.FinishCast` khi thanh mana cua phap su day.</summary>
        public void SummonKnight()
        {
            if (_knight != null && !_knight.IsDying)
            {
                _knight.Health.ResetTo(_knight.Definition.maxHealth);
                return;
            }

            float side = (_hero.GetComponent<SpriteRenderer>().flipX ? 0.6f : -0.6f) * Battlefield.UnitScale;
            _knight = Spawn(_definition.deathKnight, _hero.transform.position + new Vector3(side, 0f, 0f), 0f);
        }

        /// <summary>Linh xuong (khong phai chinh ky si) dung trong hao quang thi nhan buff.</summary>
        public void BoneAura(Hero unit, ref float damage, ref float rate)
        {
            if (_knight == null || _knight.IsDying || unit == _knight) return;
            if ((unit.transform.position - _knight.transform.position).sqrMagnitude > _definition.auraRadius * _definition.auraRadius) return;
            damage *= 1f + _definition.auraDamageBonus;
            rate *= 1f + _definition.auraAttackRateBonus;
        }

        Hero Spawn(HeroDefinition definition, Vector3 position, float lifetime)
        {
            if (definition == null || _definition.unitPrefab == null) return null;
            var unit = Instantiate(_definition.unitPrefab, position, Quaternion.identity);
            unit.Initialise(definition);
            unit.MakeSummon(this, lifetime);
            Summons.Add(unit);
            return unit;
        }

        static void Prune(List<Hero> units) => units.RemoveAll(u => u == null || u.IsDying);
    }
}
