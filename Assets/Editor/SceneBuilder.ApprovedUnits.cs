using System;
using System.Linq;
using BuildBuild.Data;
using UnityEditor;

namespace BuildBuild.Editor
{
    /// <summary>
    /// Unit da duyet du asset, noi vao game 2026-10-05: hero Bo Hung / Song Dao / Chuong Bac, quai Bo Cap Doc / Ky Nhong
    /// Than Hong / Coc Suong / Bo Khien Sat / Goblin Rung Chuong, linh Nam / Cung Thu Xuong / Cua Kim Sat / Oc Sen Duoc
    /// Thao. Chi so hero theo GDD muc 3; quai + linh la so Claude de xuat — TAT CA CHUA can. Asset da co thi giu so chinh tay.
    /// </summary>
    public static partial class SceneBuilder
    {
        /// <summary>Nhip clip: pipeline xuat 140 ms/frame.</summary>
        const float ApprovedFps = 1000f / 140f;

        static void BuildLowTrioHeroes()
        {
            HeroN("BeetleKnight", "beetleknight", "Beetle Knight", d =>
            {
                d.maxHealth = 1050; d.damage = 20; d.attackRate = 0.8f; d.range = 0.65f; d.moveSpeed = 1f;
                d.projectileSpeed = 20f; d.splashRadius = 0.1f;
                d.passiveKind = PassiveKind.Quake; d.passiveEvery = 3; d.passiveRadius = 0.5f;
                d.passiveFrames = ApprovedFrames("BeetleKnight", "mace-trail");
                d.passiveText = "Ground Tremor: every 3rd basic Hit knocks the Target back 0,5 (elites 0,25, Bosses immune to Knockback).";
            }, SkillKind.HornSlam, d =>
            {
                d.manaMax = 6; d.skillRadius = 1f; d.skillPower = 3f; d.skillDuration = 1f;
                d.skillFrames = ApprovedFrames("BeetleKnight", "ground-pulse");
                d.stunFrames = ApprovedFrames("BeetleKnight", "stun-loop");
                d.ultimateText = "Horn Slam: slam the ground in place, dealing 3× basic Hit Damage within radius 1 around the feet and Stun for 1s (elites/Bosses 0,5s).";
            });
            HeroN("Twinblade", "twinblade", "Twinblade", d =>
            {
                d.maxHealth = 800; d.damage = 32; d.attackRate = 1.2f; d.range = 0.65f; d.moveSpeed = 1.2f;
                d.projectileSpeed = 20f; d.splashRadius = 0.1f;
                d.impactFrames = ApprovedFrames("Twinblade", "blade-impact");
                d.passiveKind = PassiveKind.Combo; d.passivePower = 0.12f; d.passiveEvery = 5; d.passiveDuration = 2f;
                d.passiveFrames = ApprovedFrames("Twinblade", "combo-mark");
                d.passiveText = "Hit Chain: each Hit on the same Target grants +12% Damage to the next Hit, up to 5 Stacks; changing Target or going 2s without a Hit removes Stacks.";
            }, SkillKind.BladeStorm, d =>
            {
                d.manaMax = 6; d.skillPower = 0.65f;
                d.skillFrames = ApprovedFrames("Twinblade", "blade-trails");
                d.ultimateText = "Blade Storm: lock onto a Target, slash 6 times over 3s, each slash deals 65% basic Hit Damage using Hit Chain Stacks at the start; clear Stacks afterward.";
            }, Rarity.R);
            HeroN("BellNun", "bellnun", "Bell Nun", d =>
            {
                d.maxHealth = 360; d.damage = 14; d.attackRate = 0.7f; d.range = 4f; d.splashRadius = 0.1f;
                d.projectileFrames = ApprovedFrames("BellNun", "sound-bolt");
                d.passiveKind = PassiveKind.Purify; d.passiveDuration = 10f; d.passiveRadius = 4f;
                d.passiveFrames = ApprovedFrames("BellNun", "cleanse");
                d.passiveText = "Purifying Light: every 10s, remove 1 effect in order Poison → Burn → Slow from the nearest affected ally within Range 4.";
            }, SkillKind.BellToll, d =>
            {
                d.manaMax = 7; d.skillPower = 0.3f; d.skillDuration = 3f;
                d.skillFrames = ApprovedFrames("BellNun", "bell-wave");
                d.ultimateText = "Bell Toll: remove Poison, Burn, Slow, Root, Attack Lock and Stun from the whole team; Slow opponents by 30% for 3s (elites 15%, Bosses immune).";
            }, Rarity.R);
            foreach (string id in new[] { "BeetleKnight", "Twinblade", "BellNun" })
            {
                var anim = AssetDatabase.LoadAssetAtPath<SpriteAnimationSet>(DataDir + "/Anim_" + id + ".asset");
                anim.framesPerSecond = ApprovedFps;
                EditorUtility.SetDirty(anim);
            }
        }

        // ---------------------------------------------------------------- quai

        static void BuildStatusEnemies(WaveTuning tuning)
        {
            var scorpion = ApprovedEnemy("VenomScorpion", ("sting", "sting"), e =>
            {
                e.displayName = "Venom Scorpion"; e.kind = EnemyKind.Thuong;
                e.baseHealth = 140f; e.baseDamage = 14f; e.moveSpeed = 1.3f; e.attackRange = 0.6f; e.attackRate = 0.9f;
                e.hitStatus = HitStatus.Poison; e.hitStatusPower = 5f; e.hitStatusSeconds = 4f; e.hitStatusMaxStacks = 3;
                e.hitStatusEvery = 3; e.hitStatusFrames = ApprovedFrames("VenomScorpion", "poison-impact");
            });
            var salamander = ApprovedEnemy("EmberSalamander", ("cast", "cast"), e =>
            {
                e.displayName = "Ember Salamander"; e.kind = EnemyKind.Thuong;
                e.baseHealth = 160f; e.baseDamage = 12f; e.moveSpeed = 1.1f; e.attackRange = 0.6f; e.attackRate = 0.8f;
                e.breathCooldown = 7f; e.breathRange = 3f; e.breathRadius = 0.75f; e.breathActiveSeconds = 2.5f;
                e.breathBurnPerSecond = 8f; e.breathBurnSeconds = 3f;
                e.breathTelegraphFrames = ApprovedFrames("EmberSalamander", "fire-telegraph");
                e.breathActiveFrames = ApprovedFrames("EmberSalamander", "fire-active");
                e.breathFadeFrames = ApprovedFrames("EmberSalamander", "fire-fade");
                e.breathMouthFrames = ApprovedFrames("EmberSalamander", "fire-breath");
            });
            var toad = ApprovedEnemy("RimeToad", null, e =>
            {
                e.displayName = "Rime Toad"; e.kind = EnemyKind.BanXa;
                e.baseHealth = 110f; e.baseDamage = 10f; e.moveSpeed = 1.2f; e.attackRange = 4f; e.heroAggroRange = 4.5f;
                e.attackRate = 0.6f;
                e.projectileFrames = ApprovedFrames("RimeToad", "frost-projectile");
                e.impactFrames = ApprovedFrames("RimeToad", "frost-impact");
                e.projectileSpeed = 6f; e.projectileScale = 1f;
                e.hitStatus = HitStatus.Slow; e.hitStatusPower = 0.3f; e.hitStatusSeconds = 3f;
            });
            var beetle = ApprovedEnemy("IronshieldBeetle", null, e =>
            {
                e.displayName = "Iron Shield Beetle"; e.kind = EnemyKind.Tank;
                e.baseHealth = 450f; e.baseDamage = 18f; e.moveSpeed = 0.7f; e.attackRange = 0.6f; e.attackRate = 0.7f;
                e.damageReduction = 0.4f;
            });
            var ringer = ApprovedEnemy("GoblinBellringer", ("ring", "ring"), e =>
            {
                e.displayName = "Bell Goblin"; e.kind = EnemyKind.Thuong;
                e.baseHealth = 80f; e.baseDamage = 8f; e.moveSpeed = 1.4f; e.attackRange = 0.6f; e.attackRate = 0.8f;
                e.ringCooldown = 8f; e.ringRadius = 2.5f; e.ringHaste = 0.3f; e.ringSeconds = 4f;
                e.ringWaveFrames = Frames(ArtRoot + "GoblinBellringer/ring-wave", "impact");
                e.hasteFrames = Frames(ArtRoot + "GoblinBellringer/haste-status", "impact");
            });

            // Gioi thieu tung loai o khoang wave rieng roi moi ghep (IDEAS: khong don ca ba o wave gioi thieu).
            bool dirty = false;
            foreach (var band in tuning.bands ?? Array.Empty<WaveBand>())
            {
                if (band.hasBoss || band.enemies == null) continue;
                dirty |= AddToBand(band, scorpion, 4);
                dirty |= AddToBand(band, beetle, 7);
                dirty |= AddToBand(band, toad, 7);
                dirty |= AddToBand(band, salamander, 11);
                dirty |= AddToBand(band, ringer, 11);
            }
            if (dirty) EditorUtility.SetDirty(tuning);
        }

        static bool AddToBand(WaveBand band, EnemyDefinition enemy, int fromWave)
        {
            if (band.fromWave < fromWave || band.enemies.Contains(enemy)) return false;
            band.enemies = band.enemies.Append(enemy).ToArray();
            return true;
        }

        static EnemyDefinition ApprovedEnemy(string id, (string folder, string action)? skill, Action<EnemyDefinition> stats)
        {
            var enemy = LoadOrCreate<EnemyDefinition>(DataDir + "/Enemy_" + id + ".asset", out bool created);
            if (created)
            {
                var anim = AnimSet(id, id + "/", attack: ("attack", "attack"), skill: skill, hit: 4, skillHit: 4);
                anim.framesPerSecond = ApprovedFps;
                EditorUtility.SetDirty(anim);
                enemy.animations = anim;
                stats(enemy);
                EditorUtility.SetDirty(enemy);
            }
            return enemy;
        }

        // ---------------------------------------------------------------- linh

        static void BuildApprovedSoldiers()
        {
            var mushroom = SoldierDef("MushroomSoldier", "Mushroom Soldier",
                ApprovedSoldierAnim("MushroomSoldier", ("attack", "attack")),
                d =>
                {
                    d.maxHealth = 160f; d.damage = 10f; d.attackRate = 0.8f; d.range = 3.5f; d.moveSpeed = 1f; d.leashRange = 3f;
                    d.projectileFrames = ApprovedFrames("MushroomSoldier", "spore-projectile");
                    d.impactFrames = ApprovedFrames("MushroomSoldier", "spore-impact");
                    d.projectileSpeed = 6f; d.splashRadius = 0.1f;
                    d.passiveKind = PassiveKind.SporeField; d.passiveStar = 0;
                    d.passivePower = 0.25f; d.passiveRadius = 0.9f; d.passiveDuration = 2.5f;
                    d.passiveFrames = ApprovedFrames("MushroomSoldier", "spore-field");
                    d.skillOutroFrames = ApprovedFrames("MushroomSoldier", "spore-fade");
                    d.passiveText = "Spore Field: Hits leave a spore zone for 2,5s; Enemies in the zone suffer 25% Slow.";
                    d.squadSize = 3; d.summonCooldown = 15f;
                }, facesRight: false);
            var archer = SoldierDef("SkeletonArcher", "Skeleton Archer",
                ApprovedSoldierAnim("SkeletonArcher", ("shoot", "shoot")),
                d =>
                {
                    d.maxHealth = 120f; d.damage = 16f; d.attackRate = 0.9f; d.range = 4.5f; d.moveSpeed = 1.1f; d.leashRange = 3f;
                    d.projectileFrames = ApprovedFrames("SkeletonArcher", "arrow");
                    d.impactFrames = ApprovedFrames("SkeletonArcher", "impact");
                    d.projectileSpeed = 9f; d.splashRadius = 0.1f;
                    d.squadSize = 3; d.summonCooldown = 14f;
                }, facesRight: false);
            var crab = SoldierDef("IronClawCrab", "Iron Claw Crab",
                ApprovedSoldierAnim("IronClawCrab", ("attack", "attack")),
                d =>
                {
                    d.maxHealth = 450f; d.damage = 14f; d.attackRate = 0.8f; d.range = 0.6f; d.moveSpeed = 0.8f;
                    d.projectileSpeed = 20f; d.splashRadius = 0.3f;
                    d.impactFrames = ApprovedFrames("IronClawCrab", "clamp-impact");
                    d.passiveKind = PassiveKind.ClampBreak; d.passiveStar = 0;
                    d.passiveEvery = 3; d.passivePower = 0.2f; d.passiveDuration = 4f;
                    d.passiveFrames = ApprovedFrames("IronClawCrab", "armor-break");
                    d.passiveText = "Armorbreak Clamp: every 3rd Hit makes the Target take 20% more Damage for 4s (refresh only).";
                    d.squadSize = 2; d.summonCooldown = 18f;
                }, facesRight: false);
            var snail = SoldierDef("HerbalSnail", "Herbal Snail",
                ApprovedSoldierAnim("HerbalSnail", ("heal", "heal")),
                d =>
                {
                    d.maxHealth = 220f; d.damage = 22f; d.attackRate = 0.5f; d.range = 3.5f; d.moveSpeed = 0.6f; d.leashRange = 3f;
                    d.healsAllies = true;
                    d.projectileFrames = ApprovedFrames("HerbalSnail", "heal-drop");
                    d.healFrames = ApprovedFrames("HerbalSnail", "heal-impact");
                    d.squadSize = 2; d.summonCooldown = 18f;
                }, facesRight: false);

            var units = new[]
            {
                SoldierUnit("soldier_mushroomsoldier", mushroom, Rarity.N),
                SoldierUnit("soldier_skeletonarcher", archer, Rarity.N),
                SoldierUnit("soldier_ironclawcrab", crab, Rarity.R),
                SoldierUnit("soldier_herbalsnail", snail, Rarity.R),
            };
            var gacha = AssetDatabase.LoadAssetAtPath<GachaConfig>(DataDir + "/GachaConfig.asset");
            if (gacha == null) return;
            var pool = (gacha.pool ?? Array.Empty<UnitDefinition>()).ToList();
            foreach (var unit in units)
                if (!pool.Contains(unit)) pool.Add(unit);
            gacha.pool = pool.ToArray();
            EditorUtility.SetDirty(gacha);
        }

        static SpriteAnimationSet ApprovedSoldierAnim(string id, (string folder, string action) attack)
        {
            var anim = AnimSet(id, id + "/", attack: attack, hit: 4);
            anim.framesPerSecond = ApprovedFps;
            EditorUtility.SetDirty(anim);
            return anim;
        }
    }
}
