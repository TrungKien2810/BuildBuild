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
    public static class SceneBuilder
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

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ReimportArt();
            EnsureFolder(DataDir + "/Units");
            EnsureFolder(PrefabDir);

            var tuning = BuildWaveData(out var enemyPrefab);
            BuildNewEnemies(tuning);
            BuildOrcAndRat(tuning);
            BuildHeroData();
            BuildVespera();
            BuildCannons();
            var gacha = BuildGachaData();
            BuildSoldiers();
            BuildChainwarden();
            BuildWyrmspear();
            BuildBanneret();
            FillSkillTexts();
            BuildEquipmentConfig();
            AssetDatabase.SaveAssets();

            BuildBattleScene(tuning, enemyPrefab);
            BuildGachaScene(gacha);
            BuildUnitsScene();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath(SceneNames.Battle), true),
                new EditorBuildSettingsScene(ScenePath(SceneNames.Gacha), true),
                new EditorBuildSettingsScene(ScenePath(SceneNames.Units), true),
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
                    d.displayName = "Lính xương";
                    d.maxHealth = 150f; d.damage = 12f; d.attackRate = 1f; d.range = 0.5f; d.moveSpeed = 1f;
                    d.projectileSpeed = 20f; d.splashRadius = 0.3f;
                },
                SkillKind.None, null, summons);
            var bigSkeleton = HeroDef("BigSkeleton", null,
                AnimSet("BigSkeleton", "Bonecaller-BigSkeleton/", attack: ("attack", "attack"), hit: 4),
                d =>
                {
                    d.displayName = "Xương lớn";
                    d.maxHealth = 700f; d.damage = 40f; d.attackRate = 0.7f; d.range = 0.9f; d.moveSpeed = 0.7f;
                    d.projectileSpeed = 20f; d.splashRadius = 0.3f;
                },
                SkillKind.None, null, summons);
            // Tam van theo luat hero moi (duoi quai khap san), khong dung aggro 3,5 cua GDD — luat do da bo.
            var deathKnight = HeroDef("DeathKnight", null,
                AnimSet("DeathKnight", "Bonecaller-DeathKnight/", attack: ("attack", "attack"), skill: ("charge", "charge"), hit: 4),
                d =>
                {
                    d.displayName = "Kỵ sĩ tử thần";
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
                Unit("knight", "Hiệp sĩ", UnitCategory.Hero, Rarity.N, "avatars/avatar-ironblade"),
                Unit("archer", "Cung thủ", UnitCategory.Hero, Rarity.N, "avatars/avatar-hawkfletch"),
                Unit("mage_elemental", "Pháp sư nguyên tố", UnitCategory.Hero, Rarity.N, "avatars/avatar-frostweaver"),
                Unit("mage_summoner", "Pháp sư triệu hồi", UnitCategory.Hero, Rarity.SR, "icons/icon-mage-summoner"),
                // Do hiem theo suc manh (nguoi dung giao Claude 2026-09-26): Cragfist 1400 mau + Hoa Da mien sat thuong -> R.
                Unit("cragfist", "Người đá", UnitCategory.Hero, Rarity.R, "avatars/avatar-cragfist"),
                Unit("cannon_rapid", "Pháo Liên Thanh", UnitCategory.Cannon, Rarity.N, "icons/icon-cannon-rapid"),
                Unit("cannon_blast", "Pháo Nổ", UnitCategory.Cannon, Rarity.N, "icons/icon-cannon-blast"),
                Unit("cannon_frost", "Pháo Băng", UnitCategory.Cannon, Rarity.N, "icons/icon-cannon-frost"),
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

            var wolf = SoldierDef("WarWolf", "Sói chiến",
                AnimSet("WarWolf", "WarWolf/", attack: ("attack", "attack"), hit: 3),
                d =>
                {
                    d.maxHealth = 200f; d.damage = 14f; d.attackRate = 1.2f; d.range = 0.5f; d.moveSpeed = 2f;
                    d.projectileSpeed = 20f; d.splashRadius = 0.3f;
                    d.squadSize = 3; d.summonCooldown = 12f;
                });

            var tortoise = SoldierDef("StoneTortoise", "Rùa đá khiên",
                AnimSet("StoneTortoise", "StoneTortoise/", attack: ("attack", "attack"), hit: 2),
                d =>
                {
                    d.maxHealth = 700f; d.damage = 8f; d.attackRate = 0.5f; d.range = 0.6f; d.moveSpeed = 0.6f;
                    d.projectileSpeed = 20f; d.splashRadius = 0.3f;
                    d.squadSize = 2; d.summonCooldown = 18f;
                });

            var lizard = SoldierDef("AcidLizard", "Kỳ nhông axit",
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

            // Linh co trong gacha (IDEAS #1). Do hiem theo suc manh (2026-09-26): Rua 700 mau x2 -> R, con lai N.
            // Chi NOI THEM vao pool da co.
            var units = new[]
            {
                SoldierUnit("soldier_warwolf", wolf, Rarity.N),
                SoldierUnit("soldier_stonetortoise", tortoise, Rarity.R),
                SoldierUnit("soldier_acidlizard", lizard, Rarity.N),
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

            return new[] { wolf, tortoise, lizard };
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
            var unit = Unit("chainwarden", "Cai ngục móc xích", UnitCategory.Hero, Rarity.SR, "");
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
                    d.displayName = "Bạch Địa Long";
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
            if (hero.avatar == null)
                hero.avatar = LoadSprite(ArtRoot + "UI/avatars/avatar-wyrmspear.png")
                    ?? LoadSprite(dir + "identity/identity-1.png");
            EditorUtility.SetDirty(hero);

            var unit = Unit("wyrmspear", "Long kỵ binh", UnitCategory.Hero, Rarity.SSR, "");
            if (unit.icon == null && hero.avatar != null)
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

        static SoldierDefinition SoldierDef(string id, string displayName, SpriteAnimationSet anim, Action<SoldierDefinition> stats)
        {
            var def = LoadOrCreate<SoldierDefinition>($"{SoldierDir}/Soldier_{id}.asset", out bool created);
            if (created)
            {
                def.displayName = displayName;
                def.animations = anim;
                stats(def);
            }
            def.artFacesRight = true;   // ca 3 linh ve quay PHAI (khac hero)
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
                slinger.displayName = "Goblin Ná";
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
                orc.displayName = "Orc Búa Tạ";
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
                rat.displayName = "Chuột Cống";
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
            if (hero.avatar == null) hero.avatar = LoadSprite(dir + "identity/identity-1.png");
            EditorUtility.SetDirty(hero);

            var unit = Unit("banneret", "Kỳ thủ", UnitCategory.Hero, Rarity.SR, "");
            if (unit.icon == null && hero.avatar != null)
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
            // Chua co avatar 128x128 trong UI/avatars: tam dung frame identity.
            if (vespera.avatar == null)
            {
                vespera.avatar = LoadSprite(ArtRoot + "Vespera/identity/identity-1.png");
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
                CannonDef("Rapid", "cannon_rapid", "Pháo Liên Thanh", "icon-cannon-rapid", "CannonRapid", c =>
                {
                    c.damage = 12f; c.fireRate = 2f; c.range = 8f;
                    c.projectileScale = 0.5f; c.impactScale = 0.6f;
                }),
                CannonDef("Blast", "cannon_blast", "Pháo Nổ", "icon-cannon-blast", "CannonBlast", c =>
                {
                    c.damage = 30f; c.fireRate = 0.5f; c.range = 7f; c.aoeRadius = 2f;
                    c.projectileSpeed = 10f; c.projectileScale = 0.8f; c.impactScale = 2.5f;
                }),
                CannonDef("Frost", "cannon_frost", "Pháo Băng", "icon-cannon-frost", "CannonRapid", c =>
                {
                    c.damage = 8f; c.fireRate = 1f; c.range = 7f; c.slowPercent = 0.4f; c.slowDuration = 3f;
                    c.projectileScale = 0.6f; c.impactScale = 0.8f;
                    c.tint = new Color(0.55f, 0.8f, 1f);
                }),
            };
        }

        /// <summary>Bo idle + attack cua mot thu muc art phao — khong goi AnimSet (se tim walk/hurt/death khong ton tai).</summary>
        static SpriteAnimationSet CannonAnim(string art)
        {
            string dir = ArtRoot + art + "/";
            var anim = LoadOrCreate<SpriteAnimationSet>($"{DataDir}/Anim_{art}.asset", out bool created);
            if (created)
            {
                anim.idle.frames = Frames(dir + "idle", "idle");
                anim.attack.frames = Frames(dir + "attack", "attack");
                anim.attack.hitFrame = 2;   // frame loe nong
                EditorUtility.SetDirty(anim);
            }
            return anim;
        }

        static CannonDefinition CannonDef(string id, string unitId, string displayName, string icon, string art,
            Action<CannonDefinition> stats)
        {
            var def = LoadOrCreate<CannonDefinition>($"{CannonDir}/Cannon_{id}.asset", out bool created);
            if (created)
            {
                def.unitId = unitId;
                def.displayName = displayName;
                stats(def);
            }
            // Asset cu con tro bo art muon (vd. No dung tam Lien Thanh to cam) -> doi sang art rieng, bo tint tam.
            var anim = CannonAnim(art);
            if (def.animations != anim)
            {
                if (!created) def.tint = Color.white;
                def.animations = anim;
                def.projectileFrames = Frames(ArtRoot + art + "/bullet", "projectile");
                def.impactFrames = Frames(ArtRoot + art + "/bullet-impact", "impact");
            }
            if (def.icon == null) def.icon = LoadSprite($"{UiDir}icons/{icon}.png");
            EditorUtility.SetDirty(def);
            return def;
        }

        // ---------------------------------------------------------------- scene Battle

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
            NewSprite("Background", null, LoadSprite(ArtRoot + "Background/bg-field.png"), SortOrder.Background).transform.position = PxToWorld(RefWidth / 2f, RefHeight / 2f);
            // Hang cay duoi (cat tu bg-field, y 801..1079) ve DE len chan phao dai va unit.
            NewSprite("ForegroundTrees", null, LoadSprite(ArtRoot + "Background/fg-trees.png"), SortOrder.Foreground).transform.position = PxToWorld(RefWidth / 2f, RefHeight / 2f);

            // Phao dai: goc Castle o vach phai (giua lan theo y — Enemy chi so x), Body pivot tam sprite.
            var castleGo = new GameObject("Castle");
            castleGo.transform.position = PxToWorld(FortressLeftPx + FortressWallInSprite * FortressScale, (LaneTopPx + LaneBottomPx) / 2f);
            SetValue(castleGo.AddComponent<Health>(), "maxHealth", CastleMaxHealth);
            var castle = castleGo.AddComponent<Castle>();
            var body = NewSprite("Body", castleGo.transform, LoadSprite(ArtRoot + "Castle/fortress.png"), SortOrder.Castle);
            body.transform.localScale = Vector3.one * FortressScale;
            var fortressPx = body.sprite.rect.size * FortressScale;
            float fortressTopPx = FortressBasePx - FortressBaseInSprite * FortressScale;
            body.transform.position = PxToWorld(FortressLeftPx + fortressPx.x / 2f, fortressTopPx + fortressPx.y / 2f);
            SetRef(castle, "body", body);
            SetRef(castle, "intact", body.sprite);
            SetRef(castle, "damaged1", LoadSprite(ArtRoot + "Castle/fortress-damaged1.png"));
            SetRef(castle, "damaged2", LoadSprite(ArtRoot + "Castle/fortress-damaged2.png"));

            var field = new GameObject("Battlefield").AddComponent<Battlefield>();
            SetValue(field, "laneTop", PxToWorld(0f, LaneTopPx).y);
            SetValue(field, "laneBottom", PxToWorld(0f, LaneBottomPx).y);
            SetValue(field, "leftBound", castleGo.transform.position.x);
            SetValue(field, "rightBound", PxToWorld(RefWidth, 0f).x);

            var spawn = new GameObject("EnemySpawn").transform;
            spawn.position = PxToWorld(SpawnPx, LaneTopPx);

            var game = new GameObject("Game");
            var economy = game.AddComponent<Economy>();
            var waves = game.AddComponent<WaveManager>();
            SetRef(waves, "tuning", tuning);
            SetRef(waves, "enemyPrefab", enemyPrefab);
            SetRef(waves, "castle", castle);
            SetRef(waves, "economy", economy);
            SetRef(waves, "spawnPoint", spawn);
            SetRef(waves, "battlefield", field);

            var board = game.AddComponent<HeroBoard>();
            SetRef(board, "waves", waves);
            SetRef(board, "heroPrefab", heroPrefab);
            SetRefs(board, "slots", BuildHeroSlots(board));
            SetRef(board, "equipment", AssetDatabase.LoadAssetAtPath<EquipmentConfig>(EquipmentConfigPath));

            CreateEventSystem();
            BuildBattleHud(CreateCanvas("HUD"), waves, castle, economy, board, roster);
            BuildSoldierCamp(game, waves, heroPrefab);

            var admin = game.AddComponent<AdminPanel>();
            SetRef(admin, "waves", waves);
            SetRef(admin, "castle", castle);
            SetRef(admin, "economy", economy);
            SetRef(admin, "hud", Object.FindObjectOfType<BattleHud>());

            BuildCannonBoard(game, castleGo.transform, body);

            EditorSceneManager.SaveScene(scene, ScenePath(SceneNames.Battle));
        }

        static void BuildBattleHud(RectTransform canvas, WaveManager waves, Castle castle, Economy economy,
            HeroBoard board, HeroDefinition[] roster)
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

            var badge = Img("Wave", canvas, UiSprite("chrome/banner-stage"), Top, new Vector2(0, -16), Native(160, 26), sliced: true);
            SetRef(hud, "waveText", Label("Value", badge.transform, "0", 36, Center, Vector2.zero, Native(160, 26)));

            SetRef(hud, "goldText", GoldPill(canvas));
            SetRef(hud, "gachaButton", MakeButton("GachaButton", canvas, "Triệu hồi", TopRight, new Vector2(-24, -136), new Vector2(240, 72), out _));
            SetRef(hud, "unitsButton", MakeButton("UnitsButton", canvas, "Đội quân", TopRight, new Vector2(-24, -224), new Vector2(240, 72), out _));
            SetRef(hud, "startButton", MakeButton("StartButton", canvas, "", Bottom, new Vector2(0, 48), new Vector2(320, 96), out var startLabel));
            SetRef(hud, "startLabel", startLabel);
            SetRef(hud, "messageText", Label("Message", canvas, "", 48, Center, new Vector2(0, 120), new Vector2(1400, 120)));

            // Bang chon hero: mo khi cham o trong. Panel bat raycast de cham khong xuyen xuong o ben duoi.
            // Roster = moi HeroDefinition trong Assets/Data, moi hero mot dong xep doc.
            //
            // Skin dot UI thu 3 (`UI/panels/`): nen `panel-hero`, tieu de tren `title-ribbon`, moi dong co
            // khung chan dung + mot dai chi so + mot o skill. Chi so DOC TU HeroDefinition chu khong go tay --
            // sua asset la bang doi theo, khong co cho cho so cu nam lai.
            // Hien toi da `VisibleRows` dong, con lai CUON DOC (keo / lan chuot): 9 hero xep het ra thi bang cao 1406 >
            // man 1080 va dong dau (Banneret) tran khoi man. Roster con dai them (sau nay loc theo vai) — cuon khong vo.
            const float rowStep = 132f;
            const int VisibleRows = 5;
            float pickerHeight = 108f + Mathf.Min(roster.Length, VisibleRows) * rowStep + 110f;
            var picker = Img("HeroPicker", canvas, UiSprite("panels/panel-hero"), Center, new Vector2(0, 40),
                new Vector2(680, pickerHeight), sliced: true);
            picker.raycastTarget = true;    // keo cuon tren khoang trong cua bang
            var viewport = Rect("Viewport", picker.transform, Center, Vector2.zero, Vector2.zero);
            Stretch(viewport, 0, 0, 108, 110);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Content", viewport, Top, Vector2.zero, new Vector2(680, roster.Length * rowStep));
            var scroll = picker.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            var titleBar = Img("Title", picker.transform, UiSprite("panels/title-ribbon"), Top, new Vector2(0, 26), new Vector2(360, 64), sliced: true);
            Label("Value", titleBar.transform, "Chọn hero", 34, Center, Vector2.zero, new Vector2(340, 64));

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
            var cancel = MakeButton("Cancel", picker.transform, "Huỷ", Bottom, new Vector2(0, 24), new Vector2(200, 72), out _);
            SetRef(board, "pickerPanel", picker.gameObject);
            SetRefs(board, "roster", roster);
            SetRefs(board, "rosterButtons", heroButtons);
            SetRefs(board, "rosterSkillSlots", skillSlots);
            SetPickerProgressRefs(board, levels, stars);
            SetRef(board, "slotSkillSprite", UiSprite("panels/slot-skill"));
            SetRef(board, "slotSelectedSprite", UiSprite("panels/slot-skill-selected"));
            SetRef(board, "cancelButton", cancel);

            SetRef(hud, "resultPopup", BuildResultPopup(canvas));
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
            var rootRect = Rect("ResultPopup", canvas, Center, Vector2.zero, new Vector2(RefWidth, RefHeight));
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
            var close = MakeButton("Close", panel.transform, "Tiếp tục", Bottom, new Vector2(0, 26), new Vector2(260, 76), out _);

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
            return popup;
        }

        // ---------------------------------------------------------------- bai linh (nhanh Linh, plan muc A3)

        // Bai dat SAU thanh: mep alpha trai cua phao dai o px 260 -> linh cho dung o x 150, ba hang cung hang o hero.
        static readonly string[] CampOrder = { "WarWolf", "StoneTortoise", "AcidLizard" };
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
            Label("Value", titleBar.transform, "Chọn lính", 34, Center, Vector2.zero, new Vector2(340, 64));

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
            var cancel = MakeButton("Cancel", picker.transform, "Huỷ", Bottom, new Vector2(0, 24), new Vector2(200, 72), out _);

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
            var sprite = UiSprite("hud/hud-dropzone-edge");
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

        // 3 be phao tren fortress.png (735x900, toa do px tinh tu goc tren-trai): tam dia da tron do bang mat tren anh.
        // Chan phao (pivot feet) dat thap hon tam dia mot chut cho de phao ngoi len mat dia.
        static readonly Vector2[] MountPadsInSprite = { new Vector2(440f, 152f), new Vector2(440f, 387f), new Vector2(440f, 619f) };
        // Vung cham phu ca dia lan than phao (world, tinh tu chan).
        static readonly Vector2 MountHitSize = new Vector2(0.9f, 1.1f);
        const float MountFrameHeightPx = 60f;

        /// <summary>
        /// Toan bo phan phao cua scene Battle (nhanh Tru): `CannonBoard` tren `Game`, 3 be tren `Castle`, bang chon phao
        /// trong HUD, nut admin. Goi MOT dong o cuoi `BuildBattleScene`, sau khi HUD + admin da dung.
        /// </summary>
        static void BuildCannonBoard(GameObject game, Transform castle, SpriteRenderer body)
        {
            var board = game.AddComponent<CannonBoard>();
            SetRef(board, "waves", game.GetComponent<WaveManager>());
            SetRefs(board, "mounts", BuildCannonMounts(castle, body, board));
            BuildCannonPicker((RectTransform)Object.FindObjectOfType<BattleHud>().transform, board);
            SetRef(game.GetComponent<AdminPanel>(), "cannons", board);
        }

        static CannonMount[] BuildCannonMounts(Transform castle, SpriteRenderer body, CannonBoard board)
        {
            var sprite = UiSprite("hud/hud-dropzone-edge");
            var size = body.sprite.rect.size;
            var mounts = new CannonMount[MountPadsInSprite.Length];
            for (int i = 0; i < mounts.Length; i++)
            {
                var pad = MountPadsInSprite[i];
                var go = new GameObject($"CannonMount{i + 1}");
                go.transform.SetParent(castle, false);
                // Body pivot giua sprite (fortress.png khong co pipeline-meta).
                go.transform.position = body.transform.position +
                    new Vector3((pad.x - size.x / 2f) * FortressScale / Ppu, (size.y / 2f - pad.y) * FortressScale / Ppu, 0f);

                var hit = go.AddComponent<BoxCollider2D>();
                hit.size = MountHitSize;
                hit.offset = new Vector2(0f, MountHitSize.y * 0.35f);

                var frame = NewSprite("Frame", go.transform, sprite, SortOrder.Castle + 1);
                if (sprite != null) frame.transform.localScale = Vector3.one * (MountFrameHeightPx / Ppu / sprite.bounds.size.y);

                var mount = go.AddComponent<CannonMount>();
                SetRef(mount, "board", board);
                SetRef(mount, "frame", frame);
                mounts[i] = mount;
            }
            return mounts;
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

            const float rowStep = 132f;
            float height = 108f + roster.Length * rowStep + 110f;
            var picker = Img("CannonPicker", canvas, UiSprite("panels/panel-hero"), Center, new Vector2(0, 60), new Vector2(680, height), sliced: true);
            picker.raycastTarget = true;
            var titleBar = Img("Title", picker.transform, UiSprite("panels/title-ribbon"), Top, new Vector2(0, 26), new Vector2(360, 64), sliced: true);
            Label("Value", titleBar.transform, "Chọn pháo", 34, Center, Vector2.zero, new Vector2(340, 64));

            var buttons = new Object[roster.Length];
            var infos = new Object[roster.Length];
            for (int i = 0; i < roster.Length; i++)
            {
                var cannon = roster[i];
                float y = height / 2f - 108f - rowStep / 2f - i * rowStep;
                var row = MakeButton("Cannon_" + cannon.name, picker.transform, "", Center, new Vector2(0, y),
                    new Vector2(608, 118), out _, UiSprite("panels/stat-strip"));

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
            var cancel = MakeButton("Cancel", picker.transform, "Huỷ", Bottom, new Vector2(0, 24), new Vector2(200, 72), out _);

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
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CreateCamera(new Color(0.10f, 0.07f, 0.16f));
            var economy = new GameObject("Game").AddComponent<Economy>();
            CreateEventSystem();

            var canvas = CreateCanvas("GachaUI");
            var screen = canvas.gameObject.AddComponent<GachaScreen>();
            SetRef(screen, "config", config);
            SetRef(screen, "economy", economy);

            // Nen man trieu hoi do lane agy giao; chua co thi de mau nen camera.
            var backdrop = AssetDatabase.LoadAssetAtPath<Sprite>(UiDir + "gacha/gacha-backdrop.png");
            if (backdrop != null)
            {
                var bg = Img("Backdrop", canvas, backdrop, Center, Vector2.zero, Vector2.zero);
                Stretch(bg.rectTransform, 0, 0, 0, 0);
            }

            SetRef(screen, "titleText", Label("Title", canvas, "Triệu hồi", 56, Center, new Vector2(0, -24), new Vector2(800, 80), Top));
            SetRef(screen, "backButton", MakeButton("BackButton", canvas, "Quay lại", TopLeft, new Vector2(24, -24), new Vector2(200, 72), out _));
            SetRef(screen, "goldText", GoldPill(canvas));

            var info = Img("Info", canvas, UiSprite("chrome/panel-wood"), Top, new Vector2(0, -120), new Vector2(1100, 120), sliced: true);
            SetRef(screen, "ratesText", Label("Rates", info.transform, "", 30, Center, new Vector2(0, -14), new Vector2(1060, 44), Top));
            SetRef(screen, "pityText", Label("Pity", info.transform, "", 26, Center, new Vector2(0, 14), new Vector2(1060, 44), Bottom));

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
        static void FillSkillTexts()
        {
            var texts = new Dictionary<string, (string ult, string passive, string minor, string minor2)>
            {
                ["mage_elemental"] = ("Vùng Băng: đặt vùng băng tại cụm quái đông nhất, tồn tại vài giây.",
                    "Vỡ Băng: quái đang bị làm chậm chết trong tầm thì nổ mảnh băng — quái quanh xác ăn 50% sát thương đòn thường.", null, null),
                ["cragfist"] = ("Hoá Đá: bất động + miễn sát thương vài giây, phản lại một phần sát thương nhận.",
                    "Khiêu Khích: quái đứng gần Người đá bỏ hero khác, quay sang đánh ông (khi ông còn chỗ giữ).", null, null),
                ["knight"] = ("Chém Quạt: một nhát chém quạt dài trước mặt, sát thương gấp nhiều lần đòn thường.",
                    "Truy Kích: đòn thường vào quái dưới 30% máu +50% sát thương.", null, null),
                ["archer"] = ("Bắn Nhanh Liên Hoàn: tăng mạnh tốc đánh trong vài giây.",
                    "Nhắm Yếu Điểm: cứ phát thứ 4 là chí mạng, gấp đôi sát thương.", null, null),
                ["vespera"] = (null, "Cứu Nguy: đồng minh dưới 30% máu được hồi thêm 50%.", null, null),
                ["mage_summoner"] = (
                    "Gọi Kỵ Sĩ Tử Thần; kỵ sĩ còn sống thì hồi đầy máu cho nó. Lính xương gần kỵ sĩ được tăng công + tốc đánh.",
                    "Dựng Xác: quái chết gần pháp sư có tỉ lệ đứng dậy thành lính xương.",
                    "Vòng Ấn Ký: vòng trên đất tại cụm quái, sát thương thấp + ấn ký; quái chết khi còn ấn ký chắc chắn thành lính xương.",
                    null),
                ["chainwarden"] = (
                    "Ném Móc: kéo quái bắn xa về sát mình + choáng, rồi Cuồng Nộ VĨNH VIỄN tới khi gục — hai xích dài, tăng công/tốc đánh, giảm sát thương nhận, hút máu.",
                    "Cái Ngục: giữ tối đa 4 quái; quái đang bị giữ nhận +20% sát thương từ mọi nguồn.",
                    "Quét Xích: quay xích quanh mình, sát thương thấp + trói (Cuồng Nộ: Lốc Xích 360°).",
                    "Giật Xích: đang giữ từ 2 quái thì giật mạnh — mỗi quái bị giữ ăn sát thương tăng theo số quái bị giật + choáng 1s (Cuồng Nộ: kéo thêm mọi quái trong tầm xích)."),
                ["banneret"] = (
                    "Hiệu Lệnh Xung Phong: cả đội trên sân +30% sát thương trong 6s.",
                    "Quân Kỳ: đồng minh quanh Kỳ Thủ +15% tốc đánh.",
                    "Cắm Cờ: có đồng minh đang đánh gần thì cắm cờ tại chỗ — đồng minh trong vùng nhận ít hơn 25% sát thương trong 5s.",
                    null),
                ["wyrmspear"] = (
                    "Long Hoá: biến hình Long Nhân + gọi Bạch Địa Long (Long Giáp, Long Uy, Long Nộ, Long Khế); lần sau: Thiên Địa Long Giáng.",
                    "Nghịch Lân: đòn vào boss/tinh anh cộng thêm % máu tối đa mục tiêu.",
                    "Xuyên Long / Thiên Long Liên Thương: đâm xuyên một đường, bỏ qua giáp.",
                    "Địa Long Chấn (rồng): gai đá quanh rồng — sát thương vùng, choáng, phá giáp."),
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
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");    // ly do: xem BuildGachaScene
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

            Label("Title", canvas, "Đội quân", 56, Center, new Vector2(0, -24), new Vector2(800, 80), Top);
            SetRef(screen, "backButton", MakeButton("BackButton", canvas, "Quay lại", TopLeft, new Vector2(24, -24), new Vector2(200, 72), out _));
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
            var viewport = Rect("Viewport", listPanel.transform, Center, Vector2.zero, Vector2.zero);
            Stretch(viewport, 24, 24, 24, 24);
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
            SetRef(screen, "skillsTab", RoundTab("SkillsTab", detail.transform, "Kỹ năng", new Vector2(40, -444)));
            SetRef(screen, "gearTab", RoundTab("GearTab", detail.transform, "Trang bị", new Vector2(300, -444)));
            SetRef(screen, "roundNormal", UiSprite("chrome/btn-round"));
            SetRef(screen, "roundActive", UiSprite("chrome/btn-round-active"));
            SetRef(screen, "detailSkills", Block("Skills", detail.transform, 24, new Vector2(40, -548), new Vector2(920, 240)));
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
            var title = Label("Title", header.transform, "CHỈ SỐ", 26, MidLeft, new Vector2(20, 0), new Vector2(200, 40));
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
            text.fontSize = 28;
            return button;
        }

        /// <summary>Icon ben trai nut to + chu lui sang phai; het tien / chua du manh thi doi sang sprite btn-disabled.</summary>
        static void DecorateButton(Button button, Text label, string icon)
        {
            Img("Icon", button.transform, UiSprite(icon), MidLeft, new Vector2(28, 0), new Vector2(56, 56));
            label.rectTransform.anchoredPosition = new Vector2(30, 0);
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState { disabledSprite = UiSprite("chrome/btn-disabled") };
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
            upgradeLabel.fontSize = 22;
            SetRef(screen, "upgradeLabel", upgradeLabel);
            SetRef(screen, "unequipButton", MakeButton("Unequip", view, "Tháo", TopLeft, new Vector2(buttonsX + 246, -70), new Vector2(200, 64), out _));

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
            var nameText = Label("Name", root.transform, "", 18, Center, new Vector2(0, 6), new Vector2(180, 52), Bottom);
            var noteText = Label("Note", root.transform, "", 18, Center, new Vector2(0, -6), new Vector2(180, 28), Top);
            noteText.color = new Color(1f, 0.86f, 0.35f);

            SetRef(view, "frame", frame);
            SetRef(view, "icon", icon);
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
            // Chrome chi co MOT sprite nut xanh -> trang thai bang tint mau, khong swap sprite.
            var img = Img(name, parent, background != null ? background : UiSprite("chrome/btn-green"), anchor, pos, size, sliced: true);
            img.raycastTarget = true;
            var button = img.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            var colors = ColorBlock.defaultColorBlock;
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 1f);
            button.colors = colors;
            text = Label("Label", img.transform, label, 30, Center, Vector2.zero, size);
            return button;
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
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = value;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
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
            if (created)
            {
                anim.idle.frames = Frames(dir + idle, "idle");
                anim.walk.frames = Frames(dir + walk, "walk");
                anim.attack.frames = Frames(dir + attack.folder, attack.action);
                anim.hurt.frames = Frames(dir + "hurt", "hurt");
                anim.death.frames = Frames(dir + "death", "death");
                anim.attack.hitFrame = hit;
            }
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
            EditorUtility.SetDirty(def);
            return def;
        }

        /// <summary>
        /// Hero -> id `UnitDefinition` gacha (dat duoc khi save so huu id nay). Vespera co id nhung CHUA vao pool gacha
        /// (bo chua xong) nen chi Admin panel cho so huu de test.
        /// </summary>
        static readonly Dictionary<string, string> HeroUnitIds = new Dictionary<string, string>
        {
            { "Ironblade", "knight" }, { "Hawkfletch", "archer" }, { "Frostweaver", "mage_elemental" },
            { "Bonecaller", "mage_summoner" }, { "Cragfist", "cragfist" }, { "Chainwarden", "chainwarden" },
            { "Vespera", "vespera" }, { "Wyrmspear", "wyrmspear" }, { "Banneret", "banneret" },
        };

        /// <summary>Frame `action-N.png` trong thu muc, xep theo N (khong lay sheet-transparent.png).</summary>
        static Sprite[] Frames(string folder, string action)
        {
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
