using System;
using System.Linq;
using BuildBuild.Combat;
using BuildBuild.Data;
using UnityEditor;
using UnityEngine;

namespace BuildBuild.Editor
{
    /// <summary>
    /// Noi 8 hero da duyet art vao game (2026-10-08, so CHUA can — Claude de xuat diem khoi diem):
    /// Nguyet Tho Luan (R) + Thợ Săn Hắc Ấn / Đạo Sĩ Hồ Lô / Ngự Bút Sư / Vũ Nữ Hồng Liên / Sa Vệ /
    /// Khôi Lỗi Sư / Người Sói (SR). Moi hero co Kit + component rieng (xem `Assets/Scripts/Combat/`),
    /// tru Nguyet Tho + Tho San chay thang tren `HeroDefinition` qua `Hero.NewSR.cs`.
    /// Chay lai bao nhieu lan cung duoc: asset da co (so can tay) khong bi ghi de.
    /// Khung `action-N.png` trong tung thu muc lech nhau theo pipeline (vd. `suction-1.png` trong
    /// `skill-suction-fx/`) nen moi cho goi `NF` voi ca thu muc + tien to.
    /// </summary>
    public static partial class SceneBuilder
    {
        static void BuildNewSRHeroes()
        {
            // ---------------------------------------------------------------- Nguyet Tho Luan (R)
            NewHero("MoonHare", "moonhare", "Moon Hare", Rarity.R, SkillKind.RingReturn, null,
                d => { d.manaMax = 5f; d.skillRadius = 4.5f; d.skillPower = 1.1f; },
                d =>
                {
                    d.maxHealth = 260; d.damage = 26; d.attackRate = 0.9f; d.range = 4.5f; d.moveSpeed = 1.2f;
                    d.splashRadius = 0.1f;
                    d.passiveKind = PassiveKind.ReturnStrike;
                    d.passivePower = 0.55f;
                    d.projectileFrames = NF("MoonHare", "skill-returning-ring-fx", "projectile");
                    d.impactFrames = NF("MoonHare", "skill-returning-ring-fx", "projectile");
                    d.skillFrames = NF("MoonHare", "skill-returning-ring-fx", "projectile");
                    d.ultimateText = "Jade Wheel Return: throw a large wheel through a long line, then recall it; outward ×1,1, return ×0,8, each Enemy takes 1 Hit per pass.";
                    d.passiveText = "Returning Wheel: every third basic Hit on an Enemy, the returning wheel deals an extra ×0,55 Damage to up to 3 Enemies on its return path.";
                    d.ultimateIcon = UiSprite("icons/icon-skill-moonhare-ring");
                    d.passiveIcon = UiSprite("icons/icon-passive-moonhare-return");
                });

            // ---------------------------------------------------------------- Tho San Hac An (SR)
            NewHero("BlackSigilHunter", "blacksigilhunter", "Black Sigil Hunter", Rarity.SR, SkillKind.SigilJudgment,
                ("skill-cast", "cast"),
                d =>
                {
                    d.manaMax = 6f; d.skillRadius = 1.5f; d.skillDuration = 2f; d.skillPower = 3f;
                    d.skillFrames = NF("BlackSigilHunter", "skill-judgment-cast", "impact");
                    d.skillIntroFrames = NF("BlackSigilHunter", "skill-armor-break-fx", "impact");
                },
                d =>
                {
                    d.maxHealth = 420; d.damage = 30; d.attackRate = 0.85f; d.range = 4.5f; d.moveSpeed = 1.2f;
                    d.splashRadius = 0.1f;
                    d.impactScale = 0.5f;   // art armor-break 57x60 px: o 1.0 (x UnitScale) to hon than dich
                    d.passiveKind = PassiveKind.ShieldBane;
                    d.passivePower = 0.3f;
                    d.projectileFrames = NF("BlackSigilHunter", "skill-pursuit-bolt-fx", "fx");
                    d.impactFrames = NF("BlackSigilHunter", "skill-armor-break-fx", "impact");
                    d.passiveFrames = NF("BlackSigilHunter", "skill-armor-break-fx", "impact");
                    d.ultimateText = "Dark Seal Judgment: raise 3 seal stakes at the 3 Enemies with the most HP; explode after 2s for ×3 Damage (Armor Rend Targets +50%) + Heal Block for 4s.";
                    d.passiveText = "Seal Breaker: direct Hits on Shielded Targets deal +30% Damage.";
                    d.minorText = "Armor Rending Raven: prioritize Shielded Targets, deal ×2 Damage + strong Armor Break + Heal Block. Cooldown 10s.";
                    d.ultimateIcon = UiSprite("icons/icon-skill-blacksigil-judgment");
                    d.minorIcon = UiSprite("icons/icon-skill-blacksigil-raven-rend");
                    d.passiveIcon = UiSprite("icons/icon-skill-blacksigil-break");
                });

            // ---------------------------------------------------------------- Dao Si Ho Lo (SR)
            var gourd = LoadOrCreate<GourdTaoistKit>(DataDir + "/GourdTaoistKit.asset", out _);
            gourd.suctionCast.frames = NF("GourdTaoist", "skill-cleanse-cast", "cleanse-cast");
            gourd.suctionCast.hitFrame = 4;
            gourd.compressedCast.frames = NF("GourdTaoist", "skill-compressed-cast", "compressed-cast");
            gourd.compressedCast.hitFrame = 4;
            gourd.ultCast.frames = NF("GourdTaoist", "skill-cast", "ultimate-cast");
            gourd.ultCast.hitFrame = 4;
            gourd.suction = NF("GourdTaoist", "skill-suction-fx", "suction");
            gourd.shield = NF("GourdTaoist", "skill-cleanse-shield-fx", "cleanse-shield");
            gourd.compressedBolt = NF("GourdTaoist", "skill-compressed-bolt-fx", "compressed-bolt");
            gourd.compressedBlast = NF("GourdTaoist", "skill-poison-impact-fx", "poison-impact");
            gourd.cone = NF("GourdTaoist", "skill-ultimate-cone-fx", "ultimate-cone");
            EditorUtility.SetDirty(gourd);
            NewHero("GourdTaoist", "gourdtaoist", "Dao Si Ho Lo", Rarity.SR, SkillKind.GourdUnleash,
                ("skill-cast", "ultimate-cast"),
                d => { d.manaMax = 6f; },
                d =>
                {
                    d.maxHealth = 520; d.damage = 24; d.attackRate = 0.8f; d.range = 4.5f; d.moveSpeed = 1.1f;
                    d.splashRadius = 0.1f;
                    d.gourdTaoist = gourd;
                    d.projectileFrames = NF("GourdTaoist", "skill-spirit-bolt-fx", "spirit-bolt");
                    d.impactFrames = NF("GourdTaoist", "skill-spirit-impact-fx", "spirit-impact");
                    d.ultimateText = "Heavenly Gourd Opening: absorb all Poison/Burn/Slow around allies, then spend all Qi to spray a cone for 2s.";
                    d.passiveText = "Pure Qi Shield: personally cleansing an ally grants a Shield of 6% HP for 4s.";
                    d.minorText = "Evil Absorption: absorb Poison/Burn/Slow from up to 2 allies, each type grants +1 Qi. Cooldown 8s.";
                    d.secondMinorText = "Qi Burst: spend 2 Qi to fire an explosion for ×2 Damage + Poison for 4s. Cooldown 10s.";
                    d.ultimateIcon = UiSprite("icons/icon-skill-gourd-ultimate");
                    d.minorIcon = UiSprite("icons/icon-skill-gourd-suction");
                    d.secondMinorIcon = UiSprite("icons/icon-skill-gourd-burst");
                    d.passiveIcon = UiSprite("icons/icon-passive-gourd-pure-qi");
                });

            // ---------------------------------------------------------------- Ngu But Su (SR)
            var ink = LoadOrCreate<InkScribeKit>(DataDir + "/InkScribeKit.asset", out _);
            ink.horizontalCast.frames = NF("InkScribe", "skill-horizontal-cast", "horizontal-cast");
            ink.horizontalCast.hitFrame = 4;
            ink.sealCast.frames = NF("InkScribe", "skill-seal-cast", "seal-cast");
            ink.sealCast.hitFrame = 4;
            ink.dragonCast.frames = NF("InkScribe", "skill-cast", "dragon-cast");
            ink.dragonCast.hitFrame = 4;
            ink.brushMarks = NF("InkScribe", "skill-brush-marks-fx", "brush-marks");
            ink.horizontalStroke = NF("InkScribe", "skill-horizontal-stroke-fx", "horizontal-stroke");
            ink.sealZone = NF("InkScribe", "skill-seal-zone-fx", "seal-zone");
            ink.sealBurst = NF("InkScribe", "skill-seal-burst-fx", "seal-burst");
            ink.dragonPainting = NF("InkScribe", "skill-dragon-painting-fx", "dragon-painting");
            ink.dragonPath = NF("InkScribe", "skill-dragon-path-fx", "dragon-path");
            ink.dragonImpact = NF("InkScribe", "skill-dragon-impact-fx", "dragon-impact");
            ink.inkDragon = NF("InkScribe", "skill-ink-dragon-fx", "ink-dragon");
            EditorUtility.SetDirty(ink);
            NewHero("InkScribe", "inkscribe", "Ngu But Su", Rarity.SR, SkillKind.InkDragon,
                ("skill-cast", "dragon-cast"),
                d => { d.manaMax = 6f; },
                d =>
                {
                    d.maxHealth = 480; d.damage = 28; d.attackRate = 0.75f; d.range = 4.5f; d.moveSpeed = 1.1f;
                    d.splashRadius = 0.1f;
                    d.inkScribe = ink;
                    d.projectileFrames = NF("InkScribe", "skill-ink-bolt-fx", "ink-bolt");
                    d.impactFrames = NF("InkScribe", "skill-ink-impact-fx", "ink-impact");
                    d.ultimateText = "Dragon Awakening: an ink dragon sweeps a long strip for ×3 Damage, then explodes at the end for ×2, hitting ground/flying Targets.";
                    d.passiveText = "Triple Brush Seal: direct Hits apply Brush marks (up to 3); full marks explode for an extra ×0,8 Damage.";
                    d.minorText = "Crosswise Brush: draw an ink stroke across the advance path; Enemies crossing it take ×1,5 Damage. Cooldown 7s.";
                    d.secondMinorText = "Mountain Suppression Seal: stamp an Enemy group for ×1,5 Damage + Root for 1,2s. Cooldown 10s.";
                    d.ultimateIcon = UiSprite("icons/icon-skill-ink-dragon");
                    d.minorIcon = UiSprite("icons/icon-skill-ink-line");
                    d.secondMinorIcon = UiSprite("icons/icon-skill-ink-mountain-seal");
                    d.passiveIcon = UiSprite("icons/icon-passive-ink-three-marks");
                });

            // ---------------------------------------------------------------- Vu Nu Hong Lien (SR)
            var hong = LoadOrCreate<HongLienKit>(DataDir + "/HongLienKit.asset", out _);
            hong.fanCast.frames = NF("HongLien", "skill-fan-cast", "song-phien-vu");
            hong.fanCast.hitFrame = 4;
            hong.detonateCast.frames = NF("HongLien", "skill-detonate-cast", "lien-tam-dan");
            hong.detonateCast.hitFrame = 4;
            hong.grandCast.frames = NF("HongLien", "skill-cast", "hong-lien-dai-vu");
            hong.grandCast.hitFrame = 4;
            hong.fanFlame = NF("HongLien", "skill-fan-flame-fx", "song-phien-flame");
            hong.lotusBud = NF("HongLien", "skill-lotus-bud-fx", "lotus-bud-loop");
            hong.lotusBloom = NF("HongLien", "skill-lotus-bloom-fx", "lotus-bloom");
            hong.lotusFade = NF("HongLien", "skill-lotus-fade-fx", "lotus-fade");
            hong.greatBud = NF("HongLien", "skill-great-bud-fx", "great-lotus-bud");
            hong.greatBloom = NF("HongLien", "skill-great-bloom-fx", "great-lotus-bloom");
            EditorUtility.SetDirty(hong);
            NewHero("HongLien", "honglien", "Vu Nu Hong Lien", Rarity.SR, SkillKind.LotusBloom,
                ("skill-cast", "hong-lien-dai-vu"),
                d => { d.manaMax = 6f; },
                d =>
                {
                    d.maxHealth = 460; d.damage = 26; d.attackRate = 0.85f; d.range = 4f; d.moveSpeed = 1.1f;
                    d.splashRadius = 0.1f;
                    d.hongLien = hong;
                    d.projectileFrames = NF("HongLien", "skill-fire-fan-fx", "phien-hoa-projectile");
                    d.impactFrames = NF("HongLien", "skill-fire-impact-fx", "phien-hoa-impact");
                    d.ultimateText = "Grand Lotus Dance: throw a large bud, then bloom all buds at once; Enemies can take multiple overlapping explosions.";
                    d.passiveText = "Lotus Sowing: every third basic Hit plants 1 lotus bud on the ground beneath the Enemy (up to 3).";
                    d.minorText = "Twin Fan Dance: 2 narrow fan-shaped fire streams deal ×1,5 Damage. Cooldown 8s.";
                    d.secondMinorText = "Lotus Heart Trigger: detonate the bud nearest the Target for ×2 Damage. Cooldown 6s.";
                    d.ultimateIcon = UiSprite("icons/icon-skill-honglien-grand-lotus");
                    d.minorIcon = UiSprite("icons/icon-skill-honglien-twin-fans");
                    d.secondMinorIcon = UiSprite("icons/icon-skill-honglien-lotus-trigger");
                    d.passiveIcon = UiSprite("icons/icon-passive-honglien-lotus-seed");
                });

            // ---------------------------------------------------------------- Sa Ve (SR, tank)
            var save = LoadOrCreate<SaVeKit>(DataDir + "/SaVeKit.asset", out _);
            save.slamCast.frames = NF("SaVe", "skill-cast", "tram-sa-quyen");
            save.slamCast.hitFrame = 4;
            save.healCast.frames = NF("SaVe", "skill-heal-cast", "boi-thach");
            save.healCast.hitFrame = 4;
            save.fortCast.frames = NF("SaVe", "skill-fort-cast", "sa-thanh");
            save.fortCast.hitFrame = 4;
            save.quake = NF("SaVe", "skill-quake-fx", "tram-sa-ripple");
            save.sandCore = NF("SaVe", "skill-sand-core-fx", "tu-sa-core");
            save.heal = NF("SaVe", "skill-heal-fx", "boi-thach-heal");
            save.fortTelegraph = NF("SaVe", "skill-fort-telegraph-fx", "sa-thanh-telegraph");
            save.fortLoop = NF("SaVe", "skill-fort-loop-fx", "sa-thanh-loop");
            save.fortFade = NF("SaVe", "skill-fort-fade-fx", "sa-thanh-fade");
            EditorUtility.SetDirty(save);
            NewHero("SaVe", "save", "Sa Ve", Rarity.SR, SkillKind.SandFort,
                ("skill-fort-cast", "sa-thanh"),
                d => { d.manaMax = 6f; },
                d =>
                {
                    d.maxHealth = 1100; d.damage = 30; d.attackRate = 0.6f; d.range = 1f; d.moveSpeed = 0.9f;
                    d.saVe = save;
                    d.ultimateText = "Sand Castle Guard: 1 pulse around self + a sand zone for 6s (Enemies suffer Slow, self takes reduced Damage).";
                    d.passiveText = "Sand Gathering: taking direct Hits builds Stacks (up to 5), each Stack reduces Damage taken by 4%.";
                    d.minorText = "Sinking Sand Fist: slam the ground for ×1,5 Damage + 30% Slow for 3s. Cooldown 8s.";
                    d.secondMinorText = "Stone Restoration: spend 2 Stacks to Heal 15% HP. Cooldown 10s.";
                    d.ultimateIcon = UiSprite("icons/icon-skill-save-sand-zone");
                    d.minorIcon = UiSprite("icons/icon-skill-save-sand-fist");
                    d.secondMinorIcon = UiSprite("icons/icon-skill-save-sand-fist");
                    d.passiveIcon = UiSprite("icons/icon-passive-save-sand-stack");
                });

            // ---------------------------------------------------------------- Khoi Loi Su (SR)
            var khoi = LoadOrCreate<KhoiLoiSuKit>(DataDir + "/KhoiLoiSuKit.asset", out _);
            khoi.deployCast.frames = NF("KhoiLoiSu", "skill-deploy-cast", "nhat-khoi-trien-khai");
            khoi.deployCast.hitFrame = 4;
            khoi.sweepCast.frames = NF("KhoiLoiSu", "skill-cast", "quet-moc-lenh");
            khoi.sweepCast.hitFrame = 4;
            khoi.guardCast.frames = NF("KhoiLoiSu", "skill-guard-cast", "ho-chu-lenh");
            khoi.guardCast.hitFrame = 4;
            khoi.grandCast.frames = NF("KhoiLoiSu", "skill-grand-cast", "khoi-vuong-lenh");
            khoi.grandCast.hitFrame = 4;
            khoi.cable = NF("KhoiLoiSu", "skill-cable-fx", "control-cable");
            khoi.guardDust = NF("KhoiLoiSu", "skill-guard-dust-fx", "ho-chu-dust");
            khoi.impact = NF("KhoiLoiSu", "skill-impact-fx", "moc-impact");
            khoi.grandWave = NF("KhoiLoiSu", "skill-grand-wave-fx", "khoi-vuong-wave");
            // Ba clip TRANG THAI cua roi: truoc day art giao ve nhung khong noi vao dau (roi trien khai / do don /
            // bung khop deu dung hinh — 2026-10-09).
            khoi.puppetDeploy.frames = NF("MocKhoi", "skill-deploy", "deploy");
            khoi.puppetDeploy.hitFrame = 4;
            khoi.puppetParry.frames = NF("MocKhoi", "skill-parry", "ho-chu-parry");
            khoi.puppetParry.hitFrame = 4;
            khoi.grandTransform.frames = NF("DaiKhoi", "skill-transform", "transform");
            khoi.grandTransform.hitFrame = 4;
            khoi.unitPrefab = AssetDatabase.LoadAssetAtPath<Hero>(PrefabDir + "/Hero.prefab");
            BuildPuppet(khoi);
            EditorUtility.SetDirty(khoi);
            NewHero("KhoiLoiSu", "khoiloisu", "Khoi Loi Su", Rarity.SR, SkillKind.GrandPuppet,
                ("skill-grand-cast", "khoi-vuong-lenh"),
                d => { d.manaMax = 6f; },
                d =>
                {
                    d.maxHealth = 500; d.damage = 20; d.attackRate = 0.7f; d.range = 4.5f; d.moveSpeed = 1f;
                    d.khoiLoiSu = khoi;
                    // Don thuong cua chu = lenh roi: khong co roi la khong co sat thuong, nen roi co tu 0 sao.
                    d.passiveStar = 0;
                    d.ultimateText = "Puppet King Descent: the puppet unfolds into a giant puppet, slams a wave for ×3 Damage + Stun, and fights for 10s.";
                    d.passiveText = "Puppet Companion: deploy 1 puppet (separate HP, blocks 3); repair for 8s if broken.";
                    d.minorText = "Wooden Sweep: the puppet sweeps in a circle for ×1,5 Damage. Cooldown 8s.";
                    d.secondMinorText = "Master Guard: the puppet returns to repel Enemies close to its master, then returns to the line. Cooldown 12s.";
                    d.ultimateIcon = UiSprite("icons/icon-skill-khoiloisu-puppet-king");
                    d.minorIcon = UiSprite("icons/icon-skill-puppet");
                    d.secondMinorIcon = UiSprite("icons/icon-skill-oath-guard");
                    d.passiveIcon = UiSprite("icons/icon-passive-khoiloisu-one-puppet");
                });

            // ---------------------------------------------------------------- Nguoi Soi (SR)
            var wolf = LoadOrCreate<WerewolfKit>(DataDir + "/WerewolfKit.asset", out _);
            wolf.transformClip.frames = NF("Werewolf", "skill-transform", "transform");
            wolf.transformClip.hitFrame = 4;
            wolf.howlClip.frames = NF("Werewolf", "skill-howl", "wolf-howl");
            wolf.howlClip.hitFrame = 4;
            wolf.biteClip.frames = NF("Werewolf", "skill-bite", "wolf-bite");
            wolf.biteClip.hitFrame = 4;
            wolf.punchImpact = NF("Werewolf", "skill-punch-impact-fx", "punch-impact");
            wolf.clawSlash = NF("Werewolf", "skill-claw-slash-fx", "claw-slash");
            wolf.clawSweep = NF("Werewolf", "skill-claw-sweep-fx", "claw-sweep");
            wolf.biteImpact = NF("Werewolf", "skill-bite-impact-fx", "bite-impact");
            wolf.howlWave = NF("Werewolf", "skill-howl-wave-fx", "howl-wave");
            wolf.bloodSiphon = NF("Werewolf", "skill-blood-siphon-fx", "blood-siphon");
            wolf.bloodShield = NF("Werewolf", "skill-blood-shield-fx", "blood-shield");
            wolf.transformBurst = NF("Werewolf", "skill-transform-burst-fx", "transform-burst");
            wolf.wolfSet = BuildWolfSet();
            EditorUtility.SetDirty(wolf);
            NewHero("Werewolf", "werewolf", "Werewolf", Rarity.SR, SkillKind.MoonTransform,
                ("skill-transform", "transform"),
                d => { d.manaMax = 5f; },
                d =>
                {
                    d.maxHealth = 800; d.damage = 30; d.attackRate = 1.1f; d.range = 1f; d.moveSpeed = 1.3f;
                    d.werewolf = wolf;
                    d.ultimateText = "Moon Transformation (1 time/battle): transform into a wolf PERMANENTLY — +40% HP, +30% Attack, Unlock wolf skills.";
                    d.passiveText = "Blood Feast: Heal 12% (human) / 25% (wolf) of Damage dealt; excess becomes an Overflow Blood Shield.";
                    d.minorText = "Moon Howl (wolf): howl when surrounded to inflict Fear + gain +25% Attack Speed for 4s. Cooldown 12s.";
                    d.secondMinorText = "Finishing Bite (wolf): bite an Enemy below 30% HP for ×3 Damage; a kill grants Heal + halves Cooldown. Cooldown 8s.";
                    d.ultimateIcon = UiSprite("icons/icon-skill-werewolf-transform");
                    d.minorIcon = UiSprite("icons/icon-skill-werewolf-howl");
                    d.secondMinorIcon = UiSprite("icons/icon-skill-werewolf-bite");
                    d.passiveIcon = UiSprite("icons/icon-passive-werewolf-blood-feast");
                });
        }

        /// <summary>Roi MocKhoi: Hero day du (danh bang AI thuong), chu dieu qua `KhoiLoiSu`.</summary>
        static void BuildPuppet(KhoiLoiSuKit khoi)
        {
            var anim = AnimSet("MocKhoi", "MocKhoi/", ("attack", "attack"), ("skill-cast", "quet-moc"), walk: "skill-pulled",
                hit: 4, skillHit: 4);
            var puppet = HeroDef("MocKhoi", null, anim, d =>
            {
                d.displayName = "Wooden Puppet";
                d.maxHealth = 700; d.damage = 26; d.attackRate = 0f; d.range = 1f; d.moveSpeed = 1f;
                d.leashRange = 1.5f;
                // attackRate 0 → cooldown don tu than 100s: roi CHI dam theo lenh chu (`KhoiLoiSu.Strike`),
                // khong tu danh (kit: "chu khong dong thoi gay don rieng", roi danh thay chu).
            }, SkillKind.None, _ => { }, DataDir + "/Summons");   // Summons/: khong vao bang chon hero / doi NPC
            // Roi khong co walk rieng: clip di chuyen cua no la `move-pulled` (bi day keo luot di). Ban giao cu chep nham
            // walk cua Khoi Loi Su vao MocKhoi/walk -> roi di bo mang hinh chu. Ghi de moi lan build.
            anim.walk.frames = Frames(ArtRoot + "MocKhoi/skill-pulled", "move-pulled");
            EditorUtility.SetDirty(anim);
            khoi.puppet = puppet;
            khoi.grandSet = AnimSet("DaiKhoi", "DaiKhoi/", ("attack", "attack"), ("skill-cast", "slam"), hit: 4, skillHit: 4);
            // DaiKhoi co art walk tu 2026-10-11 (`Assets/Art/Generated/DaiKhoi/walk`, gap tu ban guide):
            // AnimSet nap san clip walk, nhanh duoi chi con la luoi do cho truong hop thieu folder.
            if (!khoi.grandSet.walk.IsValid)
            {
                khoi.grandSet.walk.frames = khoi.grandSet.idle.frames;
                EditorUtility.SetDirty(khoi.grandSet);
            }
        }

        static SpriteAnimationSet BuildWolfSet()
        {
            var anim = LoadOrCreate<SpriteAnimationSet>(DataDir + "/Anim_WerewolfWolf.asset", out _);
            string dir = ArtRoot + "Werewolf/";
            anim.idle.frames = Frames(dir + "wolf-idle", "wolf-idle");
            anim.walk.frames = Frames(dir + "wolf-walk", "wolf-walk");
            anim.attack.frames = Frames(dir + "wolf-attack", "wolf-attack");
            anim.attack.hitFrame = 4;
            anim.hurt.frames = Frames(dir + "wolf-hurt", "wolf-hurt");
            anim.death.frames = Frames(dir + "wolf-death", "wolf-death");
            EditorUtility.SetDirty(anim);
            return anim;
        }

        /// <summary>Frame `prefix-N.png` trong thu muc (khac `Frames` mac dinh lay ten thu muc).</summary>
        static Sprite[] NF(string id, string folder, string prefix) =>
            Frames(ArtRoot + id + "/" + folder, prefix);

        static void NewHero(string id, string unitId, string name, Rarity rarity, SkillKind kind,
            (string folder, string action)? skill, Action<HeroDefinition> skillStats, Action<HeroDefinition> stats)
        {
            var anim = AnimSet(id, id + "/", ("attack", "attack"), skill, hit: 4, skillHit: 4);
            var hero = NamedHero(HeroDef(id, null, anim, stats, kind, skillStats), name);
            hero.avatar = LoadSprite(UiDir + "avatars/avatar-" + unitId + ".png")
                ?? LoadSprite(ArtRoot + id + "/identity/identity-1.png");
            EditorUtility.SetDirty(hero);
            var unit = Unit(unitId, name, UnitCategory.Hero, rarity, "");
            if (hero.avatar != null) { unit.icon = hero.avatar; EditorUtility.SetDirty(unit); }
            var gacha = AssetDatabase.LoadAssetAtPath<GachaConfig>(DataDir + "/GachaConfig.asset");
            if (gacha != null && !gacha.pool.Contains(unit))
            {
                gacha.pool = gacha.pool.Append(unit).ToArray();
                EditorUtility.SetDirty(gacha);
            }
        }
    }
}
