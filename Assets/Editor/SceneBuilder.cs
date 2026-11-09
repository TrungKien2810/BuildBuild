using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BuildBuild.Combat;
using BuildBuild.Core;
using BuildBuild.Data;
using BuildBuild.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BuildBuild.Editor
{
    /// <summary>
    /// Dung scene Battle + Gacha va asset du lieu tu art da giao. Chay lai bao nhieu lan cung duoc:
    /// scene va prefab dung lai tu dau, con ScriptableObject chi TAO khi chua co — so da can bang tay
    /// trong Inspector khong bi ghi de.
    ///
    /// Toa do: art ve tren canvas 1920x1080 (Background/bg-field.png), PPU 100 -> 1 px = 0,01 unit,
    /// camera orthographic size 5,4 thay dung ca canvas. Pixel (px, py) tinh tu goc TREN-TRAI.
    /// </summary>
    public static partial class SceneBuilder
    {
        // Tam can chien (hero `range`, quai `attackRange`) tinh tu chan toi MEP than doi thu o co UnitScale 2:
        // so = (so cu do toi chan - nua than 0,2) x 2. Chi ap cho asset TAO MOI; asset da co sua tay.
        const string ArtRoot = "Assets/Art/Generated/";
        const string UiDir = ArtRoot + "UI/";
        const string DataDir = "Assets/Data";
        const string PrefabDir = "Assets/Prefabs";
        const string SceneDir = "Assets/Scenes";

        const float Ppu = 100f;
        const float RefWidth = 1920f;
        const float RefHeight = 1080f;

        // UI ve nho (nut 96x48, the 96x128) — hien o 2x nguyen de pixel deu, khong nhoe.
        const float UiScale = 2f;

        // Phao dai da v2 (Castle/fortress*.png, 3 ban chung canvas 735x900) hien o x0,75 (nguoi dung 2026-09-15:
        // x0,5 sat mep trai qua nho va chiem bai dat cho linh). Do alpha ban lanh trong canvas: trai x 246, vach
        // phai x 614 = TUONG TRUOC, day y 861. Mep alpha trai dat o px 260 — bai dat SAU thanh (x 0..~250) de
        // trong cho linh cho — nen vach phai o px ~536; day tai px 820 (sau hang cay truoc).
        // Goc GameObject Castle dat o vach tuong chu khong o tam sprite, vi Enemy do khoang cach toi transform
        // cua muc tieu: goc o tam thi quai di xuyen vao giua thanh.
        const float FortressScale = 0.75f;
        const float FortressLeftPx = 260f - 246f * FortressScale;
        const float FortressBasePx = 820f;
        const float FortressBaseInSprite = 861f;
        const float FortressWallInSprite = 614f;

        // Quai xuat hien ngoai mep phai (1920) roi di vao, khong hien ra giua duong.
        const float SpawnPx = 1980f;

        // San phu gan het man (bg-field.png 1920x1080, chot 2026-09-15): tan cay rieng tren toi y ~244,
        // ngon thong rieng duoi cao nhat y 805 (do hang toi tren anh cut) -> lan quai y 250..800. Enemy chi do khoang cach toi lau
        // dai theo x nen quai o lan nao cung cham tuong.
        const float LaneTopPx = 250f;
        const float LaneBottomPx = 800f;
        const float CastleMaxHealth = 2000f;

        static Font _font;

        [MenuItem("BuildBuild/Scenes/Build Battle + Gacha")]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            _font = PixelFont.Asset;
            ReimportArt();
            EnsureFolder(DataDir + "/Units");
            EnsureFolder(PrefabDir);

            var tuning = BuildWaveData(out var enemyPrefab);
            BuildNewEnemies(tuning);
            BuildOrcAndRat(tuning);
            BuildAlchemistAndShaman(tuning);
            BuildDreadwyrm(tuning);
            BuildRuinjaw(tuning);
            BuildGloomwing(tuning);
            BuildStatusEnemies(tuning);     // SceneBuilder.ApprovedUnits.cs
            BuildHeroData();
            BuildVespera();
            BuildCannons();
            var gacha = BuildGachaData();
            BuildSoldiers();
            BuildApprovedSoldiers();
            BuildChainwarden();
            BuildWyrmspear();
            BuildBanneret();
            BuildWukong();
            BuildStormcaller();
            BuildGalemonk();
            BuildDaoVien();
            BuildHeroN();
            BuildApprovedHeroes();
            BuildLowTrioHeroes();
            BuildNewSRHeroes();
            BuildNewSSRHeroes();
            FillSkillTexts();
            AssignHeroRoles();
            BuildEquipmentConfig();
            AssetDatabase.SaveAssets();

            BuildBattleScene(tuning, enemyPrefab);
            BuildGachaScene(gacha);
            BuildUnitsScene();
            BuildShopScene();
            BuildLoginSceneOnly();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath(SceneNames.Login), true),
                new EditorBuildSettingsScene(ScenePath(SceneNames.Battle), true),
                new EditorBuildSettingsScene(ScenePath(SceneNames.Gacha), true),
                new EditorBuildSettingsScene(ScenePath(SceneNames.Units), true),
                new EditorBuildSettingsScene(ScenePath(SceneNames.Shop), true),
            };
            EditorSceneManager.OpenScene(ScenePath(SceneNames.Battle));
            Debug.Log("[SceneBuilder] Da dung Battle + Gacha, dang ky Build Settings. Bam Play tu scene Battle.");
        }

        // Importer (pivot chan, border 9-slice) chi co hieu luc khi file duoc import LAI.
        static void ReimportArt()
        {
            foreach (var folder in new[] { ArtRoot + "Goblin", ArtRoot + "Ashfang", ArtRoot + "Frostweaver-v2", UiDir + "9slice", UiDir + "chrome", UiDir + "panels" })
                foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
                    AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
        }

        // ---------------------------------------------------------------- du lieu

        static WaveTuning BuildWaveData(out Enemy enemyPrefab)
        {
            string goblin = ArtRoot + "Goblin/";
            var anim = LoadOrCreate<SpriteAnimationSet>(DataDir + "/Anim_Goblin.asset", out bool newAnim);
            if (newAnim)
            {
                anim.idle.frames = Frames(goblin + "idle", "idle");
                anim.walk.frames = Frames(goblin + "walk", "walk");
                anim.attack.frames = Frames(goblin + "attack", "attack");
                anim.attack.hitFrame = 3;   // frame vu khi vuon xa nhat (do alpha)
                anim.hurt.frames = Frames(goblin + "hurt", "hurt");
                anim.death.frames = Frames(goblin + "death", "death");
                EditorUtility.SetDirty(anim);
            }

            // So goc GAME_DESIGN.md muc 5.
            var goblinDef = LoadOrCreate<EnemyDefinition>(DataDir + "/Enemy_Goblin.asset", out bool newDef);
            if (newDef)
            {
                goblinDef.displayName = "Goblin";
                goblinDef.kind = EnemyKind.Thuong;
                goblinDef.baseHealth = 100f;
                goblinDef.baseDamage = 15f;
                goblinDef.moveSpeed = 1.5f;
                goblinDef.attackRange = 0.4f;   // tam voi frame cham 0,20 x UnitScale 2, do toi MEP than hero
                goblinDef.animations = anim;
                EditorUtility.SetDirty(goblinDef);
            }

            string ashfang = ArtRoot + "Ashfang/";
            var ashAnim = LoadOrCreate<SpriteAnimationSet>(DataDir + "/Anim_Ashfang.asset", out bool newAshAnim);
            if (newAshAnim)
            {
                ashAnim.idle.frames = Frames(ashfang + "idle", "idle");
                ashAnim.walk.frames = Frames(ashfang + "walk", "walk");
                ashAnim.attack.frames = Frames(ashfang + "attack", "attack");
                ashAnim.attack.hitFrame = 3;
                ashAnim.hurt.frames = Frames(ashfang + "hurt", "hurt");
                ashAnim.death.frames = Frames(ashfang + "death", "death");
                EditorUtility.SetDirty(ashAnim);
            }

            var ashDef = LoadOrCreate<EnemyDefinition>(DataDir + "/Enemy_Ashfang.asset", out bool newAshDef);
            if (newAshDef)
            {
                ashDef.displayName = "Ashfang";
                ashDef.kind = EnemyKind.Nhanh;
                ashDef.baseHealth = 60f;
                ashDef.baseDamage = 10f;
                ashDef.moveSpeed = 3f;
                ashDef.attackRange = 0.5f;   // (0,45 cu - nua than hero 0,2) x UnitScale 2, do toi MEP than hero
                ashDef.animations = ashAnim;
                EditorUtility.SetDirty(ashDef);
            }

            // GDD muc 5: wave 1-3 Goblin, 4+ Goblin + Ashfang. Orc/boss chua co art nen 7-30 van chi
            // hai loai nay. Bang cu (chua co Ashfang) thi ghi lai bands; da co Ashfang thi giu so chinh tay.
            var tuning = LoadOrCreate<WaveTuning>(DataDir + "/WaveTuning.asset", out _);
            if (tuning.bands == null || !tuning.bands.Any(b => b.enemies != null && b.enemies.Contains(ashDef)))
            {
                tuning.bands = new[]
                {
                    new WaveBand { fromWave = 1, toWave = 3, enemies = new[] { goblinDef } },
                    new WaveBand { fromWave = 4, toWave = 30, enemies = new[] { goblinDef, ashDef } },
                };
                EditorUtility.SetDirty(tuning);
            }

            var go = new GameObject("Enemy");
            go.AddComponent<SpriteRenderer>();   // order dat moi frame theo chan (SortOrder.ForUnit)
            var animator = go.AddComponent<SpriteAnimator>();
            var enemy = go.AddComponent<Enemy>();
            SetRef(enemy, "animator", animator);
            enemyPrefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/Enemy.prefab").GetComponent<Enemy>();
            Object.DestroyImmediate(go);

            return tuning;
        }

        // Moi hero co bo art chien dau da giao. Chi so = GAME_DESIGN.md muc 3; moveSpeed va so ky nang
        // khong co trong GDD thi ghi ro "Claude de xuat" o GDD. Can chien (tam 1) "ban" bang dan vo hinh
        // toc 20, splash 0,3 — cung cach Cragfist da chay.
        static void BuildHeroData()
        {
            HeroDef("Frostweaver", "avatar-frostweaver",
                AnimSet("Frostweaver", "Frostweaver-v2/", attack: ("cast", "cast"), idle: "idle-anchored", walk: "walk-v3", hit: 3),
                d =>
                {
                    d.maxHealth = 260f; d.damage = 35f; d.attackRate = 0.6f; d.range = 5f; d.moveSpeed = 1.2f;
                    // Dan ve rong 80 px, to hon nguoi (31 px) -> 0,5 (runtime nhan them UnitScale); vung no + nua than quai.
                    d.splashRadius = 0.45f; d.projectileScale = 0.5f; d.impactScale = 0.6f;
                    d.projectileFrames = Frames(ArtRoot + "Frostweaver-v2/frost-bolt", "projectile");
                    d.impactFrames = Frames(ArtRoot + "Frostweaver-v2/frost-impact", "impact");
                },
                SkillKind.FrostZone, d => { d.manaMax = 6f; d.skillRadius = 1.6f; d.skillDuration = 3f; });

            var cragfist = HeroDef("Cragfist", "avatar-cragfist",
                AnimSet("Cragfist", "Cragfist/", attack: ("attack", "attack"), skill: ("petrify-body", "petrify"), hit: 4),
                d =>
                {
                    d.maxHealth = 1400f; d.damage = 10f; d.attackRate = 0.5f; d.range = 0.8f; d.moveSpeed = 0.9f;
                    d.projectileSpeed = 20f; d.splashRadius = 0.3f;
                },
                SkillKind.Petrify, d => { d.manaMax = 6f; d.skillDuration = 5f; d.skillPower = 0.2f; });
            // Vo da la FX bo sung sau khi asset da co — bu mot lan.
            if (cragfist.skillFrames == null || cragfist.skillFrames.Length == 0)
            {
                cragfist.skillIntroFrames = Frames(ArtRoot + "Cragfist/petrify-cast", "cast");
                cragfist.skillFrames = Frames(ArtRoot + "Cragfist/petrify-shell", "shell");
                EditorUtility.SetDirty(cragfist);
            }

            HeroDef("Ironblade", "avatar-ironblade",
                AnimSet("Ironblade", "Ironblade/", attack: ("attack", "attack"), skill: ("cone-sweep-body", "cone-sweep-body"), hit: 4, skillHit: 3),
                d =>
                {
                    d.maxHealth = 900f; d.damage = 30f; d.attackRate = 0.9f; d.range = 0.7f; d.moveSpeed = 1.1f;
                    d.projectileSpeed = 20f; d.splashRadius = 0.3f;
                },
                SkillKind.ConeSweep, d =>
                {
                    d.manaMax = 4f; d.skillRadius = 1.6f; d.skillPower = 4f;   // dai = be rong art cone-sweep-fx x UnitScale
                    d.skillFrames = Frames(ArtRoot + "Ironblade/cone-sweep-fx", "cone-sweep-fx");
                });

            HeroDef("Hawkfletch", "avatar-hawkfletch",
                AnimSet("Hawkfletch", "Hawkfletch/", attack: ("attack", "attack"), hit: 4),
                d =>
                {
                    d.maxHealth = 220f; d.damage = 45f; d.attackRate = 1.2f; d.range = 6f; d.moveSpeed = 1.2f;
                    d.projectileSpeed = 12f; d.splashRadius = 0.3f;
                    d.projectileFrames = Frames(ArtRoot + "Hawkfletch/arrow", "projectile");
                    d.impactFrames = Frames(ArtRoot + "Hawkfletch/arrow-impact", "impact");
                },
                SkillKind.RapidFire, d => { d.manaMax = 4f; d.skillDuration = 5f; d.skillPower = 3f; });

            // Bonecaller: don thuong + chieu xin goi ky si qua thanh mana; noi tai + chieu thuong + linh
            // nam o Summoner_Bonecaller (so Claude de xuat trong GDD muc 3, chua can bang).
            var bonecaller = HeroDef("Bonecaller", "avatar-bonecaller",
                AnimSet("Bonecaller", "Bonecaller/", attack: ("attack", "attack"), hit: 4),
                d =>
                {
                    d.maxHealth = 300f; d.damage = 15f; d.attackRate = 0.7f; d.range = 4f; d.moveSpeed = 1f;
                    d.splashRadius = 0.3f;
                    d.projectileFrames = Frames(ArtRoot + "Bonecaller/projectile", "projectile");
                    d.impactFrames = Frames(ArtRoot + "Bonecaller/impact", "impact");
                },
                SkillKind.SummonKnight, d => { d.manaMax = 5f; });

            // Linh trieu hoi nam o Summons/ — `BuildBattleScene` loai thu muc nay khoi bang chon hero.
            string summons = DataDir + "/Summons";
            EnsureFolder(summons);
            var skeleton = HeroDef("Skeleton", null,
                AnimSet("Skeleton", "Bonecaller-Skeleton/", attack: ("attack", "attack"), hit: 4),
                d =>
                {
                    d.displayName = "Skeleton Soldier";
                    d.maxHealth = 150f; d.damage = 12f; d.attackRate = 1f; d.range = 0.5f; d.moveSpeed = 1f;
                    d.projectileSpeed = 20f; d.splashRadius = 0.3f;
                },
                SkillKind.None, null, summons);
            var bigSkeleton = HeroDef("BigSkeleton", null,
                AnimSet("BigSkeleton", "Bonecaller-BigSkeleton/", attack: ("attack", "attack"), hit: 4),
                d =>
                {
                    d.displayName = "Big Skeleton";
                    d.maxHealth = 700f; d.damage = 40f; d.attackRate = 0.7f; d.range = 0.9f; d.moveSpeed = 0.7f;
                    d.projectileSpeed = 20f; d.splashRadius = 0.3f;
                },
                SkillKind.None, null, summons);
            // Tam van theo luat hero moi (duoi quai khap san), khong dung aggro 3,5 cua GDD — luat do da bo.
            var deathKnight = HeroDef("DeathKnight", null,
                AnimSet("DeathKnight", "Bonecaller-DeathKnight/", attack: ("attack", "attack"), skill: ("charge", "charge"), hit: 4),
                d =>
                {
                    d.displayName = "Death Knight";
                    d.maxHealth = 1800f; d.damage = 55f; d.attackRate = 1f; d.range = 1f; d.moveSpeed = 1.2f;
                    d.projectileSpeed = 20f; d.splashRadius = 1.2f;   // don thuong quet dien rong quanh muc tieu
                    d.knockbackOnHit = true;
                },
                SkillKind.Charge, d => { d.manaMax = 5f; d.skillRadius = 6f; d.skillPower = 150f; d.skillDuration = 2f; },
                summons);
            // Sao choang la FX bo sung sau khi asset da co — bu mot lan (cung kieu vo da Cragfist).
            if (deathKnight.stunFrames == null || deathKnight.stunFrames.Length == 0)
            {
                deathKnight.stunFrames = Frames(ArtRoot + "Enemy/stunned-status", "stun");
                EditorUtility.SetDirty(deathKnight);
            }

            var go = new GameObject("Hero");
            go.AddComponent<SpriteRenderer>();   // order dat moi frame theo chan (SortOrder.ForUnit)
            var animator = go.AddComponent<SpriteAnimator>();
            // Mau that do `HeroDefinition` dien luc `Initialise`; so o day chi de prefab khong bang 0.
            SetValue(go.AddComponent<Health>(), "maxHealth", 260f);
            var hero = go.AddComponent<Hero>();
            SetRef(hero, "animator", animator);
            var heroPrefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/Hero.prefab").GetComponent<Hero>();
            Object.DestroyImmediate(go);

            var summoner = LoadOrCreate<SummonerDefinition>(summons + "/Summoner_Bonecaller.asset", out bool newSummoner);
            if (newSummoner)
            {
                summoner.ringFrames = Frames(ArtRoot + "Bonecaller/mark-ring", "impact");
                summoner.markFrames = Frames(ArtRoot + "Bonecaller/mark-status", "impact");
            }
            summoner.unitPrefab = heroPrefab;
            summoner.skeleton = skeleton;
            summoner.bigSkeleton = bigSkeleton;
            summoner.deathKnight = deathKnight;
            // Quang phap trieu hoi la FX bo sung sau khi asset da co — bu mot lan.
            if (summoner.summonBurstFrames == null || summoner.summonBurstFrames.Length == 0)
                summoner.summonBurstFrames = Frames(ArtRoot + "DeathKnight/summon-burst", "summon-burst");
            EditorUtility.SetDirty(summoner);
            if (bonecaller.summoner == null) bonecaller.summoner = summoner;
            EditorUtility.SetDirty(bonecaller);
        }

        static GachaConfig BuildGachaData()
        {
            var pool = new[]
            {
                // Hero dung CHAN DUNG (avatars/), phao dung EMBLEM do nghe (icons/) — luat chot
                // 2026-09-12: nhan vat = mat, do vat = emblem. `mage_summoner` con emblem cu vi
                // hero nay chua co art nhan vat, doi co roi doi sang avatar cho dong bo.
                Unit("knight", "Knight", UnitCategory.Hero, Rarity.N, "avatars/avatar-ironblade"),
                Unit("archer", "Archer", UnitCategory.Hero, Rarity.N, "avatars/avatar-hawkfletch"),
                Unit("mage_elemental", "Elemental Mage", UnitCategory.Hero, Rarity.N, "avatars/avatar-frostweaver"),
                Unit("mage_summoner", "Summoner Mage", UnitCategory.Hero, Rarity.SR, "icons/icon-mage-summoner"),
                // Do hiem theo suc manh (nguoi dung giao Claude 2026-09-26): Cragfist 1400 mau + Hoa Da mien sat thuong -> R.
                Unit("cragfist", "Cragfist", UnitCategory.Hero, Rarity.R, "avatars/avatar-cragfist"),
                // Vespera: hero R da du bo (avatar + chieu Den Canh + icon ult/passive) nen vao pool THAT — truoc
                // 2026-10-20 chi Admin cho so huu vi thieu `UnitDefinition`, keo theo cua hang khong ban manh duoc.
                Unit("vespera", "Vespera", UnitCategory.Hero, Rarity.R, "avatars/avatar-vespera"),
                Unit("cannon_rapid", "Rapid Fire Cannon", UnitCategory.Cannon, Rarity.N, "icons/icon-cannon-rapid"),
                Unit("cannon_blast", "Explosive Cannon", UnitCategory.Cannon, Rarity.N, "icons/icon-cannon-blast"),
                Unit("cannon_frost", "Ice Cannon", UnitCategory.Cannon, Rarity.N, "icons/icon-cannon-frost"),
                // Phao #4 (2026-10-01): R vi xuyen hang quai — do hiem Claude de xuat. Chua co icon UI rieng.
                Unit("cannon_ballista", "Piercing Crossbow", UnitCategory.Cannon, Rarity.R, "icons/icon-cannon-ballista"),
                // Bo phao 2026-10-05 (asset nhanh asset/phao-tru + asset/phao-thaituan05). Do hiem theo the giao viec /
                // mechanics.md cua ban art; Gatling + Hoa Mai Claude chon.
                Unit("cannon_net", "Net Cannon", UnitCategory.Cannon, Rarity.R, "icons/icon-cannon-net"),
                Unit("cannon_mortar", "Mortar Cannon", UnitCategory.Cannon, Rarity.N, "icons/icon-cannon-mortar"),
                Unit("cannon_tesla", "Lightning Tower", UnitCategory.Cannon, Rarity.R, "icons/icon-cannon-tesla"),
                Unit("cannon_siege", "Siege Cannon", UnitCategory.Cannon, Rarity.SR, "icons/icon-cannon-siege"),
                Unit("cannon_gatling", "Automatic Gatling", UnitCategory.Cannon, Rarity.R, "icons/icon-cannon-gatling"),
                Unit("cannon_longgun", "Castle Musket", UnitCategory.Cannon, Rarity.SR, "icons/icon-cannon-longgun"),
                Unit("cannon_sonic", "Sonic Cannon", UnitCategory.Cannon, Rarity.SR, "icons/icon-cannon-sonic"),
                Unit("cannon_void", "Dark Star Cannon", UnitCategory.Cannon, Rarity.SSR, "icons/icon-cannon-void"),
                // Phao Dau Rong Da (muc 4.16, bo art cua ban art da co trong repo tu 2026-10-05 nhung chua vao game).
                // SSR thu hai cua kho phao, va la khau DUY NHAT khong ban tung phat — xem `DragonBreath`.
                Unit("cannon_dragonhead", "Dragon Head Cannon", UnitCategory.Cannon, Rarity.SSR, "icons/icon-cannon-dragonhead"),
            };

            // Chi NOI THEM unit con thieu vao pool da co (nhu linh) — khong xoa unit chinh tay trong Inspector.
            var config = LoadOrCreate<GachaConfig>(DataDir + "/GachaConfig.asset", out _);
            var merged = (config.pool ?? Array.Empty<UnitDefinition>()).ToList();
            foreach (var unit in pool)
                if (!merged.Contains(unit)) merged.Add(unit);
            config.pool = merged.ToArray();
            EditorUtility.SetDirty(config);
            return config;
        }

        // ---------------------------------------------------------------- du lieu nhanh Linh (linh trieu hoi, Chainwarden)

        // Linh nam o Soldiers/ — `BuildBattleScene` loai thu muc nay khoi bang chon hero (SoldierDefinition la HeroDefinition).
        const string SoldierDir = DataDir + "/Soldiers";

        /// <summary>
        /// 3 linh trieu hoi da co art (plan muc A2). So = bang plan (Claude de xuat, CHUA can bang); gia + hoi chieu
        /// plan chua ghi -> so tam o day. Can chien "ban" bang dan vo hinh toc 20, splash 0,3 nhu linh xuong.
        /// Tam can chien do toi MEP than quai (xem ghi chu dau file). hitFrame = frame vuon xa nhat (do alpha).
        /// </summary>
        static SoldierDefinition[] BuildSoldiers()
        {
            EnsureFolder(SoldierDir);

            var wolf = SoldierDef("WarWolf", "War Wolf",
                AnimSet("WarWolf", "WarWolf/", attack: ("attack", "attack"), hit: 3),
                d =>
                {
                    d.maxHealth = 200f; d.damage = 14f; d.attackRate = 1.2f; d.range = 0.5f; d.moveSpeed = 2f;
                    d.projectileSpeed = 20f; d.splashRadius = 0.3f;
                    d.squadSize = 3; d.summonCooldown = 12f;
                });

            var tortoise = SoldierDef("StoneTortoise", "Stone Shield Turtle",
                AnimSet("StoneTortoise", "StoneTortoise/", attack: ("attack", "attack"), hit: 2),
                d =>
                {
                    d.maxHealth = 700f; d.damage = 8f; d.attackRate = 0.5f; d.range = 0.6f; d.moveSpeed = 0.6f;
                    d.projectileSpeed = 20f; d.splashRadius = 0.3f;
                    d.squadSize = 2; d.summonCooldown = 18f;
                });

            var lizard = SoldierDef("AcidLizard", "Acid Lizard",
                AnimSet("AcidLizard", "AcidLizard/", attack: ("attack", "attack"), hit: 2),
                d =>
                {
                    d.maxHealth = 150f; d.damage = 18f; d.attackRate = 0.8f; d.range = 4f; d.moveSpeed = 1.2f;
                    d.leashRange = 3f;
                    // Giot axit ve co mui ten Hawkfletch (21 px) -> 1; vet no rong 48 px -> 0,6 nhu frost-impact.
                    d.projectileSpeed = 8f; d.splashRadius = 0.3f; d.impactScale = 0.6f;
                    d.projectileFrames = Frames(ArtRoot + "AcidLizard/acid", "projectile");
                    d.impactFrames = Frames(ArtRoot + "AcidLizard/acid-impact", "impact");
                    d.squadSize = 3; d.summonCooldown = 15f;
                });

            // Linh #4 SSR, top 1 con, khac che tinh anh/boss (IDEAS #1; so Claude de xuat, CHUA can). Huc = Charge chieu
            // nho: phi xuyen hang quai, choang + day lui; Khac Che mo tu 0 sao vi la vai tro chinh cua con nay.
            var warhorn = SoldierDef("Warhorn", "War Rhino",
                AnimSet("Warhorn", "Warhorn/", attack: ("attack", "attack"), skill: ("charge", "charge"), hit: 4),
                d =>
                {
                    d.maxHealth = 1500f; d.damage = 45f; d.attackRate = 0.7f; d.range = 0.7f; d.moveSpeed = 1.4f;
                    d.projectileSpeed = 20f; d.splashRadius = 0.3f;
                    d.knockbackOnHit = true;
                    d.skillKind = SkillKind.Charge; d.skillTier = SkillTier.Minor; d.skillCooldown = 8f;
                    d.skillRadius = 4f; d.skillPower = 120f; d.skillDuration = 1f;
                    d.skillFrames = Frames(ArtRoot + "Warhorn/dust", "dust");
                    d.stunFrames = Frames(ArtRoot + "Enemy/stunned-status", "stun");
                    d.passiveKind = PassiveKind.BossBane; d.passivePower = 0.5f; d.passiveStar = 0;
                    d.squadSize = 1; d.summonCooldown = 25f;
                });

            // Linh co trong gacha (IDEAS #1). Do hiem theo suc manh (2026-09-26): Rua 700 mau x2 -> R, con lai N.
            // Chi NOI THEM vao pool da co.
            var units = new[]
            {
                SoldierUnit("soldier_warwolf", wolf, Rarity.N),
                SoldierUnit("soldier_stonetortoise", tortoise, Rarity.R),
                SoldierUnit("soldier_acidlizard", lizard, Rarity.N),
                SoldierUnit("soldier_warhorn", warhorn, Rarity.SSR),
            };
            var gacha = AssetDatabase.LoadAssetAtPath<GachaConfig>(DataDir + "/GachaConfig.asset");
            if (gacha != null)
            {
                var pool = (gacha.pool ?? Array.Empty<UnitDefinition>()).ToList();
                foreach (var unit in units)
                    if (!pool.Contains(unit)) pool.Add(unit);
                gacha.pool = pool.ToArray();
                EditorUtility.SetDirty(gacha);
            }

            return new[] { wolf, tortoise, lizard, warhorn };
        }

        /// <summary>UnitDefinition gacha cua mot loai linh; icon = avatar cua linh.</summary>
        static UnitDefinition SoldierUnit(string id, SoldierDefinition soldier, Rarity rarity)
        {
            var unit = Unit(id, soldier.displayName, UnitCategory.Soldier, rarity, "");
            soldier.unitId = id;
            EditorUtility.SetDirty(soldier);
            if (soldier.avatar != null && (unit.icon == null || IsIdentityPlaceholder(unit.icon)))
            {
                unit.icon = soldier.avatar;
                EditorUtility.SetDirty(unit);
            }
            return unit;
        }

        /// <summary>
        /// Hero SR Chainwarden (plan muc A4, kit chot 2026-09-24). So = GDD muc 3 (Claude de xuat, CHUA can bang); toc chay
        /// GDD khong ghi -> 1,0 (giua Cragfist 0,9 va Ironblade 1,1). Nem Moc: clip than `skill` (moc bay ra o 3) + art
        /// `hook-fx` (dau moc bay / ngoam / doan xich) cho `HookChain`.
        /// Hero nam o Data/ goc nen tu vao bang chon hero.
        /// </summary>
        static HeroDefinition BuildChainwarden()
        {
            var warden = LoadOrCreate<WardenDefinition>(DataDir + "/Warden_Chainwarden.asset", out _);
            if (warden.stunFrames == null || warden.stunFrames.Length == 0)
                warden.stunFrames = Frames(ArtRoot + "Enemy/stunned-status", "stun");
            string hookDir = ArtRoot + "Chainwarden/hook-fx";
            if (warden.hookFrames == null || warden.hookFrames.Length == 0) warden.hookFrames = Frames(hookDir, "hook");
            if (warden.hookBite == null) warden.hookBite = LoadSprite(hookDir + "/hook-bite.png");
            if (warden.chainLink == null) warden.chainLink = LoadSprite(hookDir + "/hook-chain.png");
            // Quat Xich dang thuong (art duyet 2026-09-29): clip than + vong xich FX + vong troi chan + o khoa Xich.
            string wardenDir = ArtRoot + "Chainwarden/";
            if (warden.sweepFrames == null || warden.sweepFrames.Length == 0)
                warden.sweepFrames = Frames(wardenDir + "whirl-ring-fx", "whirl-ring");
            if (warden.sweepBody == null || !warden.sweepBody.IsValid)
                warden.sweepBody = new SpriteAnimationClip
                    { name = "skill", loop = false, frames = Frames(wardenDir + "whirl", "whirl") };
            if (warden.rootFrames == null || warden.rootFrames.Length == 0)
                warden.rootFrames = Frames(wardenDir + "chain-root", "root");
            if (warden.chainIconFrames == null || warden.chainIconFrames.Length == 0)
                warden.chainIconFrames = Frames(wardenDir + "chain-icon", "icon");
            EditorUtility.SetDirty(warden);

            var hero = HeroDef("Chainwarden", null,
                AnimSet("Chainwarden", "Chainwarden/", attack: ("attack", "attack"), skill: ("skill", "skill"), hit: 4,
                    skillHit: 3),   // hitFrame dem tu 1: o 3 = tay vung ra, moc roi tay
                d =>
                {
                    d.maxHealth = 1200f; d.damage = 28f; d.attackRate = 0.8f; d.range = 0.6f; d.moveSpeed = 1f;
                    d.projectileSpeed = 20f; d.splashRadius = 0.3f;
                    d.maxHeld = 4; d.heldDamageBonus = 0.2f;
                },
                SkillKind.HookThrow, d => { d.manaMax = 5f; });
            if (hero.warden == null) hero.warden = warden;
            // Avatar ban than cat tu identity (tempart/ui-c/make_unit_avatars.py); thay anh identity tam cu.
            if (hero.avatar == null || IsIdentityPlaceholder(hero.avatar))
                hero.avatar = LoadSprite(ArtRoot + "UI/avatars/avatar-chainwarden.png")
                    ?? LoadSprite(ArtRoot + "Chainwarden/identity/identity-1.png");
            EditorUtility.SetDirty(hero);

            // Dang Cuong No (art giao 2026-09-26): death dung death thuong vi chet la EndRage; skill = Loc Xich (clip than).
            string dir = ArtRoot + "Chainwarden/";
            var rage = LoadOrCreate<SpriteAnimationSet>(DataDir + "/Anim_Chainwarden_Rage.asset", out bool newRage);
            if (newRage)
            {
                rage.idle.frames = Frames(dir + "rage-idle", "idle");
                rage.walk.frames = Frames(dir + "rage-walk", "walk");
                rage.attack.frames = Frames(dir + "rage-attack", "attack");
                rage.attack.hitFrame = 4;   // xich vuon xa nhat (bbox rong 111 px)
                rage.hurt.frames = Frames(dir + "rage-hurt", "hurt");
                rage.death.frames = Frames(dir + "death", "death");
                rage.skill.frames = Frames(dir + "rage-storm", "storm");
                EditorUtility.SetDirty(rage);
            }
            if (warden.rageAnimations == null) warden.rageAnimations = rage;
            if (warden.rageTransform == null || !warden.rageTransform.IsValid)
                warden.rageTransform = new SpriteAnimationClip
                    { name = "skill", loop = false, frames = Frames(dir + "rage-transform", "transform") };
            // Giat Xich (chieu phu 2, art giao 2026-09-29): clip than dang Cuong No, o 3 = cu giat; FX va dap tren tung quai.
            if (warden.yankBody == null || !warden.yankBody.IsValid)
                warden.yankBody = new SpriteAnimationClip
                    { name = "skill", loop = false, hitFrame = 3, frames = Frames(dir + "rage-yank", "yank") };
            if (warden.yankFrames == null || warden.yankFrames.Length == 0)
                warden.yankFrames = Frames(dir + "yank-clash-fx", "clash");
            EditorUtility.SetDirty(warden);

            // Vao pool gacha khi du bo (GDD muc 9). Chi NOI THEM vao pool da co.
            var unit = Unit("chainwarden", "Chainwarden", UnitCategory.Hero, Rarity.SR, "");
            if (hero.avatar != null && (unit.icon == null || IsIdentityPlaceholder(unit.icon)))
            {
                unit.icon = hero.avatar;
                EditorUtility.SetDirty(unit);
            }
            var gacha = AssetDatabase.LoadAssetAtPath<GachaConfig>(DataDir + "/GachaConfig.asset");
            if (gacha != null && !gacha.pool.Contains(unit))
            {
                gacha.pool = gacha.pool.Append(unit).ToArray();
                EditorUtility.SetDirty(gacha);
            }
            return hero;
        }

        /// <summary>
        /// Hero SSR Wyrmspear (Long Ky Binh, kit chot 2026-09-27) + Bach Dia Long. So Claude de xuat, CHUA can bang. Hero nam o
        /// Data/ goc nen tu vao bang chon hero; rong nam o Summons/ (khong qua bang chon). Vao pool gacha (nguoi dung chot
        /// 2026-09-28) — unit SSR duy nhat cua pool. Chay rieng (khong dung lai scene) bang menu ben duoi.
        /// </summary>
        [MenuItem("BuildBuild/Data/Build Wyrmspear")]
        public static HeroDefinition BuildWyrmspear()
        {
            string dir = ArtRoot + "Wyrmspear/";
            string summons = DataDir + "/Summons";
            var rider = LoadOrCreate<DragonRiderDefinition>(DataDir + "/DragonRider_Wyrmspear.asset", out _);

            // Bach Dia Long: can chien, nhieu mau, giu 4 quai (GDD). skill = Dia Long Chan (clip than cua chieu thuong 2).
            var dragonAnim = LoadOrCreate<SpriteAnimationSet>(DataDir + "/Anim_BachDiaLong.asset", out bool newDragonAnim);
            if (newDragonAnim)
            {
                dragonAnim.idle.frames = Frames(dir + "dialong-idle", "idle");
                dragonAnim.walk.frames = Frames(dir + "dialong-walk", "walk");
                dragonAnim.attack.frames = Frames(dir + "dialong-attack", "attack");
                dragonAnim.attack.hitFrame = 4;
                dragonAnim.hurt.frames = Frames(dir + "dialong-hurt", "hurt");
                dragonAnim.death.frames = Frames(dir + "dialong-death", "death");
                dragonAnim.skill.frames = Frames(dir + "skill-diachan-body", "diachan");
                dragonAnim.skill.hitFrame = 4;
                EditorUtility.SetDirty(dragonAnim);
            }
            var dragon = HeroDef("BachDiaLong", null, dragonAnim, d =>
                {
                    d.displayName = "Bach Dia Long";
                    d.maxHealth = 1400f; d.damage = 30f; d.attackRate = 0.7f; d.range = 0.7f; d.moveSpeed = 0.9f;
                    d.projectileSpeed = 20f; d.splashRadius = 0.3f; d.maxHeld = 4;
                },
                SkillKind.None, _ => { }, summons);

            // Long Nhan: walk = hover (bay la la), skill = Thien Dia Long Giang (cam giao o 6).
            var dragonman = LoadOrCreate<SpriteAnimationSet>(DataDir + "/Anim_Wyrmspear_Dragonman.asset", out bool newDragonman);
            if (newDragonman)
            {
                dragonman.idle.frames = Frames(dir + "p2-idle", "idle");
                dragonman.walk.frames = Frames(dir + "p2-hover", "hover");
                dragonman.attack.frames = Frames(dir + "p2-attack", "attack");
                dragonman.attack.hitFrame = 4;
                dragonman.hurt.frames = Frames(dir + "p2-hurt", "hurt");
                dragonman.death.frames = Frames(dir + "p2-death", "death");
                dragonman.skill.frames = Frames(dir + "skill-longgiang-body", "longgiang");
                dragonman.skill.hitFrame = rider.descentHitFrame;
                EditorUtility.SetDirty(dragonman);
            }

            // Clip than chieu thuong + FX: chi dien khi con trong, so / frame chinh tay trong Inspector duoc giu.
            if (!rider.pierceBody.IsValid) rider.pierceBody.frames = Frames(dir + "skill-xuyenlong-body", "xuyenlong");
            if (!rider.lanceBody.IsValid)
            {
                // 8 o art -> 5 nhat: o 2/4/6 la ba nhat, lap 2 roi o 7 = nhat cuoi (brief wyrmspear-skills muc 5).
                var art = Frames(dir + "skill-lienthuong-body", "lienthuong");
                if (art.Length >= 8)
                    rider.lanceBody.frames = new[] { 1, 2, 3, 4, 5, 6, 5, 2, 3, 7, 8 }.Select(i => art[i - 1]).ToArray();
                rider.lanceHitFrames = new[] { 2, 4, 6, 8, 10 };
            }
            if (!rider.quakeBody.IsValid) rider.quakeBody.frames = Frames(dir + "skill-diachan-body", "diachan");
            if (!rider.descentDragon.IsValid)
                rider.descentDragon.frames = Frames(dir + "skill-longgiang-dialong", "longgiang-dialong");
            if (!rider.emerge.IsValid) rider.emerge.frames = Frames(dir + "dialong-emerge", "emerge");
            if (rider.pierceFx == null || rider.pierceFx.Length == 0) rider.pierceFx = Frames(dir + "skill-xuyenlong-fx", "xuyenlong-fx");
            if (rider.lanceFx == null || rider.lanceFx.Length == 0) rider.lanceFx = Frames(dir + "skill-lienthuong-fx", "lienthuong-fx");
            if (rider.crackFx == null || rider.crackFx.Length == 0) rider.crackFx = Frames(dir + "skill-datnut-fx", "datnut");
            if (rider.quakeFx == null || rider.quakeFx.Length == 0) rider.quakeFx = Frames(dir + "skill-gaida-fx", "gaida");
            if (rider.pillarFx == null || rider.pillarFx.Length == 0) rider.pillarFx = Frames(dir + "skill-cotda-fx", "cotda");
            if (rider.shockwaveFx == null || rider.shockwaveFx.Length == 0) rider.shockwaveFx = Frames(dir + "skill-songchan-fx", "songchan");
            if (rider.stunFrames == null || rider.stunFrames.Length == 0) rider.stunFrames = Frames(ArtRoot + "Enemy/stunned-status", "stun");
            if (rider.dragonmanAnimations == null) rider.dragonmanAnimations = dragonman;
            if (rider.dragon == null) rider.dragon = dragon;
            if (rider.unitPrefab == null) rider.unitPrefab = AssetDatabase.LoadAssetAtPath<Hero>(PrefabDir + "/Hero.prefab");
            EditorUtility.SetDirty(rider);

            // GD1: skill = Long Hoa (bien hinh 8 o, ra don cuoi clip). Can chien giao dai.
            var hero = HeroDef("Wyrmspear", null,
                AnimSet("Wyrmspear", "Wyrmspear/", attack: ("attack", "attack"), skill: ("skill-longhoa-transform", "longhoa"),
                    hit: 4),
                d =>
                {
                    d.maxHealth = 900f; d.damage = 40f; d.attackRate = 0.8f; d.range = 0.9f; d.moveSpeed = 1.1f;
                    d.projectileSpeed = 20f; d.splashRadius = 0.3f;
                },
                SkillKind.DragonRider, d => { d.manaMax = 5f; });
            if (hero.dragonRider == null) hero.dragonRider = rider;
            if (hero.avatar == null || IsIdentityPlaceholder(hero.avatar))
                hero.avatar = LoadSprite(ArtRoot + "UI/avatars/avatar-wyrmspear.png")
                    ?? LoadSprite(dir + "identity/identity-1.png");
            EditorUtility.SetDirty(hero);

            var unit = Unit("wyrmspear", "Dragon Rider", UnitCategory.Hero, Rarity.SSR, "");
            if (hero.avatar != null && (unit.icon == null || IsIdentityPlaceholder(unit.icon)))
            {
                unit.icon = hero.avatar;
                EditorUtility.SetDirty(unit);
            }
            // Chi NOI THEM vao pool da co.
            var gacha = AssetDatabase.LoadAssetAtPath<GachaConfig>(DataDir + "/GachaConfig.asset");
            if (gacha != null && !gacha.pool.Contains(unit))
            {
                gacha.pool = gacha.pool.Append(unit).ToArray();
                EditorUtility.SetDirty(gacha);
            }
            AssetDatabase.SaveAssets();
            return hero;
        }

        static SoldierDefinition SoldierDef(string id, string displayName, SpriteAnimationSet anim, Action<SoldierDefinition> stats,
            bool facesRight = true)
        {
            var def = LoadOrCreate<SoldierDefinition>($"{SoldierDir}/Soldier_{id}.asset", out bool created);
            if (created)
            {
                def.displayName = displayName;
                def.animations = anim;
                stats(def);
            }
            def.artFacesRight = facesRight;   // 4 linh dau ve quay PHAI (khac hero); linh tu 2026-10-04 quay trai
            // Avatar = tranh UI/avatars/avatar-<id>.png (cat tu identity, tempart/ui-c/make_unit_avatars.py); chua co thi
            // tam dung anh identity. Anh tam cu duoc thay khi tranh da co, avatar gan tay khac thi giu.
            if (def.avatar == null || IsIdentityPlaceholder(def.avatar))
                def.avatar = LoadSprite($"{ArtRoot}UI/avatars/avatar-{id.ToLowerInvariant()}.png")
                    ?? LoadSprite($"{ArtRoot}{id}/identity/identity-1.png");
            EditorUtility.SetDirty(def);
            return def;
        }

        /// <param name="icon">Duong dan duoi `Assets/Art/Generated/UI/`, khong co duoi `.png` —
        /// vd. "avatars/avatar-ironblade" hoac "icons/icon-cannon-frost".</param>
        static UnitDefinition Unit(string id, string displayName, UnitCategory category, Rarity rarity, string icon)
        {
            var unit = LoadOrCreate<UnitDefinition>($"{DataDir}/Units/Unit_{id}.asset", out bool created);
            if (created)
            {
                unit.id = id;
                unit.displayName = displayName;
                unit.category = category;
                unit.rarity = rarity;
            }
            // Icon phao do lane agy giao sau — chay lai builder thi tu gan, khong can sua tay.
            if (unit.icon == null)
                unit.icon = AssetDatabase.LoadAssetAtPath<Sprite>($"{UiDir}{icon}.png");
            EditorUtility.SetDirty(unit);
            return unit;
        }

        // ---------------------------------------------------------------- du lieu nhanh Tru (phao, quai moi, Vespera)

        const string CannonDir = DataDir + "/Cannons";

        /// <summary>
        /// Goblin Na (ban xa), Gatebreaker (tinh anh), Boss + bang wave GDD muc 5. So cua Goblin Na va ban kinh Chan Dia
        /// GDD chua chot — so tam (plan 2 thanh vien, muc B2). Bang wave chi ghi lai khi tuning chua co boss: da co
        /// boss nghia la da chay ban nay roi, so chinh tay trong Inspector duoc giu.
        /// </summary>
        static void BuildNewEnemies(WaveTuning tuning)
        {
            var slinger = LoadOrCreate<EnemyDefinition>(DataDir + "/Enemy_GoblinSlinger.asset", out bool newSlinger);
            if (newSlinger)
            {
                slinger.displayName = "Sling Goblin";
                slinger.kind = EnemyKind.BanXa;
                slinger.baseHealth = 80f;
                slinger.baseDamage = 12f;
                slinger.moveSpeed = 1.3f;
                slinger.attackRange = 4f;
                slinger.heroAggroRange = 4.5f;   // >= tam ban: hero / linh trong tam duoc chon truoc tuong
                slinger.attackRate = 0.8f;
                slinger.projectileFrames = Frames(ArtRoot + "GoblinSlinger/stone", "projectile");
                slinger.impactFrames = Frames(ArtRoot + "GoblinSlinger/stone-impact", "impact");
                slinger.projectileSpeed = 7f;
                slinger.projectileScale = 0.5f;
                slinger.animations = AnimSet("GoblinSlinger", "GoblinSlinger/", attack: ("attack", "attack"), hit: 4);
                EditorUtility.SetDirty(slinger);
            }

            // Gatebreaker moi co identity + walk, THIEU attack/hurt/death: tam dung walk (plan muc B2). Death bat buoc co
            // clip — Enemy doi het clip death moi bao chet; hurt de trong thi Enemy bo qua.
            var gateAnim = LoadOrCreate<SpriteAnimationSet>(DataDir + "/Anim_Gatebreaker.asset", out bool newGateAnim);
            if (newGateAnim)
            {
                var walk = Frames(ArtRoot + "Gatebreaker/walk", "walk");
                gateAnim.idle.frames = walk;
                gateAnim.walk.frames = walk;
                gateAnim.attack.frames = walk;
                gateAnim.attack.hitFrame = 4;
                gateAnim.death.frames = walk;
                EditorUtility.SetDirty(gateAnim);
            }
            var gatebreaker = LoadOrCreate<EnemyDefinition>(DataDir + "/Enemy_Gatebreaker.asset", out bool newGate);
            if (newGate)
            {
                gatebreaker.displayName = "Gatebreaker";
                gatebreaker.kind = EnemyKind.TinhAnh;
                gatebreaker.baseHealth = 1200f;
                gatebreaker.baseDamage = 60f;
                gatebreaker.moveSpeed = 0.7f;
                gatebreaker.attackRange = 0.5f;
                gatebreaker.attackRate = 0.6f;
                gatebreaker.damageReduction = 0.15f;
                gatebreaker.animations = gateAnim;
                EditorUtility.SetDirty(gatebreaker);
            }

            var bossAnim = AnimSet("Boss", "Boss/", attack: ("attack", "attack"), hit: 3);
            var bossKit = LoadOrCreate<BossDefinition>(DataDir + "/Boss_Minotaur.asset", out bool newKit);
            if (newKit)
            {
                bossKit.slamFrames = Frames(ArtRoot + "Boss/slam-fx", "slam-fx");
                bossKit.roarFrames = Frames(ArtRoot + "Boss/roar-wave", "roar-wave");
                bossKit.roarBuffFrames = Frames(ArtRoot + "Boss/roar-buff", "roar-buff");
                bossKit.slamBody.frames = Frames(ArtRoot + "Boss/slam", "slam");
                bossKit.slamBody.hitFrame = 5;   // dem tu 1: slam-5 bua cham dat (o 4 bua con vung ngang tren cao)
                bossKit.roarBody.frames = Frames(ArtRoot + "Boss/roar", "roar");
                bossKit.roarBody.hitFrame = 3;   // ha mom rong nhat
                EditorUtility.SetDirty(bossKit);
            }
            var boss = LoadOrCreate<EnemyDefinition>(DataDir + "/Enemy_Boss.asset", out bool newBoss);
            if (newBoss)
            {
                boss.displayName = "Vua Minotaur";
                boss.kind = EnemyKind.Boss;
                boss.baseHealth = 5000f;
                boss.baseDamage = 120f;
                boss.moveSpeed = 0.6f;
                boss.attackRange = 0.8f;
                boss.attackRate = 0.5f;
                boss.heroAggroRange = 2.5f;
                boss.damageReduction = 0.3f;
                boss.boss = bossKit;
                boss.animations = bossAnim;
                EditorUtility.SetDirty(boss);
            }

            if (tuning.boss != null) return;
            var goblin = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(DataDir + "/Enemy_Goblin.asset");
            var ashfang = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(DataDir + "/Enemy_Ashfang.asset");
            var mixed = new[] { goblin, ashfang, slinger };
            tuning.boss = boss;
            tuning.elite = gatebreaker;
            // Orc chua co art: wave 7-9 dung Goblin Na o cho Orc, wave 20 dung Goblin thay "4 Orc".
            tuning.bands = new[]
            {
                new WaveBand { fromWave = 1, toWave = 3, enemies = new[] { goblin } },
                new WaveBand { fromWave = 4, toWave = 6, enemies = new[] { goblin, ashfang } },
                new WaveBand { fromWave = 7, toWave = 9, enemies = mixed },
                new WaveBand { fromWave = 10, toWave = 10, enemies = new[] { goblin }, hasBoss = true, bossStatMultiplier = 1f, fixedCount = 6 },
                new WaveBand { fromWave = 11, toWave = 11, enemies = mixed },
                new WaveBand { fromWave = 12, toWave = 19, enemies = mixed, eliteCount = 1 },
                new WaveBand { fromWave = 20, toWave = 20, enemies = new[] { goblin }, hasBoss = true, bossStatMultiplier = 2f, fixedCount = 4 },
                new WaveBand { fromWave = 21, toWave = 29, enemies = mixed, eliteCount = 2 },
                new WaveBand { fromWave = 30, toWave = 30, enemies = mixed, hasBoss = true, bossStatMultiplier = 3.5f, fixedCount = 6 },
            };
            EditorUtility.SetDirty(tuning);
        }

        /// <summary>
        /// Orc Bua Ta (tank, GDD muc 5: 400 / 40 / 0.8 / 12) + Chuot Cong Chay (nhanh, so tam Claude de xuat — GDD chua co)
        /// + ghi lai bang wave de Orc vao dung cho GDD (7-9, 11-29, 4 Orc ho tong wave 20). Tam danh + hitFrame do tu alpha:
        /// frame vuon xa nhat (Orc 4, Chuot 3), cung ty le px -> tam cua Goblin/Ashfang. Bang wave chi ghi lai khi chua co
        /// Orc: da co nghia la da chay ban nay roi, so chinh tay trong Inspector duoc giu.
        /// </summary>
        static void BuildOrcAndRat(WaveTuning tuning)
        {
            var orc = LoadOrCreate<EnemyDefinition>(DataDir + "/Enemy_OrcBrute.asset", out bool newOrc);
            if (newOrc)
            {
                orc.displayName = "Orc Brute";
                orc.kind = EnemyKind.Tank;
                orc.baseHealth = 400f;
                orc.baseDamage = 40f;
                orc.moveSpeed = 0.8f;
                orc.attackRange = 0.7f;   // bua vuon 41 px truoc tam o 4 (Goblin 24 px -> 0,4)
                orc.animations = AnimSet("OrcBrute", "OrcBrute/", attack: ("attack", "attack"), hit: 4);
                EditorUtility.SetDirty(orc);
            }

            var rat = LoadOrCreate<EnemyDefinition>(DataDir + "/Enemy_RatRunner.asset", out bool newRat);
            if (newRat)
            {
                rat.displayName = "Sewer Rat";
                rat.kind = EnemyKind.Nhanh;
                rat.baseHealth = 45f;
                rat.baseDamage = 8f;
                rat.moveSpeed = 3.5f;
                rat.attackRange = 0.6f;   // dao dam 33 px truoc tam o 3
                rat.animations = AnimSet("RatRunner", "RatRunner/", attack: ("attack", "attack"), hit: 3);
                EditorUtility.SetDirty(rat);
            }

            if (tuning.bands != null && tuning.bands.Any(b => b.enemies != null && b.enemies.Contains(orc))) return;
            var goblin = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(DataDir + "/Enemy_Goblin.asset");
            var ashfang = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(DataDir + "/Enemy_Ashfang.asset");
            var slinger = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(DataDir + "/Enemy_GoblinSlinger.asset");
            var mixed = new[] { goblin, ashfang, slinger, orc, rat };
            tuning.bands = new[]
            {
                new WaveBand { fromWave = 1, toWave = 3, enemies = new[] { goblin } },
                new WaveBand { fromWave = 4, toWave = 6, enemies = new[] { goblin, ashfang, rat } },
                new WaveBand { fromWave = 7, toWave = 9, enemies = new[] { goblin, ashfang, slinger, orc } },
                new WaveBand { fromWave = 10, toWave = 10, enemies = new[] { goblin }, hasBoss = true, bossStatMultiplier = 1f, fixedCount = 6 },
                new WaveBand { fromWave = 11, toWave = 11, enemies = mixed },
                new WaveBand { fromWave = 12, toWave = 19, enemies = mixed, eliteCount = 1 },
                new WaveBand { fromWave = 20, toWave = 20, enemies = new[] { orc }, hasBoss = true, bossStatMultiplier = 2f, fixedCount = 4 },
                new WaveBand { fromWave = 21, toWave = 29, enemies = mixed, eliteCount = 2 },
                new WaveBand { fromWave = 30, toWave = 30, enemies = mixed, hasBoss = true, bossStatMultiplier = 3.5f, fixedCount = 6 },
            };
            EditorUtility.SetDirty(tuning);
        }

        /// <summary>
        /// Goblin Nem Binh Doc (ban xa, nem cong + vung doc 3s) + boss wave 20 Phap su Orc di kem Minotaur (GDD muc 5).
        /// So la de xuat cua Claude, CHUA can (GDD chua chot). hitFrame theo brief: o 4 binh / cau lua roi tay, cast o 4
        /// tay gio cao. Bang wave CHI NOI THEM (khong ghi de): Goblin Nem Binh vao moi band dang co Goblin Na, Phap su
        /// vao `extraBosses` cua band boss wave 20.
        /// </summary>
        /// <summary>
        /// Ruinjaw = Gatebreaker nang cap mot bac (GDD muc 5), thay Gatebreaker o band tinh anh tu wave 21 (ca cac wave sau 30
        /// dung lai band 21-29). So tam Claude de xuat 2026-09-30, CHUA can: ~1,5x Gatebreaker, giap nhinh hon, cham hon chut.
        /// hitFrame 5 = cong dap cham dat. Band da co `elite` (chinh tay trong Inspector) thi giu.
        /// </summary>
        static void BuildRuinjaw(WaveTuning tuning)
        {
            var ruinjaw = LoadOrCreate<EnemyDefinition>(DataDir + "/Enemy_Ruinjaw.asset", out bool created);
            // Frame doc lai MOI build, khong chi luc tao asset: walk cat lai 2026-10-12 lam 6 guid chet, ma dong
            // `AnimSet` nam trong `if (created)` nen build sau do khong bao gio sua — quai chop tat (nguoi dung bat 14/10).
            // `AnimSet` -> `Refill` giu nguyen bo frame dang khop thu muc (so chinh tay trong Inspector khong bi ghi de).
            ruinjaw.animations = AnimSet("Ruinjaw", "Ruinjaw/", attack: ("attack", "attack"), hit: 5);
            if (created)
            {
                ruinjaw.displayName = "Ruinjaw";
                ruinjaw.kind = EnemyKind.TinhAnh;
                ruinjaw.baseHealth = 1800f;
                ruinjaw.baseDamage = 90f;
                ruinjaw.moveSpeed = 0.65f;
                ruinjaw.attackRange = 0.6f;
                ruinjaw.attackRate = 0.55f;
                ruinjaw.damageReduction = 0.2f;
                EditorUtility.SetDirty(ruinjaw);
            }

            bool dirty = false;
            foreach (var band in tuning.bands ?? Array.Empty<WaveBand>())
                if (band.eliteCount > 0 && band.fromWave >= 21 && band.elite == null)
                {
                    band.elite = ruinjaw;
                    dirty = true;
                }
            if (dirty) EditorUtility.SetDirty(tuning);
        }

        /// <summary>
        /// Doi Quy `Gloomwing` — quai BAY CAO dau tien (`flyHigh`, co che A nguoi dung chot 2026-10-01): khong bi chan, chi
        /// hero / linh ban xa + phao danh trung. Walk = clip idle vo canh (nguoi dung chot), GIF duyet idle 110 ms ~ 9 fps.
        /// hitFrame 3 = o vuon xa nhat (nanh can). So tam Claude de xuat, CHUA can: mau giay, nhanh vua. Bang wave CHI NOI
        /// THEM vao band tu wave 11 da co Goblin Na (de nguoi choi da co quai ban xa / phao truoc khi gap doi).
        /// </summary>
        static void BuildGloomwing(WaveTuning tuning)
        {
            string dir = ArtRoot + "Gloomwing/";
            var anim = LoadOrCreate<SpriteAnimationSet>(DataDir + "/Anim_Gloomwing.asset", out bool newAnim);
            if (newAnim)
            {
                anim.framesPerSecond = 9f;
                anim.idle.frames = Frames(dir + "idle", "idle");
                anim.walk.frames = anim.idle.frames;   // bay: vo canh tai cho
                anim.attack.frames = Frames(dir + "attack", "attack");
                anim.attack.hitFrame = 3;
                anim.hurt.frames = Frames(dir + "hurt", "hurt");
                anim.death.frames = Frames(dir + "death", "death");
                EditorUtility.SetDirty(anim);
            }

            var bat = LoadOrCreate<EnemyDefinition>(DataDir + "/Enemy_Gloomwing.asset", out bool created);
            if (created)
            {
                bat.displayName = "Demon Bat";
                bat.kind = EnemyKind.Nhanh;
                bat.flyHigh = true;
                bat.baseHealth = 45f;
                bat.baseDamage = 12f;
                bat.moveSpeed = 1.8f;
                bat.attackRange = 0.6f;   // nanh o 3 vuon ~31 px truoc tam o
                bat.attackRate = 1f;
                bat.animations = anim;
                EditorUtility.SetDirty(bat);
            }

            var slinger = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(DataDir + "/Enemy_GoblinSlinger.asset");
            bool dirty = false;
            foreach (var band in tuning.bands ?? Array.Empty<WaveBand>())
                if (band.fromWave >= 11 && band.enemies != null && band.enemies.Contains(slinger) && !band.enemies.Contains(bat))
                {
                    band.enemies = band.enemies.Append(bat).ToArray();
                    dirty = true;
                }
            if (dirty) EditorUtility.SetDirty(tuning);
        }

        static void BuildAlchemistAndShaman(WaveTuning tuning)
        {
            string alch = ArtRoot + "GoblinAlchemist/";
            var alchemist = LoadOrCreate<EnemyDefinition>(DataDir + "/Enemy_GoblinAlchemist.asset", out bool newAlch);
            if (newAlch)
            {
                alchemist.displayName = "Alchemist Goblin";
                alchemist.kind = EnemyKind.BanXa;
                alchemist.baseHealth = 90f;
                alchemist.baseDamage = 10f;
                alchemist.moveSpeed = 1.2f;
                alchemist.attackRange = 4.5f;
                alchemist.heroAggroRange = 5f;   // >= tam nem
                alchemist.attackRate = 0.5f;
                alchemist.projectileFrames = Frames(alch + "bottle", "projectile");
                alchemist.impactFrames = Frames(alch + "bottle-shatter", "impact");
                alchemist.projectileSpeed = 5f;
                alchemist.projectileScale = 1f;   // binh ve dung co man hinh (9x13 px o 128)
                alchemist.projectileArc = 1.2f;
                alchemist.poolFrames = Frames(alch + "poison-pool", "pool");
                alchemist.poolDuration = 3f;
                alchemist.poolRadius = 0.6f;
                alchemist.poolDamagePerSecond = 12f;
                alchemist.animations = AnimSet("GoblinAlchemist", "GoblinAlchemist/", attack: ("attack", "attack"), hit: 4);
                EditorUtility.SetDirty(alchemist);
            }

            string sham = ArtRoot + "OrcShaman/";
            var kit = LoadOrCreate<ShamanDefinition>(DataDir + "/Shaman_OrcShaman.asset", out bool newKit);
            if (newKit)
            {
                kit.pitMarkFrames = Frames(sham + "pit-mark", "pit-mark");
                kit.pitBurstFrames = Frames(sham + "pit-burst", "pit-burst");
                kit.shieldFrames = Frames(sham + "shield-fx", "shield-fx");
                kit.castBody.frames = Frames(sham + "cast", "cast");
                kit.castBody.hitFrame = 4;
                kit.vanishBody.frames = Frames(sham + "teleport", "teleport");
                kit.appearBody.frames = kit.vanishBody.frames.Reverse().ToArray();
                EditorUtility.SetDirty(kit);
            }
            var shaman = LoadOrCreate<EnemyDefinition>(DataDir + "/Enemy_OrcShaman.asset", out bool newShaman);
            if (newShaman)
            {
                shaman.displayName = "Orc Shaman";
                shaman.kind = EnemyKind.Boss;
                shaman.baseHealth = 2500f;
                shaman.baseDamage = 60f;
                shaman.moveSpeed = 0.7f;
                shaman.attackRange = 5f;
                shaman.heroAggroRange = 5.5f;
                shaman.attackRate = 0.5f;
                shaman.damageReduction = 0.2f;
                shaman.projectileFrames = Frames(sham + "fireball", "fireball");
                shaman.impactFrames = Frames(sham + "fireball-impact", "fireball-impact");
                shaman.projectileSpeed = 6f;
                shaman.projectileScale = 1f;
                shaman.shaman = kit;
                shaman.animations = AnimSet("OrcShaman", "OrcShaman/", attack: ("attack", "attack"), hit: 4);
                EditorUtility.SetDirty(shaman);
            }

            var slinger = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(DataDir + "/Enemy_GoblinSlinger.asset");
            bool dirty = false;
            foreach (var band in tuning.bands ?? Array.Empty<WaveBand>())
            {
                if (band.enemies != null && band.enemies.Contains(slinger) && !band.enemies.Contains(alchemist))
                {
                    band.enemies = band.enemies.Append(alchemist).ToArray();
                    dirty = true;
                }
                if (band.hasBoss && band.Contains(20) && (band.extraBosses == null || band.extraBosses.Length == 0))
                {
                    band.extraBosses = new[] { shaman };
                    dirty = true;
                }
            }
            if (dirty) EditorUtility.SetDirty(tuning);
        }

        /// <summary>
        /// Boss wave 30 Rong Ma Hoa `Dreadwyrm` + quai trieu hoi Ma Hoa `SoulWisp` (kit GDD muc 5, art duyet 2026-10-01).
        /// Rong THAY Minotaur o band boss wave 30 (va 40/50... vi qua bang dung band boss cuoi) qua `WaveBand.boss` — gia
        /// dinh cua Claude, GDD ghi "thay hay di cung CHUA chot". Rong bay: walk = clip idle vo canh. Ma Hoa bay: walk = idle;
        /// attack/hurt/death clip rieng. Moi so CHUA can.
        /// </summary>
        static void BuildDreadwyrm(WaveTuning tuning)
        {
            string dir = ArtRoot + "Dreadwyrm/";
            string wispDir = ArtRoot + "SoulWisp/";

            var wispAnim = LoadOrCreate<SpriteAnimationSet>(DataDir + "/Anim_SoulWisp.asset", out bool newWispAnim);
            if (newWispAnim)
            {
                var hover = Frames(wispDir + "idle", "idle");
                wispAnim.framesPerSecond = 6.67f;   // GIF duyet 150 ms/frame
                wispAnim.idle.frames = hover;
                wispAnim.walk.frames = hover;
            }
            // attack/hurt/death rieng (duyet 2026-10-01) — gan ca khi asset da co (truoc do moi clip = idle).
            wispAnim.attack.frames = Frames(wispDir + "attack", "attack");
            wispAnim.attack.hitFrame = 4;   // o sap ham (can)
            wispAnim.hurt.frames = Frames(wispDir + "hurt", "hurt");
            wispAnim.death.frames = Frames(wispDir + "death", "death");
            EditorUtility.SetDirty(wispAnim);
            var wisp = LoadOrCreate<EnemyDefinition>(DataDir + "/Enemy_SoulWisp.asset", out bool newWisp);
            if (newWisp)
            {
                wisp.displayName = "Soul Wisp";
                wisp.kind = EnemyKind.Nhanh;
                wisp.baseHealth = 60f;
                wisp.baseDamage = 12f;
                wisp.moveSpeed = 2.4f;
                wisp.attackRange = 0.5f;
                wisp.attackRate = 1f;
                wisp.heroAggroRange = 4f;   // lao vao danh linh/hero
                wisp.animations = wispAnim;
                EditorUtility.SetDirty(wisp);
            }

            var kit = LoadOrCreate<DreadwyrmDefinition>(DataDir + "/Dreadwyrm_Dreadwyrm.asset", out bool newKit);
            if (newKit)
            {
                kit.clawFrames = Frames(dir + "claw-slash", "claw-slash");
                kit.breathFrames = Frames(dir + "breath-stream", "breath-stream");
                kit.burnFrames = Frames(dir + "burn-ground", "burn-ground");
                kit.galeFrames = Frames(dir + "gale-wind", "gale-wind");
                kit.soulAuraFrames = Frames(dir + "soul-aura", "soul-aura");
                kit.soulBurstFrames = Frames(dir + "soul-burst", "soul-burst");
                kit.breathBody.frames = Frames(dir + "breath", "breath");
                kit.breathBody.hitFrame = 4;   // 1-3 ngoc dau (~0,43 s o 7 fps), 4 phun
                kit.galeBody.frames = Frames(dir + "gale", "gale");
                kit.galeBody.hitFrame = 4;     // 2-3 dang canh, 4 dap xuong
                kit.roarBody.frames = Frames(dir + "roar", "roar");
                kit.roarBody.hitFrame = 3;
                kit.wisp = wisp;
                EditorUtility.SetDirty(kit);
            }

            var anim = LoadOrCreate<SpriteAnimationSet>(DataDir + "/Anim_Dreadwyrm.asset", out bool newAnim);
            {
                anim.framesPerSecond = 1f / 0.18f;
                anim.idle.frameSeconds = anim.attack.frameSeconds = anim.hurt.frameSeconds = 0.18f;
                anim.idle.frames = Frames(dir + "idle", "idle");
                anim.walk.frames = Frames(dir + "walk", "walk");
                anim.walk.frameSeconds = 0.15f;
                anim.attack.frames = Frames(dir + "attack", "attack");
                anim.attack.hitFrame = 3;   // can sat dat, mom vuon xa nhat
                anim.hurt.frames = Frames(dir + "hurt", "hurt");
                anim.death.frames = Frames(dir + "death", "death");
                anim.death.frameSeconds = 0.22f;
                EditorUtility.SetDirty(anim);
            }
            // Refresh approved body art on existing kits as well as newly created assets.
            kit.breathBody.frames = Frames(dir + "breath", "breath");
            kit.galeBody.frames = Frames(dir + "gale", "gale");
            kit.roarBody.frames = Frames(dir + "roar", "roar");
            kit.breathBody.frameSeconds = kit.galeBody.frameSeconds = kit.roarBody.frameSeconds = 0.18f;
            kit.mouth = new Vector2(-2.68f, 2.22f);
            kit.bite = new Vector2(-2.74f, 1.98f);
            EditorUtility.SetDirty(kit);
            var dragon = LoadOrCreate<EnemyDefinition>(DataDir + "/Enemy_Dreadwyrm.asset", out bool newDragon);
            if (newDragon)
            {
                dragon.displayName = "Hellfire Dragon";
                dragon.kind = EnemyKind.Boss;
                dragon.baseHealth = 5000f;
                dragon.baseDamage = 100f;
                dragon.moveSpeed = 0.55f;
                dragon.attackRange = 1.5f;   // tran quai bi giu (RangedReach): than rong dai, mom ~2,3 truoc pivot
                dragon.attackRate = 0.5f;
                dragon.heroAggroRange = 3f;
                dragon.damageReduction = 0.25f;   // Xac Khong Dau
                dragon.dragon = kit;
                dragon.animations = anim;
                EditorUtility.SetDirty(dragon);
            }

            bool dirty = false;
            foreach (var band in tuning.bands ?? Array.Empty<WaveBand>())
                if (band.hasBoss && band.Contains(30) && band.boss == null)
                {
                    band.boss = dragon;
                    dirty = true;
                }
            if (dirty) EditorUtility.SetDirty(tuning);
        }

        /// <summary>
        /// Hero SR Banneret (Ky Thu cam co, kit chot 2026-09-29; so de xuat, CHUA can). Don thuong KHONG danh quai: buff
        /// ngau nhien (+sat thuong / +toc danh / khien) cho mot dong minh dang danh — xem `Banneret`. Du bo art (walk v2
        /// duyet 2026-09-29) nen vao pool gacha.
        /// </summary>
        static HeroDefinition BuildBanneret()
        {
            string dir = ArtRoot + "Banneret/";
            var banner = LoadOrCreate<BannerDefinition>(DataDir + "/Banner_Banneret.asset", out _);
            if (banner.rallyFrames == null || banner.rallyFrames.Length == 0) banner.rallyFrames = Frames(dir + "rally-fx", "rally-fx");
            if (banner.zoneFrames == null || banner.zoneFrames.Length == 0) banner.zoneFrames = Frames(dir + "zone-fx", "zone-fx");
            if (banner.plantBody == null || !banner.plantBody.IsValid)
                banner.plantBody = new SpriteAnimationClip { name = "skill", loop = false, frames = Frames(dir + "plant", "plant") };
            // Chua co art khien rieng: muon tam vo khien cua Vespera.
            if (banner.shieldFrames == null || banner.shieldFrames.Length == 0)
                banner.shieldFrames = Frames(ArtRoot + "Vespera/shield-loop", "shield");
            if (banner.shieldBreakFrames == null || banner.shieldBreakFrames.Length == 0)
                banner.shieldBreakFrames = Frames(ArtRoot + "Vespera/shield-break", "shield-break");
            EditorUtility.SetDirty(banner);

            var anim = AnimSet("Banneret", "Banneret/", attack: ("attack", "attack"), skill: ("cast", "cast"), hit: 4, skillHit: 4);
            if (!anim.walk.IsValid)
            {
                anim.walk.frames = Frames(dir + "walk", "walk");
                EditorUtility.SetDirty(anim);
            }
            var hero = HeroDef("Banneret", null, anim,
                d =>
                {
                    // range 1,5 = can chien: di theo dong minh khap san, khong day xich.
                    d.maxHealth = 700f; d.damage = 30f; d.attackRate = 0.6f; d.range = 1.5f; d.moveSpeed = 1f;
                },
                SkillKind.Rally, d => { d.manaMax = 5f; });
            if (hero.banner == null) hero.banner = banner;
            if (hero.avatar == null || IsIdentityPlaceholder(hero.avatar))
                hero.avatar = LoadSprite(ArtRoot + "UI/avatars/avatar-banneret.png")
                    ?? LoadSprite(dir + "identity/identity-1.png");
            EditorUtility.SetDirty(hero);

            var unit = Unit("banneret", "Banneret", UnitCategory.Hero, Rarity.SR, "");
            if (hero.avatar != null && (unit.icon == null || IsIdentityPlaceholder(unit.icon)))
            {
                unit.icon = hero.avatar;
                EditorUtility.SetDirty(unit);
            }
            // Chi NOI THEM vao pool da co.
            var gacha = AssetDatabase.LoadAssetAtPath<GachaConfig>(DataDir + "/GachaConfig.asset");
            if (gacha != null && !gacha.pool.Contains(unit))
            {
                gacha.pool = gacha.pool.Append(unit).ToArray();
                EditorUtility.SetDirty(gacha);
            }
            return hero;
        }

        /// <summary>
        /// Hero SSR Ton Ngo Khong (kit chot 2026-09-30, GDD muc 3; so Claude de xuat, CHUA can). Chieu xin Nhu Y Thien Tru =
        /// clip `skill` (smash-cast, gay cham dat o 5); phan con lai o `WukongDefinition` + `Wukong`. Phan than la hero rieng
        /// o Summons/ (40% cong/mau, khong ky nang, cung bo animation) de khong vao bang chon. Du bo art (duyet 2026-09-30)
        /// nen vao pool gacha.
        /// </summary>
        static HeroDefinition BuildWukong()
        {
            string dir = ArtRoot + "Wukong/";
            var anim = AnimSet("Wukong", "Wukong/", attack: ("attack", "attack"), skill: ("skill-smash-cast", "smash-cast"),
                hit: 4, skillHit: 5);
            var clone = HeroDef("WukongClone", null, anim,
                d =>
                {
                    d.displayName = "Clone";
                    d.maxHealth = 520f; d.damage = 16f; d.attackRate = 1f; d.range = 0.6f; d.moveSpeed = 1.3f;
                    d.projectileSpeed = 20f; d.splashRadius = 0.3f;
                },
                SkillKind.None, null, DataDir + "/Summons");

            var kit = LoadOrCreate<WukongDefinition>(DataDir + "/Wukong_Wukong.asset", out bool newKit);
            if (newKit)
            {
                kit.cloneBody.frames = Frames(dir + "skill-clone-cast", "clone-cast");
                kit.cloneBody.hitFrame = 4;
                kit.spinBody.frames = Frames(dir + "skill-spin", "spin");
                kit.poofFrames = Frames(dir + "skill-hair-poof-fx", "hair-poof");
                kit.spinRingFrames = Frames(dir + "skill-spin-ring-fx", "spin-ring");
                kit.deflectFrames = Frames(dir + "skill-deflect-spark-fx", "deflect-spark");
                kit.giantStaffFrames = Frames(dir + "skill-giant-staff-fx", "giant-staff");
                kit.healFrames = Frames(ArtRoot + "Vespera/heal-fx", "impact");
            }
            kit.cloneDefinition = clone;
            if (kit.unitPrefab == null) kit.unitPrefab = AssetDatabase.LoadAssetAtPath<Hero>(PrefabDir + "/Hero.prefab");
            EditorUtility.SetDirty(kit);

            var hero = HeroDef("Wukong", null, anim,
                d =>
                {
                    d.displayName = "Ton Ngo Khong";
                    d.maxHealth = 1300f; d.damage = 40f; d.attackRate = 1f; d.range = 0.6f; d.moveSpeed = 1.3f;
                    d.projectileSpeed = 20f; d.splashRadius = 0.3f;
                    d.stunFrames = Frames(ArtRoot + "Enemy/stunned-status", "stun");
                },
                SkillKind.GiantStaff, d => { d.manaMax = 5f; d.skillRadius = 5f; d.skillPower = 4f; d.skillDuration = 1.5f; });
            if (hero.wukong == null) hero.wukong = kit;
            if (hero.avatar == null || IsIdentityPlaceholder(hero.avatar))
                hero.avatar = LoadSprite(ArtRoot + "UI/avatars/avatar-wukong.png") ?? LoadSprite(dir + "identity/identity-1.png");
            EditorUtility.SetDirty(hero);

            var unit = Unit("wukong", "Ton Ngo Khong", UnitCategory.Hero, Rarity.SSR, "");
            if (hero.avatar != null && (unit.icon == null || IsIdentityPlaceholder(unit.icon)))
            {
                unit.icon = hero.avatar;
                EditorUtility.SetDirty(unit);
            }
            // Chi NOI THEM vao pool da co.
            var gacha = AssetDatabase.LoadAssetAtPath<GachaConfig>(DataDir + "/GachaConfig.asset");
            if (gacha != null && !gacha.pool.Contains(unit))
            {
                gacha.pool = gacha.pool.Append(unit).ToArray();
                EditorUtility.SetDirty(gacha);
            }
            return hero;
        }

        /// <summary>
        /// Hero SSR Phap su sam set (kit chot 2026-10-01, GDD muc 3; so Claude de xuat, CHUA can). Don thuong Xich Loi = clip
        /// `attack` (phong set o 5); chieu xin Loi Bao = clip `skill` (skill-cast, dam truong cham dat o 5); phan con lai o
        /// `StormcallerDefinition` + `Stormcaller`. Du bo art (duyet + giao 2026-10-01) nen vao pool gacha.
        /// </summary>
        static HeroDefinition BuildStormcaller()
        {
            string dir = ArtRoot + "Stormcaller/";
            var anim = AnimSet("Stormcaller", "Stormcaller/", attack: ("attack", "attack"), skill: ("skill-cast", "cast"),
                hit: 5, skillHit: 5);

            var kit = LoadOrCreate<StormcallerDefinition>(DataDir + "/Stormcaller_Stormcaller.asset", out bool newKit);
            if (newKit)
            {
                kit.boltFrames = Frames(dir + "skill-chain-bolt-fx", "chain-bolt-fx");
                kit.hitFrames = Frames(dir + "skill-bolt-hit-fx", "bolt-hit-fx");
                kit.chargePipFrames = Frames(dir + "skill-charge-pips", "charge-pips");
                kit.burstFrames = Frames(dir + "skill-static-burst-fx", "static-burst-fx");
                kit.rodFrames = Frames(dir + "skill-rod", "rod");
                kit.blinkFrames = Frames(dir + "skill-blink-fx", "blink-fx");
                kit.strikeFrames = Frames(dir + "skill-strike-fx", "strike-fx");
            }
            EditorUtility.SetDirty(kit);

            var hero = HeroDef("Stormcaller", null, anim,
                d =>
                {
                    d.displayName = "Stormcaller";
                    d.maxHealth = 520f; d.damage = 40f; d.attackRate = 0.8f; d.range = 5.5f; d.moveSpeed = 1.1f;
                    d.stunFrames = Frames(ArtRoot + "Enemy/stunned-status", "stun");
                },
                SkillKind.ThunderStorm, d => { d.manaMax = 6f; d.skillPower = 3f; d.skillDuration = 0.5f; });
            if (hero.stormcaller == null) hero.stormcaller = kit;
            if (hero.avatar == null || IsIdentityPlaceholder(hero.avatar))
                hero.avatar = LoadSprite(ArtRoot + "UI/avatars/avatar-stormcaller.png") ?? LoadSprite(dir + "identity/identity-1.png");
            EditorUtility.SetDirty(hero);

            var unit = Unit("stormcaller", "Stormcaller", UnitCategory.Hero, Rarity.SSR, "");
            if (hero.avatar != null && (unit.icon == null || IsIdentityPlaceholder(unit.icon)))
            {
                unit.icon = hero.avatar;
                EditorUtility.SetDirty(unit);
            }
            // Chi NOI THEM vao pool da co.
            var gacha = AssetDatabase.LoadAssetAtPath<GachaConfig>(DataDir + "/GachaConfig.asset");
            if (gacha != null && !gacha.pool.Contains(unit))
            {
                gacha.pool = gacha.pool.Append(unit).ToArray();
                EditorUtility.SetDirty(gacha);
            }
            return hero;
        }

        /// <summary>
        /// Hero SR Vo Tang Gio (GDD muc 3, nguoi dung duyet kit + du bo art 2026-10-01; so Claude de xuat, CHUA can). Don thuong
        /// Chuong Phong = clip `attack` (phong gio o 5); chieu xin Long Quyen = clip `skill` (skill-cast, ha chuong o 5); phan
        /// con lai o `GalemonkDefinition` + `Galemonk`. Chua co avatar / icon ky nang rieng: avatar tam = identity.
        /// </summary>
        static HeroDefinition BuildGalemonk()
        {
            string dir = ArtRoot + "Galemonk/";
            var anim = AnimSet("Galemonk", "Galemonk/", attack: ("attack", "attack"), skill: ("skill-cast", "cast"),
                hit: 5, skillHit: 5);

            var kit = LoadOrCreate<GalemonkDefinition>(DataDir + "/Galemonk_Galemonk.asset", out bool newKit);
            if (newKit)
            {
                kit.boltFrames = Frames(dir + "skill-wind-bolt-fx", "wind-bolt-fx");
                kit.hitFrames = Frames(dir + "skill-wind-hit-fx", "wind-hit-fx");
                kit.waveFrames = Frames(dir + "skill-gale-wave-fx", "gale-wave-fx");
                kit.tornadoFrames = Frames(dir + "skill-tornado", "tornado");
            }
            EditorUtility.SetDirty(kit);

            var hero = HeroDef("Galemonk", null, anim,
                d =>
                {
                    d.displayName = "Wind Monk";
                    d.maxHealth = 700f; d.damage = 22f; d.attackRate = 0.9f; d.range = 2.5f; d.moveSpeed = 1.1f;
                },
                SkillKind.Whirlwind, d => { d.manaMax = 6f; d.skillPower = 30f / 22f; d.skillRadius = 2.5f; d.skillDuration = 3f; });
            if (hero.galemonk == null) hero.galemonk = kit;
            if (hero.avatar == null || IsIdentityPlaceholder(hero.avatar))
                hero.avatar = LoadSprite(ArtRoot + "UI/avatars/avatar-galemonk.png") ?? LoadSprite(dir + "identity/identity-1.png");
            EditorUtility.SetDirty(hero);

            var unit = Unit("galemonk", "Wind Monk", UnitCategory.Hero, Rarity.SR, "");
            if (hero.avatar != null && (unit.icon == null || IsIdentityPlaceholder(unit.icon)))
            {
                unit.icon = hero.avatar;
                EditorUtility.SetDirty(unit);
            }
            // Chi NOI THEM vao pool da co.
            var gacha = AssetDatabase.LoadAssetAtPath<GachaConfig>(DataDir + "/GachaConfig.asset");
            if (gacha != null && !gacha.pool.Contains(unit))
            {
                gacha.pool = gacha.pool.Append(unit).ToArray();
                EditorUtility.SetDirty(gacha);
            }
            return hero;
        }

        /// <summary>
        /// Bo Dao Vien SSR — Luu Bi / Quan Vu / Truong Phi (GDD muc 3, nguoi dung duyet kit + du bo art 2026-10-02; so Claude de
        /// xuat, CHUA can). Mot `DaoVienDefinition` dung chung (Ket Nghia). Don thuong = clip `attack` (o 4 cham); chieu xin =
        /// clip `skill` (o 4): Luu Bi gio kiem ban phuc, Quan Vu vung dao phong song, Truong Phi dam chan gam. Clip than chieu
        /// phu nam trong kit. Avatar + icon ky nang: UI dot 15 (`UI/avatars/avatar-<unitId>`), thieu thi tam = identity.
        /// </summary>
        /// <summary>
        /// Hero N fantasy — Phu Thuy Dam Lay / Hiep Si Nam / Ngu Nhan Dinh Ba + Phap Su Lua / Nguoi Lun Khien Rune / Tien Det
        /// To (GDD muc 3, nguoi dung duyet kit + du bo art 2026-10-03; so lay tu bang GDD, phan GDD khong ghi Claude de xuat,
        /// CHUA can). Khung N: noi tai (`passiveKind`) + chieu xin (`skillKind`), logic o
        /// Hero.Passive.cs + Hero.HeroN.cs. Don thuong + cast deu cham o o 4. Chua co avatar / icon: avatar tam = identity.
        /// </summary>
        static void BuildHeroN()
        {
            string bog = ArtRoot + "BogWitch/", shroom = ArtRoot + "ShroomKnight/", triton = ArtRoot + "TritonGuard/";
            HeroN("BogWitch", "bogwitch", "Bog Witch",
                d =>
                {
                    d.maxHealth = 250f; d.damage = 14f; d.attackRate = 0.9f; d.range = 4.5f; d.moveSpeed = 1.1f;
                    d.projectileSpeed = 7f; d.splashRadius = 0.3f;
                    d.projectileFrames = Frames(bog + "skill-poison-drop-fx", "poison-drop-fx");
                    d.passiveKind = PassiveKind.Venom;
                    d.passiveEvery = 5; d.passivePower = 2f; d.passiveDuration = 4f;
                    d.passiveFrames = Frames(bog + "skill-poison-bubble-fx", "poison-bubble-fx");
                },
                SkillKind.ToxicMist, d =>
                {
                    d.manaMax = 5f; d.skillRadius = 1.3f; d.skillDuration = 5f; d.skillPower = 0.2f;
                    d.skillFrames = Frames(bog + "skill-toxic-mist-fx", "toxic-mist-fx");
                });
            HeroN("ShroomKnight", "shroomknight", "Shroom Knight",
                d =>
                {
                    d.maxHealth = 1000f; d.damage = 12f; d.attackRate = 0.7f; d.range = 0.55f; d.moveSpeed = 1f;
                    d.projectileSpeed = 20f; d.splashRadius = 0.3f;
                    d.passiveKind = PassiveKind.Spores;
                    d.passiveRadius = 0.8f; d.passivePower = 4f;
                    d.passiveFrames = Frames(shroom + "skill-spore-puff-fx", "spore-puff-fx");
                },
                SkillKind.MushroomBloom, d =>
                {
                    d.manaMax = 6f; d.skillRadius = 1f; d.skillDuration = 2f; d.skillPower = 3f;
                    d.skillIntroFrames = Frames(shroom + "skill-mushroom-grow-fx", "mushroom-grow-fx");
                    d.skillFrames = Frames(shroom + "skill-spore-burst-fx", "spore-burst-fx");
                });
            HeroN("TritonGuard", "tritonguard", "Triton Guard",
                d =>
                {
                    d.maxHealth = 800f; d.damage = 22f; d.attackRate = 0.8f; d.range = 0.9f; d.moveSpeed = 1.1f;
                    // Don don muc tieu: con dung sau chi an qua noi tai Thoc Hang.
                    d.projectileSpeed = 20f; d.splashRadius = 0.1f;
                    d.passiveKind = PassiveKind.Pierce;
                    d.passiveRadius = 0.6f; d.passivePower = 0.5f;
                },
                SkillKind.Whirlpool, d =>
                {
                    d.manaMax = 5f; d.skillRadius = 1f; d.skillDuration = 4f; d.skillPower = 0.5f;
                    d.skillFrames = Frames(triton + "skill-whirlpool-fx", "whirlpool-fx");
                    d.skillOutroFrames = Frames(triton + "skill-whirlpool-fade-fx", "whirlpool-fade-fx");
                });

            string ember = ArtRoot + "EmberApprentice/", rune = ArtRoot + "RuneShield/", silk = ArtRoot + "SilkFae/";
            HeroN("EmberApprentice", "emberapprentice", "Ember Apprentice",
                d =>
                {
                    d.maxHealth = 240f; d.damage = 22f; d.attackRate = 0.8f; d.range = 5f; d.moveSpeed = 1.1f;
                    d.projectileSpeed = 8f; d.splashRadius = 0.3f;
                    d.projectileFrames = Frames(ember + "skill-fire-bolt-fx", "fire-bolt-fx");
                    d.passiveKind = PassiveKind.Ignite;
                    d.passivePower = 5f; d.passiveDuration = 3f; d.passiveRadius = 1.2f;
                    d.passiveFrames = Frames(ember + "skill-burn-fx", "burn-fx");
                },
                SkillKind.EmberRain, d =>
                {
                    d.manaMax = 5f; d.skillRadius = 0.7f; d.skillDuration = 4f; d.skillPower = 2f;
                    d.skillIntroFrames = Frames(ember + "skill-fireball-fall-fx", "fireball-fall-fx");
                    d.skillOutroFrames = Frames(ember + "skill-fireball-blast-fx", "fireball-blast-fx");
                    d.skillFrames = Frames(ember + "skill-fire-pool-fx", "fire-pool-fx");
                });
            HeroN("RuneShield", "runeshield", "Rune Shield",
                d =>
                {
                    d.maxHealth = 1100f; d.damage = 14f; d.attackRate = 0.7f; d.range = 0.55f; d.moveSpeed = 1f;
                    d.projectileSpeed = 20f; d.splashRadius = 0.3f;
                    d.passiveKind = PassiveKind.ShotBreak;
                    d.passiveFrames = Frames(rune + "skill-shot-break-fx", "shot-break-fx");
                },
                SkillKind.RuneWall, d =>
                {
                    // Phu hang cua ong + hai hang ke: hang o cach nhau 1,65 -> nua tuong 2,5.
                    d.manaMax = 6f; d.skillRadius = 2.5f; d.skillDuration = 5f;
                    var wall = Frames(rune + "skill-rune-wall-fx", "rune-wall-fx");     // 9 frame: moc 3 / lap 3 / tan 3
                    d.skillIntroFrames = wall.Take(3).ToArray();
                    d.skillFrames = wall.Skip(3).Take(3).ToArray();
                    d.skillOutroFrames = wall.Skip(6).ToArray();
                });
            HeroN("SilkFae", "silkfae", "Tien Det To",
                d =>
                {
                    d.maxHealth = 230f; d.damage = 16f; d.attackRate = 0.8f; d.range = 4.5f; d.moveSpeed = 1.1f;
                    d.projectileSpeed = 9f; d.splashRadius = 0.1f;
                    d.projectileFrames = Frames(silk + "skill-silk-bolt-fx", "silk-bolt-fx");
                    d.passiveKind = PassiveKind.Tangle;
                    d.passivePower = 0.25f; d.passiveDuration = 2f;
                },
                SkillKind.Puppet, d =>
                {
                    d.manaMax = 6f; d.skillDuration = 4f;
                    d.skillFrames = Frames(silk + "skill-puppet-strings-fx", "puppet-strings-fx");
                    d.skillOutroFrames = Frames(silk + "skill-string-snap-fx", "string-snap-fx");
                });
        }

        static void HeroN(string id, string unitId, string name, Action<HeroDefinition> stats, SkillKind kind,
            Action<HeroDefinition> skill, Rarity rarity = Rarity.N)
        {
            string dir = ArtRoot + id + "/";
            var anim = AnimSet(id, id + "/", attack: ("attack", "attack"), skill: ("skill-cast", "cast"), hit: 4, skillHit: 4);
            var hero = NamedHero(HeroDef(id, null, anim, stats, kind, skill), name);
            if (hero.avatar == null || IsIdentityPlaceholder(hero.avatar))
                hero.avatar = LoadSprite($"{UiDir}avatars/avatar-{unitId}.png") ?? LoadSprite(dir + "identity/identity-1.png");
            EditorUtility.SetDirty(hero);

            var unit = Unit(unitId, name, UnitCategory.Hero, rarity, "");
            if (hero.avatar != null && (unit.icon == null || IsIdentityPlaceholder(unit.icon)))
            {
                unit.icon = hero.avatar;
                EditorUtility.SetDirty(unit);
            }
            // Chi NOI THEM vao pool da co.
            var gacha = AssetDatabase.LoadAssetAtPath<GachaConfig>(DataDir + "/GachaConfig.asset");
            if (gacha != null && !gacha.pool.Contains(unit))
            {
                gacha.pool = gacha.pool.Append(unit).ToArray();
                EditorUtility.SetDirty(gacha);
            }
        }

        static void BuildDaoVien()
        {
            string lb = ArtRoot + "LuuBi/", qv = ArtRoot + "QuanVu/", tp = ArtRoot + "TruongPhi/";
            var kit = LoadOrCreate<DaoVienDefinition>(DataDir + "/DaoVien_DaoVien.asset", out bool newKit);
            if (newKit)
            {
                kit.blessBody.frames = Frames(lb + "skill-cast", "cast");
                kit.blessBody.hitFrame = 4;
                kit.healRingFrames = Frames(lb + "skill-heal-ring-fx", "heal-ring-fx");
                kit.silkFrames = Frames(lb + "skill-silk-revive-fx", "silk-revive-fx");
                kit.petalFrames = Frames(lb + "skill-peach-petals-fx", "peach-petals-fx");
                kit.slashBody.frames = Frames(qv + "skill-slash-cast", "slash-cast");
                kit.slashBody.hitFrame = 4;
                kit.crescentFrames = Frames(qv + "skill-crescent-wave-fx", "crescent-wave-fx");
                kit.slashTrailFrames = Frames(qv + "skill-slash-trail-fx", "slash-trail-fx");
                kit.dragonFrames = Frames(qv + "skill-spirit-dragon-fx", "spirit-dragon-fx");
                kit.pierceBody.frames = Frames(tp + "skill-pierce-cast", "pierce-cast");
                kit.pierceBody.hitFrame = 4;
                kit.pierceFrames = Frames(tp + "skill-pierce-fx", "pierce-fx");
                kit.drinkBody.frames = Frames(tp + "skill-drink", "drink");
                kit.drinkBody.hitFrame = 4;
                kit.roarWaveFrames = Frames(tp + "skill-roar-wave-fx", "roar-wave-fx");
                kit.groundCrackFrames = Frames(tp + "skill-ground-crack-fx", "ground-crack-fx");
            }
            EditorUtility.SetDirty(kit);

            BuildBrother("LuuBi", "luubi", "Luu Bi", SwornBrother.LuuBi, kit, ("skill-cast", "cast"),
                d =>
                {
                    // Danh xa (nguoi dung 2026-10-03: ho tro khong dung tuyen dau): don thuong ban kiem khi don muc tieu.
                    d.maxHealth = 900f; d.damage = 26f; d.attackRate = 0.8f; d.range = 4f; d.moveSpeed = 1.1f;
                    d.projectileSpeed = 9f; d.splashRadius = 0.1f; d.projectileScale = 1.5f;
                    d.projectileFrames = Frames(lb + "skill-sword-qi-fx", "sword-qi-fx");
                    d.impactFrames = Frames(lb + "skill-sword-qi-hit-fx", "sword-qi-hit-fx");
                },
                d => { d.manaMax = 6f; d.skillPower = 0.25f; d.skillDuration = 6f; });
            BuildBrother("QuanVu", "quanvu", "Quan Vu", SwornBrother.QuanVu, kit, ("skill-slash-cast", "slash-cast"),
                d => { d.maxHealth = 1100f; d.damage = 42f; d.attackRate = 0.75f; d.range = 0.9f; d.moveSpeed = 1.1f; },
                d => { d.manaMax = 6f; d.skillPower = 5f; });
            BuildBrother("TruongPhi", "truongphi", "Truong Phi", SwornBrother.TruongPhi, kit, ("skill-roar", "roar"),
                d =>
                {
                    d.maxHealth = 1700f; d.damage = 28f; d.attackRate = 0.7f; d.range = 1f; d.moveSpeed = 1f;
                    d.stunFrames = Frames(ArtRoot + "Enemy/stunned-status", "stun");
                },
                d => { d.manaMax = 7f; d.skillPower = 3f; d.skillRadius = 4f; d.skillDuration = 3f; });
        }

        static void BuildBrother(string id, string unitId, string name, SwornBrother brother, DaoVienDefinition kit,
            (string folder, string action) skill, Action<HeroDefinition> stats, Action<HeroDefinition> ultimate)
        {
            var anim = AnimSet(id, id + "/", attack: ("attack", "attack"), skill: skill, hit: 4, skillHit: 4);
            var hero = NamedHero(HeroDef(id, null, anim, stats, SkillKind.DaoVien, ultimate), name);
            if (hero.daoVien == null) hero.daoVien = kit;
            hero.swornBrother = brother;
            if (hero.avatar == null || IsIdentityPlaceholder(hero.avatar))
                hero.avatar = LoadSprite($"{ArtRoot}UI/avatars/avatar-{unitId}.png") ?? LoadSprite(ArtRoot + id + "/identity/identity-1.png");
            EditorUtility.SetDirty(hero);

            var unit = Unit(unitId, name, UnitCategory.Hero, Rarity.SSR, "");
            if (hero.avatar != null && (unit.icon == null || IsIdentityPlaceholder(unit.icon)))
            {
                unit.icon = hero.avatar;
                EditorUtility.SetDirty(unit);
            }
            // Chi NOI THEM vao pool da co.
            var gacha = AssetDatabase.LoadAssetAtPath<GachaConfig>(DataDir + "/GachaConfig.asset");
            if (gacha != null && !gacha.pool.Contains(unit))
            {
                gacha.pool = gacha.pool.Append(unit).ToArray();
                EditorUtility.SetDirty(gacha);
            }
        }

        /// <summary>Hero R Vespera (don thuong hoi mau + chieu lon Den Canh, so CHUA can) + FX buff tren dau Hawkfletch.</summary>
        static void BuildVespera()
        {
            var vespera = HeroDef("Vespera", null,
                AnimSet("Vespera", "Vespera/", attack: ("attack", "attack"), hit: 4),
                d =>
                {
                    d.maxHealth = 350f; d.damage = 20f; d.attackRate = 0.8f; d.range = 4f; d.moveSpeed = 1.1f;
                    d.healsAllies = true;
                    d.healFrames = Frames(ArtRoot + "Vespera/heal-fx", "impact");
                },
                SkillKind.Lantern, d =>
                {
                    // Den Canh (GDD muc 3, 2026-09-28): vong r 1,5 hoi 2x don thuong (40) + khien 60 trong 5s.
                    d.manaMax = 5f; d.skillRadius = 1.5f; d.skillPower = 2f; d.skillDuration = 5f; d.shieldAmount = 60f;
                    d.skillFrames = Frames(ArtRoot + "Vespera/dencanh-ring", "ring");
                    d.shieldFrames = Frames(ArtRoot + "Vespera/shield-loop", "shield");
                    d.shieldBreakFrames = Frames(ArtRoot + "Vespera/shield-break", "shield-break");
                });
            // Avatar 128x128 trong UI/avatars; chua co thi tam dung frame identity.
            if (vespera.avatar == null || IsIdentityPlaceholder(vespera.avatar))
            {
                vespera.avatar = LoadSprite(ArtRoot + "UI/avatars/avatar-vespera.png")
                    ?? LoadSprite(ArtRoot + "Vespera/identity/identity-1.png");
                EditorUtility.SetDirty(vespera);
            }

            var hawkfletch = AssetDatabase.LoadAssetAtPath<HeroDefinition>(DataDir + "/Hero_Hawkfletch.asset");
            if (hawkfletch != null && (hawkfletch.buffFrames == null || hawkfletch.buffFrames.Length == 0))
            {
                hawkfletch.buffFrames = Frames(ArtRoot + "Hawkfletch/rapid-fire-buff", "impact");
                EditorUtility.SetDirty(hawkfletch);
            }
        }

        /// <summary>
        /// 3 phao GDD muc 4. Lien Thanh (`CannonRapid/`) va No (`CannonBlast/`) co art rieng; Bang tam dung chung bo sprite
        /// Lien Thanh, to xanh qua `tint` (plan muc B1). Co art rieng thi doi tham so `art` cua `CannonDef`.
        /// </summary>
        static CannonDefinition[] BuildCannons()
        {
            EnsureFolder(CannonDir);
            return new[]
            {
                CannonDef("Rapid", "cannon_rapid", "Rapid Fire Cannon", "icon-cannon-rapid", "CannonRapid", c =>
                {
                    c.damage = 12f; c.fireRate = 2f; c.range = 8f;
                    c.projectileScale = 0.5f; c.impactScale = 0.6f;
                }),
                CannonDef("Blast", "cannon_blast", "Explosive Cannon", "icon-cannon-blast", "CannonBlast", c =>
                {
                    c.damage = 30f; c.fireRate = 0.5f; c.range = 7f; c.aoeRadius = 2f;
                    c.projectileSpeed = 10f; c.projectileScale = 0.8f; c.impactScale = 2.5f;
                }),
                CannonDef("Frost", "cannon_frost", "Ice Cannon", "icon-cannon-frost", "CannonFrost", c =>
                {
                    c.damage = 8f; c.fireRate = 1f; c.range = 7f; c.slowPercent = 0.4f; c.slowDuration = 3f;
                    // Tinh the 19x9 ~ dan Lien Thanh 18x5 -> cung scale; vu no bang 42x40 to hon no Lien Thanh -> 0,6.
                    c.projectileScale = 0.5f; c.impactScale = 0.6f;
                }),
                // So Claude de xuat 2026-10-01, CHUA can. Lao 105 px ve cung co nong no tren o 144 -> projectileScale =
                // displayScale / UnitScale de lao bay dung co lao tren no; muzzle = tam lao tren no o attack-1
                // (mui x10, duoi ~x115, y47; pivot x72 / dat 96).
                CannonDef("Ballista", "cannon_ballista", "Piercing Crossbow", "icon-cannon-ballista", "CannonBallista", c =>
                {
                    c.damage = 22f; c.fireRate = 0.6f; c.range = 9f; c.pierceCount = 3;
                    c.projectileSpeed = 16f; c.projectileScale = 0.7f; c.impactScale = 0.6f;
                    c.muzzle = new Vector2(-0.1f, 0.49f);
                }, bullet: "bolt", impact: "bolt-impact"),

                // ---- Bo phao 2026-10-05: so TAM Claude de xuat theo the giao viec (docs/giao-viec/asset-phao.md muc 4)
                // va mechanics.md cua ban art, CHUA can. muzzle do tu idle-1 (mep nong trai / dinh), art-px / 100.
                CannonDef("Net", "cannon_net", "Net Cannon", "icon-cannon-net", "CannonNet", c =>
                {
                    c.damage = 6f; c.fireRate = 0.5f; c.range = 7f; c.aoeRadius = 0.8f;
                    c.rootDuration = 1.5f; c.rootFrames = Frames(ArtRoot + "CannonNet/net-status", "status");
                    c.projectileSpeed = 10f; c.projectileScale = 0.8f; c.impactScale = 1f;
                    c.muzzle = new Vector2(-0.33f, 0.46f);
                }),
                CannonDef("Mortar", "cannon_mortar", "Mortar Cannon", "icon-cannon-mortar", "CannonMortar", c =>
                {
                    c.damage = 26f; c.fireRate = 0.4f; c.range = 11f; c.aoeRadius = 1.6f;
                    c.arcHeight = 2.5f; c.targetMarkFrames = Frames(ArtRoot + "CannonMortar/target-mark", "target-mark");
                    c.projectileSpeed = 8f; c.projectileScale = 0.6f; c.impactScale = 1.4f;
                    c.muzzle = new Vector2(-0.08f, 0.7f);
                }),
                CannonDef("Tesla", "cannon_tesla", "Lightning Tower", "icon-cannon-tesla", "CannonTesla", c =>
                {
                    c.damage = 14f; c.fireRate = 0.8f; c.range = 6.5f;
                    c.fire = CannonFire.Chain; c.chainCount = 3; c.chainRange = 2.5f;
                    c.beamFrames = Frames(ArtRoot + "CannonTesla/beam", "beam");
                    c.impactScale = 0.8f;
                    c.muzzle = new Vector2(0f, 0.62f);
                }),
                CannonDef("Siege", "cannon_siege", "Siege Cannon", "icon-cannon-siege", "CannonSiege", c =>
                {
                    c.damage = 90f; c.fireRate = 0.2f; c.range = 9f; c.aoeRadius = 0.6f; c.bigTargetMultiplier = 2f;
                    c.projectileSpeed = 11f; c.projectileScale = 0.8f; c.impactScale = 1.4f;
                    c.muzzle = new Vector2(-0.54f, 0.6f);
                }),
                CannonDef("Gatling", "cannon_gatling", "Automatic Gatling", "icon-cannon-gatling", "CannonGatling", c =>
                {
                    c.damage = 5f; c.fireRate = 2f; c.range = 7f; c.spinUpSeconds = 3f; c.spinUpMultiplier = 3f;
                    c.projectileSpeed = 18f; c.projectileScale = 0.5f; c.impactScale = 0.6f;
                    c.muzzle = new Vector2(-0.41f, 0.37f);
                }),
                CannonDef("Longgun", "cannon_longgun", "Castle Musket", "icon-cannon-longgun", "CannonLonggun", c =>
                {
                    c.damage = 100f; c.fireRate = 0.25f; c.range = 10f; c.targetHighestHealth = true;
                    c.aimMarkFrames = Frames(ArtRoot + "CannonLonggun/target-mark", "target-mark");
                    c.muzzleFrames = Frames(ArtRoot + "CannonLonggun/muzzle-smoke", "muzzle-smoke");
                    c.projectileSpeed = 30f; c.projectileScale = 0.6f; c.impactScale = 0.8f;
                    c.muzzle = new Vector2(-0.61f, 0.42f);
                }),
                CannonDef("Sonic", "cannon_sonic", "Sonic Cannon", "icon-cannon-sonic", "CannonSonic", c =>
                {
                    c.damage = 20f; c.fireRate = 1f; c.range = 4.5f;
                    c.fire = CannonFire.Cone; c.coneAngle = 60f; c.burstEvery = 4; c.burstDamageMultiplier = 1.75f;
                    c.waveFrames = Frames(ArtRoot + "CannonSonic/wave-normal", "wave");
                    c.burstWaveFrames = Frames(ArtRoot + "CannonSonic/wave-shock", "wave");
                    c.burstImpactFrames = Frames(ArtRoot + "CannonSonic/hit-shock", "hit");
                    c.muzzle = new Vector2(-0.34f, 0.58f);
                }, impact: "hit-normal", impactPrefix: "hit", skill: "attack-shock"),
                CannonDef("Void", "cannon_void", "Dark Star Cannon", "icon-cannon-void", "CannonVoid", c =>
                {
                    c.damage = 100f; c.fireRate = 0.1f; c.range = 8f; c.aoeRadius = 1.6f;
                    c.fire = CannonFire.Vortex; c.vortexSeconds = 1.5f; c.vortexStackBonus = 0.15f; c.vortexMaxStacks = 5;
                    string v = ArtRoot + "CannonVoid/";
                    c.vortexOpenFrames = Frames(v + "vortex-open", "vortex-open");
                    c.vortexLoop0 = Frames(v + "vortex-loop-0", "vortex");
                    c.vortexLoop1 = Frames(v + "vortex-loop-1", "vortex");
                    c.vortexLoop2 = Frames(v + "vortex-loop-2", "vortex");
                    c.absorbFrames = Frames(v + "absorb", "absorb");
                    c.impactFrames1 = Frames(v + "bullet-impact-1", "impact");
                    c.impactFrames2 = Frames(v + "bullet-impact-2", "impact");
                    c.projectileSpeed = 9f; c.projectileScale = 0.7f; c.impactScale = 1f;
                    c.muzzle = new Vector2(-0.44f, 0.44f);
                }),

                // ---- Dau Rong Da (muc 4.16): phao KHONG ban tung phat — `dragonKit` khac null thi `Cannon` giao han viec
                // ra don cho `DragonBreath` (non lua lien tuc theo DPS + Long Nộ + Long Tức + Thiêu Đốt + nổ lan).
                // `damage` o day la DPS goc, `range` la DAI non; `fireRate`/`projectile*` khong dung. So TAM theo
                // `tempart/CannonDragonHead/brief.md` (D = 30), CHUA can.
                CannonDef("DragonHead", "cannon_dragonhead", "Dragon Head Cannon", "icon-cannon-dragonhead", "CannonDragonHead", c =>
                {
                    c.damage = 30f; c.fireRate = 1f; c.range = 5.5f;
                    // Mep mom do tu attack-1 (art-px tu pivot: mom x 24 / be da chan y 97 cua o 144x128).
                    c.muzzle = new Vector2(-0.48f, 0.42f);
                    c.displayScale = 1.35f;
                    c.dragonKit = BuildDragonKit();
                }, skill: "skill"),
            }.Select(WithSlowIcon).ToArray();
        }

        /// <summary>
        /// Bo so + hinh cua Long Viêm (`DragonCannonKit`). So lay tu `tempart/CannonDragonHead/brief.md` — ban thiet ke cua
        /// ben art, ghi ro la "ban thu, chua can"; sua o day chu khong sua tung cho trong asset.
        /// </summary>
        static DragonCannonKit BuildDragonKit()
        {
            var kit = LoadOrCreate<DragonCannonKit>(DataDir + "/DragonCannonKit.asset", out _);
            string art = ArtRoot + "CannonDragonHead/";
            // Tien to file = ten thu muc (bo art giao theo kieu <action>/<action>-N.png).
            kit.breathLow = Frames(art + "breath-low", "breath-low");
            kit.breathMid = Frames(art + "breath-mid", "breath-mid");
            kit.breathHigh = Frames(art + "breath-high", "breath-high");
            kit.breathUlt = Frames(art + "ult-breath", "ult-breath");
            kit.burnFrames = Frames(art + "burn-status", "burn-status");
            kit.groundFrames = Frames(art + "scorched-ground", "scorched-ground");
            kit.burstFrames = Frames(art + "fire-burst", "fire-burst");
            EditorUtility.SetDirty(kit);
            return kit;
        }

        /// <summary>Bong tuyet tren dau quai bi lam cham (art giao sau asset phao) — bu mot lan cho phao co slow.</summary>
        static CannonDefinition WithSlowIcon(CannonDefinition def)
        {
            if (def.slowPercent <= 0f || (def.slowIconFrames != null && def.slowIconFrames.Length > 0)) return def;
            def.slowIconFrames = Frames(ArtRoot + "CannonFrost/slow-status", "impact");
            EditorUtility.SetDirty(def);
            return def;
        }

        /// <summary>Bo idle + attack cua mot thu muc art phao — khong goi AnimSet (se tim walk/hurt/death khong ton tai).</summary>
        static SpriteAnimationSet CannonAnim(string art, string skill = null)
        {
            string dir = ArtRoot + art + "/";
            var anim = LoadOrCreate<SpriteAnimationSet>($"{DataDir}/Anim_{art}.asset", out bool created);
            if (created)
            {
                anim.idle.frames = Frames(dir + "idle", "idle");
                anim.attack.frames = Frames(dir + "attack", "attack");
                anim.attack.hitFrame = 2;   // frame loe nong
                if (skill != null)
                {
                    // Phat dac biet (xung kich Phao Am Thanh) dung clip skill.
                    anim.skill.frames = Frames(dir + skill, skill);
                    anim.skill.hitFrame = 2;
                }
                EditorUtility.SetDirty(anim);
            }
            return anim;
        }

        static CannonDefinition CannonDef(string id, string unitId, string displayName, string icon, string art,
            Action<CannonDefinition> stats, string bullet = "bullet", string impact = "bullet-impact",
            string impactPrefix = "impact", string skill = null)
        {
            var def = LoadOrCreate<CannonDefinition>($"{CannonDir}/Cannon_{id}.asset", out bool created);
            if (created)
            {
                def.unitId = unitId;
                def.displayName = displayName;
                stats(def);
            }
            // Asset cu con tro bo art muon (vd. No dung tam Lien Thanh to cam) -> doi sang art rieng, bo tint tam.
            var anim = CannonAnim(art, skill);
            if (def.animations != anim)
            {
                if (!created) def.tint = Color.white;
                def.animations = anim;
                def.projectileFrames = Frames(ArtRoot + art + "/" + bullet, "projectile");
                def.impactFrames = Frames(ArtRoot + art + "/" + impact, impactPrefix);
            }            if (def.icon == null) def.icon = LoadSprite($"{UiDir}icons/{icon}.png");
            EditorUtility.SetDirty(def);
            return def;
        }

        // ---------------------------------------------------------------- scene Battle

        /// <summary>
        /// Dung lai RIENG scene Battle — khi vua sua HUD/panel ma khong muon chay lai ca bo scene (Gacha,
        /// Units, Shop) cung du lieu. Cung mot ham dung voi `BuildAll`.
        /// </summary>
        [MenuItem("BuildBuild/Scenes/Rebuild Battle Only")]
        public static void RebuildBattleSceneOnly()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            _font = PixelFont.Asset;   // ly do: xem BuildGachaScene
            BuildBattleScene(null, null);   // tham so khong dung: scene tu nap lai tu Data/Prefabs sau khi mo scene moi
            EditorSceneManager.OpenScene(ScenePath(SceneNames.Battle));
            Debug.Log("[SceneBuilder] Da dung lai scene Battle.");
        }

        static void BuildBattleScene(WaveTuning tuning, Enemy enemyPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // NewScene(Single) don asset khong dung khoi RAM: object C# vua tao thanh "fake null" va
            // gan vao field se luu {fileID: 0}. Nap lai tu dia SAU khi mo scene moi.
            tuning = AssetDatabase.LoadAssetAtPath<WaveTuning>(DataDir + "/WaveTuning.asset");
            enemyPrefab = AssetDatabase.LoadAssetAtPath<Enemy>(PrefabDir + "/Enemy.prefab");
            var heroPrefab = AssetDatabase.LoadAssetAtPath<Hero>(PrefabDir + "/Hero.prefab");
            var roster = AssetDatabase.FindAssets("t:HeroDefinition", new[] { DataDir })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !path.StartsWith(DataDir + "/Summons/") && !path.StartsWith(SoldierDir + "/"))
                .OrderBy(path => path)
                .Select(AssetDatabase.LoadAssetAtPath<HeroDefinition>)
                .ToArray();

            CreateCamera(new Color(0.08f, 0.07f, 0.12f));
            // O dat hero la object 2D trong the gioi (khong phai UI): can raycaster nay de nhan cham.
            Object.FindObjectOfType<Camera>().gameObject.AddComponent<Physics2DRaycaster>();

            // Nen san: mot tam 1920x1080 phu kin camera, pivot giua.
            var waveBackground = NewSprite("Background", null, LoadSprite(ArtRoot + "Background/bg-field.png"), SortOrder.Background);
            waveBackground.transform.position = PxToWorld(RefWidth / 2f, RefHeight / 2f);
            // Hang cay duoi (cat tu bg-field, y 801..1079) ve DE len chan phao dai va unit.
            var waveTrees = NewSprite("ForegroundTrees", null, LoadSprite(ArtRoot + "Background/fg-trees.png"), SortOrder.Foreground);
            waveTrees.transform.position = PxToWorld(RefWidth / 2f, RefHeight / 2f);

            // Duong chan: goc Castle o CHAN HANG RAO (giua lan theo y — Enemy chi so x), Body pivot tam sprite.
            var castleGo = new GameObject("Castle");
            castleGo.transform.position = PxToWorld(FenceWallPx, (LaneTopPx + LaneBottomPx) / 2f);
            SetValue(castleGo.AddComponent<Health>(), "maxHealth", CastleMaxHealth);
            var castle = castleGo.AddComponent<Castle>();
            var body = NewSprite("Body", castleGo.transform, LoadSprite(ArtRoot + "Fence/fence-1.png"), SortOrder.Castle);
            SetRef(castle, "body", body);
            SetRef(castle, "intact", body.sprite);
            // Hang rao CHUA co sprite hung hai (hoan hau ki) -> de trong; `Castle.RefreshSprite` giu ban lanh.
            var castleTiers = BuildCastleTiers(castleGo.transform);
            var initialFence = LoadSprite(ArtRoot + "Fence/" + FenceTierStats[0].sprite);
            body.transform.position = PxToWorld(FenceWallPx, FenceBasePx - FenceHeightPx / 2f);
            body.transform.localScale = Vector3.one * (FenceHeightPx / initialFence.rect.height * initialFence.pixelsPerUnit / Ppu);
            BuildFenceGround(castleGo.transform);

            var field = new GameObject("Battlefield").AddComponent<Battlefield>();
            SetValue(field, "laneTop", PxToWorld(0f, LaneTopPx).y);
            SetValue(field, "laneBottom", PxToWorld(0f, LaneBottomPx).y);
            SetValue(field, "leftBound", castleGo.transform.position.x);
            SetValue(field, "rightBound", PxToWorld(RefWidth, 0f).x);
            // Lop dong tung biome nam chung mot container: ConquestArena bat/tat ca container, WaveBiome bat dung con.
            var waveAmbience = new GameObject("WaveAmbience");
            var fieldAmbience = BuildFieldAmbience();
            fieldAmbience.transform.SetParent(waveAmbience.transform, false);
            // Ba nen con lai (dam lay / dat chay / tuyet) truoc day KHONG co lop dong nao: ca man chi co nen tinh.
            var swampAmbience = BuildBiomeDrift("SwampAmbience",
                MoteDef("Firefly", "swamp/firefly", "firefly", 6f, 12, Box(-4.6f, -1.4f, 8.6f, 3.2f),
                    DriftAmbience.Motion.Wander, 0.5f, 0.7f),
                MoteDef("Bubble", "swamp/bubble", "bubble", 5f, 5, Box(-4.6f, -2.9f, 6.4f, 1.1f),
                    DriftAmbience.Motion.Rise, 0.28f, 0.5f));
            swampAmbience.transform.SetParent(waveAmbience.transform, false);
            var scorchedAmbience = BuildBiomeDrift("ScorchedAmbience",
                MoteDef("Ember", "scorched/ember", "ember", 7f, 14, Box(-2.4f, -2.4f, 9.4f, 5.4f),
                    DriftAmbience.Motion.Rise, 0.45f, 0.6f),
                MoteDef("Ash", "scorched/ash", "ash", 5f, 10, Box(-6.4f, -2.8f, 13f, 5.6f),
                    DriftAmbience.Motion.Fall, 0.22f, 0.9f));
            scorchedAmbience.transform.SetParent(waveAmbience.transform, false);
            var snowAmbience = BuildBiomeDrift("SnowAmbience",
                MoteDef("Snowflake", "snow/snowflake", "snowflake", 6f, 26, Box(-9.4f, -5.2f, 18.8f, 10.4f),
                    DriftAmbience.Motion.Fall, 0.55f, 0.35f),
                MoteDef("SnowDust", "snow/snow-dust", "snow-dust", 9f, 8, Box(-7.4f, -4.2f, 15f, 1.8f),
                    DriftAmbience.Motion.Wander, 0.35f, 1.1f));
            snowAmbience.transform.SetParent(waveAmbience.transform, false);
            BuildConquestArena(new[] { waveBackground.gameObject, waveTrees.gameObject, waveAmbience });

            var spawn = new GameObject("EnemySpawn").transform;
            spawn.position = PxToWorld(SpawnPx, LaneTopPx);

            var game = new GameObject("Game");
            // Bam Play thang vao Battle khi chua dang nhap (bo qua scene Login) -> day ve man dang nhap.
            game.AddComponent<LoginGate>();
            var economy = game.AddComponent<Economy>();
            var waves = game.AddComponent<WaveManager>();
            SetRef(waves, "tuning", tuning);
            SetRef(waves, "enemyPrefab", enemyPrefab);
            SetRef(waves, "castle", castle);
            SetRef(waves, "economy", economy);
            SetRef(waves, "spawnPoint", spawn);
            SetRef(waves, "battlefield", field);
            var waveStats = game.AddComponent<WaveStats>();
            SetRef(waveStats, "waves", waves);

            // Nen doi ngau nhien sau moi wave thang, qua man den chuyen canh (nguoi dung chot 2026-10-03). Thu tu biome: co
            // (dang dung), dam lay, dat chay, tuyet — giao bang tempart/biomes/deliver_biomes.py; 3 biome sau chua co lop dong.
            var biome = game.AddComponent<WaveBiome>();
            SetRef(biome, "field", waveBackground);
            SetRef(biome, "trees", waveTrees);
            string[] biomeDirs = { "Background/", "Background/swamp/", "Background/scorched/", "Background/snow/" };
            SetRefs(biome, "fields", biomeDirs.Select(d => LoadSprite(ArtRoot + d + "bg-field.png")).ToArray());
            SetRefs(biome, "treeLayers", biomeDirs.Select(d => LoadSprite(ArtRoot + d + "fg-trees.png")).ToArray());
            // Them da + dai dat day hang rao doi mau theo biome (tools/art/make_fence_biomes.py); biome 0 = art goc.
            var fenceTerrace = castleGo.transform.Find("Terrace");
            var fenceSkirt = castleGo.transform.Find("GroundSkirt");
            if (fenceTerrace != null && fenceSkirt != null)
            {
                string[] fenceDirs = { "", "swamp/", "scorched/", "snow/" };
                SetRef(biome, "terrace", fenceTerrace.GetComponent<SpriteRenderer>());
                SetRef(biome, "skirt", fenceSkirt.GetComponent<SpriteRenderer>());
                SetRefs(biome, "terraces", fenceDirs.Select(d => LoadSprite(ArtRoot + "Fence/" + d + "terrace-aligned.png")).ToArray());
                SetRefs(biome, "skirts", fenceDirs.Select(d => LoadSprite(ArtRoot + "Fence/" + d + "ground-skirt.png")).ToArray());
            }
            SetRefs(biome, "ambience", new Object[]
            {
                fieldAmbience, swampAmbience, scorchedAmbience, snowAmbience,
            });

            var board = game.AddComponent<HeroBoard>();
            SetRef(board, "waves", waves);
            SetRef(board, "heroPrefab", heroPrefab);
            SetRefs(board, "slots", BuildHeroSlots(board));
            SetRef(board, "equipment", AssetDatabase.LoadAssetAtPath<EquipmentConfig>(EquipmentConfigPath));

            CreateEventSystem();
            var hudCanvas = CreateCanvas("HUD");
            BuildBattleHud(hudCanvas, waves, castle, economy, board, roster, waveStats);
            SetRef(hudCanvas.GetComponent<BattleHud>(), "biome", biome);
            // Man den chuyen canh: canvas rieng ve tren moi UI, tam den phu kin, tat san (WaveBiome bat khi chay).
            var wipeCanvas = CreateCanvas("ScreenWipe");
            wipeCanvas.GetComponent<Canvas>().sortingOrder = 30000;
            var wipe = new GameObject("Wipe", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            wipe.SetParent(wipeCanvas, false);
            wipe.anchorMin = Vector2.zero;
            wipe.anchorMax = Vector2.one;
            wipe.sizeDelta = Vector2.zero;
            wipe.GetComponent<Image>().color = Color.black;
            wipe.gameObject.SetActive(false);
            SetRef(biome, "wipe", wipe);
            BuildSoldierCamp(game, waves, heroPrefab);

            var admin = game.AddComponent<AdminPanel>();
            SetRef(admin, "waves", waves);
            SetRef(admin, "castle", castle);
            SetRef(admin, "economy", economy);
            SetRef(admin, "hud", Object.FindObjectOfType<BattleHud>());

            BuildCannonBoard(game, castle, castleTiers);
            SetRef(Object.FindObjectOfType<BattleHud>(), "castleUpgradeButton", BuildCastleUpgradeButton(hudCanvas, out var upgradeLabel));
            SetRef(Object.FindObjectOfType<BattleHud>(), "castleUpgradeLabel", upgradeLabel);
            SetRef(Object.FindObjectOfType<BattleHud>(), "castleUpgradePanel", BuildCastleUpgradePanel(hudCanvas));
            BuildConquestMap(hudCanvas, waves, economy, Object.FindObjectOfType<BattleHud>(), roster);

            EditorSceneManager.SaveScene(scene, ScenePath(SceneNames.Battle));
        }

        static void BuildBattleHud(RectTransform canvas, WaveManager waves, Castle castle, Economy economy,
            HeroBoard board, HeroDefinition[] roster, WaveStats waveStats)
        {
            var hud = canvas.gameObject.AddComponent<BattleHud>();
            SetRef(hud, "equipment", AssetDatabase.LoadAssetAtPath<EquipmentConfig>(EquipmentConfigPath));
            SetRef(hud, "waves", waves);
            SetRef(hud, "castle", castle);
            SetRef(hud, "economy", economy);

            // Mau lau dai — khung chrome/bar-frame (3-slice, long cach tren/duoi 4 px) + icon tim ben trai.
            var hpFrame = Img("CastleHp", canvas, UiSprite("chrome/bar-frame"), TopLeft, new Vector2(80, -36), Native(200, 23), sliced: true);
            var hpFill = Img("Fill", hpFrame.transform, UiSprite("chrome/bar-fill-hp"), Center, Vector2.zero, Vector2.zero);
            Stretch(hpFill.rectTransform, 6 * UiScale, 5 * UiScale, 4 * UiScale, 4 * UiScale);
            hpFill.type = Image.Type.Filled;
            hpFill.fillMethod = Image.FillMethod.Horizontal;
            Img("Icon", hpFrame.transform, UiSprite("icons/icon-hp"), MidLeft, new Vector2(-56, 0), new Vector2(48, 48));
            var hpText = Label("Value", hpFrame.transform, "", 26, Center, Vector2.zero, Native(200, 23));
            SetRef(hud, "castleHpFill", hpFill);
            SetRef(hud, "castleHpText", hpText);
            // Thanh mau thanh chi co nghia TRONG TRAN (ngoai tran thanh luon day mau va da duoc sua sau moi wave) —
            // BattleHud bat/tat ca khung nay theo `waves.IsFighting`, o sanh chi con avatar + Lv/XP.
            SetRef(hud, "castleHpPanel", hpFrame.gameObject);

            // Cap nguoi choi (GDD muc 10.0): chan dung + `Lv` + thanh kinh nghiem CO SO, nam duoi thanh mau thanh.
            // Chan dung dang lay tam avatar Frostweaver vi chua co man chon avatar (IDEAS #3) — doi 1 dong la du.
            // XP la nguon duy nhat mo khoa `icon-xp`/`bar-fill-xp` (truoc day `bar-fill-xp` bi muon lam thanh chi so).
            // Gom 3 phan vao mot `PlayerBadge`: la nut bam (mo `PlayerProfileScreen`) va dich len cho thanh mau thanh khi thanh mau an.
            // Toa do con = toa do cu tru (60, -92) (goc tren-trai cua badge).
            // Mot khoi nen toi phang (khong vien: hai khung trang tri long nhau nhin roi) + khung avatar de len dau trai nhu icon cua `GoldPill`.
            // Khong dung panel-stone: vien dinh tan cua no an het chieu cao 88 px, long hai khung vao nhau nhin roi.
            var playerBadge = Img("PlayerBadge", canvas, null, TopLeft, new Vector2(60, -92), new Vector2(380, 92));
            playerBadge.color = Color.clear;
            // Plate dung stat-strip (cung ho khung go voi avatar-frame) thay dim-tile phang, va mot o nen toi sau chan dung de anh
            // nhan vat nen trong suot khong lo canh nen game xuyen qua khung rong.
            Img("Plate", playerBadge.transform, UiSprite("panels/stat-strip"), MidLeft, new Vector2(50, 0), new Vector2(330, 72), sliced: true);
            Img("AvatarBacking", playerBadge.transform, UiSprite("chrome/dim-tile"), MidLeft, new Vector2(14, 0), new Vector2(64, 64)).color = new Color(0.12f, 0.09f, 0.16f, 1f);
            playerBadge.raycastTarget = true;   // `Img` mac dinh tat raycast: khong bat thi nut khong nhan duoc cu bam nao
            var badgeButton = playerBadge.gameObject.AddComponent<Button>();
            badgeButton.targetGraphic = playerBadge;
            SetRef(hud, "playerBadge", playerBadge.rectTransform);
            SetRef(hud, "playerBadgeButton", badgeButton);
            var avatarFrame = Img("PlayerAvatarFrame", playerBadge.transform, UiSprite("chrome/avatar-frame"), MidLeft, Vector2.zero, new Vector2(92, 92), sliced: true);
            var avatar = Img("PlayerAvatar", avatarFrame.transform, UiSprite("avatars/avatar-frostweaver"), Center, Vector2.zero, new Vector2(62, 62));
            avatar.preserveAspect = true;
            // Lv va thanh XP cung le trai x=112 (ngay sau khung avatar), thanh keo het be ngang plate; bo icon-xp ngoai thanh (no tran ra de len khung).
            var levelText = Label("PlayerLevel", playerBadge.transform, "Lv 1", 28, TopLeft, new Vector2(112, -10), new Vector2(150, 34));
            levelText.alignment = TextAnchor.MiddleLeft;
            SetRef(hud, "levelText", levelText);
            var xpFrame = Img("PlayerXp", playerBadge.transform, UiSprite("chrome/bar-frame-thin"), TopLeft, new Vector2(112, -50), new Vector2(256, 30), sliced: true);
            xpFrame.raycastTarget = false;
            var xpFill = Img("Fill", xpFrame.transform, UiSprite("chrome/bar-fill-xp"), Center, Vector2.zero, Vector2.zero);
            Stretch(xpFill.rectTransform, 6 * UiScale, 5 * UiScale, 4 * UiScale, 4 * UiScale);
            xpFill.type = Image.Type.Filled;
            xpFill.fillMethod = Image.FillMethod.Horizontal;
            SetRef(hud, "xpFill", xpFill);
            SetRef(hud, "xpText", Label("Value", xpFrame.transform, "", 20, Center, Vector2.zero, new Vector2(256, 30)));

            var badge = Img("Wave", canvas, UiSprite("chrome/banner-stage"), Top, new Vector2(0, -16), Native(160, 26), sliced: true);
            SetRef(hud, "waveText", Label("Value", badge.transform, "0", 36, Center, Vector2.zero, Native(160, 26)));

            SetRef(hud, "goldText", GoldPill(canvas));
            // Cot nut dieu huong duoi o vang: Trieu hoi / Doi quan / Chinh Phat, cung co 280 (du cho "Roi Chinh Phat" canh icon), cach deu 16 px.
            // Ruong AFK la vat pham nhan thuong chu khong phai nut man hinh -> dat DUOI ca cot, khong chen giua.
            var gachaButton = MakeButton("GachaButton", canvas, "Summon", TopRight, new Vector2(-24, -92), new Vector2(280, 80), out var gachaLabel);
            DecorateButton(gachaButton, gachaLabel, "icons/icon-shard");
            SetRef(hud, "gachaButton", gachaButton);
            var unitsButton = MakeButton("UnitsButton", canvas, "Roster", TopRight, new Vector2(-24, -188), new Vector2(280, 80), out var unitsLabel);
            DecorateButton(unitsButton, unitsLabel, "icons/icon-tab-hero");
            SetRef(hud, "unitsButton", unitsButton);
            // Chinh Phat: BattleHud doi chu thanh "Roi Chinh Phat" luc dang chuan bi.
            var conquestButton = MakeButton("ConquestButton", canvas, "Conquest", TopRight, new Vector2(-24, -284), new Vector2(280, 80), out var conquestLabel);
            DecorateButton(conquestButton, conquestLabel, "icons/icon-raid");
            SetRef(hud, "conquestButton", conquestButton);
            SetRef(hud, "conquestLabel", conquestLabel);
            // Nhiem vu (GDD muc 11): nut mo man Hanh trinh + cham do bao con o bam nhan duoc. Icon lay o `lobby/menu-quest`
            // — icon nay da lam tu dot UI lobby cho hang icon GDD 10.4 nhung chua noi vao code nao.
            var questButton = MakeButton("QuestButton", canvas, "Journey", TopRight, new Vector2(-24, -380), new Vector2(280, 80), out var questLabel);
            DecorateButton(questButton, questLabel, "lobby/menu-quest");
            var questDot = Img("Dot", questButton.transform, UiSprite("chrome/notify-dot"), TopRight, new Vector2(-4, -4), new Vector2(30, 30));
            SetRef(hud, "questButton", questButton);
            SetRef(hud, "questDot", questDot.gameObject);
            // Boss Rush: 3 hiep boss lien tiep tren cung mot thanh mau (che do phu, khong doi NextWave).
            // Anh `lobby/hud-skull-boss` da ve tu dot UI truoc va chua file nao dung.
            var bossRushButton = MakeButton("BossRushButton", canvas, "Boss Rush", TopRight, new Vector2(-24, -476), new Vector2(280, 80), out var bossRushLabel);
            DecorateButton(bossRushButton, bossRushLabel, "lobby/hud-skull-boss");
            SetRef(hud, "bossRushButton", bossRushButton);

            // Ruong AFK (GDD muc 8, asset dot 10 `UI/raid/`): sprite 96x72 ve 2 file-px cho 1 art-px, nen hien 1:1
            // (Native 48x36 = 96x72 px man) moi dung luoi 2 px cua game; o x2 thi 1 art-px = 4 px, to va choi hon
            // moi icon quanh no (audit 2026-10-12, T4). Duoi cot nut; dong ho tich luy o duoi.
            // Ba hinh (dong / he sang / day tran) do BattleHud doi theo gio da tich. Nut KHONG nen: chi sprite ruong.
            // y -572: cot 5 nut cao 80, buoc 96 (nut cuoi day o -556) -> ruong cach 16 px.
            var chestClosed = UiSprite("raid/afk-chest-closed");
            var chest = Img("AfkChest", canvas, chestClosed, TopRight, new Vector2(-68, -572), Native(48, 36));
            chest.raycastTarget = true;
            var chestButton = chest.gameObject.AddComponent<Button>();
            var chestColors = ColorBlock.defaultColorBlock;
            chestColors.disabledColor = Color.white;   // chua co gi de nhan van ve ruong sang, khong xam
            chestButton.colors = chestColors;
            var timerPill = Img("Timer", chest.transform, UiSprite("chrome/currency-pill"), Bottom, new Vector2(0, -36), Native(70, 18), sliced: true);
            SetRef(hud, "chestTimer", Label("Value", timerPill.transform, "", 26, Center, Vector2.zero, Native(70, 18)));
            SetRef(hud, "chestButton", chestButton);
            SetRef(hud, "chestImage", chest);
            SetRef(hud, "chestClosed", chestClosed);
            SetRef(hud, "chestGlowing", UiSprite("raid/afk-chest-glowing"));
            SetRef(hud, "chestFull", UiSprite("raid/afk-chest-overflowing"));
            SetRef(hud, "tuning", AssetDatabase.LoadAssetAtPath<WaveTuning>(DataDir + "/WaveTuning.asset"));
            SetRef(hud, "startButton", MakeButton("StartButton", canvas, "", Bottom, new Vector2(0, 48), new Vector2(320, 96), out var startLabel));
            // Dong ho du bi Chinh Phat: cung cho nut Bat dau (nut do an khi dang danh); HeroBoard bat/tat + ghi dem nguoc.
            var reinforce = MakeButton("ReinforceButton", canvas, "", Bottom, new Vector2(0, 48), new Vector2(360, 96), out var reinforceLabel);
            reinforce.transition = Selectable.Transition.SpriteSwap;
            var reinforceState = reinforce.spriteState;   // giu sprite nhan cua MakeButton
            reinforceState.disabledSprite = UiSprite("chrome/btn-disabled");
            reinforce.spriteState = reinforceState;
            reinforceLabel.fontSize = PixelFont.Snap(26);
            SetRef(board, "reinforceButton", reinforce);
            SetRef(board, "reinforceLabel", reinforceLabel);
            SetRef(hud, "startLabel", startLabel);
            SetRef(hud, "messageText", Label("Message", canvas, "", 48, Center, new Vector2(0, 120), new Vector2(1400, 120)));

            // Goi y ngu canh phai nam DUOI moi man phu: dung SAU cung thi bang goi y de len ca Town/Shop va che
            // mat nut Close cua chung (2026-10-20). Anh em ve sau nam de tren, nen no phai dung TRUOC cac man.
            SetRef(hud, "tipPanel", BuildTipPanel(canvas));

            // Bang chon hero: mo khi cham o trong. Panel bat raycast de cham khong xuyen xuong o ben duoi.
            // Roster = moi HeroDefinition trong Assets/Data, moi hero mot dong xep doc.
            //
            // Skin dot UI thu 3 (`UI/panels/`): nen `panel-hero`, tieu de tren `title-ribbon`, moi dong co
            // khung chan dung + mot dai chi so + mot o skill. Chi so DOC TU HeroDefinition chu khong go tay --
            // sua asset la bang doi theo, khong co cho cho so cu nam lai.
            // Hien toi da `VisibleRows` dong, con lai CUON DOC (keo / lan chuot): 9 hero xep het ra thi bang cao 1406 >
            // man 1080 va dong dau (Banneret) tran khoi man. Roster con dai them — cuon khong vo.
            // Hang loc vai duoi tieu de: nut "Tat ca" + mot nut icon moi vai; HeroBoard an dong khong khop, Content
            // xep doc bang VerticalLayoutGroup nen cac dong con lai don len, khong de lo.
            const float rowStep = 132f;
            const int VisibleRows = 5;
            const float filterTop = 100f, filterSize = 72f, filterStep = 84f;
            const float listTop = filterTop + filterSize + 12f;
            float pickerHeight = listTop + Mathf.Min(roster.Length, VisibleRows) * rowStep + 110f;
            var picker = Img("HeroPicker", canvas, UiSprite("panels/panel-hero"), Center, new Vector2(0, 40),
                new Vector2(680, pickerHeight), sliced: true);
            picker.raycastTarget = true;    // keo cuon tren khoang trong cua bang
            var viewport = Rect("Viewport", picker.transform, Center, Vector2.zero, Vector2.zero);
            Stretch(viewport, 0, 0, listTop, 110);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Content", viewport, Top, Vector2.zero, new Vector2(680, roster.Length * rowStep));
            var rows = content.gameObject.AddComponent<VerticalLayoutGroup>();
            rows.childAlignment = TextAnchor.UpperCenter;
            rows.spacing = rowStep - 118f;
            rows.padding = new RectOffset(0, 0, 7, 7);
            rows.childControlWidth = rows.childControlHeight = false;
            rows.childForceExpandWidth = rows.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = picker.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            var titleBar = Img("Title", picker.transform, UiSprite("panels/title-ribbon"), Top, new Vector2(0, 26), new Vector2(360, 64), sliced: true);
            Label("Value", titleBar.transform, "Select Hero", 34, Center, Vector2.zero, new Vector2(340, 64));

            var roles = AssetDatabase.LoadAssetAtPath<HeroRoleCatalog>(HeroRoleCatalogPath);
            var roleButtons = new Object[roles.entries.Length + 1];
            float filterLeft = -(roleButtons.Length - 1) * filterStep / 2f;
            for (int i = 0; i < roleButtons.Length; i++)
            {
                var roleButton = MakeButton(i == 0 ? "RoleAll" : $"Role{roles.entries[i - 1].role}", picker.transform, i == 0 ? "All" : "",
                    Top, new Vector2(filterLeft + i * filterStep, -filterTop), new Vector2(filterSize, filterSize), out var roleLabel, UiSprite("chrome/btn-round"));
                roleLabel.fontSize = PixelFont.Snap(16);
                if (i > 0) Img("Icon", roleButton.transform, roles.entries[i - 1].icon, Center, Vector2.zero, new Vector2(46, 46));
                roleButtons[i] = roleButton;
            }
            SetRef(board, "roles", roles);
            SetRefs(board, "roleButtons", roleButtons);
            SetRef(board, "roundNormal", UiSprite("chrome/btn-round"));
            SetRef(board, "roundActive", UiSprite("chrome/btn-round-active"));

            var heroButtons = new Object[roster.Length];
            var skillSlots = new Object[roster.Length];
            var levels = new Object[roster.Length];
            var stars = new Object[roster.Length * 5];
            for (int i = 0; i < roster.Length; i++)
            {
                var heroDef = roster[i];
                float y = roster.Length * rowStep / 2f - rowStep / 2f - i * rowStep;
                var row = MakeButton("Hero_" + heroDef.name, content, "", Center, new Vector2(0, y),
                    new Vector2(608, 118), out _, UiSprite("panels/stat-strip"));

                // Avatar lay TU heroDef, khong ghim cung ten file: them hero thu hai vao roster ma icon
                // van la cua hero thu nhat thi khong co loi nao bao, chi co nguoi choi bam nham.
                var portrait = Img("Portrait", row.transform, UiSprite("panels/portrait-frame-big"), MidLeft, new Vector2(10, 0), new Vector2(98, 98), sliced: true);
                if (heroDef.avatar != null)
                    Img("Avatar", portrait.transform, heroDef.avatar, Center, Vector2.zero, new Vector2(72, 72));
                var roleIcon = roles.IconOf(heroDef.role);
                if (roleIcon != null)
                    Img("Role", portrait.transform, roleIcon, TopLeft, new Vector2(-6, 6), new Vector2(40, 40)).raycastTarget = false;

                var nameText = Label("Name", row.transform, heroDef.displayName, 32, MidLeft, new Vector2(122, 26), new Vector2(300, 40), MidLeft);
                nameText.alignment = TextAnchor.MiddleLeft;
                levels[i] = PickerProgress(row.transform, stars, i);

                // O skill: `slot-skill` = co ky nang, `slot-skill-locked` = HeroDefinition de skillKind None.
                // Sprite "dang dung" (`slot-skill-selected`) do HeroBoard doi khi hero da dung tren san.
                bool hasSkill = heroDef.skillKind != SkillKind.None;
                var slot = Img("SkillSlot", row.transform, UiSprite(hasSkill ? "panels/slot-skill" : "panels/slot-skill-locked"),
                    MidLeft, new Vector2(500, 0), new Vector2(88, 88), sliced: true);
                if (hasSkill && heroDef.avatar != null)
                    Img("Icon", slot.transform, heroDef.avatar, Center, Vector2.zero, new Vector2(52, 52));

                heroButtons[i] = row;
                skillSlots[i] = hasSkill ? slot : null;
            }
            var cancel = MakeButton("Cancel", picker.transform, "Cancel", Bottom, new Vector2(0, 24), new Vector2(200, 72), out _);
            SetRef(board, "pickerPanel", picker.gameObject);
            SetRefs(board, "roster", roster);
            SetRefs(board, "rosterButtons", heroButtons);
            SetRefs(board, "rosterSkillSlots", skillSlots);
            SetPickerProgressRefs(board, levels, stars);
            SetRef(board, "slotSkillSprite", UiSprite("panels/slot-skill"));
            SetRef(board, "slotSelectedSprite", UiSprite("panels/slot-skill-selected"));
            SetRef(board, "cancelButton", cancel);

            // Man Nhiem vu phai dung TRUOC `BuildResultPopup`: thu tu trong hierarchy la thu tu ve, popup nhan thuong
            // phai nam TREN bang nhiem vu (khong thi o qua tang bi bang che mat).
            var questScreen = BuildQuestScreen(canvas, waves, economy, hud, roster);
            // Town phai la ANH EM DUNG TRUOC man con (Skill Board): trong hierarchy, anh em dung sau nam DE TREN.
            // Shop khong con la man con — no la scene rieng (`SceneNames.Shop`), bam cong trinh Shop la doi scene.
            var townScreen = BuildTownScreen(canvas);
            var boardScreen = BuildSkillBoardScreen(canvas, economy);
            SetRef(townScreen, "board", boardScreen);
            SetRef(hud, "challengeScreen", BuildChallengeScreen(canvas, waves, AssetDatabase.LoadAssetAtPath<WaveTuning>(DataDir + "/WaveTuning.asset")));
            BuildPlayerProfileScreen(canvas, badgeButton);
            var resultPopup = BuildResultPopup(canvas);
            var contributionPanel = BuildContributionPanel(canvas, waveStats);
            SetRef(resultPopup, "contributionPanel", contributionPanel);
            SetRef(hud, "resultPopup", resultPopup);
            SetRef(hud, "questScreen", questScreen);
            SetRef(hud, "townScreen", townScreen);
            SetRef(questScreen, "resultPopup", resultPopup);
            BuildHeroInfoPanel(canvas);
            BuildBattleControls(canvas);
        }

        /// <summary>
        /// Nut dieu khien trong tran (GDD 10.1) + toast + hai bang phu. Toan bo anh o day la asset DA VE tu
        /// cac dot UI truoc nhung chua file nao dung: `hud/hud-pause-btn` · `lobby/hud-fast-forward` ·
        /// `lobby/hud-flag-surrender` · `chrome/btn-menu` · `chrome/toast`.
        /// </summary>
        static void BuildBattleControls(RectTransform canvas)
        {
            var hud = canvas.GetComponent<BattleHud>();

            var pause = MakeButton("PauseButton", canvas, "", Top, new Vector2(-204, -84), new Vector2(84, 84), out _, UiSprite("hud/hud-pause-btn"));
            var speed = MakeButton("SpeedButton", canvas, "", Top, new Vector2(-68, -84), new Vector2(84, 84), out _, UiSprite("lobby/hud-fast-forward"));
            var flag = MakeButton("SurrenderButton", canvas, "", Top, new Vector2(68, -84), new Vector2(84, 84), out _, UiSprite("lobby/hud-flag-surrender"));
            // Nut thu 4 = "uu tien muc tieu" (GDD 10.1: co · tua nhanh · auto/uu tien · thoat). Anh `hud/hud-auto`
            // da ve tu dot UI lobby va chua file nao dung.
            var target = MakeButton("TargetButton", canvas, "", Top, new Vector2(204, -84), new Vector2(84, 84), out _, UiSprite("lobby/hud-auto"));

            // Ten duoi MOI nut: bon icon nay khong tu noi duoc chung lam gi (co = dung tran, la co = dau hang,
            // vong tron = chon muc tieu). O toc do / muc tieu thi o day hien TRANG THAI thay cho ten nut.
            // Bon o cung rong 120 nen cum phai cach 136; truoc day cum con lech phai 52 px so voi banner Wave.
            Text Pill(Button owner, string value)
            {
                var plate = Img("Pill", owner.transform, UiSprite("chrome/currency-pill"), Bottom, new Vector2(0, -34), Native(60, 18), sliced: true);
                return Label("Value", plate.transform, value, 20, Center, Vector2.zero, Native(60, 18));
            }
            Pill(pause, "Pause");
            var speedLabel = Pill(speed, "x1");
            Pill(flag, "Leave");
            var targetLabel = Pill(target, "Auto Near");

            // Ba nut sanh (Cai dat · Thu thach ngay · Town) xep DOC sat mep trai, moi nut 84 px + o ten 84 px duoi.
            // Truoc 2026-10-20 chung nam ngang o x 24..404, de len COT O LINH (x 102..198) va be phao trai (x 295+):
            // nguoi dung bao "town, daily, menu ben trai da che mat cho dat linh". Dai x 16..100 nay nam NGOAI o
            // linh dau tien (102) nen khong che gi bam duoc tren san.
            // COT TRAI xep mot mach tu tren xuong: thanh mau rao (36..82) -> chan dung + XP (100..160) -> nut Nang rao
            // (x 24..404, 196..268) -> dai nut nay. Truoc 2026-10-22 dai bat dau o y -196, TRUNG dung dai nut Nang rao
            // (cung x, rong 380) nen nut Settings ve DE LEN no; nguoi dung bao "nut nang cap thanh bi cac nut daily
            // de". Nay railY TINH tu rect cua nut Nang rao (`CastleUpgradePos/Size`) chu khong chep so: doi nut do thi
            // dai nut tu tut theo, khong the lech lai.
            const float railX = 16f;
            const float railStep = 164f;   // 84 nut + 30 khe + 32 o ten + 18 tho
            const float railGap = 32f;     // khe giua day nut Nang rao va nut Settings
            float railY = CastleUpgradePos.y - CastleUpgradeSize.y - railGap;
            Text RailPill(Button owner, string caption)
            {
                var plate = Img("Pill", owner.transform, UiSprite("chrome/currency-pill"), Bottom, new Vector2(0, -30),
                    Native(42, 16), sliced: true);
                return Label("Value", plate.transform, caption, 20, Center, Vector2.zero, Native(42, 16));
            }

            var menu = MakeButton("MenuButton", canvas, "", TopLeft, new Vector2(railX, railY), new Vector2(84, 84), out _, UiSprite("chrome/btn-menu"));
            RailPill(menu, "Settings");

            // Thu thach ngay: khung `btn-square-stone` + icon vuong mien (ca hai deu la art da ve ma chua file nao dung).
            var daily = MakeButton("DailyButton", canvas, "", TopLeft, new Vector2(railX, railY - railStep), new Vector2(84, 84), out _,
                UiSprite("chrome/btn-square-stone"));
            Img("Icon", daily.transform, UiSprite("lobby/icon-crown"), Center, new Vector2(0, 8), new Vector2(48, 48));
            SetRef(hud, "dailyButton", daily);
            SetRef(hud, "dailyLabel", RailPill(daily, "Daily"));

            // Town: hub mo Cua hang (scene rieng) + Bang ky nang (bang phu).
            var town = MakeButton("TownButton", canvas, "", TopLeft, new Vector2(railX, railY - railStep * 2), new Vector2(84, 84), out _,
                UiSprite("chrome/btn-square-stone"));
            Img("Icon", town.transform, UiSprite("lobby/lobby-hive"), Center, new Vector2(0, 6), new Vector2(56, 56));
            RailPill(town, "Town");
            SetRef(hud, "townButton", town);

            SetRef(hud, "pauseButton", pause);
            SetRef(hud, "surrenderButton", flag);
            SetRef(hud, "speedButton", speed);
            SetRef(hud, "speedLabel", speedLabel);
            SetRef(hud, "targetButton", target);
            SetRef(hud, "targetLabel", targetLabel);
            SetRef(hud, "menuButton", menu);
            SetRef(hud, "toast", BuildToast(canvas));
            SetRef(hud, "pausePanel", BuildPausePanel(canvas));
            SetRef(hud, "settingsPanel", BuildSettingsPanel(canvas));
        }

        /// <summary>
        /// The huong dan tan thu o duoi san: "2 / 8" goc tren phai, tieu de + noi dung ben trai, hai nut ben
        /// phai ("Got it" = qua buoc nay, "Skip guide" = bo ca day). Buoc nao va noi dung gi do
        /// `Meta/Tutorial` quyet — o day chi la khung.
        ///
        /// Be ngang o noi dung 540 (truoc 600) vi cot nut ben phai bat dau o x=722: chu dai cham vao nut
        /// "Got it" thi khong co loi nao bao, chi co chu de len nut.
        /// </summary>
        static TipPanel BuildTipPanel(RectTransform canvas)
        {
            var host = Rect("TipPanel", canvas, Bottom, new Vector2(0, 210), new Vector2(900, 190));
            var panel = host.gameObject.AddComponent<TipPanel>();
            var root = Rect("Root", host, Center, Vector2.zero, new Vector2(900, 190));
            var body = Img("Bg", root, UiSprite("panels/panel-hero"), Center, Vector2.zero, new Vector2(900, 190), sliced: true);
            body.raycastTarget = true;
            Img("Portrait", body.transform, UiSprite("avatars/avatar-frostweaver"), MidLeft, new Vector2(84, 0), new Vector2(120, 120));
            var step = Label("Step", body.transform, "", 20, TopRight, new Vector2(-24, -14), new Vector2(160, 30));
            step.alignment = TextAnchor.MiddleRight;
            var title = Label("Title", body.transform, "", 30, TopLeft, new Vector2(164, -26), new Vector2(540, 40));
            title.alignment = TextAnchor.MiddleLeft;
            var text = Label("Body", body.transform, "", 24, TopLeft, new Vector2(164, -68), new Vector2(520, 104));
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;   // cau dai phai xuong dong, khong tran ra ngoai khung
            var ok = MakeButton("GotIt", body.transform, "Got it", MidRight, new Vector2(-28, 16), new Vector2(180, 66), out _);
            var skip = MakeButton("SkipGuide", body.transform, "Skip guide", MidRight, new Vector2(-28, -54), new Vector2(180, 52), out var skipLabel);
            skipLabel.fontSize = PixelFont.Snap(20);

            SetRef(panel, "root", root.gameObject);
            SetRef(panel, "stepText", step);
            SetRef(panel, "titleText", title);
            SetRef(panel, "bodyText", text);
            SetRef(panel, "dismissButton", ok);
            SetRef(panel, "skipButton", skip);
            return panel;
        }

        /// <summary>Thong bao ngan giua tren man (xep hang doi trong `Toast`), khong de len bang Stage.</summary>
        static Toast BuildToast(RectTransform canvas)
        {
            // Day man, tren nut Fight: o giua man thi toast de len hang hero (luc nao cung o day trong sanh).
            var host = Rect("Toast", canvas, Bottom, new Vector2(0, 150), new Vector2(720, 56));
            var toast = host.gameObject.AddComponent<Toast>();
            var root = Rect("Root", host, Center, Vector2.zero, new Vector2(720, 56));
            Img("Bg", root, UiSprite("chrome/toast"), Center, Vector2.zero, new Vector2(720, 56), sliced: true);
            var label = Label("Text", root, "", 28, Center, Vector2.zero, new Vector2(680, 48));
            SetRef(toast, "root", root.gameObject);
            SetRef(toast, "label", label);
            return toast;
        }

        /// <summary>
        /// Tam dung voi Resume / Leave Battle. Leave doi xac nhan ro tinh thua va giu tien trinh.
        /// </summary>
        static PausePanel BuildPausePanel(RectTransform canvas)
        {
            var host = FullScreen("PausePanel", canvas);
            var panel = host.gameObject.AddComponent<PausePanel>();
            var root = FullScreen("Root", host);
            var dim = Img("Dim", root, UiSprite("chrome/dim-tile"), Center, Vector2.zero, Vector2.zero);
            Stretch(dim.rectTransform, 0, 0, 0, 0);
            dim.color = new Color(1f, 1f, 1f, 0.72f);
            dim.raycastTarget = true;

            var body = Img("Body", root, UiSprite("panels/panel-hero"), Center, new Vector2(0, 20), new Vector2(680, 560), sliced: true);
            body.raycastTarget = true;
            var titleBar = Img("Title", body.transform, UiSprite("panels/title-ribbon"), Top, new Vector2(0, 26), new Vector2(360, 64), sliced: true);
            var titleText = Label("Value", titleBar.transform, "Paused", 32, Center, Vector2.zero, new Vector2(340, 64));
            var hintText = Label("Hint", body.transform, "", 24, Top, new Vector2(0, -92), new Vector2(580, 156));
            hintText.horizontalOverflow = HorizontalWrapMode.Wrap;

            var resume = MakeButton("Resume", body.transform, "Resume", Bottom, new Vector2(0, 156), new Vector2(440, 84), out var resumeLabel);
            var surrender = MakeButton("Surrender", body.transform, "Leave Battle", Bottom, new Vector2(0, 48), new Vector2(440, 84), out var surrenderLabel, UiSprite("chrome/btn-red"));

            SetRef(panel, "root", root.gameObject);
            SetRef(panel, "resumeButton", resume);
            SetRef(panel, "surrenderButton", surrender);
            SetRef(panel, "surrenderLabel", surrenderLabel);
            SetRef(panel, "titleText", titleText);
            SetRef(panel, "hintText", hintText);
            SetRef(panel, "resumeLabel", resumeLabel);
            return panel;
        }

        /// <summary>Man Cai dat: am luong nhac + tat SFX + xoa tien trinh (bam 2 lan) + duong dan file save.</summary>
        static SettingsPanel BuildSettingsPanel(RectTransform canvas)
        {
            var host = FullScreen("SettingsPanel", canvas);
            var panel = host.gameObject.AddComponent<SettingsPanel>();
            var root = FullScreen("Root", host);
            var dim = Img("Dim", root, UiSprite("chrome/dim-tile"), Center, Vector2.zero, Vector2.zero);
            Stretch(dim.rectTransform, 0, 0, 0, 0);
            dim.color = new Color(1f, 1f, 1f, 0.72f);
            dim.raycastTarget = true;

            var body = Img("Body", root, UiSprite("panels/panel-hero"), Center, new Vector2(0, 20), new Vector2(780, 640), sliced: true);
            body.raycastTarget = true;
            var titleBar = Img("Title", body.transform, UiSprite("panels/title-ribbon"), Top, new Vector2(0, 26), new Vector2(360, 64), sliced: true);
            Label("Value", titleBar.transform, "Settings", 34, Center, Vector2.zero, new Vector2(340, 64));
            var closeImage = Img("Close", body.transform, UiSprite("chrome/btn-close"), TopRight, new Vector2(-16, -16), new Vector2(64, 64));
            closeImage.raycastTarget = true;
            var close = closeImage.gameObject.AddComponent<Button>();

            Text RowCaption(string name, string caption, float y)
            {
                var label = Label(name, body.transform, caption, 26, TopLeft, new Vector2(48, y), new Vector2(300, 48));
                label.alignment = TextAnchor.MiddleLeft;
                return label;
            }

            RowCaption("MusicCaption", "Music Volume", -110);
            var minus = MakeButton("VolumeDown", body.transform, "-", TopLeft, new Vector2(400, -96), new Vector2(76, 76), out _);
            var volumeText = Label("VolumeValue", body.transform, "40%", 28, TopLeft, new Vector2(486, -110), new Vector2(120, 48));
            var plus = MakeButton("VolumeUp", body.transform, "+", TopLeft, new Vector2(612, -96), new Vector2(76, 76), out _);

            RowCaption("SoundCaption", "Sound Effects", -196);
            var mute = MakeButton("Mute", body.transform, "Sound: ON", TopLeft, new Vector2(400, -182), new Vector2(320, 76), out var muteLabel);

            RowCaption("ProgressCaption", "Progress", -282);
            var reset = MakeButton("Reset", body.transform, "Reset Progress", TopLeft, new Vector2(400, -268), new Vector2(320, 76), out var resetLabel);

            // Huong dan tan thu: da Skip / da xong van xem lai duoc tu day (`Meta/Tutorial.Restart`). Khong
            // phai nut xoa gi — no chi dat lai chi so buoc trong save.
            RowCaption("GuideCaption", "Guide", -366);
            var guide = MakeButton("ReplayGuide", body.transform, "Replay Guide", TopLeft, new Vector2(400, -352),
                new Vector2(320, 76), out _);

            // Doi acc: dang xuat roi ve man dang nhap (`LoginScreen`); save cua acc cu van nam trong file cua no.
            RowCaption("AccountCaption", "Account", -450);
            var logout = MakeButton("Logout", body.transform, "Log Out", TopLeft, new Vector2(400, -436), new Vector2(320, 76), out _,
                UiSprite("chrome/btn-blue"));

            var path = Label("Path", body.transform, "", 20, TopLeft, new Vector2(48, -520), new Vector2(660, 104));
            path.alignment = TextAnchor.UpperLeft;
            path.horizontalOverflow = HorizontalWrapMode.Wrap;   // duong dan dai -> xuong dong, khong tran ra ngoai khung

            SetRef(panel, "root", root.gameObject);
            SetRef(panel, "volumeText", volumeText);
            SetRef(panel, "volumeDownButton", minus);
            SetRef(panel, "volumeUpButton", plus);
            SetRef(panel, "muteButton", mute);
            SetRef(panel, "muteLabel", muteLabel);
            SetRef(panel, "resetButton", reset);
            SetRef(panel, "resetLabel", resetLabel);
            SetRef(panel, "guideButton", guide);
            SetRef(panel, "logoutButton", logout);
            SetRef(panel, "closeButton", close);
            SetRef(panel, "pathText", path);
            return panel;
        }

        /// <summary>Icon hieu ung theo thu tu `EffectKind`; null = chua co icon rieng (o chi hien chu).</summary>
        static readonly string[] EffectIconPaths =
        {
            null,                                   // Stun
            "icons/icon-stat-slow",                 // Slow
            "icons/icon-stat-attack-speed",         // Haste
            "icons/icon-passive-chain-prison",      // Root
            "icons/icon-skill-mark-circle",         // Mark
            "icons/icon-passive-weakpoint",         // ArmorBreak
            "icons/icon-skill-dragon-rider",        // Awe
            "icons/icon-skill-chain-yank",          // Chain
            "icons/icon-stat-tank",                 // Shield
            "icons/icon-skill-plant-flag",          // Guard
            null,                                   // NoHeal
            "icons/icon-stat-damage",               // DamageBuff
            "icons/icon-stat-attack-speed",         // RateBuff
            null,                                   // Skill — icon chieu cuoi cua hero
            null,                                   // Passive — icon noi tai cua hero
            null,                                   // Burn
            null,                                   // Poison
            null,                                   // Freeze
        };

        /// <summary>
        /// Bang thong tin hero (cham hero tren san khi dang danh): THANH NGANG duoi day man, trong dai trong duoi lane
        /// (lane ket thuc o y 800/1080) va tren nut Bat dau/Tiep vien (day, cao toi ~144) — khong che tran. Trai -> phai:
        /// chan dung | ten/sao + thanh mau | chi so | hang o hieu ung (icon, so tang goc duoi phai, giay con lai goc tren
        /// phai, ten duoi icon). Khong co lop Dim: tran van chay.
        /// </summary>
        /// <summary>O chi so `index` (0..3, luoi 2x2) trong thanh thong tin hero: icon trai, so phai. Tra ve Text so.</summary>
        static Text HeroInfoChip(Transform body, string name, int index, string iconPath, out Image icon)
        {
            var chip = Img(name, body, UiSprite("chrome/stat-chip"), TopLeft,
                new Vector2(406 + (index % 2) * 174, -10 - (index / 2) * 52), new Vector2(166, 48), sliced: true);
            icon = Img("Icon", chip.transform, UiSprite(iconPath), MidLeft, new Vector2(6, 0), new Vector2(36, 36));
            var value = Label("Value", chip.transform, "", 24, MidRight, new Vector2(-12, 0), new Vector2(110, 48));
            value.alignment = TextAnchor.MiddleRight;
            value.color = new Color(1f, 0.94f, 0.7f);   // cung mau so voi o chi so man Doi quan
            return value;
        }

        static void BuildHeroInfoPanel(RectTransform canvas)
        {
            // Component nam tren goc LUON bat (no do cham moi frame); `Body` la phan bat / tat.
            var host = Rect("HeroInfoPanel", canvas, Bottom, new Vector2(0, 150), new Vector2(1500, 120));
            var panel = host.gameObject.AddComponent<HeroInfoPanel>();
            var body = Img("Body", host, UiSprite("chrome/panel-stone"), Center, Vector2.zero, new Vector2(1500, 120), sliced: true);
            body.raycastTarget = true;   // cham vao bang khong roi xuong san
            var closeImage = Img("Close", body.transform, UiSprite("chrome/btn-close"), TopRight, new Vector2(-12, -12), new Vector2(56, 56));
            closeImage.raycastTarget = true;
            var close = closeImage.gameObject.AddComponent<Button>();

            var frame = Img("Avatar", body.transform, UiSprite("chrome/avatar-frame"), MidLeft, new Vector2(12, 0), new Vector2(96, 96), sliced: true);
            var avatar = Img("Icon", frame.transform, null, Center, Vector2.zero, new Vector2(80, 80));
            avatar.preserveAspect = true;
            var name = Label("Name", body.transform, "", 28, TopLeft, new Vector2(122, -14), new Vector2(270, 40));
            name.alignment = TextAnchor.MiddleLeft;
            Img("HealthIcon", body.transform, UiSprite("icons/icon-hp"), TopLeft, new Vector2(120, -64), new Vector2(34, 34));
            var hpFrame = Img("Health", body.transform, UiSprite("chrome/bar-frame"), TopLeft, new Vector2(158, -66), new Vector2(234, 30), sliced: true);
            var hpFill = Img("Fill", hpFrame.transform, UiSprite("chrome/bar-fill-hp"), Center, Vector2.zero, Vector2.zero);
            Stretch(hpFill.rectTransform, 3 * UiScale, 3 * UiScale, 3 * UiScale, 3 * UiScale);
            hpFill.type = Image.Type.Filled;
            hpFill.fillMethod = Image.FillMethod.Horizontal;
            var hpText = Label("Value", hpFrame.transform, "", 20, Center, Vector2.zero, new Vector2(234, 30));

            // 4 o stat-chip 2x2, cung icon voi man Doi quan (`BuildStatChips`): cong/hoi, toc danh, tam, giam sat thuong.
            var power = HeroInfoChip(body.transform, "Power", 0, "icons/icon-stat-damage", out var powerIcon);
            var rate = HeroInfoChip(body.transform, "Rate", 1, "icons/icon-stat-attack-speed", out _);
            var range = HeroInfoChip(body.transform, "Range", 2, "icons/icon-stat-range", out _);
            var guard = HeroInfoChip(body.transform, "Guard", 3, "icons/icon-stat-tank", out _);

            var content = Rect("Effects", body.transform, MidLeft, new Vector2(760, 0), new Vector2(680, 110));
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(92, 110);
            grid.spacing = new Vector2(4, 0);
            grid.childAlignment = TextAnchor.MiddleLeft;
            var empty = Label("Empty", body.transform, "No Effects", 22, MidLeft, new Vector2(760, 0), new Vector2(680, 40));
            empty.alignment = TextAnchor.MiddleLeft;
            empty.color = new Color(0.75f, 0.75f, 0.75f);

            var cell = Rect("EffectTemplate", content, TopLeft, Vector2.zero, new Vector2(92, 110));
            Img("Slot", cell, UiSprite("chrome/reward-slot"), Top, new Vector2(0, -2), new Vector2(64, 64), sliced: true);
            var icon = Img("Icon", cell, null, Top, new Vector2(0, -8), new Vector2(52, 52));
            icon.preserveAspect = true;
            var stacks = Label("Stacks", cell, "", 24, Top, new Vector2(22, -40), new Vector2(36, 28));
            stacks.alignment = TextAnchor.MiddleRight;
            var time = Label("Time", cell, "", 16, Top, new Vector2(26, 0), new Vector2(40, 20));
            time.alignment = TextAnchor.MiddleRight;
            time.color = new Color(0.8f, 0.9f, 1f);
            var label = Label("Label", cell, "", 14, Bottom, Vector2.zero, new Vector2(92, 40));
            label.alignment = TextAnchor.UpperCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            cell.gameObject.SetActive(false);

            var icons = new Object[EffectIconPaths.Length];
            for (int i = 0; i < icons.Length; i++) icons[i] = EffectIconPaths[i] != null ? UiSprite(EffectIconPaths[i]) : null;

            SetRef(panel, "root", body.gameObject);
            SetRef(panel, "avatar", avatar);
            SetRef(panel, "nameText", name);
            SetRef(panel, "healthFill", hpFill);
            SetRef(panel, "healthText", hpText);
            SetRef(panel, "powerIcon", powerIcon);
            SetRef(panel, "damageIcon", UiSprite("icons/icon-stat-damage"));
            SetRef(panel, "healIcon", UiSprite("icons/icon-hp"));   // man Doi quan cung dung icon-hp cho "Hoi mau"
            SetRef(panel, "powerText", power);
            SetRef(panel, "rateText", rate);
            SetRef(panel, "rangeText", range);
            SetRef(panel, "guardText", guard);
            SetRef(panel, "emptyText", empty);
            SetRef(panel, "effectContent", content);
            SetRef(panel, "effectTemplate", cell.gameObject);
            SetRef(panel, "closeButton", close);
            SetRefs(panel, "effectIcons", icons);
        }

        /// <summary>
        /// Bang ket qua sau moi wave (thang/thua + vang). Dung bo sprite `UI/panels/` dot 3.
        ///
        /// Lop `Dim` phu kin man va BAT raycast: no vua lam noi bang, vua chan cham xuyen xuong o hero
        /// ben duoi luc bang dang mo. Ruy bang thang/thua dung chung mot Image, dat o Top cua panel voi
        /// nua chieu cao tran ra ngoai -- hai file da cat trung khit bbox nen doi sprite khong xe khung.
        /// </summary>
        static ResultPopup BuildResultPopup(RectTransform canvas)
        {
            var rootRect = FullScreen("ResultPopup", canvas);
            var popup = rootRect.gameObject.AddComponent<ResultPopup>();

            var dim = Img("Dim", rootRect, UiSprite("chrome/dim-tile"), Center, Vector2.zero, Vector2.zero);
            Stretch(dim.rectTransform, 0, 0, 0, 0);
            dim.color = new Color(1f, 1f, 1f, 0.72f);
            dim.raycastTarget = true;
            dim.transform.SetAsFirstSibling();

            var body = Rect("Body", rootRect, Center, Vector2.zero, new Vector2(760, 420));
            // Tia sang quanh crest, phong so nguyen x4 (83 -> 332); ban 760 cu ra khoi pixel tho.
            var rays = Img("Rays", body, UiSprite("panels/rays-burst"), Center, new Vector2(0, 330), new Vector2(332, 332));
            var panel = Img("Panel", body, UiSprite("panels/panel-result"), Center, Vector2.zero, new Vector2(760, 420), sliced: true);
            var sparkle = Img("Sparkle", body, UiSprite("panels/sparkle"), Center, new Vector2(-268, 96), new Vector2(166, 140));

            // Crest thang/thua dung sau ruy bang, day giau 20 px duoi mep tren ruy bang.
            var crest = Img("Crest", panel.transform, UiSprite("lobby/victory-crest"), Top, new Vector2(0, 196), new Vector2(172, 176));
            crest.preserveAspect = true;
            var ribbon = Img("Ribbon", panel.transform, UiSprite("panels/ribbon-victory"), Top, new Vector2(0, 40), new Vector2(600, 80), sliced: true);
            var title = Label("Value", ribbon.transform, "", 40, Center, new Vector2(0, 2), new Vector2(560, 80));

            // O thuong reward-slot 64x72 (2x): icon o long toi phia tren, so o dai sang phia duoi.
            var rewardRow = Img("Reward", panel.transform, UiSprite("chrome/reward-slot"), Center, new Vector2(0, 40), Native(64, 72), sliced: true);
            Img("Icon", rewardRow.transform, UiSprite("icons/icon-gold"), Top, new Vector2(0, -16), new Vector2(72, 72));
            var gold = Label("Value", rewardRow.transform, "0", 28, Bottom, new Vector2(0, 2), new Vector2(128, 44));
            // O do roi (trang bi) canh o vang, khung hiem; ResultPopup dich o vang sang trai khi co do roi.
            var drop = Img("Drop", panel.transform, UiSprite("chrome/reward-slot-rare"), Center, new Vector2(80, 40), Native(64, 72), sliced: true);
            var dropIcon = Img("Icon", drop.transform, null, Top, new Vector2(0, -16), new Vector2(72, 72));
            var dropText = Label("Value", drop.transform, "", 22, Bottom, new Vector2(0, 2), new Vector2(128, 44));

            var hint = Label("Hint", panel.transform, "", 26, Center, new Vector2(0, -70), new Vector2(660, 40));
            var stats = MakeButton("Stats", panel.transform, "Stats", Bottom, new Vector2(-150, 26), new Vector2(260, 76), out var statsLabel);
            var statsIcon = Img("Icon", stats.transform, UiSprite("icons/icon-stats"), MidLeft, new Vector2(30, 0), new Vector2(30, 30));
            statsIcon.gameObject.SetActive(statsIcon.sprite != null);
            statsLabel.rectTransform.anchoredPosition += new Vector2(16, 0);
            var close = MakeButton("Close", panel.transform, "Continue", Bottom, new Vector2(150, 26), new Vector2(260, 76), out _);

            SetRef(popup, "root", body.gameObject);
            SetRef(popup, "dim", dim.gameObject);
            SetRef(popup, "rays", rays.gameObject);
            SetRef(popup, "sparkle", sparkle.gameObject);
            SetRef(popup, "ribbon", ribbon);
            SetRef(popup, "victoryRibbon", UiSprite("panels/ribbon-victory"));
            SetRef(popup, "defeatRibbon", UiSprite("panels/ribbon-defeat"));
            SetRef(popup, "titleText", title);
            SetRef(popup, "crest", crest);
            SetRef(popup, "victoryCrest", UiSprite("lobby/victory-crest"));
            SetRef(popup, "defeatCrest", UiSprite("lobby/defeat-crest"));
            SetRef(popup, "rewardRow", rewardRow.gameObject);
            SetRef(popup, "goldText", gold);
            SetRef(popup, "dropSlot", drop.gameObject);
            SetRef(popup, "dropIcon", dropIcon);
            SetRef(popup, "dropText", dropText);
            SetRef(popup, "hintText", hint);
            SetRef(popup, "closeButton", close);
            SetRef(popup, "statsButton", stats);
            return popup;
        }

        /// <summary>Bang doc dong gop cua wave vua chot. Template an, dong that tao luc runtime tu snapshot.</summary>
        static ContributionPanel BuildContributionPanel(RectTransform canvas, WaveStats waveStats)
        {
            var rootRect = FullScreen("ContributionPanel", canvas);
            var panel = rootRect.gameObject.AddComponent<ContributionPanel>();
            var dim = Img("Dim", rootRect, UiSprite("chrome/dim-tile"), Center, Vector2.zero, Vector2.zero);
            Stretch(dim.rectTransform, 0, 0, 0, 0);
            dim.color = new Color(1f, 1f, 1f, 0.72f);
            dim.raycastTarget = true;
            var body = Img("Body", rootRect, UiSprite("panels/panel-result"), Center, Vector2.zero, new Vector2(1100, 700), sliced: true);
            body.raycastTarget = true;   // ScrollRect nam tren Body: can raycast moi keo cuon duoc
            var title = Label("Title", body.transform, "Contribution", 40, Top, new Vector2(0, -46), new Vector2(800, 56));
            var closeImage = Img("Close", body.transform, UiSprite("chrome/btn-close"), TopRight, new Vector2(-24, -24), new Vector2(64, 64));
            closeImage.raycastTarget = true;
            var close = closeImage.gameObject.AddComponent<Button>();
            // Tab phe: chi hien o tran Chinh Phat (ContributionPanel bat / tat).
            var allyTab = MakeButton("AllyTab", body.transform, "Allies", TopLeft, new Vector2(24, -24), new Vector2(150, 56), out var allyLabel);
            var enemyTab = MakeButton("EnemyTab", body.transform, "Enemies", TopLeft, new Vector2(184, -24), new Vector2(150, 56), out var enemyLabel);
            allyLabel.fontSize = enemyLabel.fontSize = PixelFont.Snap(26);
            var viewport = Rect("Viewport", body.transform, Center, new Vector2(0, -42), new Vector2(1030, 540));
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Content", viewport, Top, Vector2.zero, new Vector2(1030, 76));
            var scroll = body.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 8f;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var row = Img("RowTemplate", content, UiSprite("panels/stat-strip"), Center, Vector2.zero, new Vector2(1030, 76), sliced: true);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 76f;
            var highlight = Img("Highlight", row.transform, UiSprite("chrome/bar-fill-hp"), Center, Vector2.zero, new Vector2(1020, 68));
            highlight.color = new Color(1f, 0.8f, 0.45f, 0.16f);
            var avatar = Img("Avatar", row.transform, UiSprite("chrome/avatar-frame"), MidLeft, new Vector2(14, 0), new Vector2(62, 62), sliced: true);
            var avatarIcon = Img("Icon", avatar.transform, null, Center, Vector2.zero, new Vector2(50, 50));
            var name = Label("Name", row.transform, "", 26, MidLeft, new Vector2(96, 0), new Vector2(230, 52), MidLeft);
            name.alignment = TextAnchor.MiddleLeft;
            BuildContributionColumn(row.transform, "Damage", 330f, "icons/icon-stat-damage", "chrome/bar-fill-hp");
            BuildContributionColumn(row.transform, "Tank", 565f, "icons/icon-stat-tank", "chrome/bar-fill-xp");
            BuildContributionColumn(row.transform, "Heal", 800f, "icons/icon-stat-heal", "chrome/bar-fill-shard-full");
            row.gameObject.SetActive(false);
            SetRef(panel, "root", rootRect.gameObject);
            SetRef(panel, "dim", dim.gameObject);
            SetRef(panel, "titleText", title);
            SetRef(panel, "closeButton", close);
            SetRef(panel, "allyTab", allyTab);
            SetRef(panel, "enemyTab", enemyTab);
            SetRef(panel, "content", content);
            SetRef(panel, "rowTemplate", row.gameObject);
            SetRef(panel, "stats", waveStats);
            return panel;
        }

        static void BuildContributionColumn(Transform row, string name, float x, string iconPath, string fillPath)
        {
            // Ngang 226: icon 0-32 | thanh 38-134 | so 140-200 | huy hieu MVP 200-226 — khong o nao de len nhau.
            var column = Rect(name, row, MidLeft, new Vector2(x, 0), new Vector2(226, 60));
            var icon = Img("Icon", column, UiSprite(iconPath), MidLeft, new Vector2(0, 0), new Vector2(32, 32));
            icon.gameObject.SetActive(icon.sprite != null);
            var bar = Img("Bar", column, UiSprite("chrome/bar-frame-thin"), MidLeft, new Vector2(38, 0), new Vector2(96, 22), sliced: true);
            var fill = Img("Fill", bar.transform, UiSprite(fillPath), Center, Vector2.zero, Vector2.zero);
            Stretch(fill.rectTransform, 3 * UiScale, 3 * UiScale, 3 * UiScale, 3 * UiScale);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            var value = Label("Value", column, "0", 22, MidLeft, new Vector2(140, 0), new Vector2(60, 40));
            value.alignment = TextAnchor.MiddleLeft;
            var mvp = Img("Mvp", column, UiSprite("icons/badge-mvp"), MidLeft, new Vector2(200, 0), new Vector2(26, 26));
            mvp.preserveAspect = true;
            mvp.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- bai linh (nhanh Linh, plan muc A3)

        // Bai dat SAU thanh: mep alpha trai cua phao dai o px 260 -> linh cho dung o x 150, ba hang cung hang o hero.
        static readonly string[] CampOrder =
        {
            "WarWolf", "StoneTortoise", "AcidLizard", "Warhorn",
            "MushroomSoldier", "SkeletonArcher", "IronClawCrab", "HerbalSnail",
        };
        const float CampXPx = 150f;
        static readonly float[] CampRowsPx = { 360f, 525f, 690f };

        /// <summary>
        /// `SoldierCamp` + `SoldierBoard` tren Game, 3 cho dung `SoldierPost` o bai sau thanh, bang chon linh trong HUD.
        /// Cho dung cham giong o hero: trong -> bang chon, co linh -> bo chon; doi linh luu save. Khong co thanh tai
        /// nguyen: linh chi cho vong hoi chieu. SummonConfig chi TAO khi chua co — so chinh tay khong bi ghi de.
        /// Goi SAU `BuildBattleHud` (bang chon nam trong canvas HUD).
        /// </summary>
        static void BuildSoldierCamp(GameObject game, WaveManager waves, Hero heroPrefab)
        {
            var config = LoadOrCreate<SummonConfig>(DataDir + "/SummonConfig.asset", out _);
            var roster = CampOrder
                .Select(id => AssetDatabase.LoadAssetAtPath<SoldierDefinition>($"{SoldierDir}/Soldier_{id}.asset"))
                .Where(def => def != null)
                .ToArray();

            var camp = game.AddComponent<SoldierCamp>();
            SetRef(camp, "waves", waves);
            SetRef(camp, "config", config);
            SetRef(camp, "unitPrefab", heroPrefab);
            SetRefs(camp, "roster", roster);
            SetInt(camp, "postCount", CampRowsPx.Length);

            var board = game.AddComponent<SoldierBoard>();
            SetRef(board, "camp", camp);

            // Collider chung cho moi loai linh: lay than to nhat roster.
            float radius = 0f, height = 0f;
            foreach (var soldier in roster)
            {
                Battlefield.MeasureBody(soldier.animations, out float r, out float h);
                radius = Mathf.Max(radius, r);
                height = Mathf.Max(height, h);
            }
            var root = new GameObject("SoldierPosts").transform;
            for (int i = 0; i < CampRowsPx.Length; i++)
                BuildSoldierPost(root, camp, board, i, PxToWorld(CampXPx, CampRowsPx[i]), radius, height);

            BuildSoldierPicker((RectTransform)Object.FindObjectOfType<BattleHud>().transform, board, roster);
        }

        /// <summary>
        /// Mot cho dung o bai: khung vet tren dat (cho trong), con linh idle (bo animation gan luc chay theo doi linh),
        /// collider de cham/keo, va canvas world-space tren dau: vong hoi chieu (`ring-fill` Filled Radial) tren nen
        /// `ring-bg`. Canvas 1 px = 0,01 unit nhu art.
        /// </summary>
        static void BuildSoldierPost(Transform root, SoldierCamp camp, SoldierBoard board, int index, Vector3 position,
            float radius, float height)
        {
            var go = new GameObject($"Post{index + 1}");
            go.transform.SetParent(root, false);
            go.transform.position = position;

            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(Mathf.Max(0.8f, radius * 2f + 0.2f), height + 0.5f);
            collider.offset = new Vector2(0f, (height + 0.5f) * 0.5f);

            // Vet tren dat nhu o hero: chi hien khi cho trong va dang chuan bi.
            var frameSprite = UiSprite("hud/hud-dropzone-edge");
            var frame = NewSprite("Frame", go.transform, frameSprite, SortOrder.GroundDecal);
            if (frameSprite != null) frame.transform.localScale = Vector3.one * (SlotHeightPx / Ppu / frameSprite.bounds.size.y);

            var body = NewSprite("Body", go.transform, null, 0);
            body.transform.localScale = Vector3.one * Battlefield.UnitScale;
            body.sortingOrder = SortOrder.ForUnit(body);
            body.gameObject.AddComponent<SpriteAnimator>();

            var ringCanvas = new GameObject("Cooldown", typeof(RectTransform));
            ringCanvas.transform.SetParent(go.transform, false);
            var canvas = ringCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = SortOrder.Overlay;
            var canvasRect = (RectTransform)ringCanvas.transform;
            canvasRect.sizeDelta = new Vector2(80f, 80f);
            canvasRect.localScale = Vector3.one / Ppu;
            canvasRect.localPosition = new Vector3(0f, height + 0.35f, 0f);

            Img("RingBg", canvasRect, UiSprite("style-b/chrome/ring-bg"), Center, new Vector2(0, 8), new Vector2(40, 40));
            var ring = Img("RingFill", canvasRect, UiSprite("style-b/chrome/ring-fill"), Center, new Vector2(0, 8), new Vector2(40, 40));
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillOrigin = (int)Image.Origin360.Top;
            ring.fillClockwise = true;

            var post = go.AddComponent<SoldierPost>();
            SetRef(post, "camp", camp);
            SetRef(post, "board", board);
            SetInt(post, "index", index);
            SetRef(post, "body", body);
            SetRef(post, "frame", frame);
            SetRef(post, "cooldownFill", ring);
        }

        /// <summary>
        /// Bang chon linh: cung skin + cach cham voi bang chon hero (`panels/`). Moi dong: chan dung + ten + Lv + sao
        /// (`PickerProgress`). Duoi cung: huy. Dat TRUOC ResultPopup de bang ket qua luon de len tren.
        /// </summary>
        static void BuildSoldierPicker(RectTransform canvas, SoldierBoard board, SoldierDefinition[] roster)
        {
            const float rowStep = 132f;
            float height = 108f + roster.Length * rowStep + 110f;
            var picker = Img("SoldierPicker", canvas, UiSprite("panels/panel-hero"), Center, new Vector2(0, 60), new Vector2(680, height), sliced: true);
            picker.raycastTarget = true;
            var titleBar = Img("Title", picker.transform, UiSprite("panels/title-ribbon"), Top, new Vector2(0, 26), new Vector2(360, 64), sliced: true);
            Label("Value", titleBar.transform, "Select Soldier", 34, Center, Vector2.zero, new Vector2(340, 64));

            var buttons = new Object[roster.Length];
            var levels = new Object[roster.Length];
            var stars = new Object[roster.Length * 5];
            for (int i = 0; i < roster.Length; i++)
            {
                var soldier = roster[i];
                float y = height / 2f - 108f - rowStep / 2f - i * rowStep;
                var row = MakeButton("Soldier_" + soldier.name, picker.transform, "", Center, new Vector2(0, y),
                    new Vector2(608, 118), out _, UiSprite("panels/stat-strip"));

                var portrait = Img("Portrait", row.transform, UiSprite("panels/portrait-frame-big"), MidLeft, new Vector2(10, 0), new Vector2(98, 98), sliced: true);
                if (soldier.avatar != null)
                    Img("Avatar", portrait.transform, soldier.avatar, Center, Vector2.zero, new Vector2(72, 72));

                var nameText = Label("Name", row.transform, soldier.displayName, 32, MidLeft, new Vector2(122, 26), new Vector2(300, 40), MidLeft);
                nameText.alignment = TextAnchor.MiddleLeft;
                levels[i] = PickerProgress(row.transform, stars, i);
                buttons[i] = row;
            }
            var cancel = MakeButton("Cancel", picker.transform, "Cancel", Bottom, new Vector2(0, 24), new Vector2(200, 72), out _);

            SetRef(board, "pickerPanel", picker.gameObject);
            SetRefs(board, "rosterButtons", buttons);
            SetPickerProgressRefs(board, levels, stars);
            SetRef(board, "cancelButton", cancel);

            var popup = canvas.Find("ResultPopup");
            if (popup != null) popup.SetAsLastSibling();
        }

        /// <summary>
        /// Dong chon hero / linh: "Lv N" + hang 5 sao (thay dong chi so goc cu — nguoi dung 2026-09-29). Chu va sao do
        /// `UnitCardView.ShowLevelAndStars` dien luc mo bang; sao cua dong `row` nam o `stars[row*5 .. row*5+4]`.
        /// </summary>
        static Text PickerProgress(Transform rowParent, Object[] stars, int row)
        {
            var level = Label("Level", rowParent, "", 24, MidLeft, new Vector2(122, -18), new Vector2(340, 34), MidLeft);
            level.alignment = TextAnchor.MiddleLeft;
            for (int k = 0; k < 5; k++)
                stars[row * 5 + k] = Img($"Star{k}", rowParent, UiSprite("chrome/star-off"), MidLeft, new Vector2(240 + k * 44, -18), Native(20, 20));
            return level;
        }

        static void SetPickerProgressRefs(Object board, Object[] levels, Object[] stars)
        {
            SetRefs(board, "rosterLevels", levels);
            SetRefs(board, "rosterStars", stars);
            SetRef(board, "starOn", UiSprite("chrome/star-on"));
            SetRef(board, "starOff", UiSprite("chrome/star-off"));
        }

        static void SetInt(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"[SceneBuilder] {target.GetType().Name} khong co field '{field}'"); return; }
            prop.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 9 o dat hero (3x3) tren SAN, ngay truoc tuong lau dai — GDD muc 2. O la DIEM XUAT HIEN dau
        // wave, khong phai cho dung co dinh: hero roi o de danh va an don duoc. Hero chi dat duoi san;
        // cac tang thanh danh cho phao. Toa do canvas px: 3 hang rai deu lan quai 250..800; cot dau cach vach
        // phao dai (px 604) 92 px nhu lau dai cu, vi quai dung danh tuong o x ~664 thi khong dung len o.
        // Linh trieu hoi CHO o bai dat SAU thanh (trai phao dai), khong o day.
        static readonly float[] SlotColumnsPx = { 696f, 816f, 936f };
        static readonly float[] SlotRowsPx = { 360f, 525f, 690f };
        const float SlotHeightPx = 54f;     // hang cach nhau 60 px -> khung khong chong len nhau

        static HeroSlot[] BuildHeroSlots(HeroBoard board)
        {
            // Mang dat chu khong phai khung net dut vang: o hero la CHO DAT tren san, khong phai gizmo
            // (audit 2026-10-12, T4). O SUNG (BuildSoldierPosts) va be phao van dung khung cu.
            var sprite = UiSprite("hud/hud-slot-ground");
            var root = new GameObject("HeroSlots").transform;
            var slots = new HeroSlot[SlotRowsPx.Length * SlotColumnsPx.Length];
            int i = 0;
            foreach (float y in SlotRowsPx)
                foreach (float x in SlotColumnsPx)
                {
                    // Vet tren dat: khung luon nam DUOI hero va quai.
                    var frame = NewSprite($"Slot{i + 1}", root, sprite, SortOrder.GroundDecal);
                    frame.transform.position = PxToWorld(x, y);
                    if (sprite != null) frame.transform.localScale = Vector3.one * (SlotHeightPx / Ppu / sprite.bounds.size.y);
                    frame.gameObject.AddComponent<BoxCollider2D>();     // tu khop theo sprite
                    var slot = frame.gameObject.AddComponent<HeroSlot>();
                    SetRef(slot, "board", board);
                    SetRef(slot, "frame", frame);
                    slots[i++] = slot;
                }
            return slots;
        }

        // Be phao: MOT cot x `PadXPx` tren mat them, hang y lay tu `FencePadRows` (xem `BuildCastleTiers`).
        // Vung cham phu ca dia lan than phao (world, tinh tu chan).
        static readonly Vector2 MountHitSize = new Vector2(0.9f, 1.1f);
        const float MountFrameHeightPx = 60f;

        /// <summary>
        /// Toan bo phan phao cua scene Battle: `CannonBoard` tren `Game`, be tren `Castle`, bang chon phao trong HUD,
        /// nut admin. Goi MOT dong o cuoi `BuildBattleScene`, sau khi HUD + admin da dung.
        /// </summary>
        static void BuildCannonBoard(GameObject game, Castle castle, int mountCount)
        {
            var board = game.AddComponent<CannonBoard>();
            SetRef(board, "waves", game.GetComponent<WaveManager>());
            SetRef(board, "castle", castle);
            SetRefs(board, "mounts", BuildCannonMounts(castle.transform, board, mountCount));
            BuildCannonPicker((RectTransform)Object.FindObjectOfType<BattleHud>().transform, board);
            SetRef(game.GetComponent<AdminPanel>(), "cannons", board);
        }

        /// <summary>
        /// Du be cho cap rao cao nhat (<paramref name="mountCount"/>); vi tri tung cap do `CannonBoard` dat luc chay theo
        /// `Castle.Tier.mounts`. Vi tri dung san o day cung lay tu cot be do, du phong khi rao khong co bang cap.
        /// </summary>
        static CannonMount[] BuildCannonMounts(Transform castle, CannonBoard board, int mountCount)
        {
            var sprite = UiSprite("hud/hud-dropzone-edge");
            var rows = FencePadRows(FenceTierStats[0].pads);
            var mounts = new CannonMount[Mathf.Max(mountCount, 1)];
            for (int i = 0; i < mounts.Length; i++)
            {
                var go = new GameObject($"CannonMount{i + 1}");
                go.transform.SetParent(castle, false);
                go.transform.position = PxToWorld(PadXPx, rows[Mathf.Min(i, rows.Length - 1)] + PadCannonDropPx);
                go.transform.localScale = Vector3.one * CannonMountScale;

                var hit = go.AddComponent<BoxCollider2D>();
                hit.size = MountHitSize;
                hit.offset = new Vector2(0f, MountHitSize.y * 0.35f);

                var frame = NewSprite("Frame", go.transform, sprite, SortOrder.Castle + 1);
                if (sprite != null) frame.transform.localScale = Vector3.one * (MountFrameHeightPx / Ppu / sprite.bounds.size.y);

                var mount = go.AddComponent<CannonMount>();
                SetRef(mount, "board", board);
                SetRef(mount, "frame", frame);
                mounts[i] = mount;
                go.SetActive(i < rows.Length);
            }
            return mounts;
        }

        // Hang rao 3 cap (fence-g1, nguoi dung chot 14/10): thay thanh tri bang THEM DA thap + HANG RAO + BE PHAO.
        // Linh / phao / o hero GIU NGUYEN toa do game; chi doi dat trong + thanh tri. So lieu o
        // tempart/fence-g1/refs/layout.json (`make_mock.py`), cach ghep da duyet o `compose.py`,
        // gate bo cuc `check_layout.py` PASS 4/4. Moi cap mot cot rao cao `FenceHeightPx` + 3/4/5 be phao.
        const float FenceFarPx = 250f;       // mep xa mat tren them
        const float FenceNearPx = 840f;      // mep gan mat tren them
        const float FenceLipPx = 34f;        // go nam (be day mat nam)
        const float FenceWallPx = 630f;      // goc GameObject Castle = chan rao: quai do khoang cach toi transform
        // Khung Chinh Phat RONG hon khung wave 128,5 px ve phia TAY, nen them dat ve theo khung wave bi hut mep
        // trai: vao Chinh Phat thay mot dai co truoc mat linh (nguoi dung bat 2026-10-14: "o truoc linh thi
        // them chua phu het nen co"). Mep tay = mep trai man Chinh Phat (canvas x 0 = wave -128,5), lui them
        // 3,5 px cho khoi lo khe do lam tron. `terrace.png` phai cat RONG theo so nay (`deliver_g1.py`).
        const float FenceTerraceLeftPx = -132f;
        // Cot rao phai phu DUNG chieu sau mat them (nguoi dung 14/10: "nền đất không phủ hết nền cỏ" —
        // dau xa cua rao dung tren co vi rao cu 722 px = 128..850 con them chi 250..874).
        const float FenceBasePx = 882f;      // chan cot rao = day them 874 + 8 px thua
        const float FenceHeightPx = 640f;    // = chieu sau them 624 + 8 px thua MOI dau
        const float PadXPx = 505f;           // be phao: MOT cot nhu cot go cu
        const float PadWidthPx = 124f;       // be rong mat be (px man) — `pedestal.png` scale theo so nay
        const float PadHalfHeightPx = 35f;   // `pedestal.png` hien 124x70 px man
        // Mat da di duoc cua `terrace-aligned.png`: y man 254..750; phan duoi la vach nam.
        // QC mep dong va ranh mat/vach: tempart/fortress-alignment-20261020/deliver_terrace.py.
        const float FenceSurfaceTopPx = 254f;
        const float FenceSurfaceBottomPx = 750f;
        const float PadEdgeMarginPx = 6f;    // giu be khong sat mep mat da
        // Chan phao = TAM MAT BE, do tu `pedestal.png`: hang rong nhat (tam elip mat be) o art y 163
        // = tam sprite - 8,4 px man. De +4 nhu ban dau thi phao lun xuong duoi mat be ~12 px.
        const float PadCannonDropPx = -8f;
        const int FencePadCount = 5;         // cap cao nhat dung du 5 be; cap thap rai deu trong cung doan
        // MOT scale cho MOI be (nguoi dung 2026-10-10: "phao gio scale khong deu"). Do lai art phao:
        // bbox rong 60-124 px, TRUNG VI 73 px (comment cu ghi 128-144 la be rong CA O, khong phai net ve).
        // `displayScale` 1,4 -> 0,72 cho phao trung vi ~74 px tren be 124 px. Lech giua cac khau con lai
        // la do ART rong khac nhau (Longgun 124 vs Blast 63), khong phai do hang so nay.
        const float CannonMountScale = 0.72f;
        static readonly (string name, string sprite, float hp, int cost, int unlock, int pads)[] FenceTierStats =
        {
            ("Wooden Palisade", "fence-1.png", 2000f, 0, 0, 3),
            ("Ironbound Fence", "fence-2.png", 3000f, 3000, 10, 4),
            ("Stone Rampart", "fence-3.png", 4500f, 8000, 20, FencePadCount),
        };

        /// <summary>
        /// `n` be phao rai deu tren MAT DA DI DUOC cua them — KHONG phai ca anh: ~116 px cuoi anh la go nam,
        /// dat be len do la be thoa ra ngoai mat (nguoi dung bat 14/10 voi be tren cung va be duoi cung).
        /// Chua vao trong mep `PadEdgeMarginPx` de be khong cham mep mat da. n luon >= 3 nen khong chia cho 0.
        /// </summary>
        static float[] FencePadRows(int n)
        {
            float lo = FenceSurfaceTopPx + PadHalfHeightPx + PadEdgeMarginPx;
            float hi = FenceSurfaceBottomPx - PadHalfHeightPx - PadEdgeMarginPx;
            var rows = new float[n];
            for (int i = 0; i < n; i++)
                rows[i] = Mathf.Lerp(lo, hi, n == 1 ? 0.5f : i / (float)(n - 1));
            return rows;
        }

        /// <summary>
        /// Dien `Castle.tiers` cho HANG RAO: doc sprite tung cap, dat cot rao o (FenceWallPx, chan FenceBasePx)
        /// va cac be phao theo cot `PadXPx`. Tra ve so be lon nhat (de dung du `CannonMount`).
        /// </summary>
        static int BuildCastleTiers(Transform castle)
        {
            var tiers = new Castle.Tier[FenceTierStats.Length];
            int maxMounts = 0;
            for (int t = 0; t < tiers.Length; t++)
            {
                var stats = FenceTierStats[t];
                var intact = LoadSprite(ArtRoot + "Fence/" + stats.sprite);
                float scale = FenceHeightPx / intact.rect.height;
                // Sprite cot rao khong co pipeline-meta -> pivot giua: tam o (cot x, chan - nua cao).
                var centre = PxToWorld(FenceWallPx, FenceBasePx - FenceHeightPx / 2f);
                var mounts = FencePadRows(stats.pads).Select(y => PxToWorld(PadXPx, y + PadCannonDropPx) - castle.position).ToArray();
                tiers[t] = new Castle.Tier
                {
                    name = stats.name, maxHealth = stats.hp, cost = stats.cost, unlockWave = stats.unlock,
                    intact = intact,
                    damaged1 = null,   // hoan hau ki: rao chua co ban hung hai 1
                    damaged2 = null,   // hoan hau ki: rao chua co ban hung hai 2
                    bodyPosition = centre - castle.position,
                    bodyScale = scale * (intact.pixelsPerUnit / Ppu),
                    mounts = mounts,
                    mountScales = mounts.Select(_ => CannonMountScale).ToArray(),
                };
                maxMounts = Mathf.Max(maxMounts, mounts.Length);
            }
            var so = new SerializedObject(castle.GetComponent<Castle>());
            var list = so.FindProperty("tiers");
            list.arraySize = tiers.Length;
            for (int t = 0; t < tiers.Length; t++)
            {
                var e = list.GetArrayElementAtIndex(t);
                var tier = tiers[t];
                e.FindPropertyRelative("name").stringValue = tier.name;
                e.FindPropertyRelative("maxHealth").floatValue = tier.maxHealth;
                e.FindPropertyRelative("cost").intValue = tier.cost;
                e.FindPropertyRelative("unlockWave").intValue = tier.unlockWave;
                e.FindPropertyRelative("intact").objectReferenceValue = tier.intact;
                e.FindPropertyRelative("damaged1").objectReferenceValue = tier.damaged1;
                e.FindPropertyRelative("damaged2").objectReferenceValue = tier.damaged2;
                e.FindPropertyRelative("bodyPosition").vector3Value = tier.bodyPosition;
                e.FindPropertyRelative("bodyScale").floatValue = tier.bodyScale;
                var mounts = e.FindPropertyRelative("mounts");
                mounts.arraySize = tier.mounts.Length;
                for (int m = 0; m < tier.mounts.Length; m++) mounts.GetArrayElementAtIndex(m).vector3Value = tier.mounts[m];
                var scales = e.FindPropertyRelative("mountScales");
                scales.arraySize = tier.mountScales.Length;
                for (int m = 0; m < tier.mountScales.Length; m++) scales.GetArrayElementAtIndex(m).floatValue = tier.mountScales[m];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return maxMounts;
        }

        /// <summary>
        /// Them da thap + be phao. Du phong du 5 be; Castle dat va bat/tat cung hang voi phao theo cap.
        /// Theo dung cach ghep da duyet `tempart/fence-g1/compose.py`: tham cat mep phai = mep dong,
        /// goc tren-trai o (FenceTerraceLeftPx, far), cao `near + lip - far`; be la tam o (PadXPx, hang y).
        /// </summary>
        static void BuildFenceGround(Transform castle)
        {
            var terrace = LoadSprite(ArtRoot + "Fence/terrace-aligned.png");
            float terraceWidth = 0f;
            float terraceSeamPx = FenceFarPx;
            if (terrace != null)
            {
                float scale = (FenceNearPx + FenceLipPx - FenceFarPx) / terrace.rect.height;
                var go = NewSprite("Terrace", castle, terrace, SortOrder.Castle - 2);
                go.transform.localScale = Vector3.one * scale;
                float width = terrace.rect.width * scale;
                go.transform.position = PxToWorld(FenceTerraceLeftPx + width / 2f,
                    FenceFarPx + terrace.rect.height * scale / 2f);
                terraceWidth = width;
                terraceSeamPx = FenceFarPx + terrace.rect.height * scale;   // mep GAN cua them = day man da
            }

            BuildFenceSkirt(castle, terraceWidth, terraceSeamPx);

            var pedestal = LoadSprite(ArtRoot + "Fence/pedestal.png");
            if (pedestal == null) return;
            float pedScale = PadWidthPx / pedestal.rect.width;
            var rows = FencePadRows(FenceTierStats[0].pads);
            var pedestals = new SpriteRenderer[FencePadCount];
            for (int i = 0; i < pedestals.Length; i++)
            {
                var go = NewSprite("Pedestal", castle, pedestal, SortOrder.Castle - 1);
                go.transform.localScale = Vector3.one * pedScale;
                go.transform.position = PxToWorld(PadXPx, rows[Mathf.Min(i, rows.Length - 1)]);
                go.gameObject.SetActive(i < rows.Length);
                pedestals[i] = go;
            }
            SetRefs(castle.GetComponent<Castle>(), "pedestals", pedestals);
            SetValue(castle.GetComponent<Castle>(), "pedestalDrop", -PadCannonDropPx / Ppu);
        }

        /// <summary>Hang y (art px, tinh tu dinh) cua duong cat trong `Fence/ground-skirt.png` — khop `tools/art/make_fence_skirt.py`.</summary>
        const float SkirtSeamPx = 48f;

        /// <summary>
        /// Dai dat o DAY them da: pha duong cat ngang tuyet doi cua mep gan them bang co dai / so i dat / bong tiep xuc
        /// (art `Fence/ground-skirt.png`, 1 art px = 1 px man). Khong co art thi bo qua — scene cu van dung.
        /// Ve tren them (Castle-2) va duoi than thanh (Castle) nen dai nam de len mep da nhung khong che vet chan phao.
        /// </summary>
        static void BuildFenceSkirt(Transform castle, float terraceWidth, float seamPx)
        {
            var skirt = LoadSprite(ArtRoot + "Fence/ground-skirt.png");
            if (skirt == null || terraceWidth <= 0f) return;
            // Con cua Castle (nhu Terrace/Pedestal) de ban sao thanh NPC o Chinh Phat mang theo, khong phai object roi.
            var go = NewSprite("GroundSkirt", castle, skirt, SortOrder.Castle - 1);
            go.transform.position = PxToWorld(FenceTerraceLeftPx + terraceWidth / 2f,
                seamPx + (skirt.rect.height / 2f - SkirtSeamPx));
        }

        /// <summary>Trung vi mep alpha phai cua cac hang 20-97% chieu cao (px sprite, tinh tu trai) — mat thap/vach phai.</summary>
        static float MeasureRightWall(Texture2D tex)
        {
            string path = AssetDatabase.GetAssetPath(tex);
            var copy = new Texture2D(2, 2);
            copy.LoadImage(File.ReadAllBytes(path));
            var px = copy.GetPixels32();
            int w = copy.width, h = copy.height;
            var edges = new List<int>();
            // GetPixels32 tinh tu DUOI len: hang anh y (tu tren) = h - 1 - row.
            for (int y = (int)(h * 0.2f); y < (int)(h * 0.97f); y++)
            {
                int row = h - 1 - y;
                for (int x = w - 1; x >= 0; x--)
                    if (px[row * w + x].a > 0) { edges.Add(x); break; }
            }
            Object.DestroyImmediate(copy);
            edges.Sort();
            return edges.Count > 0 ? edges[edges.Count / 2] : w;
        }

        // Rect cua nut Nang rao: MOT cho. Nut doc cap nay, dai nut sanh ben duoi doc lai de tinh `railY`
        // (xem `BuildBattleControls`) — dung chep so ra cho khac.
        static readonly Vector2 CastleUpgradePos = new Vector2(24, -196);
        static readonly Vector2 CastleUpgradeSize = new Vector2(380, 72);

        /// <summary>Nut nang hang rao: goc tren-trai duoi thanh mau (BattleHud an khi dang danh / da toi da).</summary>
        static Button BuildCastleUpgradeButton(RectTransform canvas, out Text label)
        {
            var button = MakeButton("CastleUpgradeButton", canvas, "", TopLeft, CastleUpgradePos, CastleUpgradeSize, out label);
            button.transition = Selectable.Transition.SpriteSwap;
            var buttonState = button.spriteState;   // giu sprite nhan cua MakeButton
            buttonState.disabledSprite = UiSprite("chrome/btn-disabled");
            button.spriteState = buttonState;
            label.fontSize = PixelFont.Snap(26);
            // Ve ngay tren thanh mau lau dai, duoi moi bang/popup dung truoc do (bang ket qua, ban do Chinh Phat...).
            var hp = canvas.Find("CastleHp");
            if (hp != null) button.transform.SetSiblingIndex(hp.GetSiblingIndex() + 1);
            return button;
        }

        /// <summary>
        /// Bang nang hang rao: anh cap hien tai -> cap sau (sprite lanh cua tung cap, `CastleUpgradePanel` dien luc mo),
        /// dong Mau / Be phao truoc -> sau, dieu kien wave, nut Nang + Dong. Lop Dim chan cham xuong san luc bang mo.
        /// </summary>
        static CastleUpgradePanel BuildCastleUpgradePanel(RectTransform canvas)
        {
            var host = FullScreen("CastleUpgradePanel", canvas);
            var panel = host.gameObject.AddComponent<CastleUpgradePanel>();
            var root = FullScreen("Root", host);
            var dim = Img("Dim", root, UiSprite("chrome/dim-tile"), Center, Vector2.zero, Vector2.zero);
            Stretch(dim.rectTransform, 0, 0, 0, 0);
            dim.color = new Color(1f, 1f, 1f, 0.72f);
            dim.raycastTarget = true;

            var body = Img("Body", root, UiSprite("panels/panel-hero"), Center, new Vector2(0, 20), new Vector2(960, 640), sliced: true);
            body.raycastTarget = true;
            var titleBar = Img("Title", body.transform, UiSprite("panels/title-ribbon"), Top, new Vector2(0, 26), new Vector2(420, 64), sliced: true);
            Label("Value", titleBar.transform, "Upgrade Fence", 34, Center, Vector2.zero, new Vector2(400, 64));
            var closeImage = Img("Close", body.transform, UiSprite("chrome/btn-close"), TopRight, new Vector2(-16, -16), new Vector2(64, 64));
            closeImage.raycastTarget = true;
            var close = closeImage.gameObject.AddComponent<Button>();

            // Hai o anh rao hai ben mui ten; anh giu ti le (sprite cot rao rat cao).
            Image TierSlot(string name, float x, out Text caption)
            {
                var slot = Img(name, body.transform, UiSprite("chrome/reward-slot"), Top, new Vector2(x, -70), new Vector2(300, 280), sliced: true);
                var image = Img("Image", slot.transform, null, Center, new Vector2(0, 6), new Vector2(260, 250));
                image.preserveAspect = true;
                caption = Label("Name", body.transform, "", 26, Top, new Vector2(x, -360), new Vector2(380, 40));
                return image;
            }
            var current = TierSlot("Current", -220, out var currentName);
            var next = TierSlot("Next", 220, out var nextName);
            nextName.color = new Color(1f, 0.94f, 0.7f);
            Label("Arrow", body.transform, "→", 64, Top, new Vector2(0, -180), new Vector2(100, 80));

            // Dong chi so: stat-chip rong, icon trai, "truoc -> sau" phai.
            Text StatRow(string name, string iconPath, string caption, float y)
            {
                var chip = Img(name, body.transform, UiSprite("chrome/stat-chip"), Top, new Vector2(0, y), new Vector2(640, 48), sliced: true);
                Img("Icon", chip.transform, UiSprite(iconPath), MidLeft, new Vector2(8, 0), new Vector2(36, 36));
                var label = Label("Caption", chip.transform, caption, 24, MidLeft, new Vector2(54, 0), new Vector2(220, 48));
                label.alignment = TextAnchor.MiddleLeft;
                var value = Label("Value", chip.transform, "", 26, MidRight, new Vector2(-16, 0), new Vector2(340, 48));
                value.alignment = TextAnchor.MiddleRight;
                value.color = new Color(1f, 0.94f, 0.7f);
                return value;
            }
            var health = StatRow("Health", "icons/icon-hp", "Fence HP", -404);
            var mountCount = StatRow("Mounts", "icons/icon-cannon-blast", "Cannon Mounts", -458);
            var condition = Label("Condition", body.transform, "", 24, Top, new Vector2(0, -518), new Vector2(640, 36));

            var upgrade = MakeButton("Upgrade", body.transform, "", Bottom, new Vector2(0, -38), new Vector2(340, 80), out var upgradeLabel);
            Img("Icon", upgrade.transform, UiSprite("icons/icon-gold"), MidLeft, new Vector2(24, 0), new Vector2(40, 40));
            upgradeLabel.rectTransform.anchoredPosition += new Vector2(16, 0);

            SetRef(panel, "root", root.gameObject);
            SetRef(panel, "currentImage", current);
            SetRef(panel, "nextImage", next);
            SetRef(panel, "currentName", currentName);
            SetRef(panel, "nextName", nextName);
            SetRef(panel, "healthText", health);
            SetRef(panel, "mountsText", mountCount);
            SetRef(panel, "conditionText", condition);
            SetRef(panel, "upgradeButton", upgrade);
            SetRef(panel, "upgradeLabel", upgradeLabel);
            SetRef(panel, "closeButton", close);
            return panel;
        }

        /// <summary>
        /// Bang chon phao: cung skin + cach cham voi bang chon hero (`panels/`). Moi dong: emblem + ten + dong chi so
        /// (CannonBoard dien luc mo). Duoi cung: huy. Go phao = cham be dang co phao; nang level o man Doi quan.
        /// Dat TRUOC ResultPopup de bang ket qua luon de len tren.
        /// </summary>
        static void BuildCannonPicker(RectTransform canvas, CannonBoard board)
        {
            var roster = AssetDatabase.FindAssets("t:CannonDefinition", new[] { CannonDir })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path)
                .Select(AssetDatabase.LoadAssetAtPath<CannonDefinition>)
                .ToArray();

            // 13 phao (2026-10-30, them Dau Rong Da) xep 1 cot thi bang cao ~1800 > man 1080 -> 2 cot.
            // Buoc dong 120 (nut cao 114): 7 hang -> cao 1058, van lot man 1080; de 124 thi bang cao 1086 va tieu de
            // bi cat 9 px o mep tren (Khoa 2026-10-30).
            const float rowStep = 120f;
            const int columns = 2;
            int rows = (roster.Length + columns - 1) / columns;
            float height = 108f + rows * rowStep + 110f;
            var picker = Img("CannonPicker", canvas, UiSprite("panels/panel-hero"), Center, new Vector2(0, 40), new Vector2(1300, height), sliced: true);
            picker.raycastTarget = true;
            var titleBar = Img("Title", picker.transform, UiSprite("panels/title-ribbon"), Top, new Vector2(0, 26), new Vector2(360, 64), sliced: true);
            Label("Value", titleBar.transform, "Select Cannon", 34, Center, Vector2.zero, new Vector2(340, 64));

            var buttons = new Object[roster.Length];
            var infos = new Object[roster.Length];
            for (int i = 0; i < roster.Length; i++)
            {
                var cannon = roster[i];
                float y = height / 2f - 108f - rowStep / 2f - (i / columns) * rowStep;
                float x = (i % columns - (columns - 1) / 2f) * 624f;
                var row = MakeButton("Cannon_" + cannon.name, picker.transform, "", Center, new Vector2(x, y),
                    new Vector2(608, 114), out _, UiSprite("panels/stat-strip"));

                var portrait = Img("Portrait", row.transform, UiSprite("panels/portrait-frame-big"), MidLeft, new Vector2(10, 0), new Vector2(98, 98), sliced: true);
                if (cannon.icon != null)
                {
                    var icon = Img("Icon", portrait.transform, cannon.icon, Center, Vector2.zero, new Vector2(72, 72));
                    icon.color = cannon.tint;
                }

                var nameText = Label("Name", row.transform, cannon.displayName, 32, MidLeft, new Vector2(122, 26), new Vector2(300, 40), MidLeft);
                nameText.alignment = TextAnchor.MiddleLeft;
                var info = Label("Info", row.transform, "", 20, MidLeft, new Vector2(122, -18), new Vector2(330, 34), MidLeft);
                info.alignment = TextAnchor.MiddleLeft;
                info.horizontalOverflow = HorizontalWrapMode.Wrap;

                buttons[i] = row;
                infos[i] = info;
            }
            var cancel = MakeButton("Cancel", picker.transform, "Cancel", Bottom, new Vector2(0, 24), new Vector2(200, 72), out _);

            SetRef(board, "pickerPanel", picker.gameObject);
            SetRefs(board, "roster", roster);
            SetRefs(board, "rosterButtons", buttons);
            SetRefs(board, "rosterInfo", infos);
            SetRef(board, "cancelButton", cancel);

            var popup = canvas.Find("ResultPopup");
            if (popup != null) popup.SetAsLastSibling();
        }

        // ---------------------------------------------------------------- scene Gacha

        static void BuildGachaScene(GachaConfig config)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            config = AssetDatabase.LoadAssetAtPath<GachaConfig>(DataDir + "/GachaConfig.asset");   // ly do: xem BuildBattleScene
            // Font built-in cung thanh fake null sau NewScene thu hai -> moi Text cua Gacha luu `m_Font: {fileID: 0}`, khong hien chu.
            _font = PixelFont.Asset;
            CreateCamera(new Color(0.10f, 0.07f, 0.16f));
            var economy = new GameObject("Game").AddComponent<Economy>();
            CreateEventSystem();

            var canvas = CreateCanvas("GachaUI");
            var screen = canvas.gameObject.AddComponent<GachaScreen>();
            SetRef(screen, "config", config);
            SetRef(screen, "economy", economy);
            SetRefs(screen, "heroes", LoadAll<HeroDefinition>(DataDir).Where(h => !string.IsNullOrEmpty(h.unitId)).ToArray());
            SetRef(screen, "roles", AssetDatabase.LoadAssetAtPath<HeroRoleCatalog>(HeroRoleCatalogPath));

            // Nen man trieu hoi do lane agy giao; chua co thi de mau nen camera.
            var backdrop = AssetDatabase.LoadAssetAtPath<Sprite>(UiDir + "gacha/gacha-backdrop.png");
            if (backdrop != null)
            {
                var bg = Img("Backdrop", canvas, backdrop, Center, Vector2.zero, Vector2.zero);
                Stretch(bg.rectTransform, 0, 0, 0, 0);
            }

            SetRef(screen, "titleText", Label("Title", canvas, "Summon", 56, Center, new Vector2(0, -24), new Vector2(800, 80), Top));
            SetRef(screen, "backButton", MakeButton("BackButton", canvas, "Back", TopLeft, new Vector2(24, -24), new Vector2(200, 72), out _));
            SetRef(screen, "goldText", GoldPill(canvas));

            var info = Img("Info", canvas, UiSprite("chrome/panel-wood"), Top, new Vector2(0, -120), new Vector2(1100, 120), sliced: true);
            var ratesLabel = Label("Rates", info.transform, "", 20, Center, new Vector2(0, -14), new Vector2(1060, 44), Top);
            var pityLabel = Label("Pity", info.transform, "", 26, Center, new Vector2(0, 14), new Vector2(1060, 44), Bottom);
            // Chu trang tren nen go sang (panel-wood) gan nhu khong doc duoc -> chu nau dam, bo vien den.
            foreach (var label in new[] { ratesLabel, pityLabel })
            {
                label.color = new Color(0.24f, 0.14f, 0.06f);
                Object.DestroyImmediate(label.GetComponent<Outline>());
            }
            SetRef(screen, "ratesText", ratesLabel);
            SetRef(screen, "pityText", pityLabel);

            // 4 tab banner giua bang ti le va luoi ket qua; nhan tab do GachaScreen ghi.
            var tabs = new Button[4];
            for (int i = 0; i < tabs.Length; i++)
                tabs[i] = MakeButton($"Banner{i}", canvas, "", Top, new Vector2((i - 1.5f) * 260f, -256), new Vector2(240, 72), out _);
            SetRefs(screen, "bannerButtons", tabs);

            // 10 the = 2 hang x 5 cot, moi the 96x128 hien o 2x.
            var cell = Native(96, 128);
            var gridRect = Rect("Results", canvas, Center, new Vector2(0, -70), new Vector2(5 * cell.x + 4 * 16, 2 * cell.y + 16));
            var grid = gridRect.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = cell;
            grid.spacing = new Vector2(16, 16);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.childAlignment = TextAnchor.MiddleCenter;
            SetRef(screen, "resultGrid", gridRect);
            SetRef(screen, "cardTemplate", BuildCardTemplate(gridRect));

            SetRef(screen, "rollOneButton", MakeButton("RollOne", canvas, "", Bottom, new Vector2(-190, 40), new Vector2(340, 110), out var oneLabel));
            SetRef(screen, "rollOneLabel", oneLabel);
            SetRef(screen, "rollTenButton", MakeButton("RollTen", canvas, "", Bottom, new Vector2(190, 40), new Vector2(340, 110), out var tenLabel));
            SetRef(screen, "rollTenLabel", tenLabel);

            EditorSceneManager.SaveScene(scene, ScenePath(SceneNames.Gacha));
        }

        /// <summary>
        /// Mo ta ky nang cho man Doi quan, theo `unitId`. Chi dien o con TRONG — sua chu trong Inspector thi build lai khong
        /// ghi de. Chu tom tu comment kit trong code (`Warden`, `Necromancer`, `DragonRider`, `SkillKind`), chua doi chieu GDD.
        /// </summary>
        const string HeroRoleCatalogPath = DataDir + "/HeroRoleCatalog.asset";

        /// <summary>
        /// Vai tung hero theo GDD muc 3 (cot "Vai"). Ghi MOT lan khi asset con `None` — doi tay trong Inspector thi giu.
        /// Hero moi: them mot dong o day. Unit trieu hoi / linh khong co dong -> giu `None`, the khong hien icon.
        /// </summary>
        static readonly Dictionary<string, HeroRole> HeroRoles = new Dictionary<string, HeroRole>
        {
            { "Cragfist", HeroRole.Tank }, { "Chainwarden", HeroRole.Tank }, { "TruongPhi", HeroRole.Tank },
            { "RuneShield", HeroRole.Tank }, { "ShroomKnight", HeroRole.Tank }, { "BeetleKnight", HeroRole.Tank },
            { "Ironblade", HeroRole.Melee }, { "Wyrmspear", HeroRole.Melee }, { "Wukong", HeroRole.Melee },
            { "QuanVu", HeroRole.Melee }, { "TritonGuard", HeroRole.Melee }, { "Bloodswordsman", HeroRole.Melee },
            { "Galemonk", HeroRole.Melee }, { "Twinblade", HeroRole.Melee },
            { "Hawkfletch", HeroRole.Archer }, { "PetalSprite", HeroRole.Archer },
            { "Frostweaver", HeroRole.Mage }, { "Stormcaller", HeroRole.Mage }, { "EmberApprentice", HeroRole.Mage },
            { "BogWitch", HeroRole.Mage }, { "SilkFae", HeroRole.Mage },
            { "Bonecaller", HeroRole.Support }, { "Banneret", HeroRole.Support }, { "LuuBi", HeroRole.Support },
            { "SigilMage", HeroRole.Support }, { "KoboldTrapper", HeroRole.Support }, { "Soulbinder", HeroRole.Support },
            { "BellNun", HeroRole.Support },
            { "GourdTaoist", HeroRole.Support }, { "KhoiLoiSu", HeroRole.Support },
            { "BlackSigilHunter", HeroRole.Archer }, { "MoonHare", HeroRole.Archer },
            { "InkScribe", HeroRole.Mage }, { "HongLien", HeroRole.Mage },
            { "SaVe", HeroRole.Tank }, { "Werewolf", HeroRole.Melee },
            { "HauNghe", HeroRole.Archer }, { "BachLocTien", HeroRole.Healer },
            { "Vespera", HeroRole.Healer },
        };

        /// <summary>
        /// Chay SAU moi buoc tao hero (HeroN, DaoVien, BuildApprovedHeroes khong qua `HeroDef`), roi tao catalog ten + icon
        /// vai cho UI. Catalog chi dien khi moi tao — sua ten/icon trong asset thi builder khong ghi de.
        /// </summary>
        static void AssignHeroRoles()
        {
            foreach (var hero in LoadAll<HeroDefinition>(DataDir))
            {
                string id = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(hero)).Replace("Hero_", "");
                if (hero.role != HeroRole.None || !HeroRoles.TryGetValue(id, out var role)) continue;
                hero.role = role;
                EditorUtility.SetDirty(hero);
            }

            var catalog = LoadOrCreate<HeroRoleCatalog>(HeroRoleCatalogPath, out bool created);
            if (!created) return;
            HeroRoleCatalog.Entry Role(HeroRole role, string name, string icon) =>
                new HeroRoleCatalog.Entry { role = role, displayName = name, icon = UiSprite($"icons/icon-role-{icon}") };
            catalog.entries = new[]
            {
                Role(HeroRole.Tank, "Tank", "tank"),
                Role(HeroRole.Melee, "Melee", "melee"),
                Role(HeroRole.Archer, "Ranged", "archer"),
                Role(HeroRole.Mage, "Mage", "mage"),
                Role(HeroRole.Support, "Support", "support"),
                Role(HeroRole.Healer, "Heal", "healer"),
            };
            EditorUtility.SetDirty(catalog);
        }

        static void FillSkillTexts()
        {
            var texts = new Dictionary<string, (string ult, string passive, string minor, string minor2)>
            {
                ["mage_elemental"] = ("Frost Zone: place an ice zone at the largest Enemy group, lasting a few seconds.",
                    "Ice Shatter: when an Enemy suffering Slow dies in Range, ice Shards explode — Enemies around the corpse take 50% basic Hit Damage.", null, null),
                ["cragfist"] = ("Stone Form: become immobile + Invulnerable for a few seconds, reflecting part of the Damage taken.",
                    "Enemy Taunt: Enemies near Nguoi Da abandon other heroes and Attack him (while he has room to block).", null, null),
                ["knight"] = ("Fan Slash: a long fan-shaped slash in front, dealing several times basic Hit Damage.",
                    "Enemy Pursuit: basic Hits against Enemies below 30% HP deal +50% Damage.", null, null),
                ["archer"] = ("Rapid Fire: greatly increase Attack Speed for a few seconds.",
                    "Weakpoint Aim: every 4th shot is a Crit, dealing double Damage.", null, null),
                ["vespera"] = ("Watch Lantern: place a light ring around the ally with the lowest HP — heroes inside Heal and gain a small Shield for a few seconds.",
                    "Emergency Rescue: allies below 30% HP receive 50% more Heal.", null, null),
                ["mage_summoner"] = (
                    "Summon Ky Si Tu Than; if the knight is alive, Heal it to full HP. Linh Xuong near the knight gain Attack + Attack Speed.",
                    "Corpse Raising: Enemies dying near the mage have a chance to rise as Linh Xuong.",
                    "Seal Ring: a ground ring at an Enemy group deals low Damage + a seal; Enemies dying while sealed always become Linh Xuong.",
                    null),
                ["chainwarden"] = (
                    "Hook Throw: pull a ranged Enemy close + Stun, then enter PERMANENT Berserk until death — two long chains, increased Attack/Attack Speed, reduced Damage taken, lifesteal.",
                    "Enemy Prison: hold up to 4 Enemies; held Enemies take +20% Damage from all sources.",
                    "Chain Sweep: spin chains around self, dealing low Damage + Root (Berserk: Chain Cyclone 360°).",
                    "Chain Yank: while holding at least 2 Enemies, yank hard — each held Enemy takes Damage scaling with the number yanked + Stun for 1s (Berserk: also pull all Enemies within chain Range)."),
                ["banneret"] = (
                    "Charge Command: the whole team on the field gains +30% Damage for 6s.",
                    "War Banner: allies around Ky Thu gain +15% Attack Speed.",
                    "Banner Planting: if an ally is fighting nearby, plant a banner in place — allies in the zone take 25% less Damage for 5s.",
                    null),
                ["wukong"] = (
                    "Heavenly Staff Pillar: a giant staff slams horizontally onto the largest Enemy group — high Damage along a long strip, Stun for 1,5s, the crack applies Slow for 3s; each clone slams a small staff for 30%.",
                    "Body Substitute · Returning Hairs: the first time HP runs out each Wave, a clone becomes the real body (40% HP); each fading clone Heals 8% HP; lifesteal 10%.",
                    "Hair Clone Summon: blow out 2 clones lasting 8s (40% Attack/HP), attacking independently, each blocking 3 Enemies.",
                    "Whirlwind Staff: spin the staff for 3s — Hit around self, block and reflect projectiles; each Enemy Hit Heals 3% HP (cap 25%)."),
                ["stormcaller"] = (
                    "Thunder Burst: 5 lightning pillars strike Enemy positions in Range — each deals high Damage + Stun for 0,5s.",
                    "Electric Overload: each lightning Hit on an Enemy adds +1 electric Stack; 3 Stacks explode (+60% Damage, Stun for 0,3s). Stunned Enemies take +35% lightning Damage; lightning on Shields deals ×2.",
                    "Lightning Rod: plant a copper rod among Enemies for 6s, shocking nearby Enemies each second; Chain Lightning bouncing through the rod gains +2 bounces with no Damage reduction.",
                    "Lightning Step: when an Enemy closes in, turn into lightning and retreat 2 slots; the old position explodes with Stun for 0,5s."),
                ["galemonk"] = (
                    "Wind Cyclone: a tornado at the largest Enemy group — pull Enemies within radius 2,5 to the center, hold for 3s, deal Damage each second (Bosses immune to pull).",
                    "Following Wind: Enemies pushed/pulled by wind suffer 20% Slow for 2s.",
                    "Wind Sweep: strike straight along the lane, piercing the entire line, Knockback 1,5 slots (elites/Bosses immune to Knockback).",
                    null),
                ["luubi"] = (
                    "Peach Garden Oath: peach blossoms fall across the field — all allies Heal 25% HP + gain 30% Attack for 6s; sworn brothers gain +2 Mana.",
                    "Brotherhood Oath: 2/3 brothers on the field → +10% Attack and HP; all 3/3 → an extra +25% Mana regeneration. Benevolent Heart: heroes around Luu Bi gain +15% Mana regeneration (brothers ×2).",
                    "Benevolent Grace: the ally missing the most HP Heals 15% max HP + removes Stun, Root and Slow.",
                    "Shared Life: the first sworn brother to fall each Wave is raised by peach silk after 1s with 30% HP + Invulnerable for 2s."),
                ["quanvu"] = (
                    "Azure Dragon Manifest: a green dragon spirit charges along the lane through all Enemies, dealing ×5 Damage + extra based on missing HP; each Enemy killed refunds 10% Mana.",
                    "Brotherhood Oath (as Luu Bi). Martial Saint: basic fan slashes Hit up to 3 Enemies; each Enemy killed grants +1 Might Stack (+5% Attack, +2% lifesteal, up to 6, lost at Wave end).",
                    "Azure Crescent Blade: a crescent blade wave flies along the lane, piercing Enemies for ×2 Damage.",
                    "Feigning Retreat: below 40% HP → retreat 1 slot, feign defeat, then return to slash pursuing Enemies for ×3 Damage (1 time per Wave)."),
                ["truongphi"] = (
                    "Dangyang Bridge Roar: stomp and roar — Enemies within radius 4 take ×3 Damage, Knockback + Stun for 3s + 30% Armor Break; taunt the whole area, take -50% Damage for 6s. Bosses only suffer Slow + Armor Break.",
                    "Brotherhood Oath (as Luu Bi). Thousand Foe Defiance: each Enemy nearby grants -6% Damage taken (up to 30%).",
                    "Serpent Spear: spin the serpent spear to charge, then thrust through 2 slots; Enemies Hit suffer 15% Armor Break for 4s.",
                    "Drunken Wine: below 60% HP, drink from the wine gourd — Heal 20% HP, Stun immunity, -30% Damage taken for 5s."),
                ["bogwitch"] = (
                    "Poison Mist Cauldron: a Poison mist of radius 1,3 at the largest Enemy group for 5s — Enemies inside gain +1 Poison Stack each second and take 20% more Damage.",
                    "Venom Seep: each Poison drop Hit adds +1 Poison Stack (up to 5), each Stack deals 2 Damage/second; another Hit refreshes 4s.", null, null),
                ["shroomknight"] = (
                    "Mushroom Bloom: 3 mushrooms grow around the feet, exploding into spores after 2s — each deals 3 times basic Hit Damage to nearby Enemies.",
                    "Spore Field: melee Hits release spores, dealing 4 Damage to Enemies around self (up to 2 times/second).", null, null),
                ["tritonguard"] = (
                    "Water Whirlpool: a whirlpool around the feet for 4s — Enemies inside cannot Attack and lose HP each second (elites only deal -50% Damage, Bosses immune to Attack Lock).",
                    "Line Thrust: basic Hits pierce through, dealing 50% Damage to the Enemy directly behind the Target.", null, null),
                ["emberapprentice"] = (
                    "Ember Rain: 3 fireballs fall around the largest Enemy group — each explodes for double basic Hit Damage + Burn, leaving a fire pool for 4s.",
                    "Flame Catch: basic Hits apply Burn for 3s (5 Damage/second); if a Burning Enemy dies, the fire jumps to the nearest Enemy.", null, null),
                ["runeshield"] = (
                    "Rune Wall: plant a Shield to raise a rune wall in front for 5s, covering his row and two adjacent rows — block Enemies and Enemy projectiles; stand still behind the Shield.",
                    "Projectile Block: ranged Enemy projectiles shatter on contact, dealing no Damage.", null, null),
                ["silkfae"] = (
                    "Puppet Dance: attach silk to the Enemy with the most HP in Range (except Bosses) — it Attacks its own faction for 4s (elites 2s).",
                    "Puppet Strings: Enemies Hit by silk have a 25% miss chance for 2s.", null, null),
                ["soldier_warhorn"] = (null,
                    "Boss Bane: basic Hits and charges against elites/Bosses deal +50% Damage.",
                    "Horn Charge: charge through the Enemy line, applying Stun + Knockback to all Enemies along the path.", null),
                ["wyrmspear"] = (
                    "Dragon Transformation: transform into a Dragonkin + Summon Bach Dia Long (Dragon Armor, Dragon Awe, Dragon Rage, Dragon Pact); next cast: Twin Dragon Descent.",
                    "Reverse Scale: Hits against Bosses/elites add a % of the Target's max HP.",
                    "Dragon Pierce / Heavenly Dragon Flurry: thrust through a line, ignoring Armor.",
                    "Earth Dragon Quake (dragon): stone spikes around the dragon — area Damage, Stun, Armor Break."),
            };
            foreach (var hero in LoadAll<HeroDefinition>(DataDir))
            {
                if (string.IsNullOrEmpty(hero.unitId) || !texts.TryGetValue(hero.unitId, out var t)) continue;
                if (string.IsNullOrEmpty(hero.ultimateText)) hero.ultimateText = t.ult;
                if (string.IsNullOrEmpty(hero.passiveText)) hero.passiveText = t.passive;
                if (string.IsNullOrEmpty(hero.minorText)) hero.minorText = t.minor;
                if (string.IsNullOrEmpty(hero.secondMinorText)) hero.secondMinorText = t.minor2;
                EditorUtility.SetDirty(hero);
            }

            // Icon ky nang (UI dot 9 + 11 + 14, `UI/icons/icon-skill-*`): ghi de moi lan build, icon la art cua builder.
            var icons = new Dictionary<string, (string ult, string minor, string minor2)>
            {
                ["mage_elemental"] = ("frost-zone", null, null),
                ["cragfist"] = ("petrify", null, null),
                ["knight"] = ("cone-sweep", null, null),
                ["archer"] = ("rapid-fire", null, null),
                ["vespera"] = ("lantern", null, null),
                ["mage_summoner"] = ("summon-knight", "mark-circle", null),
                ["chainwarden"] = ("hook-throw", "chain-sweep", "chain-yank"),
                ["banneret"] = ("rally", "plant-flag", null),
                ["wukong"] = ("giant-staff", "clone", "spin-staff"),
                ["stormcaller"] = ("thunder-storm", "lightning-rod", "lightning-step"),
                ["wyrmspear"] = ("dragon-rider", "dragon-pierce", "earth-spike"),
                ["galemonk"] = ("tornado", "gale-sweep", null),
                ["soldier_warhorn"] = (null, "charge", null),
                ["luubi"] = ("peach-oath", "benevolence", "silk-revive"),
                ["quanvu"] = ("green-dragon", "crescent-wave", "feint-retreat"),
                ["truongphi"] = ("bridge-roar", "serpent-spear", "wine-gourd"),
                // Dot 2026-10-15: loat hero SR/R + linh da co art icon trong `UI/icons/` tu truoc nhung
                // khong nam trong bang nay nen o ky nang hien TRONG.
                ["beetleknight"] = ("horn-slam", null, null),
                ["twinblade"] = ("blade-storm", null, null),
                ["bellnun"] = ("bell-toll", null, null),
                ["bogwitch"] = ("toxic-mist", null, null),
                ["emberapprentice"] = ("ember-rain", null, null),
                ["koboldtrapper"] = ("trap-detonate", null, null),
                ["petalsprite"] = ("seed-rain", null, null),
                ["runeshield"] = ("rune-wall", null, null),
                ["shroomknight"] = ("mushroom-bloom", null, null),
                ["sigilmage"] = ("sigil-blast", null, null),
                ["soulbinder"] = ("great-pact", "soul-link", "soul-rip"),
                ["tritonguard"] = ("whirlpool", null, null),
                ["bloodswordsman"] = ("blood-shatter", "blood-offer", "blood-parry"),
                ["silkfae"] = ("puppet-dance", null, null),
            };
            // Icon noi tai (UI dot 14 + 15, `UI/icons/icon-passive-*`); hero chua co thi de trong.
            var passiveIcons = new Dictionary<string, string>
            {
                ["luubi"] = "kindness", ["quanvu"] = "war-saint", ["truongphi"] = "thousand-foes",
                ["mage_elemental"] = "shatter", ["cragfist"] = "taunt", ["knight"] = "execute", ["archer"] = "weakpoint",
                ["vespera"] = "rescue", ["mage_summoner"] = "raise-dead", ["wukong"] = "substitute",
                ["stormcaller"] = "overload", ["chainwarden"] = "chain-prison", ["wyrmspear"] = "reverse-scale",
                ["banneret"] = "war-banner", ["galemonk"] = "tailwind", ["soldier_warhorn"] = "boss-bane",
                // Noi tai cua cac hero/linh chua co icon o tren (cung dot 2026-10-15).
                ["beetleknight"] = "tremor", ["twinblade"] = "combo", ["bellnun"] = "cleanse",
                ["bogwitch"] = "poison-soak", ["emberapprentice"] = "ignite", ["koboldtrapper"] = "set-trap",
                ["petalsprite"] = "seed-cling", ["runeshield"] = "shot-break", ["shroomknight"] = "spore",
                ["sigilmage"] = "weak-sigil", ["soulbinder"] = "soul-transfer", ["tritonguard"] = "pierce-row",
                ["bloodswordsman"] = "blood-will", ["silkfae"] = "tangle",
                ["soldier_ironclawcrab"] = "armor-crush",
                ["soldier_mushroomsoldier"] = "spore",
            };
            Sprite SkillIcon(string id) => id != null ? UiSprite("icons/icon-skill-" + id) : null;
            // Hai bang DOC LAP: truoc 2026-10-15 phan `passiveIcon` nam SAU `continue` cua bang `icons`, nen hero
            // khong co icon chieu cuoi (vd. Tien Det To — chua ve icon chieu cuoi) khong bao gio nhan icon noi tai.
            foreach (var hero in LoadAll<HeroDefinition>(DataDir))
            {
                if (string.IsNullOrEmpty(hero.unitId)) continue;
                bool touched = false;
                if (icons.TryGetValue(hero.unitId, out var ic))
                {
                    hero.ultimateIcon = SkillIcon(ic.ult);
                    hero.minorIcon = SkillIcon(ic.minor);
                    hero.secondMinorIcon = SkillIcon(ic.minor2);
                    touched = true;
                }
                if (passiveIcons.TryGetValue(hero.unitId, out var pi))
                {
                    hero.passiveIcon = UiSprite("icons/icon-passive-" + pi);
                    touched = true;
                }
                if (touched) EditorUtility.SetDirty(hero);
            }

            // Noi tai hero khong co bo chieu rieng (chot 2026-09-29, so CHUA can) — xem `PassiveKind`. Chi ghi khi o con
            // None: so chinh tay trong Inspector khong bi de.
            var passives = new Dictionary<string, (PassiveKind kind, float power, float threshold, float radius, int every)>
            {
                ["knight"] = (PassiveKind.Execute, 0.5f, 0.3f, 0f, 1),
                ["cragfist"] = (PassiveKind.Taunt, 0f, 0f, 1.5f, 1),
                ["archer"] = (PassiveKind.Crit, 2f, 0f, 0f, 4),
                ["mage_elemental"] = (PassiveKind.Shatter, 0.5f, 0f, 1f, 1),
                ["vespera"] = (PassiveKind.Rescue, 0.5f, 0.3f, 0f, 1),
            };
            foreach (var hero in LoadAll<HeroDefinition>(DataDir))
            {
                if (hero.passiveKind != PassiveKind.None || string.IsNullOrEmpty(hero.unitId)
                    || !passives.TryGetValue(hero.unitId, out var p)) continue;
                hero.passiveKind = p.kind;
                hero.passivePower = p.power;
                hero.passiveThreshold = p.threshold;
                hero.passiveRadius = p.radius;
                hero.passiveEvery = p.every;
                EditorUtility.SetDirty(hero);
            }
        }

        const string EquipmentConfigPath = DataDir + "/EquipmentConfig.asset";

        /// <summary>Tao config trang bi neu chua co; icon 4 o = icon trang bi UI C dot 5 (thay icon chi so tam).</summary>
        static void BuildEquipmentConfig()
        {
            var config = LoadOrCreate<EquipmentConfig>(EquipmentConfigPath, out _);
            config.weaponIcon = UiSprite("icons/icon-equip-weapon");
            config.armorIcon = UiSprite("icons/icon-equip-armor");
            config.helmetIcon = UiSprite("icons/icon-equip-helmet");
            config.ringIcon = UiSprite("icons/icon-equip-ring");
            EditorUtility.SetDirty(config);
        }

        static T[] LoadAll<T>(string folder) where T : Object =>
            AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(asset => asset != null).ToArray();

        /// <summary>Man Doi quan (GDD 10.2 + 10.3): trai luoi the cuon doc, phai bang chi tiet + 2 nut nang.</summary>
        static void BuildUnitsScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var config = AssetDatabase.LoadAssetAtPath<GachaConfig>(DataDir + "/GachaConfig.asset");
            _font = PixelFont.Asset;    // ly do: xem BuildGachaScene
            CreateCamera(new Color(0.08f, 0.09f, 0.14f));
            var economy = new GameObject("Game").AddComponent<Economy>();
            CreateEventSystem();

            var canvas = CreateCanvas("UnitsUI");
            var screen = canvas.gameObject.AddComponent<UnitsScreen>();
            SetRef(screen, "config", config);
            SetRef(screen, "economy", economy);
            var units = LoadAll<UnitDefinition>(DataDir + "/Units");
            var unitHeroes = LoadAll<HeroDefinition>(DataDir).Where(h => !string.IsNullOrEmpty(h.unitId)).ToArray();
            var unitCannons = LoadAll<CannonDefinition>(DataDir);
            SetRefs(screen, "units", units);
            SetRefs(screen, "heroes", unitHeroes);
            SetRefs(screen, "cannons", unitCannons);
            SetFootPivots(screen, units, unitHeroes, unitCannons);
            SetRef(screen, "equipment", AssetDatabase.LoadAssetAtPath<EquipmentConfig>(EquipmentConfigPath));
            var roles = AssetDatabase.LoadAssetAtPath<HeroRoleCatalog>(HeroRoleCatalogPath);
            SetRef(screen, "roles", roles);

            Label("Title", canvas, "Roster", 56, Center, new Vector2(0, -24), new Vector2(800, 80), Top);
            SetRef(screen, "backButton", MakeButton("BackButton", canvas, "Back", TopLeft, new Vector2(24, -24), new Vector2(200, 72), out _));
            SetRef(screen, "goldText", GoldPill(canvas));

            // Tab: UnitsScreen doi sprite tab-normal/tab-active khi chon; icon ben trai chu.
            var tabs = new Button[3];
            string[] tabIcons = { "icons/icon-tab-hero", "icons/icon-role-soldier", "icons/icon-cannon-blast" };
            for (int i = 0; i < tabs.Length; i++)
            {
                tabs[i] = MakeButton($"Tab{i}", canvas, "", TopLeft, new Vector2(40 + i * 290, -120), new Vector2(270, 72),
                    out var tabLabel, UiSprite("chrome/tab-normal"));
                tabLabel.rectTransform.anchoredPosition = new Vector2(28, 0);
                Img("Icon", tabs[i].transform, UiSprite(tabIcons[i]), MidLeft, new Vector2(28, 0), new Vector2(48, 48));
            }
            SetRefs(screen, "tabButtons", tabs);
            SetRef(screen, "tabNormal", UiSprite("chrome/tab-normal"));
            SetRef(screen, "tabActive", UiSprite("chrome/tab-active"));

            // Trai: luoi the 4 cot trong khung cuon doc (the 96x128 hien o 2x nhu the gacha).
            var cell = Native(96, 128);
            var listPanel = Img("ListPanel", canvas, UiSprite("chrome/panel-wood"), TopLeft, new Vector2(24, -210),
                new Vector2(4 * cell.x + 3 * 16 + 64, 846), sliced: true);
            listPanel.raycastTarget = true;    // keo cuon tren khoang trong cua khung
            // Hang loc vai tren dau khung (chi hien o tab Hero): nut tron "Tat ca" + mot nut icon moi vai trong catalog.
            var roleFilter = Rect("RoleFilter", listPanel.transform, TopLeft, new Vector2(40, -24), new Vector2(800, Native(44, 44).y));
            var roleButtons = new Button[roles.entries.Length + 1];
            for (int i = 0; i < roleButtons.Length; i++)
            {
                roleButtons[i] = MakeButton(i == 0 ? "All" : $"Role{roles.entries[i - 1].role}", roleFilter, i == 0 ? "All" : "",
                    TopLeft, new Vector2(i * 104, 0), Native(44, 44), out var roleLabel, UiSprite("chrome/btn-round"));
                roleLabel.fontSize = 20;
                if (i > 0) Img("Icon", roleButtons[i].transform, roles.entries[i - 1].icon, Center, Vector2.zero, new Vector2(56, 56));
            }
            SetRef(screen, "roleFilter", roleFilter.gameObject);
            SetRefs(screen, "roleButtons", roleButtons);

            var viewport = Rect("Viewport", listPanel.transform, Center, Vector2.zero, Vector2.zero);
            Stretch(viewport, 24, 24, 24 + Native(44, 44).y + 16, 24);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Content", viewport, Top, Vector2.zero, new Vector2(4 * cell.x + 3 * 16, 0));
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = cell;
            grid.spacing = new Vector2(16, 16);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.childAlignment = TextAnchor.UpperCenter;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = listPanel.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            SetRef(screen, "grid", content);
            SetRef(screen, "cardTemplate", BuildUnitCardTemplate(content));

            // Phai: chi tiet unit dang chon.
            var detail = Img("Detail", canvas, UiSprite("chrome/panel-wood"), TopRight, new Vector2(-24, -120), new Vector2(1000, 936), sliced: true);
            // Cot trai: unit (frame idle dau) dung tren be da 160x117. Icon neo o tam mat be (140, -186);
            // UnitsScreen.StandOnPedestal dat pivot rect = pivot chan cua sprite.
            Img("Pedestal", detail.transform, UiSprite("panels/unit-pedestal"), TopLeft, new Vector2(60, -150), new Vector2(160, 117));
            var unitImage = Img("Icon", detail.transform, null, TopLeft, new Vector2(140, -186), Native(128, 128));
            SetRef(screen, "detailIcon", unitImage);
            var name = Label("Name", detail.transform, "", 40, TopLeft, new Vector2(290, -40), new Vector2(670, 56));
            name.alignment = TextAnchor.MiddleLeft;
            SetRef(screen, "detailName", name);
            var sub = Label("Sub", detail.transform, "", 26, TopLeft, new Vector2(290, -96), new Vector2(670, 40));
            sub.alignment = TextAnchor.MiddleLeft;
            SetRef(screen, "detailSub", sub);
            BuildDetailProgress(screen, detail.transform);
            BuildStatChips(screen, detail.transform);

            // Ky nang / Trang bi: nut tron (sprite btn-round / btn-round-active), chu ben phai nut.
            SetRef(screen, "skillsTab", RoundTab("SkillsTab", detail.transform, "Skills", new Vector2(40, -436)));
            SetRef(screen, "gearTab", RoundTab("GearTab", detail.transform, "Equipment", new Vector2(300, -436)));
            // Vach ngan (chrome/divider 80x8, 9-slice ngang) giua hai nut tron va noi dung ben duoi, nhu mock dot 5.
            var divider = Img("TabDivider", detail.transform, UiSprite("chrome/divider"), TopLeft, new Vector2(40, -524), new Vector2(920, 16), sliced: true);
            divider.raycastTarget = false;
            SetRef(screen, "tabDivider", divider.gameObject);
            SetRef(screen, "roundNormal", UiSprite("chrome/btn-round"));
            SetRef(screen, "roundActive", UiSprite("chrome/btn-round-active"));
            BuildSkillRows(screen, detail.transform);
            BuildGearView(screen, detail.transform);

            var levelButton = MakeButton("LevelButton", detail.transform, "", Bottom, new Vector2(-230, 30), new Vector2(400, 104), out var levelLabel);
            DecorateButton(levelButton, levelLabel, "icons/icon-gold");
            SetRef(screen, "levelButton", levelButton);
            SetRef(screen, "levelLabel", levelLabel);
            var starButton = MakeButton("StarButton", detail.transform, "", Bottom, new Vector2(230, 30), new Vector2(400, 104), out var starLabel,
                UiSprite("chrome/btn-purple"));
            DecorateButton(starButton, starLabel, "icons/icon-star-up");
            SetRef(screen, "starButton", starButton);
            SetRef(screen, "starLabel", starLabel);

            EditorSceneManager.SaveScene(scene, ScenePath(SceneNames.Units));
        }

        /// <summary>
        /// Scene Cua hang (GDD 10.4) — thay bang phu `ShopScreen` trong scene Battle tu 2026-10-20: cua hang co
        /// bon loai hang (Daily · Weekly · Monthly · Offers) nen phai la man rieng nhu Doi quan. Cung khuon voi
        /// `BuildUnitsScene`: camera nen toi, Back goc trai, o vang goc phai; noi dung do `Meta/Shop` quyet.
        /// </summary>
        // ---------------------------------------------------------------- man dang nhap

        /// <summary>Dung LAI mot minh scene Login (khong phai chay ca `BuildAll`) — scene nay chi dung UI chrome.</summary>
        [MenuItem("BuildBuild/Scenes/Build Login")]
        public static void BuildLoginSceneMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildLoginSceneOnly();
            EditorSceneManager.OpenScene(ScenePath(SceneNames.Login));
            Debug.Log("[SceneBuilder] Da dung man dang nhap va dat lam scene dau tien trong Build Settings.");
        }

        /// <summary>
        /// Man dang nhap / dang ky (chot 2026-10-27: tai khoan cuc bo, save theo acc — xem `Meta/Accounts`).
        /// Scene nay dung DAU danh sach Build Settings: mo game la ra form truoc; may con phien dang nhap thi
        /// `LoginScreen.Start` nhay thang sang Battle nen nguoi choi binh thuong khong thay no.
        /// </summary>
        public static void BuildLoginSceneOnly()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            _font = PixelFont.Asset;   // ly do: xem BuildGachaScene
            CreateCamera(new Color(0.06f, 0.05f, 0.11f));
            CreateEventSystem();

            var canvas = CreateCanvas("LoginUI");
            var screen = canvas.gameObject.AddComponent<LoginScreen>();

            var panel = Img("Panel", canvas, UiSprite("panels/panel-hero"), Center, new Vector2(0, -20), new Vector2(780, 740), sliced: true);
            panel.raycastTarget = true;
            var ribbon = Img("Title", panel.transform, UiSprite("panels/title-ribbon"), Top, new Vector2(0, 26), new Vector2(360, 64), sliced: true);
            var title = Label("Value", ribbon.transform, "Sign in", 34, Center, Vector2.zero, new Vector2(340, 64));

            Text Caption(Transform parent, string name, string caption, float y)
            {
                var label = Label(name, parent, caption, 26, TopLeft, new Vector2(48, y), new Vector2(400, 44));
                label.alignment = TextAnchor.MiddleLeft;
                label.color = new Color(0.78f, 0.78f, 0.9f);
                return label;
            }

            Caption(panel.transform, "UserCaption", "Username", -92);
            var user = MakeInput("Username", panel.transform, "3-16 letters, digits, _", TopLeft, new Vector2(48, -140), new Vector2(684, 80));
            Caption(panel.transform, "PassCaption", "Password", -248);
            var password = MakeInput("Password", panel.transform, "at least 4 characters", TopLeft, new Vector2(48, -296), new Vector2(684, 80), secret: true);

            // O nhap lai mat khau nam trong MOT container: che do dang nhap chi viec tat ca cum, khong phai
            // bat/tat tung object mot.
            var confirmRow = Rect("ConfirmRow", panel.transform, TopLeft, new Vector2(0, -404), new Vector2(780, 156));
            Caption(confirmRow, "ConfirmCaption", "Confirm password", 0);
            var confirm = MakeInput("Confirm", confirmRow, "repeat password", TopLeft, new Vector2(48, -48), new Vector2(684, 80), secret: true);
            confirmRow.gameObject.SetActive(false);

            var submit = MakeButton("Submit", panel.transform, "Sign in", TopLeft, new Vector2(48, -596), new Vector2(336, 84), out var submitLabel);
            var mode = MakeButton("Mode", panel.transform, "Create account", TopLeft, new Vector2(396, -596), new Vector2(336, 84), out var modeLabel,
                UiSprite("chrome/btn-blue"));
            var message = Label("Message", panel.transform, "", 22, TopLeft, new Vector2(48, -690), new Vector2(684, 44));
            message.color = new Color(1f, 0.62f, 0.58f);   // chu loi, cung sac do voi nhan "Not enough Gold" cua Shop

            SetRef(screen, "titleText", title);
            SetRef(screen, "userField", user);
            SetRef(screen, "passwordField", password);
            SetRef(screen, "confirmField", confirm);
            SetRef(screen, "confirmRow", confirmRow.gameObject);
            SetRef(screen, "messageText", message);
            SetRef(screen, "submitButton", submit);
            SetRef(screen, "submitLabel", submitLabel);
            SetRef(screen, "modeButton", mode);
            SetRef(screen, "modeLabel", modeLabel);

            EditorSceneManager.SaveScene(scene, ScenePath(SceneNames.Login));

            // Login dung dau danh sach scene; cac scene con lai giu nguyen thu tu da co.
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath(SceneNames.Login), true) }
                .Concat(EditorBuildSettings.scenes.Where(s => s.path != ScenePath(SceneNames.Login)))
                .ToArray();
        }

        static void BuildShopScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var config = AssetDatabase.LoadAssetAtPath<GachaConfig>(DataDir + "/GachaConfig.asset");
            _font = PixelFont.Asset;    // ly do: xem BuildGachaScene
            CreateCamera(new Color(0.08f, 0.09f, 0.14f));
            var economy = new GameObject("Game").AddComponent<Economy>();
            CreateEventSystem();

            var canvas = CreateCanvas("ShopUI");
            var screen = canvas.gameObject.AddComponent<ShopScene>();
            SetRef(screen, "economy", economy);
            SetRef(screen, "equipment", AssetDatabase.LoadAssetAtPath<EquipmentConfig>(EquipmentConfigPath));
            SetRefs(screen, "units", config != null ? config.pool.Cast<Object>().ToArray() : new Object[0]);

            Label("Title", canvas, "Shop", 56, Center, new Vector2(0, -24), new Vector2(800, 80), Top);
            SetRef(screen, "backButton", MakeButton("BackButton", canvas, "Back to Town", TopLeft, new Vector2(24, -24), new Vector2(280, 72), out _));
            SetRef(screen, "goldText", GoldPill(canvas));

            // Bon tab, thu tu TRUNG voi gia tri `ShopKind` (Daily, Weekly, Monthly, Offer) — doi enum la doi ca hai.
            string[] tabs = { "Daily", "Weekly", "Monthly", "Offers" };
            var tabButtons = new Object[tabs.Length];
            for (int i = 0; i < tabs.Length; i++)
                tabButtons[i] = MakeButton($"Tab{i}", canvas, tabs[i], TopLeft, new Vector2(40 + i * 300, -118), new Vector2(280, 72), out _,
                    UiSprite("chrome/tab-normal"));
            SetRefs(screen, "tabButtons", tabButtons);
            SetRef(screen, "tabNormal", UiSprite("chrome/tab-normal"));
            SetRef(screen, "tabActive", UiSprite("chrome/tab-active"));

            var panel = Img("Panel", canvas, UiSprite("panels/panel-hero"), Center, new Vector2(0, -50), new Vector2(1400, 720), sliced: true);
            panel.raycastTarget = true;
            var hint = Label("Hint", panel.transform, "", 24, Top, new Vector2(0, -30), new Vector2(1300, 44));
            SetRef(screen, "hintText", hint);
            SetRef(screen, "messageText", Label("Message", canvas, "", 34, Bottom, new Vector2(0, 34), new Vector2(900, 60)));

            // 4 dong CO DINH (`Shop.MaxSlots`) — loai nao it o hon thi an bot dong, khong them/bot widget luc chay.
            const int Rows = 4;   // = `Shop.MaxSlots`
            var rowRoots = new Object[Rows];
            var rowIcons = new Object[Rows];
            var rowNames = new Object[Rows];
            var rowDetails = new Object[Rows];
            var rowButtons = new Object[Rows];
            var rowLabels = new Object[Rows];
            for (int i = 0; i < Rows; i++)
            {
                var row = Img($"Row{i + 1}", panel.transform, UiSprite("panels/stat-strip"), Center,
                    new Vector2(0, 190f - i * 128f), new Vector2(1300, 112), sliced: true);
                var icon = Img("Icon", row.transform, null, MidLeft, new Vector2(70, 0), new Vector2(88, 88));
                icon.preserveAspect = true;
                var name = Label("Name", row.transform, "", 30, MidLeft, new Vector2(184, 24), new Vector2(716, 42));
                name.alignment = TextAnchor.MiddleLeft;
                var detail = Label("Detail", row.transform, "", 22, MidLeft, new Vector2(184, -26), new Vector2(716, 38));
                detail.alignment = TextAnchor.MiddleLeft;
                var buy = MakeButton($"Buy{i + 1}", row.transform, "", MidRight, new Vector2(-16, 0), new Vector2(280, 92), out var buyLabel);

                rowRoots[i] = row.gameObject;
                rowIcons[i] = icon;
                rowNames[i] = name;
                rowDetails[i] = detail;
                rowButtons[i] = buy;
                rowLabels[i] = buyLabel;
            }
            SetRefs(screen, "rowRoots", rowRoots);
            SetRefs(screen, "rowIcons", rowIcons);
            SetRefs(screen, "rowNames", rowNames);
            SetRefs(screen, "rowDetails", rowDetails);
            SetRefs(screen, "rowBuyButtons", rowButtons);
            SetRefs(screen, "rowBuyLabels", rowLabels);

            EditorSceneManager.SaveScene(scene, ScenePath(SceneNames.Shop));
        }
        /// <summary>
        /// Pivot chan (0..1) cua frame idle dau tung unit, do tu alpha PNG: day = hang thap nhat co >= 3 px dac, x = giua
        /// be ngang dai day 12% chieu cao than (ca hai chan). Cung cach chon sprite voi UnitsScreen (phao truoc, roi hero/linh).
        /// </summary>
        static void SetFootPivots(UnitsScreen screen, UnitDefinition[] units, HeroDefinition[] heroes, CannonDefinition[] cannons)
        {
            var pivots = new Vector2[units.Length];
            for (int i = 0; i < units.Length; i++)
            {
                var cannon = cannons.FirstOrDefault(c => c != null && c.unitId == units[i].id);
                var hero = heroes.FirstOrDefault(h => h != null && h.unitId == units[i].id);
                var anims = cannon != null ? cannon.animations : hero != null ? hero.animations : null;
                if (anims != null && anims.idle.IsValid) pivots[i] = MeasureFoot(anims.idle.frames[0]);
            }
            var so = new SerializedObject(screen);
            var prop = so.FindProperty("footPivots");
            prop.arraySize = pivots.Length;
            for (int i = 0; i < pivots.Length; i++) prop.GetArrayElementAtIndex(i).vector2Value = pivots[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Vector2 MeasureFoot(Sprite sprite)
        {
            var tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));
            var r = sprite.rect;
            int x0 = (int)r.x, y0 = (int)r.y, w = (int)r.width, h = (int)r.height;
            var px = tex.GetPixels32();
            bool Solid(int x, int y) => px[(y0 + y) * tex.width + x0 + x].a > 128;
            int bottom = -1, top = -1;
            for (int y = 0; y < h; y++)
            {
                int n = 0;
                for (int x = 0; x < w; x++) if (Solid(x, y)) n++;
                if (n < 3) continue;
                if (bottom < 0) bottom = y;
                top = y;
            }
            // x = giua be ngang dai day 12% chieu cao than: unit dung dang chan thi hang thap nhat chi co MOT ban chan
            // (Cragfist, Soi Chien) — lay tam 4 hang day keo pivot lech ve chan do.
            int left = w, right = -1;
            for (int y = Mathf.Max(bottom, 0); bottom >= 0 && y <= bottom + Mathf.CeilToInt(0.12f * (top - bottom)); y++)
                for (int x = 0; x < w; x++)
                    if (Solid(x, y)) { left = Mathf.Min(left, x); right = Mathf.Max(right, x); }
            Object.DestroyImmediate(tex);
            return bottom < 0 ? Vector2.zero : new Vector2((left + right + 1) * 0.5f / w, (float)bottom / h);
        }

        /// <summary>Hang sao 5 o + vien "Lv N" + thanh manh (icon manh + so) canh ten unit.</summary>
        static void BuildDetailProgress(UnitsScreen screen, Transform detail)
        {
            var stars = new Image[5];
            for (int i = 0; i < stars.Length; i++)
                stars[i] = Img($"Star{i}", detail, UiSprite("chrome/star-off"), TopLeft, new Vector2(290 + i * 44, -150), Native(20, 20));
            SetRefs(screen, "detailStars", stars);
            SetRef(screen, "starOn", UiSprite("chrome/star-on"));
            SetRef(screen, "starOff", UiSprite("chrome/star-off"));

            var badge = Img("LevelBadge", detail, UiSprite("chrome/level-badge"), TopLeft, new Vector2(520, -150), Native(60, 20), sliced: true);
            SetRef(screen, "detailLevelBadge", badge.gameObject);
            SetRef(screen, "detailLevel", Label("Value", badge.transform, "", 24, Center, Vector2.zero, Native(60, 20)));

            var bar = Img("ShardBar", detail, UiSprite("chrome/bar-frame-thin"), TopLeft, new Vector2(670, -154), Native(145, 16), sliced: true);
            SetRef(screen, "detailShardBar", bar.gameObject);
            var fill = Img("Fill", bar.transform, UiSprite("chrome/bar-fill-shard"), Center, Vector2.zero, Vector2.zero, sliced: true);
            Stretch(fill.rectTransform, 6, 6, 6, 6);
            SetRef(screen, "detailShardFill", fill.rectTransform);
            SetRef(screen, "detailShardFillImage", fill);
            SetRef(screen, "shardFillSprite", UiSprite("chrome/bar-fill-shard"));
            SetRef(screen, "shardFullSprite", UiSprite("chrome/bar-fill-shard-full"));
            Img("Icon", bar.transform, UiSprite("icons/icon-shard"), MidLeft, new Vector2(-20, 0), new Vector2(48, 48));
            SetRef(screen, "detailShardText", Label("Value", bar.transform, "", 22, Center, new Vector2(12, 0), Native(145, 16)));
        }

        /// <summary>Tieu de "CHI SO" (kem dong % tang ben phai) + 6 o stat-chip 2 cot; UnitsScreen an o thua.</summary>
        static void BuildStatChips(UnitsScreen screen, Transform detail)
        {
            var header = Img("StatsHeader", detail, UiSprite("chrome/section-header"), TopLeft, new Vector2(40, -280), new Vector2(920, 40), sliced: true);
            var title = Label("Title", header.transform, "Stats", 26, MidLeft, new Vector2(20, 0), new Vector2(200, 40));
            title.alignment = TextAnchor.MiddleLeft;
            title.color = new Color(1f, 0.94f, 0.7f);
            var note = Label("Note", header.transform, "", 20, MidRight, new Vector2(-16, 0), new Vector2(660, 40));
            note.alignment = TextAnchor.MiddleRight;
            note.color = new Color(0.7f, 0.74f, 0.82f);
            SetRef(screen, "statNote", note);

            const int count = 6;
            var roots = new GameObject[count];
            var icons = new Image[count];
            var labels = new Text[count];
            var values = new Text[count];
            for (int i = 0; i < count; i++)
            {
                var chip = Img($"Chip{i}", detail, UiSprite("chrome/stat-chip"), TopLeft,
                    new Vector2(40 + (i % 2) * 468, -332 - (i / 2) * 54), new Vector2(452, 48), sliced: true);
                roots[i] = chip.gameObject;
                icons[i] = Img("Icon", chip.transform, null, MidLeft, new Vector2(6, 0), new Vector2(36, 36));
                labels[i] = Label("Label", chip.transform, "", 24, MidLeft, new Vector2(60, 0), new Vector2(200, 48));
                labels[i].alignment = TextAnchor.MiddleLeft;
                labels[i].color = new Color(0.72f, 0.8f, 0.92f);
                values[i] = Label("Value", chip.transform, "", 24, MidRight, new Vector2(-14, 0), new Vector2(240, 48));
                values[i].alignment = TextAnchor.MiddleRight;
                values[i].color = new Color(1f, 0.94f, 0.7f);
            }
            SetRefs(screen, "chipRoots", roots);
            SetRefs(screen, "chipIcons", icons);
            SetRefs(screen, "chipLabels", labels);
            SetRefs(screen, "chipValues", values);
            SetRef(screen, "iconHp", UiSprite("icons/icon-hp"));
            SetRef(screen, "iconDamage", UiSprite("icons/icon-stat-damage"));
            SetRef(screen, "iconAttackSpeed", UiSprite("icons/icon-stat-attack-speed"));
            SetRef(screen, "iconRange", UiSprite("icons/icon-stat-range"));
            SetRef(screen, "iconSquad", UiSprite("icons/icon-stat-squad-size"));
            SetRef(screen, "iconCooldown", UiSprite("icons/icon-stat-cooldown"));
            SetRef(screen, "iconSplash", UiSprite("icons/icon-stat-splash"));
            SetRef(screen, "iconSlow", UiSprite("icons/icon-stat-slow"));
        }

        /// <summary>Nut tron 44x44 (2x) cua cot chi tiet, chu nam ben PHAI nut.</summary>
        static Button RoundTab(string name, Transform parent, string label, Vector2 pos)
        {
            var button = MakeButton(name, parent, label, TopLeft, pos, Native(44, 44), out var text, UiSprite("chrome/btn-round"));
            text.rectTransform.sizeDelta = new Vector2(160, 88);
            text.rectTransform.anchoredPosition = new Vector2(136, 0);
            text.alignment = TextAnchor.MiddleLeft;
            text.fontSize = PixelFont.Snap(28);
            return button;
        }

        /// <summary>Icon ben trai nut to + chu lui sang phai; het tien / chua du manh thi doi sang sprite btn-disabled.</summary>
        static void DecorateButton(Button button, Text label, string icon)
        {
            Img("Icon", button.transform, UiSprite(icon), MidLeft, new Vector2(28, 0), new Vector2(56, 56));
            label.rectTransform.anchoredPosition = new Vector2(30, 0);
            button.transition = Selectable.Transition.SpriteSwap;
            var buttonState = button.spriteState;   // giu sprite nhan cua MakeButton
            buttonState.disabledSprite = UiSprite("chrome/btn-disabled");
            button.spriteState = buttonState;
        }

        /// <summary>
        /// Cot Trang bi (GDD 10.3): hang 4 o dang mac + thong tin/nut cua o dang chon ben phai, duoi la tui do cua o do.
        /// O dung vien the unit 48x64 hien o 2x (96x128), chu 3 dong nam trong o.
        /// </summary>
        static void BuildGearView(UnitsScreen screen, Transform detail)
        {
            var view = Rect("GearView", detail, TopLeft, new Vector2(40, -540), new Vector2(920, 264));
            SetRef(screen, "gearView", view.gameObject);

            var cell = Native(48, 64);
            SetRef(screen, "slotRow", Row("Slots", view, new Vector2(0, 0), 4, cell));
            SetRef(screen, "bagRow", Row("Bag", view, new Vector2(0, -cell.y - 8), 8, cell));

            var info = Label("Info", view, "", 22, TopLeft, new Vector2(4 * cell.x + 3 * 12 + 24, 0), new Vector2(480, 64));
            info.alignment = TextAnchor.UpperLeft;
            info.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetRef(screen, "gearInfo", info);
            float buttonsX = 4 * cell.x + 3 * 12 + 24;
            SetRef(screen, "upgradeButton", MakeButton("Upgrade", view, "", TopLeft, new Vector2(buttonsX, -70), new Vector2(230, 64), out var upgradeLabel));
            upgradeLabel.fontSize = PixelFont.Snap(22);
            SetRef(screen, "upgradeLabel", upgradeLabel);
            SetRef(screen, "unequipButton", MakeButton("Unequip", view, "Unequip", TopLeft, new Vector2(buttonsX + 246, -70), new Vector2(200, 64), out _));

            // Mau o: vien do hiem + icon + chu, outline vang khi dang chon. Nam trong tui cho co san, UnitsScreen an di.
            var root = Img("CellTemplate", view, UiSprite("hud/hud-hero-card"), TopLeft, Vector2.zero, cell);
            root.raycastTarget = true;
            var cellView = root.gameObject.AddComponent<EquipCellView>();
            var button = root.gameObject.AddComponent<Button>();
            var outline = root.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.85f, 0.2f);
            outline.effectDistance = new Vector2(5f, -5f);
            var icon = Img("Icon", root.transform, null, Top, new Vector2(0, -10 * UiScale), Native(16, 16));
            var frame = Img("Frame", root.transform, UiSprite("cards/card-frame-n"), Center, Vector2.zero, Vector2.zero);
            Stretch(frame.rectTransform, 0, 0, 0, 0);
            var label = Label("Label", root.transform, "", 15, Center, new Vector2(0, 8), new Vector2(92, 64), Bottom);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetRef(cellView, "button", button);
            SetRef(cellView, "frame", frame);
            SetRef(cellView, "icon", icon);
            SetRef(cellView, "label", label);
            SetRef(cellView, "selectedOutline", outline);
            SetRef(screen, "cellTemplate", cellView);
            SetRefs(screen, "rarityFrames", new Object[]
            {
                UiSprite("cards/card-frame-n"), UiSprite("cards/card-frame-r"),
                UiSprite("cards/card-frame-sr"), UiSprite("cards/card-frame-ssr"),
            });
        }

        static RectTransform Row(string name, Transform parent, Vector2 pos, int columns, Vector2 cell)
        {
            var row = Rect(name, parent, TopLeft, pos, new Vector2(columns * cell.x + (columns - 1) * 12, cell.y));
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperLeft;
            return row;
        }

        /// <summary>Khoi chu nhieu dong can trai-tren, xuong dong trong khung, co rich text (so xanh, dong khoa xam).</summary>
        static Text Block(string name, Transform parent, int fontSize, Vector2 pos, Vector2 size)
        {
            var text = Label(name, parent, "", fontSize, TopLeft, pos, size);
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.supportRichText = true;
            text.lineSpacing = 1.15f;
            return text;
        }

        /// <summary>
        /// So dong ky nang o cot chi tiet: 5 = chieu cuoi + noi tai + 3 chieu phu (bo SSR 3 phu nhu Hau Nghe, Bach Loc
        /// Tien). Hero chi co 2 chieu phu thi dong thua bi an (`UnitsScreen.RefreshSkills`).
        /// </summary>
        const int SkillRowCount = 5;

        /// <summary>Chieu cao mot dong ky nang (ca o icon) — xem phep tinh vua 5 dong o `BuildSkillRows`.</summary>
        const float SkillRowHeight = 50;

        /// <summary>
        /// Khoi ky nang o cot chi tiet: <see cref="SkillRowCount"/> dong (icon ben trai, chu tu xuong dong ben phai),
        /// xep doc bang layout group vi chieu cao moi dong theo do dai chu; khoi cao theo noi dung, moc tu tren xuong.
        /// Khong co tieu de rieng (nut tab "Ky nang" da ghi), chu 20 / gian dong 0,7.
        ///
        /// Chieu cao phai vua 5 dong: nut Nang level / Nang sao neo DAY bang (y 30, cao 104) nen mep tren cua nut o
        /// 802 tinh tu dinh bang; khoi bat dau o -548 nen vung trong la 802 - 548 = 254 px. Nam dong cao
        /// <see cref="SkillRowHeight"/> = 48 voi gian 2 chiem 5x48 + 4x2 = 248 -> vua, va mot dong chu 2 hang o co 20
        /// / gian dong 0,7 can 48 px -> khong bi cat. Dong chu 3 hang (~72 px) thi khoi 5 dong = 368 -> tran xuong
        /// duoi nut, nen MO TA KY NANG phai giu trong 2 hang (kho 860 px, co 20): do bang
        /// `tempart/art-sync-20261012/t2-english/fit-check.py` tren chinh file font, roi doan lai bang Play.
        /// Doi co chu / gian dong / chieu cao dong o day thi phai chay lai phep do do.
        /// </summary>
        static void BuildSkillRows(Object screen, Transform detail)
        {
            var root = Rect("Skills", detail, TopLeft, new Vector2(40, -546), new Vector2(920, 254));
            var column = root.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 1;
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            root.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var icons = new Image[SkillRowCount];
            var texts = new Text[SkillRowCount];
            for (int i = 0; i < SkillRowCount; i++)
            {
                var row = Rect($"Row{i}", root, TopLeft, Vector2.zero, new Vector2(920, SkillRowHeight));
                var line = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                line.spacing = 12;
                line.childAlignment = TextAnchor.UpperLeft;
                line.childControlWidth = line.childControlHeight = true;
                line.childForceExpandWidth = line.childForceExpandHeight = false;
                icons[i] = Img("Icon", row, null, TopLeft, Vector2.zero, new Vector2(SkillRowHeight, SkillRowHeight));
                icons[i].raycastTarget = false;
                var iconSize = icons[i].gameObject.AddComponent<LayoutElement>();
                iconSize.minWidth = iconSize.preferredWidth = iconSize.minHeight = iconSize.preferredHeight = SkillRowHeight;
                texts[i] = Block("Text", row, 20, Vector2.zero, new Vector2(860, SkillRowHeight));
                texts[i].lineSpacing = 0.6f;
                // Nen kem sang: chu trang + vien den day (2 px) lam chu pixel 20 px nhoe thanh dam. Chu nau dam, bo vien.
                texts[i].color = new Color(0.24f, 0.14f, 0.06f);
                Object.DestroyImmediate(texts[i].GetComponent<Outline>());
                texts[i].gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            }
            SetRef(screen, "detailSkills", root.gameObject);
            SetRefs(screen, "skillIcons", icons);
            SetRefs(screen, "skillTexts", texts);
        }

        static UnitCardView BuildUnitCardTemplate(Transform grid)
        {
            // Cung khuon the gacha: nen the hero, icon tren, vien do hiem phu len, chu o dai duoi.
            var root = Img("CardTemplate", grid, UiSprite("hud/hud-hero-card"), Center, Vector2.zero, Native(96, 128));
            root.raycastTarget = true;
            var view = root.gameObject.AddComponent<UnitCardView>();
            var button = root.gameObject.AddComponent<Button>();
            var outline = root.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.85f, 0.2f);
            outline.effectDistance = new Vector2(6f, -6f);
            var icon = Img("Icon", root.transform, null, Top, new Vector2(0, -20 * UiScale), Native(48, 48));
            var frame = Img("Frame", root.transform, UiSprite("cards/card-frame-n"), Center, Vector2.zero, Vector2.zero);
            Stretch(frame.rectTransform, 0, 0, 0, 0);
            var lockIcon = Img("Lock", root.transform, UiSprite("icons/icon-lock"), Top, new Vector2(0, -52), new Vector2(72, 72));
            // Icon vai goc tren trai, doi xung voi vien Lv goc tren phai.
            var roleIcon = Img("Role", root.transform, null, TopLeft, new Vector2(8, -8), new Vector2(44, 44));
            roleIcon.raycastTarget = false;
            var nameText = Label("Name", root.transform, "", 17, Center, new Vector2(0, 60), new Vector2(180, 44), Bottom);
            nameText.lineSpacing = 0.95f;

            // Vien Lv goc tren phai, hang 5 sao + thanh manh o dai duoi the.
            var badge = Img("LevelBadge", root.transform, UiSprite("chrome/level-badge"), TopRight, new Vector2(-10, -10), Native(38, 16), sliced: true);
            var levelText = Label("Value", badge.transform, "", 20, Center, Vector2.zero, Native(38, 16));
            var stars = new Image[5];
            for (int i = 0; i < stars.Length; i++)
                stars[i] = Img($"Star{i}", root.transform, UiSprite("chrome/star-off"), Bottom, new Vector2(-40 + i * 20, 36), new Vector2(20, 20));
            var shardBar = Img("ShardBar", root.transform, UiSprite("chrome/bar-frame-thin"), Bottom, new Vector2(0, 10), new Vector2(160, 24), sliced: true);
            var shardFill = Img("Fill", shardBar.transform, UiSprite("chrome/bar-fill-shard"), Center, Vector2.zero, Vector2.zero, sliced: true);
            Stretch(shardFill.rectTransform, 6, 6, 6, 6);

            SetRef(view, "frameLocked", UiSprite("cards/card-frame-locked"));
            SetRef(view, "levelBadge", badge.gameObject);
            SetRef(view, "levelText", levelText);
            SetRefs(view, "stars", stars);
            SetRef(view, "starOn", UiSprite("chrome/star-on"));
            SetRef(view, "starOff", UiSprite("chrome/star-off"));
            SetRef(view, "shardBar", shardBar.gameObject);
            SetRef(view, "shardFill", shardFill.rectTransform);
            SetRef(view, "shardFillImage", shardFill);
            SetRef(view, "shardFillSprite", UiSprite("chrome/bar-fill-shard"));
            SetRef(view, "shardFullSprite", UiSprite("chrome/bar-fill-shard-full"));
            SetRef(view, "lockIcon", lockIcon.gameObject);
            SetRef(view, "roleIcon", roleIcon);
            SetRef(view, "button", button);
            SetRef(view, "background", root);
            SetRef(view, "frame", frame);
            SetRef(view, "icon", icon);
            SetRef(view, "nameText", nameText);
            SetRef(view, "selectedOutline", outline);
            SetRef(view, "frameN", UiSprite("cards/card-frame-n"));
            SetRef(view, "frameR", UiSprite("cards/card-frame-r"));
            SetRef(view, "frameSR", UiSprite("cards/card-frame-sr"));
            SetRef(view, "frameSSR", UiSprite("cards/card-frame-ssr"));
            return view;
        }

        static GachaCardView BuildCardTemplate(Transform grid)
        {
            // Nen the hero (o anh tren + dai duoi) roi phu vien do hiem trong suot ruot len tren.
            var root = Img("CardTemplate", grid, UiSprite("hud/hud-hero-card"), Center, Vector2.zero, Native(96, 128));
            var view = root.gameObject.AddComponent<GachaCardView>();
            var icon = Img("Icon", root.transform, null, Top, new Vector2(0, -20 * UiScale), Native(48, 48));
            var frame = Img("Frame", root.transform, UiSprite("cards/card-frame-n"), Center, Vector2.zero, Vector2.zero);
            Stretch(frame.rectTransform, 0, 0, 0, 0);
            var roleIcon = Img("Role", root.transform, null, TopLeft, new Vector2(8, -8), new Vector2(44, 44));
            roleIcon.raycastTarget = false;
            var nameText = Label("Name", root.transform, "", 18, Center, new Vector2(0, 28), new Vector2(140, 52), Bottom);
            nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
            var noteText = Label("Note", root.transform, "", 18, Center, new Vector2(12, -16), new Vector2(180, 28), Top);
            noteText.color = new Color(1f, 0.86f, 0.35f);

            SetRef(view, "frame", frame);
            SetRef(view, "icon", icon);
            SetRef(view, "roleIcon", roleIcon);
            SetRef(view, "nameText", nameText);
            SetRef(view, "noteText", noteText);
            SetRef(view, "frameN", UiSprite("cards/card-frame-n"));
            SetRef(view, "frameR", UiSprite("cards/card-frame-r"));
            SetRef(view, "frameSR", UiSprite("cards/card-frame-sr"));
            SetRef(view, "frameSSR", UiSprite("cards/card-frame-ssr"));
            return view;
        }

        // ---------------------------------------------------------------- widget dung chung

        static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        static readonly Vector2 Top = new Vector2(0.5f, 1f);
        static readonly Vector2 TopRight = new Vector2(1f, 1f);
        static readonly Vector2 MidLeft = new Vector2(0f, 0.5f);
        static readonly Vector2 MidRight = new Vector2(1f, 0.5f);
        static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        static readonly Vector2 Bottom = new Vector2(0.5f, 0f);
        static readonly Vector2 BottomLeft = new Vector2(0f, 0f);

        static Text GoldPill(Transform canvas)
        {
            var pill = Img("Gold", canvas, UiSprite("chrome/currency-pill"), TopRight, new Vector2(-24, -28), Native(100, 20), sliced: true);
            Img("Icon", pill.transform, UiSprite("icons/icon-gold"), MidLeft, new Vector2(-12, 0), new Vector2(48, 48));
            return Label("Value", pill.transform, "0", 30, Center, new Vector2(16, 0), Native(80, 20));
        }

        static Button MakeButton(string name, Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size, out Text text,
            Sprite background = null)
        {
            // Nut go/dong V3 mac dinh (`btn-green`, ten giu tu ban xanh la cu) co du state: nhan = dao bevel + toi,
            // tat = khu bao hoa + phang (audit 2026-10-12, T4). Nut nen rieng (tron, tim...) chi co MOT sprite -> tint.
            var img = Img(name, parent, background != null ? background : UiSprite("chrome/btn-green"), anchor, pos, size, sliced: true);
            img.raycastTarget = true;
            var button = img.gameObject.AddComponent<Button>();
            if (background == null)
            {
                button.transition = Selectable.Transition.SpriteSwap;
                button.spriteState = new SpriteState
                {
                    pressedSprite = UiSprite("chrome/btn-green-pressed"),
                    disabledSprite = UiSprite("chrome/btn-disabled"),
                };
            }
            else
            {
                button.transition = Selectable.Transition.ColorTint;
                var colors = ColorBlock.defaultColorBlock;
                colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 1f);
                button.colors = colors;
            }
            text = Label("Label", img.transform, label, 30, Center, Vector2.zero, size);
            return button;
        }

        /// <summary>
        /// O nhap chu cua UGUI `InputField` kieu cu (game khong dung TextMeshPro): nen `chrome/panel-paper`
        /// 9-slice, chu ben trong + placeholder mo; `secret` = hien dau cham (o mat khau).
        /// </summary>
        static InputField MakeInput(string name, Transform parent, string placeholder, Vector2 anchor, Vector2 pos, Vector2 size,
            bool secret = false)
        {
            var background = Img(name, parent, UiSprite("chrome/panel-paper"), anchor, pos, size, sliced: true);
            background.raycastTarget = true;
            // `panel-paper` la nen SANG (trung binh 133/255) ma chu trong o lai mau trang — lam toi bot de chu va
            // con tro doc duoc, o nhap van nang hon nen panel (56) nen van thay duong vien.
            background.color = new Color(0.55f, 0.55f, 0.62f, 1f);
            var field = background.gameObject.AddComponent<InputField>();
            // Chu va placeholder keo kin o (cach mep 24 px), pivot giua — dung bo cuc mac dinh cua UGUI cho
            // InputField, de con tro chu ve dung cho ngoi cua no.
            var text = Label("Text", background.transform, "", 28, Center, Vector2.zero, Vector2.zero);
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = false;
            Stretch(text.rectTransform, 24, 24, 0, 0);
            var hint = Label("Placeholder", background.transform, placeholder, 28, Center, Vector2.zero, Vector2.zero);
            hint.alignment = TextAnchor.MiddleLeft;
            hint.color = new Color(1f, 1f, 1f, 0.35f);
            Stretch(hint.rectTransform, 24, 24, 0, 0);
            field.targetGraphic = background;
            field.textComponent = text;
            field.placeholder = hint;
            // Con tro mac dinh cua UGUI la mau XAM DAM, tren nen toi gan nhu khong thay.
            field.customCaretColor = true;
            field.caretColor = new Color(1f, 0.95f, 0.8f);
            field.lineType = InputField.LineType.SingleLine;
            // Gioi han trung voi `Accounts` (ten 16, mat khau 32) de o nhap khong nhan chu ma lop du lieu chac
            // chan tu choi.
            field.characterLimit = secret ? 32 : 16;
            if (secret) field.contentType = InputField.ContentType.Password;
            return field;
        }

        static Image Img(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 pos, Vector2 size, bool sliced = false)
        {
            var img = Rect(name, parent, anchor, pos, size).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            if (sliced)
            {
                img.type = Image.Type.Sliced;
                // Border 12 px cua sprite hien thanh 24 px — cung mat do pixel 2x voi phan UI con lai.
                img.pixelsPerUnitMultiplier = 1f / UiScale;
            }
            return img;
        }

        static Text Label(string name, Transform parent, string value, int fontSize, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivotAnchor = null)
        {
            var rect = Rect(name, parent, pivotAnchor ?? anchor, pos, size);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = _font;
            text.fontSize = PixelFont.Snap(fontSize);
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = value;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            // Phai dat CA hai truc: mac dinh cua Unity la VerticalWrapMode.Truncate, ma o nao thap hon
            // MOT dong (font pixel = 1,4 x co chu) thi Truncate KHONG VE GI CA — chu bien mat im lang,
            // khong loi console. Da dinh: `GoldPill` cao 40 < 42 (co 30) va bang wave cao 52 < 56 (co 40).
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            return rect;
        }

        /// <summary>
        /// Lop phu kin MAN (popup, overlay): keo gian theo canvas thay vi co dinh 1920x1080 — man 21:9 / 4:3 thi canvas
        /// khong con 1920x1080 (match 0,5), lop co dinh de ho dai HUD hai ben (hoac tren/duoi) van bam xuyen duoc.
        /// </summary>
        static RectTransform FullScreen(string name, Transform parent)
        {
            var rect = Rect(name, parent, Center, Vector2.zero, Vector2.zero);
            Stretch(rect, 0, 0, 0, 0);
            return rect;
        }

        static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = Center;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        static Vector2 Native(float w, float h) => new Vector2(w * UiScale, h * UiScale);

        static RectTransform CreateCanvas(string name)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return (RectTransform)go.transform;
        }

        /// <summary>
        /// Ban do Do Tham Chinh Phat (GDD muc 8, asset dot 10 `UI/raid/`): `world-map` 960x540 o x2 phu kin man, thanh minh
        /// giua, 3 token thanh NPC (vi tri do `ConquestMapScreen` dat theo save), suong + vong quet, bang xem thanh. Mo tu nut
        /// Pho ban tren HUD sanh.
        /// </summary>
        static void BuildConquestMap(RectTransform canvas, WaveManager waves, Economy economy, BattleHud hud, HeroDefinition[] roster)
        {
            var rootRect = FullScreen("ConquestMap", canvas);
            var screen = rootRect.gameObject.AddComponent<ConquestMapScreen>();
            // Nen den phu kin man: ban do co dinh 1920x1080, man rong hon thi hai ben khong con lo HUD (va bam xuyen).
            var backdrop = Img("Backdrop", rootRect, UiSprite("chrome/dim-tile"), Center, Vector2.zero, Vector2.zero);
            Stretch(backdrop.rectTransform, 0, 0, 0, 0);
            backdrop.color = Color.black;
            backdrop.raycastTarget = true;
            var mapImage = Img("Map", rootRect, UiSprite("raid/world-map"), Center, Vector2.zero, Native(960, 540));
            mapImage.raycastTarget = true;   // chan cham xuyen xuong san
            var map = mapImage.rectTransform;

            var fog = Img("Fog", map, UiSprite("chrome/dim-tile"), Center, Vector2.zero, Vector2.zero);
            Stretch(fog.rectTransform, 0, 0, 0, 0);
            fog.color = new Color(0.75f, 0.8f, 0.9f, 0.55f);
            var hintBack = Img("HintBack", fog.transform, null, Center, new Vector2(0, -170), new Vector2(1480, 70));
            hintBack.color = new Color(0.05f, 0.07f, 0.12f, 0.75f);
            hintBack.raycastTarget = false;
            Label("Hint", fog.transform, "Fog covers the border — press Scout to find Enemy Castles", 32, Center, new Vector2(0, -170), new Vector2(1400, 60));

            var ring = Img("ScanRing", map, null, Center, Vector2.zero, Vector2.zero);

            // Thanh minh ve SAU suong de luon ro.
            var playerToken = Img("PlayerCastle", map, UiSprite("raid/map-castle-player"), Center, new Vector2(0, 29), Native(64, 62));   // bai dat giua y 0,527
            var playerPill = Img("Label", playerToken.transform, UiSprite("chrome/currency-pill"), Bottom, new Vector2(0, -40), Native(70, 18), sliced: true);
            Label("Value", playerPill.transform, "Your Castle", 24, Center, Vector2.zero, Native(70, 18));

            var tokens = new Object[3];
            var tokenLabels = new Object[3];
            for (int i = 0; i < 3; i++)
            {
                var token = MakeButton($"Castle{i + 1}", map, "", Center, Vector2.zero, Native(64, 62), out var empty,
                    UiSprite("raid/map-castle-npc-small"));
                ((Image)token.targetGraphic).type = Image.Type.Simple;
                Object.DestroyImmediate(empty.gameObject);
                var pill = Img("Label", token.transform, UiSprite("chrome/currency-pill"), Bottom, new Vector2(0, -40), Native(110, 18), sliced: true);
                tokenLabels[i] = Label("Value", pill.transform, "", 24, Center, Vector2.zero, Native(110, 18));
                tokens[i] = token;
            }

            var title = Img("Title", rootRect, UiSprite("panels/title-ribbon"), Top, new Vector2(0, -12), new Vector2(420, 72), sliced: true);
            Label("Value", title.transform, "Conquest", 38, Center, Vector2.zero, new Vector2(400, 72));
            var close = MakeButton("Close", rootRect, "Close", TopRight, new Vector2(-24, -24), new Vector2(180, 72), out _);
            var infoPill = Img("Info", rootRect, UiSprite("chrome/currency-pill"), Top, new Vector2(0, -96), new Vector2(1240, 56), sliced: true);
            var info = Label("Value", infoPill.transform, "", 26, Center, Vector2.zero, new Vector2(1220, 56));
            var scan = MakeButton("Scan", rootRect, "Scout", Bottom, new Vector2(0, 32), new Vector2(380, 92), out var scanLabel);
            DecorateButton(scan, scanLabel, "icons/icon-scout");

            // Bang xem thanh: chan dung + ten lanh chua, luc chien, doi hinh 3x3 (cot gan thanh NPC o ben PHAI — doi xung
            // voi doi hinh minh), linh, phao, mau thanh, thuong, nut Tan cong.
            var dim = Img("PreviewDim", rootRect, UiSprite("chrome/dim-tile"), Center, Vector2.zero, Vector2.zero);
            Stretch(dim.rectTransform, 0, 0, 0, 0);
            dim.color = new Color(1f, 1f, 1f, 0.6f);
            dim.raycastTarget = true;
            var panel = Img("Panel", dim.transform, UiSprite("panels/panel-hero"), Center, Vector2.zero, new Vector2(900, 760), sliced: true);
            panel.raycastTarget = true;
            var portraitFrame = Img("Portrait", panel.transform, UiSprite("panels/portrait-frame-big"), TopLeft, new Vector2(56, -56), new Vector2(160, 160), sliced: true);
            var portrait = Img("Avatar", portraitFrame.transform, null, Center, Vector2.zero, new Vector2(128, 128));
            var lordName = Label("Name", panel.transform, "", 34, TopLeft, new Vector2(240, -70), new Vector2(600, 48));
            lordName.alignment = TextAnchor.MiddleLeft;
            var power = Label("Power", panel.transform, "", 28, TopLeft, new Vector2(240, -126), new Vector2(600, 44));
            power.alignment = TextAnchor.MiddleLeft;
            var castleInfo = Label("Castle", panel.transform, "", 24, TopLeft, new Vector2(240, -172), new Vector2(600, 40));
            castleInfo.alignment = TextAnchor.MiddleLeft;

            Label("LineupTitle", panel.transform, "Formation", 26, TopLeft, new Vector2(56, -236), new Vector2(300, 40)).alignment = TextAnchor.MiddleLeft;
            var cells = new Object[9];
            for (int i = 0; i < 9; i++)
            {
                int row = i / 3, column = 2 - i % 3;
                var cell = Img($"Cell{i + 1}", panel.transform, UiSprite("panels/portrait-frame-big"), TopLeft,
                    new Vector2(56 + column * 96, -284 - row * 96), new Vector2(88, 88), sliced: true);
                cells[i] = Img("Avatar", cell.transform, null, Center, Vector2.zero, new Vector2(68, 68));
            }
            Object[] IconRow(string name, string label, float y)
            {
                Label(name + "Title", panel.transform, label, 26, TopLeft, new Vector2(420, y), new Vector2(200, 40)).alignment = TextAnchor.MiddleLeft;
                var icons = new Object[3];
                for (int i = 0; i < 3; i++)
                {
                    var slot = Img($"{name}{i + 1}", panel.transform, UiSprite("panels/portrait-frame-big"), TopLeft,
                        new Vector2(420 + i * 96, y - 48), new Vector2(88, 88), sliced: true);
                    icons[i] = Img("Icon", slot.transform, null, Center, Vector2.zero, new Vector2(68, 68));
                }
                return icons;
            }
            var soldierIcons = IconRow("Soldier", "Soldiers", -236);
            var cannonIcons = IconRow("Cannon", "Cannon", -392);
            var reward = Label("Reward", panel.transform, "", 26, Bottom, new Vector2(0, 128), new Vector2(800, 44));
            var attack = MakeButton("Attack", panel.transform, "Attack", Bottom, new Vector2(140, 28), new Vector2(260, 84), out var attackLabel);
            var previewClose = MakeButton("Back", panel.transform, "Back", Bottom, new Vector2(-140, 28), new Vector2(260, 84), out _);

            var soldiers = AssetDatabase.FindAssets("t:SoldierDefinition", new[] { SoldierDir })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).Select(AssetDatabase.LoadAssetAtPath<SoldierDefinition>).ToArray();
            var cannons = AssetDatabase.FindAssets("t:CannonDefinition", new[] { DataDir })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).Select(AssetDatabase.LoadAssetAtPath<CannonDefinition>).ToArray();

            SetRef(screen, "waves", waves);
            SetRef(screen, "economy", economy);
            SetRef(screen, "tuning", AssetDatabase.LoadAssetAtPath<WaveTuning>(DataDir + "/WaveTuning.asset"));
            SetRef(screen, "gacha", AssetDatabase.LoadAssetAtPath<GachaConfig>(DataDir + "/GachaConfig.asset"));
            SetRef(screen, "equipment", AssetDatabase.LoadAssetAtPath<EquipmentConfig>(EquipmentConfigPath));
            SetRef(screen, "hud", hud);
            SetRef(screen, "cannonBoard", Object.FindObjectOfType<CannonBoard>());
            SetRefs(screen, "heroes", roster);
            SetRefs(screen, "cannons", cannons);
            SetRefs(screen, "soldiers", soldiers);
            SetRef(screen, "root", rootRect.gameObject);
            SetRef(screen, "map", map);
            SetRef(screen, "fog", fog.gameObject);
            SetRef(screen, "scanRing", ring.rectTransform);
            SetRef(screen, "scanButton", scan);
            SetRef(screen, "scanLabel", scanLabel);
            SetRef(screen, "infoText", info);
            SetRef(screen, "closeButton", close);
            SetRefs(screen, "tokens", tokens);
            SetRefs(screen, "tokenLabels", tokenLabels);
            SetRefs(screen, "tierTokens", new Object[] { UiSprite("raid/map-castle-npc-small"), UiSprite("raid/map-castle-npc-mid"), UiSprite("raid/map-castle-npc-large") });
            SetRef(screen, "preview", dim.gameObject);
            SetRef(screen, "lordPortrait", portrait);
            SetRef(screen, "lordName", lordName);
            SetRef(screen, "powerText", power);
            SetRefs(screen, "heroCells", cells);
            SetRefs(screen, "soldierIcons", soldierIcons);
            SetRefs(screen, "cannonIcons", cannonIcons);
            SetRef(screen, "castleText", castleInfo);
            SetRef(screen, "rewardText", reward);
            SetRef(screen, "attackButton", attack);
            SetRef(screen, "attackLabel", attackLabel);
            SetRef(screen, "previewClose", previewClose);
            SetRefs(screen, "lordPortraits", new Object[] { UiSprite("avatars/avatar-lord-baron"), UiSprite("avatars/avatar-lord-countess"), UiSprite("avatars/avatar-lord-young") });
            SetRef(hud, "conquestMap", screen);
        }

        /// <summary>
        /// Man Nhiem vu Hanh trinh (GDD muc 11): mot moc mot lan — 3 nhiem vu + o thuong cuoi moc, moi dong co thanh
        /// tien do va nut Claim (hoac dau tick khi da nhan). O thuong cuoi moc co the la VE chon tuong ("sr"/"ssr"),
        /// bam Claim thi mo bang chon ben duoi.
        /// Dung lai chrome dang co (`panel-hero`, `title-ribbon`, `stat-strip`/`reward-row`, `bar-frame-thin`,
        /// `btn-green`, `icon-check`) — khong sinh art moi; do o `tempart/quest-ui-audit/check_ui_assets.py`.
        /// </summary>
        /// <summary>
        /// Town (GDD 10.4): hub mo cac cong trinh con. Phai dung TRUOC `BuildSkillBoardScreen` trong hierarchy —
        /// anh em ve sau nam DE tren, nen dong man con la thay lai hub (khong phai mo lai tu dau).
        /// Cong trinh Shop KHONG phai man con: no la scene rieng (`BuildShopScene`, `SceneNames.Shop`).
        /// </summary>
        static TownScreen BuildTownScreen(RectTransform canvas)
        {
            var rootRect = FullScreen("TownScreen", canvas);
            var screen = rootRect.gameObject.AddComponent<TownScreen>();
            var dim = Img("Dim", rootRect, UiSprite("chrome/dim-tile"), Center, Vector2.zero, Vector2.zero);
            Stretch(dim.rectTransform, 0, 0, 0, 0);
            dim.color = new Color(0f, 0f, 0f, 0.72f);
            dim.raycastTarget = true;

            var panel = Img("Panel", rootRect, UiSprite("panels/panel-hero"), Center, Vector2.zero, new Vector2(1080, 780), sliced: true);
            panel.raycastTarget = true;
            var title = Img("Title", panel.transform, UiSprite("panels/title-ribbon"), Top, new Vector2(0, 30), new Vector2(380, 68), sliced: true);
            Label("Value", title.transform, "Town", 36, Center, Vector2.zero, new Vector2(360, 68));

            Label("Subtitle", panel.transform, "Prepare your army between battles", 24, Top, new Vector2(0, -72), new Vector2(960, 44));

            Button Building(string name, string caption, string description, string action, string iconPath, float x)
            {
                var button = MakeButton(name, panel.transform, "", Top, new Vector2(x, -144), new Vector2(440, 440), out var label,
                    UiSprite("panels/portrait-frame-big"));
                label.text = caption;
                label.rectTransform.anchorMin = label.rectTransform.anchorMax = label.rectTransform.pivot = Top;
                label.rectTransform.anchoredPosition = new Vector2(0, -220);
                label.rectTransform.sizeDelta = new Vector2(400, 48);
                label.fontSize = PixelFont.Snap(32);
                var icon = Img("Icon", button.transform, UiSprite(iconPath), Top, new Vector2(0, -24), new Vector2(192, 192));
                icon.preserveAspect = true;
                var detail = Label("Description", button.transform, description, 22, Top, new Vector2(0, -276), new Vector2(380, 64));
                detail.horizontalOverflow = HorizontalWrapMode.Wrap;
                var actionBar = Img("Action", button.transform, UiSprite("panels/stat-strip"), Bottom, new Vector2(0, 24), new Vector2(352, 60), sliced: true);
                Label("Value", actionBar.transform, action, 26, Center, Vector2.zero, new Vector2(336, 60));
                return button;
            }
            var shopButton = Building("ShopBuilding", "Shop", "Shards, equipment and special offers\nDaily / Weekly / Monthly", "Browse Shop", "lobby/menu-shop", -236);
            var boardButton = Building("BoardBuilding", "Skill Board", "Permanent army upgrades\nAttack / Health / Attack speed", "Upgrade Army", "town/training-hall", 236);

            SetRef(screen, "progressText", Label("Hint", panel.transform, "", 22, Bottom, new Vector2(0, 132), new Vector2(960, 68)));
            var close = MakeButton("Close", panel.transform, "Back to Lobby", Bottom, new Vector2(0, 36), new Vector2(320, 76), out _);

            SetRef(screen, "root", rootRect.gameObject);
            SetRef(screen, "shopButton", shopButton);
            SetRef(screen, "boardButton", boardButton);
            SetRef(screen, "closeButton", close);
            return screen;
        }

        /// <summary>Bang ky nang toan tai khoan (`Meta/SkillBoard`): 3 nhanh co dinh, moi nhanh 10 cap.</summary>
        static SkillBoardScreen BuildSkillBoardScreen(RectTransform canvas, Economy economy)
        {
            const int Rows = 3;   // = so gia tri cua `BoardNode`
            var rootRect = FullScreen("SkillBoardScreen", canvas);
            var screen = rootRect.gameObject.AddComponent<SkillBoardScreen>();
            var dim = Img("Dim", rootRect, UiSprite("chrome/dim-tile"), Center, Vector2.zero, Vector2.zero);
            Stretch(dim.rectTransform, 0, 0, 0, 0);
            dim.color = new Color(0f, 0f, 0f, 0.72f);
            dim.raycastTarget = true;

            var panel = Img("Panel", rootRect, UiSprite("panels/panel-hero"), Center, Vector2.zero, new Vector2(1100, 780), sliced: true);
            panel.raycastTarget = true;
            var title = Img("Title", panel.transform, UiSprite("panels/title-ribbon"), Top, new Vector2(0, 30), new Vector2(420, 68), sliced: true);
            Label("Value", title.transform, "Skill Board", 36, Center, Vector2.zero, new Vector2(400, 68));
            var summary = Label("Summary", panel.transform, "", 26, Top, new Vector2(0, -66), new Vector2(1000, 48));
            Img("GoldIcon", panel.transform, UiSprite("icons/icon-gold"), Top, new Vector2(-190, -132), new Vector2(44, 44));
            var gold = Label("Gold", panel.transform, "0", 30, Top, new Vector2(-40, -132), new Vector2(240, 48));
            gold.alignment = TextAnchor.MiddleLeft;

            var rowRoots = new Object[Rows];
            var rowNames = new Object[Rows];
            var rowBonuses = new Object[Rows];
            var rowButtons = new Object[Rows];
            var rowLabels = new Object[Rows];
            var rankBars = new Object[Rows];
            for (int i = 0; i < Rows; i++)
            {
                var row = Img($"Row{i + 1}", panel.transform, UiSprite("panels/stat-strip"), Center, new Vector2(0, 144f - i * 132f), new Vector2(1000, 120), sliced: true);
                var icon = Img("UpgradeIcon", row.transform, UiSprite("icons/" + new[] { "icon-board-attack", "icon-board-health", "icon-board-haste" }[i]), MidLeft, new Vector2(22, 0), new Vector2(90, 90));
                icon.preserveAspect = true;
                var name = Label("Name", row.transform, "", 26, MidLeft, new Vector2(128, 30), new Vector2(590, 40));
                name.alignment = TextAnchor.MiddleLeft;
                var bonus = Label("Bonus", row.transform, "", 24, MidLeft, new Vector2(128, -10), new Vector2(590, 34));
                bonus.alignment = TextAnchor.MiddleLeft;
                var buy = MakeButton($"Upgrade{i + 1}", row.transform, "", MidRight, new Vector2(-16, 0), new Vector2(240, 96), out var buyLabel);
                var bar = Img("RankBar", row.transform, UiSprite("chrome/bar-fill-shard"), MidLeft, new Vector2(132, -44), new Vector2(540, 10));
                bar.type = Image.Type.Filled; bar.fillMethod = Image.FillMethod.Horizontal;
                rankBars[i] = bar;
                rowRoots[i] = row.gameObject;
                rowNames[i] = name;
                rowBonuses[i] = bonus;
                rowButtons[i] = buy;
                rowLabels[i] = buyLabel;
            }

            var hint = Label("Hint", panel.transform, "", 24, Bottom, new Vector2(0, 150), new Vector2(1000, 48));
            hint.text = "Army HP and speed affect heroes and soldiers; attack also boosts cannons.\nBuy permanent ranks with Gold. No reset when you leave.";
            hint.rectTransform.sizeDelta = new Vector2(1000, 76);
            var close = MakeButton("Close", panel.transform, "Back to Town", Bottom, new Vector2(0, 40), new Vector2(320, 76), out _);

            SetRef(screen, "economy", economy);
            SetRef(screen, "root", rootRect.gameObject);
            SetRef(screen, "goldText", gold);
            SetRef(screen, "summaryText", summary);
            SetRef(screen, "closeButton", close);
            SetRefs(screen, "rowRoots", rowRoots);
            SetRefs(screen, "rowNames", rowNames);
            SetRefs(screen, "rankBars", rankBars);
            SetRefs(screen, "rowBonuses", rowBonuses);
            SetRefs(screen, "rowButtons", rowButtons);
            SetRefs(screen, "rowLabels", rowLabels);
            return screen;
        }

        static QuestScreen BuildQuestScreen(RectTransform canvas, WaveManager waves, Economy economy, BattleHud hud, HeroDefinition[] roster)
        {
            var rootRect = FullScreen("QuestScreen", canvas);
            var screen = rootRect.gameObject.AddComponent<QuestScreen>();

            var dim = Img("Dim", rootRect, UiSprite("chrome/dim-tile"), Center, Vector2.zero, Vector2.zero);
            Stretch(dim.rectTransform, 0, 0, 0, 0);
            dim.color = new Color(0f, 0f, 0f, 0.72f);
            dim.raycastTarget = true;   // chan cham xuyen xuong san

            var panel = Img("Panel", rootRect, UiSprite("panels/panel-hero"), Center, Vector2.zero, new Vector2(1100, 840), sliced: true);
            panel.raycastTarget = true;
            var title = Img("Title", panel.transform, UiSprite("panels/title-ribbon"), Top, new Vector2(0, 30), new Vector2(380, 68), sliced: true);
            Label("Value", title.transform, "Journey", 36, Center, Vector2.zero, new Vector2(360, 68));
            var chapter = Label("Chapter", panel.transform, "", 30, Top, new Vector2(0, -60), new Vector2(900, 48));

            var chapterButtons = new Object[QuestCatalog.StageCount];
            for (int i = 0; i < chapterButtons.Length; i++)
                chapterButtons[i] = MakeButton("Chapter" + (i + 1), panel.transform, (i + 1).ToString(), Top, new Vector2((i - 3) * 128, -118), new Vector2(108, 58), out _);
            SetRefs(screen, "chapterButtons", chapterButtons);
            var rewardIcons = new Object[QuestCatalog.RowsPerStage];
            var rowTitles = new Object[QuestCatalog.RowsPerStage];
            var rowRewards = new Object[QuestCatalog.RowsPerStage];
            var rowProgress = new Object[QuestCatalog.RowsPerStage];
            var rowBars = new Object[QuestCatalog.RowsPerStage];
            var rowClaims = new Object[QuestCatalog.RowsPerStage];
            var rowChecks = new Object[QuestCatalog.RowsPerStage];
            for (int i = 0; i < QuestCatalog.RowsPerStage; i++)
            {
                // Dong cuoi = o THUONG CUOI MOC: nen rieng + quang am de tach khoi 3 dong nhiem vu.
                bool chapterRow = i == QuestCatalog.RowsPerStage - 1;
                var row = Img($"Row{i + 1}", panel.transform, UiSprite(chapterRow ? "panels/reward-row" : "panels/stat-strip"),
                    Center, new Vector2(0, 190f - i * 128f), new Vector2(1000, 120), sliced: true);
                if (chapterRow) row.color = new Color(1f, 0.94f, 0.78f);

                var rewardIcon = Img("RewardIcon", row.transform, UiSprite("icons/icon-quest-scroll"), MidLeft, new Vector2(16, 0), new Vector2(72, 72));
                rewardIcon.preserveAspect = true; rewardIcons[i] = rewardIcon;
                var titleText = Label("Title", row.transform, "", 26, MidLeft, new Vector2(102, 32), new Vector2(380, 46), MidLeft);
                titleText.alignment = TextAnchor.MiddleLeft;
                var rewardText = Label("Reward", row.transform, "", 22, MidLeft, new Vector2(102, -28), new Vector2(380, 42), MidLeft);
                rewardText.alignment = TextAnchor.MiddleLeft;
                var progressText = Label("Progress", row.transform, "", 26, MidRight, new Vector2(-270, 36), new Vector2(260, 46), MidRight);
                progressText.alignment = TextAnchor.MiddleRight;

                var barFrame = Img("Bar", row.transform, UiSprite("chrome/bar-frame-thin"), MidRight, new Vector2(-270, -36), new Vector2(260, 20), sliced: true);
                var barFill = Img("Fill", barFrame.transform, UiSprite("chrome/bar-fill-shard"), Center, Vector2.zero, Vector2.zero);
                Stretch(barFill.rectTransform, 4, 4, 4, 4);
                barFill.type = Image.Type.Filled;
                barFill.fillMethod = Image.FillMethod.Horizontal;

                var claim = MakeButton($"Claim{i + 1}", row.transform, "Claim", MidRight, new Vector2(-16, 0), new Vector2(210, 92), out _);
                var check = Img($"Check{i + 1}", row.transform, UiSprite("icons/icon-check"), MidRight, new Vector2(-24, 0), new Vector2(72, 72));

                rowTitles[i] = titleText;
                rowRewards[i] = rewardText;
                rowProgress[i] = progressText;
                rowBars[i] = barFill;
                rowClaims[i] = claim;
                rowChecks[i] = check.gameObject;
            }
            var hint = Label("Hint", panel.transform, "", 24, Bottom, new Vector2(0, 150), new Vector2(1000, 48));
            var close = MakeButton("Close", panel.transform, "Close", Bottom, new Vector2(0, 40), new Vector2(240, 76), out _);

            // Bang chon tuong cho VE (4 o "sr" / 8 o "ssr"). "Later" de chon sau — ve nam trong save nen khong mat.
            var voucherDim = Img("VoucherDim", rootRect, UiSprite("chrome/dim-tile"), Center, Vector2.zero, Vector2.zero);
            Stretch(voucherDim.rectTransform, 0, 0, 0, 0);
            voucherDim.color = new Color(0f, 0f, 0f, 0.78f);
            voucherDim.raycastTarget = true;
            var voucher = Img("VoucherPanel", voucherDim.transform, UiSprite("panels/panel-hero"), Center, Vector2.zero, new Vector2(1000, 700), sliced: true);
            voucher.raycastTarget = true;
            var voucherTitle = Img("Title", voucher.transform, UiSprite("panels/title-ribbon"), Top, new Vector2(0, 34), new Vector2(480, 68), sliced: true);
            Label("Value", voucherTitle.transform, "Choose Your Hero", 32, Center, Vector2.zero, new Vector2(460, 68));

            const int Slots = 8;
            var voucherSlots = new Object[Slots];
            var voucherAvatars = new Object[Slots];
            var voucherLabels = new Object[Slots];
            for (int i = 0; i < Slots; i++)
            {
                var position = new Vector2((i % 4 - 1.5f) * 236f, 90f - i / 4 * 280f);
                var slot = MakeButton($"Pick{i + 1}", voucher.transform, "", Center, position, new Vector2(220, 260), out var slotLabel,
                    UiSprite("panels/portrait-frame-big"));
                slotLabel.rectTransform.anchorMin = slotLabel.rectTransform.anchorMax = slotLabel.rectTransform.pivot = Bottom;
                slotLabel.rectTransform.anchoredPosition = new Vector2(0, 14);
                slotLabel.rectTransform.sizeDelta = new Vector2(212, 76);
                slotLabel.fontSize = PixelFont.Snap(22);
                voucherAvatars[i] = Img("Avatar", slot.transform, null, Center, new Vector2(0, 34), new Vector2(150, 150));
                voucherSlots[i] = slot;
                voucherLabels[i] = slotLabel;
            }
            var voucherClose = MakeButton("Later", voucher.transform, "Later", Bottom, new Vector2(0, 26), new Vector2(220, 72), out _);

            SetRef(screen, "waves", waves);
            SetRef(screen, "economy", economy);
            SetRef(screen, "hud", hud);
            SetRef(screen, "gacha", AssetDatabase.LoadAssetAtPath<GachaConfig>(DataDir + "/GachaConfig.asset"));
            SetRefs(screen, "roster", roster);
            SetRef(screen, "root", rootRect.gameObject);
            SetRef(screen, "chapterText", chapter);
            SetRef(screen, "hintText", hint);
            SetRef(screen, "closeButton", close);
            SetRefs(screen, "rewardIcons", rewardIcons);
            SetRef(screen, "questIcon", UiSprite("icons/icon-quest-scroll"));
            SetRef(screen, "goldRewardIcon", UiSprite("icons/icon-gold"));
            SetRef(screen, "shardRewardIcon", UiSprite("icons/icon-shard"));
            SetRef(screen, "voucherRewardIcon", UiSprite("icons/icon-tab-hero"));
            SetRefs(screen, "rowTitles", rowTitles);
            SetRefs(screen, "rowRewards", rowRewards);
            SetRefs(screen, "rowProgress", rowProgress);
            SetRefs(screen, "rowBars", rowBars);
            SetRefs(screen, "rowClaims", rowClaims);
            SetRefs(screen, "rowChecks", rowChecks);
            SetRef(screen, "voucherPanel", voucherDim.gameObject);
            SetRefs(screen, "voucherSlots", voucherSlots);
            SetRefs(screen, "voucherAvatars", voucherAvatars);
            SetRefs(screen, "voucherLabels", voucherLabels);
            SetRef(screen, "voucherClose", voucherClose);
            return screen;
        }

        // Lop dong nen san co (Background/Field/, nguoi dung duyet 2026-10-03; cat bang tempart/field-anim/deliver.py):
        // tuc co lay dat san, chim + buom do FieldAmbience chay. Toa do px tren bg-field; vi tri tuc co la gia dinh cua Claude.
        static GameObject BuildFieldAmbience()
        {
            const string dir = ArtRoot + "Background/Field/";
            Sprite[] Frames(string name, int count) =>
                Enumerable.Range(1, count).Select(i => LoadSprite($"{dir}{name}/{name}-{i}.png")).ToArray();

            var root = new GameObject("FieldAmbience");
            // Goc tuc co (px) — sprite 14x32 pivot giua, day sprite dat tai goc.
            var tufts = new[] { (520f, 292f), (860f, 770f), (1110f, 300f), (1350f, 780f), (1590f, 288f), (1820f, 760f), (700f, 520f), (1480f, 540f) };
            foreach (var (x, y) in tufts)
            {
                var tuft = NewSprite("GrassTuft", root.transform, null, SortOrder.Background + 1);
                tuft.transform.position = PxToWorld(x, y - 16f);
                tuft.gameObject.AddComponent<BackdropLoop>().Setup(Frames("grass", 8), 6f);
            }

            var ambience = root.AddComponent<FieldAmbience>();
            var so = new SerializedObject(ambience);
            so.FindProperty("birdBandY").vector2Value = new Vector2(PxToWorld(0f, 230f).y, PxToWorld(0f, 160f).y);
            so.FindProperty("left").floatValue = PxToWorld(0f, 0f).x;
            so.FindProperty("right").floatValue = PxToWorld(RefWidth, 0f).x;
            var areaMin = PxToWorld(450f, 760f);
            var areaMax = PxToWorld(1800f, 300f);
            so.FindProperty("butterflyArea").rectValue = UnityEngine.Rect.MinMaxRect(areaMin.x, areaMin.y, areaMax.x, areaMax.y);
            so.FindProperty("order").intValue = SortOrder.Background + 3;
            so.ApplyModifiedPropertiesWithoutUndo();
            SetRefs(ambience, "birdFrames", Frames("bird", 4));
            SetRefs(ambience, "butterflyFrames", Frames("butterfly", 4));
            return root;
        }

        /// <summary>
        /// Lop dong cua mot biome khong co `FieldAmbience` (dam lay / dat chay / tuyet): moi vat the mot bo frame lap,
        /// chay theo kieu chuyen dong trong mot khung world. Ten thu muc lay theo `Background/<Biome>/<prop>/<prop>-N.png`,
        /// cung kieu voi `Background/Field/`.
        /// </summary>
        static GameObject BuildBiomeDrift(string name, params DriftAmbience.Mote[] motes)
        {
            var root = new GameObject(name);
            root.AddComponent<DriftAmbience>().motes = motes;
            return root;
        }

        /// <param name="folder">Duong dan tuong doi trong `Background/`, vd `Swamp/firefly`.</param>
        /// <param name="prefix">Tien to file frame (`firefly` -> `firefly-1.png`).</param>
        /// <param name="area">Khung chay TRONG THE GIOI (x sang phai, y len) — xem `Box`.</param>
        static DriftAmbience.Mote MoteDef(string name, string folder, string prefix, float fps, int count, UnityEngine.Rect area,
            DriftAmbience.Motion motion, float speed, float stagger)
        {
            return new DriftAmbience.Mote
            {
                name = name,
                frames = Frames(ArtRoot + "Background/" + folder, prefix),
                fps = fps,
                count = count,
                area = area,
                motion = motion,
                speed = speed,
                stagger = stagger,
                order = SortOrder.Background + 2,
            };
        }

        /// <summary>Khung trong THE GIOI: goc duoi-trai (x, y) + be ngang/cao. Nen ve 1 art px = 1 px man nen 1 unit = 100 px.</summary>
        static UnityEngine.Rect Box(float xMin, float yMin, float width, float height) =>
            UnityEngine.Rect.MinMaxRect(xMin, yMin, xMin + width, yMin + height);

        // Nen Chinh Phat (Background/Conquest/, nguoi dung duyet g1 2026-10-01): canvas 2880x1620 cung ti le 1 px = 0,01 unit        // nhu tran wave, camera lui 1,5x thay ca tam. Thanh minh dung yen: dat nen sao cho phao dai tren nen (mep alpha trai
        // px 204, tam bbox alpha y 805 = giua duong dat ngang hang 728-882) trung phao dai dang dung. So do o
        // tempart/conquest-bg/process.py + anim/preview.html.
        const float ConquestWidth = 2880f;
        const float ConquestHeight = 1620f;
        const float ConquestFortressLeftPx = 204f;
        const float ConquestRoadPx = 805f;
        const float FortressTopInSprite = 2f;
        // San co giua hang coc/leu tren (het ~y 270) va hang cay duoi (dinh cay y 1236).
        const float ConquestLaneTopPx = 300f;
        const float ConquestLaneBottomPx = 1225f;

        static void BuildConquestArena(GameObject[] waveBackdrop)
        {
            const string dir = ArtRoot + "Background/Conquest/";
            float fortressCentreWavePx = FortressBasePx - (FortressBaseInSprite - (FortressTopInSprite + FortressBaseInSprite) / 2f) * FortressScale;
            var centre = new Vector3(
                PxToWorld(FortressLeftPx, 0f).x - (ConquestFortressLeftPx - ConquestWidth / 2f) / Ppu,
                PxToWorld(0f, fortressCentreWavePx).y - (ConquestHeight / 2f - ConquestRoadPx) / Ppu, 0f);
            Vector3 Px(float px, float py) => centre + new Vector3((px - ConquestWidth / 2f) / Ppu, (ConquestHeight / 2f - py) / Ppu, 0f);
            Sprite[] Frames(string name, int count) =>
                Enumerable.Range(1, count).Select(i => LoadSprite($"{dir}{name}/{name}-{i}.png")).ToArray();

            var root = new GameObject("ConquestBackdrop");
            NewSprite("Background", root.transform, LoadSprite(dir + "bg-conquest.png"), SortOrder.Background).transform.position = centre;
            NewSprite("ForegroundTrees", root.transform, LoadSprite(dir + "fg-trees.png"), SortOrder.Foreground).transform.position = centre;

            // Cot co: (x cot, y dinh cot, mau) — sprite 104x64 dat goc tren-trai tai (x-2, y+6). 2 co xanh da xoa vai tren nen.
            var flags = new[] { (607f, 12f, "blue"), (941f, 14f, "brown"), (1827f, 35f, "blue"), (2141f, 20f, "brown"), (2723f, 15f, "brown") };
            foreach (var (x, y, colour) in flags)
            {
                var flag = NewSprite("Flag", root.transform, null, SortOrder.Background + 2);
                flag.transform.position = Px(x - 2f + 52f, y + 6f + 32f);
                flag.gameObject.AddComponent<BackdropLoop>().Setup(Frames("flag-" + colour, 8), 9f);
            }
            // Khoi lua trai 70x140: day giua sprite tai dong lua.
            foreach (var (x, y) in new[] { (340f, 168f), (1240f, 166f), (2330f, 170f) })
            {
                var smoke = NewSprite("CampSmoke", root.transform, null, SortOrder.Background + 1);
                smoke.color = new Color(1f, 1f, 1f, 0.85f);
                smoke.transform.position = Px(x, y - 70f);
                smoke.gameObject.AddComponent<BackdropLoop>().Setup(Frames("smoke", 8), 7f);
            }
            // Bong may (goc tren-trai px, toc do px/s), mo 14%, de len unit duoi hang cay truoc.
            var clouds = new List<Transform>();
            foreach (var (name, x, y) in new[] { ("cloud-shadow-1", 200f, 520f), ("cloud-shadow-2", 1700f, 930f) })
            {
                var cloud = NewSprite("CloudShadow", root.transform, LoadSprite(dir + name + ".png"), SortOrder.Foreground - 1);
                cloud.color = new Color(1f, 1f, 1f, 0.14f);
                var size = cloud.sprite != null ? cloud.sprite.rect.size : Vector2.zero;
                cloud.transform.position = Px(x + size.x / 2f, y + size.y / 2f);
                clouds.Add(cloud.transform);
            }
            root.SetActive(false);

            // Lop dieu khien nam ngoai nen: object tat thi Awake khong chay, ConquestArena.Show se khong tim thay no.
            var arena = new GameObject("ConquestArena").AddComponent<ConquestArena>();
            var so = new SerializedObject(arena);
            so.FindProperty("conquestBackdrop").objectReferenceValue = root;
            so.FindProperty("cameraCentre").vector3Value = centre;
            so.FindProperty("cameraSize").floatValue = ConquestHeight / 2f / Ppu;
            so.FindProperty("laneTop").floatValue = Px(0f, ConquestLaneTopPx).y;
            so.FindProperty("laneBottom").floatValue = Px(0f, ConquestLaneBottomPx).y;
            var speeds = so.FindProperty("cloudSpeeds");
            speeds.arraySize = 2;
            speeds.GetArrayElementAtIndex(0).floatValue = 0.22f;
            speeds.GetArrayElementAtIndex(1).floatValue = 0.17f;
            so.FindProperty("crowOrder").intValue = SortOrder.Background + 3;
            so.FindProperty("crowBandY").vector2Value = new Vector2(Px(0f, 89f).y, Px(0f, 29f).y);
            so.FindProperty("left").floatValue = Px(0f, 0f).x;
            so.FindProperty("right").floatValue = Px(ConquestWidth, 0f).x;
            so.ApplyModifiedPropertiesWithoutUndo();
            SetRefs(arena, "waveBackdrop", waveBackdrop);
            SetRefs(arena, "clouds", clouds.ToArray());
            SetRefs(arena, "crowFrames", Frames("crow", 4));
        }

        // Project dung Input System MOI (activeInputHandler = 1): StandaloneInputModule se nem loi moi frame.
        static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        static void CreateCamera(Color background)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = RefHeight / 2f / Ppu;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            go.transform.position = new Vector3(0f, 0f, -10f);
            // Sprite cung sortingOrder thi con nao chan THAP hon (gan camera hon) ve de len con o sau.
            // Dat o GraphicsSettings (luu vao ProjectSettings) vi Camera.transparencySortMode KHONG
            // duoc serialize vao scene — gan tren camera thi mo lai scene la mat.
            GraphicsSettings.transparencySortMode = TransparencySortMode.CustomAxis;
            GraphicsSettings.transparencySortAxis = Vector3.up;
            go.AddComponent<AudioListener>();
        }

        static SpriteRenderer NewSprite(string name, Transform parent, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        static Vector3 PxToWorld(float px, float py) =>
            new Vector3((px - RefWidth / 2f) / Ppu, (RefHeight / 2f - py) / Ppu, 0f);

        // ---------------------------------------------------------------- asset

        static Sprite UiSprite(string relative) => LoadSprite(UiDir + relative + ".png");

        /// <summary>Anh identity dung tam lam avatar / icon khi unit chua co tranh trong UI/avatars/.</summary>
        static bool IsIdentityPlaceholder(Sprite sprite) =>
            sprite != null && AssetDatabase.GetAssetPath(sprite).EndsWith("/identity/identity-1.png");

        static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Debug.LogWarning($"[SceneBuilder] Khong tim thay sprite {path}");
            return sprite;
        }

        /// <summary>Tao (neu chua co) bo 5 clip + clip skill tuy chon. Asset da co: chi bu clip skill con trong.</summary>
        static SpriteAnimationSet AnimSet(string id, string root, (string folder, string action) attack,
            (string folder, string action)? skill = null, string idle = "idle", string walk = "walk", int hit = 0, int skillHit = 0)
        {
            string dir = ArtRoot + root;
            var anim = LoadOrCreate<SpriteAnimationSet>($"{DataDir}/Anim_{id}.asset", out bool created);
            anim.idle.frames = Refill(dir + idle, "idle", anim.idle.frames);
            anim.walk.frames = Refill(dir + walk, "walk", anim.walk.frames);
            anim.attack.frames = Refill(dir + attack.folder, attack.action, anim.attack.frames);
            anim.hurt.frames = Refill(dir + "hurt", "hurt", anim.hurt.frames);
            anim.death.frames = Refill(dir + "death", "death", anim.death.frames);
            if (created) anim.attack.hitFrame = hit;
            if (skill != null && !anim.skill.IsValid)
            {
                anim.skill.frames = Frames(dir + skill.Value.folder, skill.Value.action);
                anim.skill.hitFrame = skillHit;
            }
            EditorUtility.SetDirty(anim);
            return anim;
        }

        static HeroDefinition HeroDef(string id, string avatar, SpriteAnimationSet anim, Action<HeroDefinition> stats,
            SkillKind skillKind, Action<HeroDefinition> skill, string folder = DataDir)
        {
            var def = LoadOrCreate<HeroDefinition>($"{folder}/Hero_{id}.asset", out bool created);
            if (created)
            {
                def.displayName = id;
                def.animations = anim;
                stats(def);
            }
            // Asset tao truoc khi co SkillKind (Frostweaver, Cragfist) mang skillKind None: ghi khoi ky nang
            // MOT lan. Da co loai roi thi giu nguyen so chinh tay trong Inspector.
            if (skillKind != SkillKind.None && def.skillKind == SkillKind.None)
            {
                def.skillKind = skillKind;
                skill(def);
            }
            if (def.avatar == null && avatar != null)
                def.avatar = LoadSprite($"{UiDir}avatars/{avatar}.png");
            if (string.IsNullOrEmpty(def.unitId) && HeroUnitIds.TryGetValue(id, out string unitId)) def.unitId = unitId;
            // Ten trong khong bao gio duoc song sot qua mot lan build: asset tao tay trong Inspector (hoac o luot build
            // loi) mang ten mac dinh cua lop, ma bang chon hero lai bake ten do luc build scene — loi chi lo ra o lan
            // build sau. Ten that do `NamedHero` ghi; day chi la luoi an toan.
            if (string.IsNullOrEmpty(def.displayName)) def.displayName = id;
            EditorUtility.SetDirty(def);
            return def;
        }

        /// <summary>
        /// Ghi TEN HIEN THI moi lan build. `HeroDef` chi chay `stats` (nơi cac helper khac dat ten) khi **tao** asset,
        /// nen asset co san se giu ten cu/mac dinh. Dot 2026-10-09: dong "Hau Nghe" tren bang chon hero hien chu
        /// "Frostweaver" (ten mac dinh cua lop) vi asset tao o luot build loi truoc do, scene chi duoc dung lai sau.
        /// </summary>
        static HeroDefinition NamedHero(HeroDefinition hero, string name)
        {
            if (hero.displayName == name) return hero;
            hero.displayName = name;
            EditorUtility.SetDirty(hero);
            return hero;
        }

        /// <summary>
        /// Hero -> id `UnitDefinition` gacha (dat duoc khi save so huu id nay). Vespera co id nhung CHUA vao pool gacha
        /// (bo chua xong) nen chi Admin panel cho so huu de test.
        /// </summary>
        static readonly Dictionary<string, string> HeroUnitIds = new Dictionary<string, string>
        {
            { "Ironblade", "knight" }, { "Hawkfletch", "archer" }, { "Frostweaver", "mage_elemental" },
            { "Bonecaller", "mage_summoner" }, { "Cragfist", "cragfist" }, { "Chainwarden", "chainwarden" },
            { "Vespera", "vespera" }, { "Wyrmspear", "wyrmspear" }, { "Banneret", "banneret" }, { "Wukong", "wukong" },
            { "Stormcaller", "stormcaller" }, { "Galemonk", "galemonk" },
            { "LuuBi", "luubi" }, { "QuanVu", "quanvu" }, { "TruongPhi", "truongphi" },
            { "BogWitch", "bogwitch" }, { "ShroomKnight", "shroomknight" }, { "TritonGuard", "tritonguard" },
            { "EmberApprentice", "emberapprentice" }, { "RuneShield", "runeshield" }, { "SilkFae", "silkfae" },
            { "PetalSprite", "petalsprite" }, { "KoboldTrapper", "koboldtrapper" }, { "SigilMage", "sigilmage" },
            { "Bloodswordsman", "bloodswordsman" }, { "Soulbinder", "soulbinder" },
            { "BeetleKnight", "beetleknight" }, { "Twinblade", "twinblade" }, { "BellNun", "bellnun" },
            { "MoonHare", "moonhare" }, { "BlackSigilHunter", "blacksigilhunter" },
            { "GourdTaoist", "gourdtaoist" }, { "InkScribe", "inkscribe" },
            { "HongLien", "honglien" }, { "SaVe", "save" }, { "KhoiLoiSu", "khoiloisu" },
            { "Werewolf", "werewolf" },
            { "HauNghe", "haunghe" }, { "BachLocTien", "bachloctien" },
        };

        /// <summary>
        /// Frame cua clip da co: CHI doc lai tu thu muc khi danh sach sprite cu KHONG con la danh sach cua thu muc
        /// (thu muc bi cat lai -> guid cu chet, sprite tro vao asset da xoa; hoac so luong doi). Khop y nguyen thi
        /// giu so chinh tay trong Inspector.
        /// `AnimSet` cu chi dien frame luc TAO asset, nen mot lan cat lai thu muc la clip ket gia tri chet vinh vien:
        /// Ruinjaw walk (cat lai 2026-10-12) tro vao 6 sprite da xoa -> SpriteAnimator roi ve idle tung frame mat
        /// -> quai chop tat trong game, ma buid van bao thanh cong (nguoi dung bat 2026-10-14).
        /// </summary>
        static Sprite[] Refill(string folder, string action, Sprite[] stored)
        {
            var fresh = Frames(folder, action);
            if (fresh.Length == 0) return stored;                      // thu muc khong co clip nay
            if (stored != null && stored.Length == fresh.Length)
            {
                bool same = true;
                for (int i = 0; i < fresh.Length && same; i++) same = stored[i] == fresh[i];
                if (same) return stored;
            }
            return fresh;
        }

        /// <summary>Frame `action-N.png` trong thu muc, xep theo N (khong lay sheet-transparent.png).</summary>
        static Sprite[] Frames(string folder, string action)
        {
            // Thu muc chua ton tai (bo art chua giao, vd phao khong co `bullet/`) -> bo rong, khong de FindAssets bao loi.
            if (!AssetDatabase.IsValidFolder(folder)) return System.Array.Empty<Sprite>();
            var pattern = new Regex("^" + action + @"-(\d+)\.png$");
            return AssetDatabase.FindAssets("t:Sprite", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct()
                .Select(path => (path, match: pattern.Match(Path.GetFileName(path))))
                .Where(x => x.match.Success)
                .OrderBy(x => int.Parse(x.match.Groups[1].Value))
                .Select(x => AssetDatabase.LoadAssetAtPath<Sprite>(x.path))
                .ToArray();
        }

        static T LoadOrCreate<T>(string path, out bool created) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            created = asset == null;
            if (created)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static string ScenePath(string name) => $"{SceneDir}/{name}.unity";

        // ---------------------------------------------------------------- gan field private

        static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"[SceneBuilder] {target.GetType().Name} khong co field '{field}'"); return; }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetRefs(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"[SceneBuilder] {target.GetType().Name} khong co field '{field}'"); return; }
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetValue(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"[SceneBuilder] {target.GetType().Name} khong co field '{field}'"); return; }
            prop.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
