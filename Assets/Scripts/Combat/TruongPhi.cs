using System.Collections.Generic;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Truong Phi — chong chiu + khong che (bo Dao Vien, `DaoVienDefinition`). Moi khoan giam sat thuong nhan gop nhan vao
    /// `Health.Armor`, dat lai moi frame:
    ///   - Van Nhan Dich (passiveStar): moi quai trong `bulwarkRadius` -`bulwarkPerEnemy` sat thuong nhan (toi da `bulwarkMax`).
    ///   - Bat Xa Mau (minorStar): cooldown an, xoay xa mau nap luc roi dam xuyen `pierceLength` truoc mat, don thuong x
    ///     `piercePower` + pha giap `pierceArmorBreak`.
    ///   - Tuy Tuu (secondMinorStar): mau duoi `drinkThreshold` thi tu bau ruou — hoi `drinkHeal` mau toi da, mien choang
    ///     va -`drinkReduction` sat thuong nhan trong `drinkSeconds`.
    ///   - Duong Duong Kieu Hong (chieu xin): dam chan gam — quai trong `skillRadius` an don thuong x `skillPower`, pha giap
    ///     `roarArmorBreak`; quai thuong + tinh anh bi hat lui `roarPush` + choang `skillDuration`, boss chi cham
    ///     `roarBossSlow`. Truong Phi khieu khich ca vung + -`roarReduction` sat thuong nhan trong `roarBreakSeconds`.
    /// </summary>
    public class TruongPhi : DaoVienBrother
    {
        /// <summary>Thoi gian bi hat lui (PullTo).</summary>
        const float PushTime = 0.25f;

        float _pierceCooldown;
        float _pierceAt = -1f;
        float _drinkCooldown;
        float _drinkAt = -1f;
        float _drinkUntil;
        float _roarGuardUntil;

        public override bool StunImmune => Time.time < _drinkUntil;

        public override void CollectEffects(List<UnitEffect> into)
        {
            if (Time.time < _drinkUntil)
                into.Add(new UnitEffect(EffectKind.Guard, $"Túy Tửu -{Percent(_definition.drinkReduction)} ST, miễn choáng", _drinkUntil));
            if (Time.time < _roarGuardUntil)
                into.Add(new UnitEffect(EffectKind.Guard, $"Gầm -{Percent(_definition.roarReduction)} ST", _roarGuardUntil));
        }

        static string Percent(float fraction) => Mathf.RoundToInt(fraction * 100f) + "%";

        public override void Initialise(Hero hero, DaoVienDefinition definition)
        {
            base.Initialise(hero, definition);
            _pierceCooldown = definition.pierceCooldown;
            _drinkCooldown = 0f;
            _pierceAt = _drinkAt = -1f;
            _drinkUntil = _roarGuardUntil = 0f;
        }

        protected override void Update()
        {
            base.Update();
            if (_definition == null || _hero == null || _hero.IsDying) return;
            _hero.Health.Armor = DamageReduction();

            if (_pierceAt > 0f && Time.time >= _pierceAt)
            {
                _pierceAt = -1f;
                Pierce();
            }
            if (_drinkAt > 0f && Time.time >= _drinkAt)
            {
                _drinkAt = -1f;
                Drink();
            }
            if (_pierceAt > 0f || _drinkAt > 0f) return;

            _drinkCooldown -= Time.deltaTime;
            if (_hero.SecondMinorUnlocked && _drinkCooldown <= 0f && !_hero.Health.IsDead &&
                _hero.Health.Fraction < _definition.drinkThreshold)
            {
                float at = Play(_definition.drinkBody);
                if (at >= 0f)
                {
                    _drinkCooldown = _definition.drinkCooldown;
                    _drinkAt = at;
                    return;
                }
            }

            if (!_hero.MinorUnlocked) return;
            _pierceCooldown -= Time.deltaTime;
            if (_pierceCooldown > 0f || !EnemyAhead(_definition.pierceLength, _definition.pierceHalfDepth)) return;
            float pierceAt = Play(_definition.pierceBody);
            if (pierceAt < 0f) return;
            _pierceCooldown = _definition.pierceCooldown;
            _pierceAt = pierceAt;
        }

        /// <summary>Van Nhan Dich, Tuy Tuu, Duong Duong Kieu Hong gop nhan: 1 - (1-a)(1-b)(1-c).</summary>
        float DamageReduction()
        {
            float taken = 1f;
            if (_hero.PassiveUnlocked)
            {
                int near = 0;
                foreach (var enemy in Unit.Hostiles(_hero.Side))
                    if (enemy != null && !enemy.IsDying &&
                        Battlefield.GroundDistance(enemy.AimPoint(transform.position), transform.position) - enemy.BodyRadius <=
                        _definition.bulwarkRadius) near++;
                taken *= 1f - Mathf.Min(_definition.bulwarkMax, near * _definition.bulwarkPerEnemy);
            }
            if (Time.time < _drinkUntil) taken *= 1f - _definition.drinkReduction;
            if (Time.time < _roarGuardUntil) taken *= 1f - _definition.roarReduction;
            return 1f - taken;
        }

        // ---------------------------------------------------------------- Bat Xa Mau

        void Pierce()
        {
            float side = Side;
            float damage = _hero.AttackDamage * _definition.piercePower;
            foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
            {
                if (enemy == null || enemy.IsDying || enemy.FliesHigh) continue;
                var d = enemy.transform.position - transform.position;
                if (d.x * side < -enemy.BodyRadius || d.x * side > _definition.pierceLength + enemy.BodyRadius ||
                    Mathf.Abs(d.y) > _definition.pierceHalfDepth) continue;
                enemy.ApplyArmorBreak(_definition.pierceArmorBreak, _definition.pierceBreakSeconds);
                Hit(enemy, damage);
            }
            var fxAt = transform.position + new Vector3(side * _definition.pierceLength * 0.5f, _hero.BodyHeight * 0.45f, 0f);
            OneShotFx.Spawn(fxAt, _definition.pierceFrames, side > 0f, SortOrder.Air, Battlefield.UnitScale);
        }

        // ---------------------------------------------------------------- Tuy Tuu

        void Drink()
        {
            _hero.Health.Heal(_hero.Health.Max * _definition.drinkHeal, _hero.Health);
            _drinkUntil = Time.time + _definition.drinkSeconds;
        }

        // ---------------------------------------------------------------- Duong Duong Kieu Hong

        public override void CastUltimate()
        {
            if (_hero == null || _hero.IsDying) return;
            var definition = _hero.Definition;
            float radius = definition.skillRadius;
            var centre = transform.position;

            float fxScale = Battlefield.UnitScale * _definition.roarFxScale;
            OneShotFx.Spawn(centre, _definition.roarWaveFrames, false, SortOrder.Air, fxScale);
            OneShotFx.Spawn(centre, _definition.groundCrackFrames, false, SortOrder.GroundDecal, fxScale);

            float damage = _hero.AttackDamage * definition.skillPower;
            foreach (var enemy in Unit.Hostiles(_hero.Side).ToArray())
            {
                if (enemy == null || enemy.IsDying) continue;
                if (Battlefield.GroundDistance(enemy.AimPoint(centre), centre) - enemy.BodyRadius > radius) continue;
                enemy.ApplyArmorBreak(_definition.roarArmorBreak, _definition.roarBreakSeconds);
                if (enemy.IsBoss) enemy.ApplySlow(_definition.roarBossSlow, _definition.roarBreakSeconds);
                else
                {
                    float away = enemy.transform.position.x >= centre.x ? 1f : -1f;
                    var to = Battlefield.Clamp(enemy.transform.position + new Vector3(away * _definition.roarPush, 0f, 0f),
                        enemy.BodyRadius, clampRight: true);
                    enemy.PullTo(to, PushTime);
                    enemy.ApplyStun(definition.skillDuration, definition.stunFrames);
                }
                Hit(enemy, damage);
            }

            _roarGuardUntil = Time.time + _definition.roarBreakSeconds;
            _hero.Taunt(radius, _definition.roarBreakSeconds);
        }
    }
}
