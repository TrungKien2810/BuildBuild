using System.Collections.Generic;
using System.Collections;
using System.Linq;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Chieu xin cua bo hero N fantasy (GAME_DESIGN.md muc 3; so Claude de xuat, CHUA can) — noi tai nam o Hero.Passive.cs:
    ///   - Vac Suong Doc (Phu Thuy Dam Lay): suong o cum quai dong nhat, +1 tang Doc moi giay + pha giap;
    ///   - No Nam (Hiep Si Nam): 3 cay nam moc quanh chan, het gio thi no bao tu;
    ///   - Xoay Nuoc (Ngu Nhan Dinh Ba): xoay quanh chan khoa don quai trong vung + sat thuong moi giay;
    ///   - Mua Tan Lua (Phap Su Lua Tap Su): 3 cau lua roi vao cum quai, no + Chay + vung lua;
    ///   - Tuong Rune (Nguoi Lun Khien Rune): tuong chan quai + dan truoc mat, ong dung yen sau khien;
    ///   - Mua Roi (Tien Det To): quai nhieu mau nhat quay sang danh phe minh.
    /// </summary>
    public partial class Hero
    {
        /// <summary>Ban kinh vu no bao tu cua moi cay nam (GDD: 0,6).</summary>
        const float MushroomBurstRadius = 0.6f;

        /// <summary>Cau lua bay tu diem nay (so voi cho no) xuong — cung huong vet duoi ve trong art (duoi chech len phai).</summary>
        static readonly Vector3 FireballDrop = new Vector3(0.8f, 2.4f, 0f);
        const float FireballFallSeconds = 0.45f;
        const float FireballStagger = 0.2f;

        /// <summary>Tuong Rune dung truoc mat hero bao xa.</summary>
        const float RuneWallOffset = 0.5f;

        float _bracedUntil;

        /// <summary>Dang dung sau Tuong Rune: khong di, khong danh, giu frame cuoi clip cast.</summary>
        bool Braced => Time.time < _bracedUntil;

        /// <summary>
        /// Scale NGUYEN cho FX vung de be ngang art gan bang duong kinh gameplay (art pixel phai giu luoi, khong scale le).
        /// </summary>
        static float ZoneFxScale(Sprite[] frames, float radius)
        {
            if (frames == null || frames.Length == 0 || frames[0] == null) return Battlefield.UnitScale;
            float width = frames[0].bounds.size.x;
            return width > 0f ? Mathf.Max(1f, Mathf.Round(2f * radius / width)) : Battlefield.UnitScale;
        }

        void CastToxicMist()
        {
            var centre = DensestCluster(_definition.range, _definition.skillRadius);
            if (centre == null) return;
            float armorBreak = _definition.skillPower;
            SkillZone.Spawn(centre.Value, _definition.skillRadius, _definition.skillDuration, Side, _definition.skillFrames,
                ZoneFxScale(_definition.skillFrames, _definition.skillRadius),
                everySecond: enemy =>
                {
                    AddVenom(enemy, 1);
                    enemy.ApplyArmorBreak(armorBreak, 1.1f);   // ra khoi suong thi het pha giap sau ~1 giay
                });
        }

        void CastMushroomBloom()
        {
            float damage = BaseDamage * _definition.skillPower;
            var source = _health;
            var side = Side;
            var burst = _definition.skillFrames;
            var grow = _definition.skillIntroFrames;
            // Moc xong giu frame cuoi (nam dung) toi luc no.
            var hold = grow != null && grow.Length > 0 ? new[] { grow[grow.Length - 1] } : null;
            for (int i = 0; i < 3; i++)
            {
                // Ba goc deu quanh chan; mat dat bep doi theo truc doc nhu moi vung tren san.
                float angle = (90f + 120f * i) * Mathf.Deg2Rad;
                var at = transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * 0.5f, 0f) * _definition.skillRadius;
                SkillZone.Spawn(at, MushroomBurstRadius, _definition.skillDuration, side, hold, Battlefield.UnitScale,
                    intro: grow,
                    onExpire: () =>
                    {
                        foreach (var enemy in Unit.Hostiles(side).ToArray())
                        {
                            if (enemy == null || enemy.IsDying) continue;
                            if (Battlefield.GroundDistance(enemy.AimPoint(at), at) > MushroomBurstRadius + enemy.BodyRadius) continue;
                            enemy.Health.TakeDamage(damage, source != null ? source : null);
                        }
                        OneShotFx.Spawn(at, burst, false, SortOrder.Air, Battlefield.UnitScale);
                    });
            }
        }

        void CastWhirlpool()
        {
            float damage = BaseDamage * _definition.skillPower;
            var source = _health;
            SkillZone.Spawn(transform.position, _definition.skillRadius, _definition.skillDuration, Side,
                _definition.skillFrames, ZoneFxScale(_definition.skillFrames, _definition.skillRadius),
                everyFrame: enemy =>
                {
                    // Boss khong bi khoa don, tinh anh danh duoc nhung -50% sat thuong (GDD).
                    if (enemy.IsBoss) return;
                    if (enemy.IsElite) enemy.ApplyAwe(0.5f, 0f);
                    else enemy.ApplyDisarm(0.1f);
                },
                everySecond: enemy => enemy.Health.TakeDamage(damage, source != null ? source : null, kind: DamageKind.Periodic),
                outro: _definition.skillOutroFrames);
        }

        void CastEmberRain()
        {
            float radius = _definition.skillRadius;
            var centre = DensestCluster(_definition.range, radius);
            if (centre == null) return;
            float damage = BaseDamage * _definition.skillPower;
            var source = _health;
            var side = Side;
            var blast = _definition.skillOutroFrames;
            var pool = _definition.skillFrames;
            float poolScale = ZoneFxScale(pool, radius);
            for (int i = 0; i < 3; i++)
            {
                // Qua dau roi giua cum, hai qua sau lech hai ben (mat dat bep doi theo truc doc).
                float angle = (90f + 120f * i) * Mathf.Deg2Rad;
                var at = i == 0 ? centre.Value
                    : centre.Value + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * 0.5f, 0f) * radius;
                FallingFx.Spawn(at + FireballDrop, at, i * FireballStagger, FireballFallSeconds, _definition.skillIntroFrames,
                    Battlefield.UnitScale, () =>
                    {
                        foreach (var enemy in Unit.Hostiles(side).ToArray())
                        {
                            if (enemy == null || enemy.IsDying) continue;
                            if (Battlefield.GroundDistance(enemy.AimPoint(at), at) > radius + enemy.BodyRadius) continue;
                            enemy.Health.TakeDamage(damage, source != null ? source : null);
                            if (this != null) AddBurn(enemy);
                        }
                        OneShotFx.Spawn(at, blast, false, SortOrder.Air, Battlefield.UnitScale);
                        SkillZone.Spawn(at, radius, _definition.skillDuration, side, pool, poolScale,
                            everySecond: enemy => { if (this != null) AddBurn(enemy); });
                    });
            }
        }

        void CastRuneWall()
        {
            float facing = FacingRight ? 1f : -1f;
            RuneWall.Spawn(transform.position + new Vector3(facing * RuneWallOffset, 0f, 0f), facing, _definition.skillRadius,
                _definition.skillDuration, Side, _definition.skillIntroFrames, _definition.skillFrames,
                _definition.skillOutroFrames, _definition.passiveFrames);
            _bracedUntil = Time.time + _definition.skillDuration;
        }

        void CastPuppet()
        {
            Enemy best = null;
            foreach (var unit in Unit.Hostiles(Side))
            {
                if (!(unit is Enemy enemy) || enemy.IsDying || enemy.IsBoss || enemy.IsPuppet) continue;
                float distance = Battlefield.GroundDistance(enemy.AimPoint(transform.position), transform.position) - enemy.BodyRadius;
                if (distance > AttackRange) continue;
                if (best == null || enemy.Health.Current > best.Health.Current) best = enemy;
            }
            if (best == null) return;
            float seconds = best.IsElite ? _definition.skillDuration * 0.5f : _definition.skillDuration;
            best.ApplyPuppet(Side, seconds, _definition.skillFrames, _definition.skillOutroFrames);
        }

        // ---------------------------------------------------------------- Hoa Yeu, Kobold Gai Bay, Thay Bua An Sao

        float _trapReady, _seedRainUntil;
        GameObject _sigilAura;
        readonly List<HeroTrap> _traps = new List<HeroTrap>();

        void InitialiseHeroN() => _trapReady = Time.time + 8f;

        void ClearHeroN()
        {
            foreach (var trap in _traps) if (trap != null) Destroy(trap.gameObject);
            _traps.Clear();
        }

        /// <summary>Noi tai chay nen: An Yeu (Thay Bua) pha giap quanh minh, Dat Bay (Kobold) moi 8s.</summary>
        void UpdateHeroN()
        {
            if (!WaveManager.Fighting || _dying) return;
            if (HasPassive(PassiveKind.SigilWeakness))
            {
                if (_sigilAura == null)
                    _sigilAura = OneShotFx.SpawnLoop(transform, Vector3.zero, _definition.passiveFrames, 3600f,
                        SortOrder.GroundDecal + 1, scale: ZoneFxScale(_definition.passiveFrames, 2f) / Battlefield.UnitScale);
                foreach (var enemy in Unit.Hostiles(Side).ToArray())
                    if (enemy != null && !enemy.IsDying && Battlefield.GroundDistance(enemy.transform.position, transform.position) <= 2f)
                        enemy.ApplyArmorBreak(0.15f, 0.2f);
            }
            if (HasPassive(PassiveKind.Traps) && Time.time >= _trapReady)
            {
                _trapReady = Time.time + 8f;
                _traps.RemoveAll(t => t == null);
                if (_traps.Count < 3) PlaceTrap(_traps.Count);
            }
        }

        void PlaceTrap(int slot)
        {
            float facing = FacingRight ? 1f : -1f;
            var at = transform.position + new Vector3(facing * (1f + slot * 0.8f), 0f, 0f);
            _traps.Add(HeroTrap.Spawn(this, at));
        }

        void CastTrapDetonation()
        {
            foreach (var trap in _traps.ToArray()) if (trap != null) trap.Detonate();
            _traps.Clear();
            for (int i = 0; i < 3; i++) PlaceTrap(i);
        }

        void CastSeedRain()
        {
            _seedRainUntil = Time.time + 1.6f;
            StartCoroutine(SeedRain());
        }

        IEnumerator SeedRain()
        {
            var centre = DensestCluster(AttackRange, 1f);
            if (centre == null) yield break;
            for (int i = 0; i < 8; i++)
            {
                if (_dying) yield break;
                var target = Unit.Hostiles(Side).ToArray().Where(u => u != null && !u.IsDying && InRange(u))
                    .OrderBy(u => Battlefield.GroundDistance(u.transform.position, centre.Value)).FirstOrDefault();
                if (target != null)
                {
                    var go = new GameObject("SeedRain");
                    go.transform.position = BodyCenter;
                    go.transform.localScale = Vector3.one * Battlefield.UnitScale;
                    go.AddComponent<SpriteRenderer>().sortingOrder = SortOrder.Air;
                    go.AddComponent<Projectile>().Launch(target, AttackDamage, 0.1f, _definition.projectileSpeed,
                        _definition.projectileFrames, _definition.impactFrames, this);
                }
                yield return new WaitForSeconds(0.2f);
            }
            _seedRainUntil = 0f;
            ReleaseHeldClip();
        }

        void CastSigilBombs()
        {
            foreach (var target in Unit.Hostiles(Side).ToArray().Where(u => u != null && !u.IsDying && InRange(u))
                .OrderByDescending(u => u.Health.Current).Take(3).ToArray())
                StartCoroutine(SigilBomb(target, AttackDamage * 3f));
        }

        IEnumerator SigilBomb(Unit target, float damage)
        {
            var position = target.transform.position;
            var lift = target.BodyCenter - position;    // no pivot tam o: dat o tam than, khong lot nua duoi xuong dat
            OneShotFx.SpawnLoop(target.transform, new Vector3(0, target.BodyHeight / Battlefield.UnitScale),
                _definition.skillIntroFrames, 2f, SortOrder.Air);
            yield return new WaitForSeconds(2f);
            if (target != null) position = target.transform.position;
            foreach (var enemy in Unit.Hostiles(Side).ToArray())
                if (enemy != null && !enemy.IsDying && Battlefield.GroundDistance(enemy.transform.position, position) <= 0.6f + enemy.BodyRadius)
                    enemy.Health.TakeDamage(damage, _health);
            OneShotFx.Spawn(position + lift, _definition.skillFrames, false, SortOrder.Air, Battlefield.UnitScale);
        }
    }
}
