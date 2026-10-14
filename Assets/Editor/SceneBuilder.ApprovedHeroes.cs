using System;
using System.Linq;
using BuildBuild.Data;
using UnityEditor;

namespace BuildBuild.Editor
{
    public static partial class SceneBuilder
    {
        static void BuildApprovedHeroes()
        {
            HeroN("PetalSprite", "petalsprite", "Petal Sprite", d =>
            {
                d.maxHealth = 220; d.damage = 24; d.attackRate = 1; d.range = 4.5f; d.splashRadius = 0.1f;
                d.passiveKind = PassiveKind.SeedStacks;
                d.projectileFrames = ApprovedFrames("PetalSprite", "seed-bolt-fx");
                d.passiveFrames = ApprovedFrames("PetalSprite", "seed-stuck-fx");
                d.impactFrames = ApprovedFrames("PetalSprite", "sprout-burst-fx");
                d.passiveText = "Clinging Seed: each Hit plants 1 seed for 4s; 3 seeds explode for 2× basic Hit Damage on the Target.";
            }, SkillKind.SeedRain, d => { d.manaMax = 5; d.ultimateText = "Seed Rain: fire 8 seeds over 1,6s at the largest group; apply Clinging Seed."; });
            HeroN("KoboldTrapper", "koboldtrapper", "Kobold Trapper", d =>
            {
                d.maxHealth = 300; d.damage = 15; d.attackRate = 0.8f; d.range = 3.5f; d.splashRadius = 0.1f;
                d.passiveKind = PassiveKind.Traps;
                d.projectileFrames = ApprovedFrames("KoboldTrapper", "knife-bolt-fx");
                d.passiveFrames = ApprovedFrames("KoboldTrapper", "trap-open-fx");
                d.passiveText = "Trap Setting: place 1 trap every 8s, up to 3; stepping on it deals 1× Damage and roots for 1,5s (elites 0,75s, Bosses immune to root).";
            }, SkillKind.TrapDetonation, d =>
            {
                d.manaMax = 6; d.skillRadius = 0.8f;
                d.skillIntroFrames = ApprovedFrames("KoboldTrapper", "trap-snap-fx");
                d.skillFrames = ApprovedFrames("KoboldTrapper", "trap-blast-fx");
                d.ultimateText = "Trap Detonation: all traps explode for 3× Damage around them and root for 1s (elites 0,5s), then replace 3 traps.";
            });
            HeroN("SigilMage", "sigilmage", "Sigil Mage", d =>
            {
                d.maxHealth = 240; d.damage = 18; d.attackRate = 0.8f; d.range = 4.5f; d.splashRadius = 0.1f;
                d.passiveKind = PassiveKind.SigilWeakness;
                d.projectileFrames = ApprovedFrames("SigilMage", "sigil-bolt-fx");
                d.passiveFrames = ApprovedFrames("SigilMage", "sigil-ring-fx");
                d.passiveText = "Weakening Seal: Enemies within radius 2 take 15% more Damage; does not Stack with stronger Armor Break.";
            }, SkillKind.SigilBombs, d =>
            {
                d.manaMax = 6; d.skillIntroFrames = ApprovedFrames("SigilMage", "sigil-mark-fx");
                d.skillFrames = ApprovedFrames("SigilMage", "star-blast-fx");
                d.ultimateText = "Explosive Charm: mark the 3 Enemies with the most HP; explode after 2s for 3× Damage within radius 0,6.";
            });
            foreach (string id in new[] { "PetalSprite", "KoboldTrapper", "SigilMage" })
            {
                var anim = AssetDatabase.LoadAssetAtPath<SpriteAnimationSet>(DataDir + "/Anim_" + id + ".asset");
                anim.framesPerSecond = 1000f / 150f;
                EditorUtility.SetDirty(anim);
            }

            var b = LoadOrCreate<BloodswordsmanKit>(DataDir + "/BloodswordsmanKit.asset", out _);
            b.offer.frames = ApprovedFrames("Bloodswordsman", "blood-offer");
            b.offer.hitFrame = 3;
            b.parry.frames = ApprovedFrames("Bloodswordsman", "blood-parry");
            b.aura = ApprovedFrames("Bloodswordsman", "blood-edge-fx");
            b.parryHit = ApprovedFrames("Bloodswordsman", "blood-parry-hit-fx");
            b.shardOut = ApprovedFrames("Bloodswordsman", "blood-shard-out-fx");
            b.shardStuck = ApprovedFrames("Bloodswordsman", "blood-shard-stuck-fx");
            b.shardReturn = ApprovedFrames("Bloodswordsman", "blood-shard-return-fx");
            b.reform = ApprovedFrames("Bloodswordsman", "blood-reform-fx");
            EditorUtility.SetDirty(b);
            ApprovedSR("Bloodswordsman", "bloodswordsman", "Blood Swordsman", "blood-shatter-cast", SkillKind.BloodShatter, d =>
            {
                d.maxHealth = 900; d.damage = 32; d.attackRate = 0.8f; d.range = 0.8f; d.bloodswordsman = b;
                d.impactFrames = ApprovedFrames("Bloodswordsman", "blood-hit-fx");
                d.healFrames = ApprovedFrames("Bloodswordsman", "blood-heal-fx");
                d.passiveText = "Blood Will: each 10% HP lost grants +5% Damage (up to 35%); personal Enemy kills Heal 6% HP, Cooldown 2s.";
                d.minorText = "Blood Sacrifice: spend 15% current HP; gain +40% Attack for 6s, slash up to 2 Enemies and Heal 4% HP per Hit. Cooldown 10s.";
                d.secondMinorText = "Blood Reversal: below 40% HP, block 50% of the first direct Hit within 1,5s, then counter-slash for 2× Damage and Heal 8% HP. Cooldown 8s.";
                d.ultimateText = "Bloodblade Recall: spend 20% current HP, impale up to 3 Enemies for 2× Damage; recall Shards after 0,8s for 3× Damage and Heal 8% HP per Target.";
            });
            var s = LoadOrCreate<SoulbinderKit>(DataDir + "/SoulbinderKit.asset", out _);
            s.link.frames = ApprovedFrames("Soulbinder", "soul-link-cast");
            s.link.hitFrame = 3;
            s.rip.frames = ApprovedFrames("Soulbinder", "soul-rip-cast");
            s.rip.hitFrame = 4;
            s.marker = ApprovedFrames("Soulbinder", "soul-link-marker-fx");
            s.pactMarker = ApprovedFrames("Soulbinder", "great-pact-marker-fx");
            s.segment = ApprovedFrames("Soulbinder", "soul-link-segment-fx");
            s.fade = ApprovedFrames("Soulbinder", "soul-link-fade-fx");
            s.transfer = ApprovedFrames("Soulbinder", "soul-transfer-fx");
            s.fragment = ApprovedFrames("Soulbinder", "soul-rip-fragment-fx");
            s.ripHit = ApprovedFrames("Soulbinder", "soul-rip-hit-fx");
            s.pactHit = ApprovedFrames("Soulbinder", "soul-pact-hit-fx");
            EditorUtility.SetDirty(s);
            ApprovedSR("Soulbinder", "soulbinder", "Soulbinder", "great-pact-cast", SkillKind.SoulPact, d =>
            {
                d.maxHealth = 320; d.damage = 24; d.attackRate = 0.8f; d.range = 4.5f; d.soulbinder = s; d.splashRadius = 0.1f;
                d.projectileFrames = ApprovedFrames("Soulbinder", "soul-bolt-fx");
                d.impactFrames = ApprovedFrames("Soulbinder", "soul-hit-fx");
                d.passiveText = "Link Transfer: when a linked Enemy dies, transfer the link to the nearest Enemy in the selection area, keeping the remaining duration.";
                d.minorText = "Triple Soul Link: link up to 3 Enemies for 6s; direct Damage spreads 25% of actual HP lost to each other Enemy. Cooldown 10s.";
                d.secondMinorText = "Soul Jolt: Stun linked Enemies, each soul deals 1,5× Damage to the original Target; total Damage spreads exactly once. Cooldown 10s.";
                d.ultimateText = "Great Soul Pact: link up to 5 Enemies for 8s, spread 40%; Soul Shock deals 2× Damage to the original Target. Star Damage does not spread again.";
            });
        }

        static UnityEngine.Sprite[] ApprovedFrames(string id, string action) => Frames(ArtRoot + id + "/" + action, action);
        static void ApprovedSR(string id, string unitId, string name, string cast, SkillKind kind, Action<HeroDefinition> stats)
        {
            var anim = AnimSet(id, id + "/", ("attack", "attack"), (cast, cast), hit: 4, skillHit: 2);
            anim.framesPerSecond = 1000f / 140f;
            EditorUtility.SetDirty(anim);
            var hero = NamedHero(HeroDef(id, null, anim, stats, kind, d => d.manaMax = 6f), name);
            hero.avatar = LoadSprite(ArtRoot + id + "/identity/identity-1.png");
            EditorUtility.SetDirty(hero);
            var unit = Unit(unitId, name, UnitCategory.Hero, Rarity.SR, "");
            unit.icon = hero.avatar;
            EditorUtility.SetDirty(unit);
            var gacha = AssetDatabase.LoadAssetAtPath<GachaConfig>(DataDir + "/GachaConfig.asset");
            if (!gacha.pool.Contains(unit)) { gacha.pool = gacha.pool.Append(unit).ToArray(); EditorUtility.SetDirty(gacha); }
        }
    }
}
