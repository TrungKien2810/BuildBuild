using System.Collections.Generic;
using BuildBuild.Core;
using BuildBuild.Data;
using UnityEngine;

namespace BuildBuild.Combat
{
    /// <summary>
    /// Bo chieu rieng cua Banneret (Ky Thu cam co, SR buff — nguoi dung chot kit 2026-09-29). `Hero` tu gan component nay
    /// khi `HeroDefinition.banner` co gia tri, giong `Warden` cua Chainwarden.
    ///   - DANH THUONG: khong danh quai. O hitFrame cua clip attack (vut can co), buff NGAU NHIEN mot trong ba — +sat thuong,
    ///     +toc danh, khien ao — cho MOT dong minh trong tam, uu tien dong minh DANG DANH (co quai trong tam danh cua no);
    ///     khong ai dang danh thi mot dong minh bat ky trong tam. Khong bao gio tu buff;
    ///   - NOI TAI Quan Ky: dong minh trong `auraRadius` quanh Banneret +toc danh;
    ///   - CHIEU PHU Cam Co (cooldown AN): co dong minh dang danh trong vung thi cam co tai cho — vung giam sat thuong nhan;
    ///   - CHIEU CUOI Hieu Lenh Xung Phong (thanh mana cua `Hero`, `SkillKind.Rally`): ca doi tren san +sat thuong.
    /// Di chuyen: `Hero.UpdateSupport` di toi dong minh do `FollowTarget` chon, giong hero hoi mau.
    /// </summary>
    [RequireComponent(typeof(Hero))]
    public class Banneret : MonoBehaviour
    {
        static readonly List<Banneret> Active = new List<Banneret>();
        static readonly List<Hero> Picks = new List<Hero>();

        /// <summary>Vang = mau buff (GDD muc 7), cung mau vien co.</summary>
        static readonly Color LabelColor = new Color(1f, 0.85f, 0.3f);

        /// <summary>Be rong elip trong art `zone-fx` (79 px, PPU 100) — vung ve dung 2 x `plantRadius` world.</summary>
        const float ZoneArtDiameter = 0.79f;

        /// <summary>Vung Cam Co lam moi giam sat thuong moi frame; ra khoi vung thi het sau chung nay giay.</summary>
        const float GuardRefresh = 0.2f;

        Hero _hero;
        BannerDefinition _definition;
        float _plantCooldown;
        Vector3 _zoneAt;
        float _zoneUntil;

        public void Initialise(Hero hero, BannerDefinition definition)
        {
            _hero = hero;
            _definition = definition;
            _plantCooldown = definition.plantCooldown;
            _zoneUntil = 0f;
        }

        void OnEnable() => Active.Add(this);

        void OnDisable() => Active.Remove(this);

        /// <summary>Quan Ky: cong toc danh cho <paramref name="ally"/> neu dung gan mot Banneret da mo noi tai (nhieu cay co khong cong don).</summary>
        public static void AuraRate(Hero ally, ref float rate)
        {
            float best = 0f;
            foreach (var banner in Active)
            {
                if (banner._definition == null || banner._hero.IsDying || !banner._hero.PassiveUnlocked) continue;
                if (Battlefield.GroundDistance(ally.transform.position, banner.transform.position) > banner._definition.auraRadius)
                    continue;
                best = Mathf.Max(best, banner._definition.auraRateBonus);
            }
            rate *= 1f + best;
        }

        void Update()
        {
            if (_definition == null || _hero == null || _hero.IsDying) return;
            if (Time.time < _zoneUntil) GuardZone();

            if (!_hero.MinorUnlocked) return;    // Cam Co la chieu phu — mo theo sao
            _plantCooldown -= Time.deltaTime;
            if (_plantCooldown > 0f || _hero.Casting || _hero.IsStunned || !AnyEngagedNear(transform.position)) return;
            _plantCooldown = _definition.plantCooldown;
            _hero.PlayAction(_definition.plantBody);
            Plant();
        }

        void Plant()
        {
            _zoneAt = transform.position;
            _zoneUntil = Time.time + _definition.plantDuration;
            // Khong gan vao Banneret: co da cam xuong dat, Banneret di tiep thi vung van o cho cu.
            OneShotFx.SpawnLoop(null, _zoneAt, _definition.zoneFrames, _definition.plantDuration, SortOrder.GroundDecal,
                scale: _definition.plantRadius * 2f / ZoneArtDiameter);
        }

        void GuardZone()
        {
            foreach (var ally in Hero.Alive)
            {
                if (!IsAlly(ally)) continue;
                if (Battlefield.GroundDistance(ally.transform.position, _zoneAt) > _definition.plantRadius) continue;
                ally.ApplyGuard(_definition.plantDamageReduction, GuardRefresh);
            }
        }

        bool AnyEngagedNear(Vector3 at)
        {
            foreach (var ally in Hero.Alive)
                if (ally != _hero && IsAlly(ally) && Engaged(ally)
                    && Battlefield.GroundDistance(ally.transform.position, at) <= _definition.plantRadius) return true;
            return false;
        }

        /// <summary>
        /// Dong minh de di toi: dang danh gan nhat; khong ai dang danh ma san con quai thi dong minh gan nhat; het quai thi
        /// null (ve o). Chon GAN NHAT chu khong ngau nhien — ngau nhien moi frame thi Banneret di giat qua lai.
        /// </summary>
        public Hero FollowTarget()
        {
            Hero engaged = null, any = null;
            float engagedDistance = float.MaxValue, anyDistance = float.MaxValue;
            foreach (var ally in Hero.Alive)
            {
                if (ally == _hero || !IsAlly(ally)) continue;
                float distance = (ally.transform.position - transform.position).sqrMagnitude;
                if (distance < anyDistance)
                {
                    anyDistance = distance;
                    any = ally;
                }
                if (distance < engagedDistance && Engaged(ally))
                {
                    engagedDistance = distance;
                    engaged = ally;
                }
            }
            return engaged != null ? engaged : AnyEnemy() ? any : null;
        }

        /// <summary>Goi o hitFrame cua clip attack: chon lai nguoi nhan luc do (nguoi cu co the da chet / ra khoi tam).</summary>
        public void Strike()
        {
            var ally = PickStrikeTarget();
            if (ally == null) return;

            float seconds = _definition.strikeDuration;
            string label;
            switch (Random.Range(0, 3))
            {
                case 0:
                    ally.ApplyBuff("banner-damage", _definition.strikeDamageBonus, 0f, seconds);
                    label = "+Công";
                    break;
                case 1:
                    ally.ApplyBuff("banner-rate", 0f, _definition.strikeRateBonus, seconds);
                    label = "+Tốc đánh";
                    break;
                default:
                    ally.ApplyShield(_hero.AttackDamage * _definition.strikeShieldPower, seconds,
                        _definition.shieldFrames, _definition.shieldBreakFrames);
                    label = "Khiên";
                    break;
            }
            ally.ShowRally(_definition.rallyFrames, seconds);
            FloatingNumber.Spawn(ally.transform.position + new Vector3(0f, ally.BodyHeight, 0f), label, LabelColor);
            _hero.OnHitsLanded(1);    // khong danh trung quai: moi lan buff tinh nhu mot don trung de nap mana
        }

        /// <summary>Hieu Lenh Xung Phong: ca doi dang song tren san, ke ca chinh Banneret.</summary>
        public void CastUltimate()
        {
            foreach (var ally in Hero.Alive)
            {
                if (!IsAlly(ally)) continue;
                ally.ApplyBuff("banner-rally", _definition.rallyDamageBonus, 0f, _definition.rallyDuration);
                ally.ShowRally(_definition.rallyFrames, _definition.rallyDuration);
            }
        }

        /// <summary>Ngau nhien trong tam: uu tien nhom dang danh, rong thi ca nhom trong tam.</summary>
        Hero PickStrikeTarget()
        {
            Picks.Clear();
            float range = _hero.CurrentAttackRange;
            foreach (var ally in Hero.Alive)
                if (ally != _hero && IsAlly(ally) && InReach(ally, range) && Engaged(ally)) Picks.Add(ally);
            if (Picks.Count == 0)
                foreach (var ally in Hero.Alive)
                    if (ally != _hero && IsAlly(ally) && InReach(ally, range)) Picks.Add(ally);
            return Picks.Count == 0 ? null : Picks[Random.Range(0, Picks.Count)];
        }

        bool InReach(Hero ally, float range) => (ally.transform.position - transform.position).sqrMagnitude <= range * range;

        static bool IsAlly(Hero ally) => ally != null && !ally.IsDying && ally.Definition != null;

        /// <summary>Dang danh = co quai con song trong tam danh hien tai cua no (do toi mep than quai).</summary>
        static bool Engaged(Hero ally)
        {
            float range = ally.CurrentAttackRange;
            foreach (var enemy in Enemy.Alive)
            {
                if (enemy == null || enemy.IsDying) continue;
                if ((enemy.transform.position - ally.transform.position).magnitude - enemy.BodyRadius <= range) return true;
            }
            return false;
        }

        static bool AnyEnemy()
        {
            foreach (var enemy in Enemy.Alive)
                if (enemy != null && !enemy.IsDying) return true;
            return false;
        }
    }
}
