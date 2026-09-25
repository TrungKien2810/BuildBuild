using System;
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
            BuildHeroData();
            BuildVespera();
            BuildCannons();
            var gacha = BuildGachaData();
            AssetDatabase.SaveAssets();

            BuildBattleScene(tuning, enemyPrefab);
            BuildGachaScene(gacha);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath(SceneNames.Battle), true),
                new EditorBuildSettingsScene(ScenePath(SceneNames.Gacha), true),
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
                goblinDef.goldReward = 5;
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
                ashDef.goldReward = 4;
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
                    d.maxHealth = 800f; d.damage = 25f; d.attackRate = 0.8f; d.range = 0.7f; d.moveSpeed = 1.1f;
                    d.projectileSpeed = 20f; d.splashRadius = 0.3f;
                },
                SkillKind.ConeSweep, d =>
                {
                    d.manaMax = 5f; d.skillRadius = 1.6f; d.skillPower = 3f;   // dai = be rong art cone-sweep-fx x UnitScale
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
                Unit("cannon_rapid", "Pháo Liên Thanh", UnitCategory.Cannon, Rarity.N, "icons/icon-cannon-rapid"),
                Unit("cannon_blast", "Pháo Nổ", UnitCategory.Cannon, Rarity.N, "icons/icon-cannon-blast"),
                Unit("cannon_frost", "Pháo Băng", UnitCategory.Cannon, Rarity.N, "icons/icon-cannon-frost"),
            };

            var config = LoadOrCreate<GachaConfig>(DataDir + "/GachaConfig.asset", out bool created);
            if (created)
            {
                config.pool = pool;
                EditorUtility.SetDirty(config);
            }
            return config;
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
                slinger.goldReward = 6;
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
                gatebreaker.goldReward = 40;
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
                boss.goldReward = 200;
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
                new WaveBand { fromWave = 30, toWave = 30, enemies = new EnemyDefinition[0], hasBoss = true, bossStatMultiplier = 3.5f, fixedCount = 0 },
            };
            EditorUtility.SetDirty(tuning);
        }

        /// <summary>Hero R Vespera (don thuong hoi mau, kit chua chot — so tam) + FX buff tren dau Hawkfletch.</summary>
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
                SkillKind.None, null);
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
        /// 3 phao GDD muc 4. Chi Lien Thanh co art (`CannonRapid/`); No va Bang tam dung chung bo sprite do, to cam / xanh
        /// qua `tint` (plan muc B1). Co art rieng thi doi `animations` / frame dan trong asset.
        /// </summary>
        static CannonDefinition[] BuildCannons()
        {
            EnsureFolder(CannonDir);
            string dir = ArtRoot + "CannonRapid/";
            // Phao chi co idle + attack — khong goi AnimSet (se tim walk/hurt/death khong ton tai).
            var anim = LoadOrCreate<SpriteAnimationSet>(DataDir + "/Anim_CannonRapid.asset", out bool newAnim);
            if (newAnim)
            {
                anim.idle.frames = Frames(dir + "idle", "idle");
                anim.attack.frames = Frames(dir + "attack", "attack");
                anim.attack.hitFrame = 2;   // frame loe nong
                EditorUtility.SetDirty(anim);
            }

            return new[]
            {
                CannonDef("Rapid", "cannon_rapid", "Pháo Liên Thanh", "icon-cannon-rapid", anim, c =>
                {
                    c.damage = 12f; c.fireRate = 2f; c.range = 8f;
                    c.projectileScale = 0.5f; c.impactScale = 0.6f;
                }),
                CannonDef("Blast", "cannon_blast", "Pháo Nổ", "icon-cannon-blast", anim, c =>
                {
                    c.damage = 30f; c.fireRate = 0.5f; c.range = 7f; c.aoeRadius = 2f;
                    c.projectileSpeed = 10f; c.projectileScale = 0.8f; c.impactScale = 2.5f;
                    c.tint = new Color(1f, 0.62f, 0.3f);
                }),
                CannonDef("Frost", "cannon_frost", "Pháo Băng", "icon-cannon-frost", anim, c =>
                {
                    c.damage = 8f; c.fireRate = 1f; c.range = 7f; c.slowPercent = 0.4f; c.slowDuration = 3f;
                    c.projectileScale = 0.6f; c.impactScale = 0.8f;
                    c.tint = new Color(0.55f, 0.8f, 1f);
                }),
            };
        }

        static CannonDefinition CannonDef(string id, string unitId, string displayName, string icon, SpriteAnimationSet anim,
            Action<CannonDefinition> stats)
        {
            var def = LoadOrCreate<CannonDefinition>($"{CannonDir}/Cannon_{id}.asset", out bool created);
            if (created)
            {
                def.unitId = unitId;
                def.displayName = displayName;
                def.animations = anim;
                def.projectileFrames = Frames(ArtRoot + "CannonRapid/bullet", "projectile");
                def.impactFrames = Frames(ArtRoot + "CannonRapid/bullet-impact", "impact");
                stats(def);
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
                .Where(path => !path.StartsWith(DataDir + "/Summons/"))
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

            CreateEventSystem();
            BuildBattleHud(CreateCanvas("HUD"), waves, castle, economy, board, roster);

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
            SetRef(hud, "startButton", MakeButton("StartButton", canvas, "", Bottom, new Vector2(0, 48), new Vector2(320, 96), out var startLabel));
            SetRef(hud, "startLabel", startLabel);
            SetRef(hud, "messageText", Label("Message", canvas, "", 48, Center, new Vector2(0, 120), new Vector2(1400, 120)));

            // Bang chon hero: mo khi cham o trong. Panel bat raycast de cham khong xuyen xuong o ben duoi.
            // Roster = moi HeroDefinition trong Assets/Data, moi hero mot dong xep doc.
            //
            // Skin dot UI thu 3 (`UI/panels/`): nen `panel-hero`, tieu de tren `title-ribbon`, moi dong co
            // khung chan dung + mot dai chi so + mot o skill. Chi so DOC TU HeroDefinition chu khong go tay --
            // sua asset la bang doi theo, khong co cho cho so cu nam lai.
            const float rowStep = 132f;
            float pickerHeight = 108f + roster.Length * rowStep + 110f;
            var picker = Img("HeroPicker", canvas, UiSprite("panels/panel-hero"), Center, new Vector2(0, 60), new Vector2(680, pickerHeight), sliced: true);
            picker.raycastTarget = true;
            var titleBar = Img("Title", picker.transform, UiSprite("panels/title-ribbon"), Top, new Vector2(0, 26), new Vector2(360, 64), sliced: true);
            Label("Value", titleBar.transform, "Chọn hero", 34, Center, Vector2.zero, new Vector2(340, 64));

            var heroButtons = new Object[roster.Length];
            var skillSlots = new Object[roster.Length];
            for (int i = 0; i < roster.Length; i++)
            {
                var heroDef = roster[i];
                float y = pickerHeight / 2f - 108f - rowStep / 2f - i * rowStep;
                var row = MakeButton("Hero_" + heroDef.name, picker.transform, "", Center, new Vector2(0, y),
                    new Vector2(608, 118), out _, UiSprite("panels/stat-strip"));

                // Avatar lay TU heroDef, khong ghim cung ten file: them hero thu hai vao roster ma icon
                // van la cua hero thu nhat thi khong co loi nao bao, chi co nguoi choi bam nham.
                var portrait = Img("Portrait", row.transform, UiSprite("panels/portrait-frame-big"), MidLeft, new Vector2(10, 0), new Vector2(98, 98), sliced: true);
                if (heroDef.avatar != null)
                    Img("Avatar", portrait.transform, heroDef.avatar, Center, Vector2.zero, new Vector2(72, 72));

                var nameText = Label("Name", row.transform, heroDef.displayName, 32, MidLeft, new Vector2(122, 26), new Vector2(300, 40), MidLeft);
                nameText.alignment = TextAnchor.MiddleLeft;
                var statText = Label("Stats", row.transform,
                    $"ST {heroDef.damage:0}   Tầm {heroDef.range:0.#}   Máu {heroDef.maxHealth:0}",
                    24, MidLeft, new Vector2(122, -18), new Vector2(340, 34), MidLeft);
                statText.alignment = TextAnchor.MiddleLeft;

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
            var rays = Img("Rays", body, UiSprite("panels/rays-burst"), Center, Vector2.zero, new Vector2(760, 760));
            var panel = Img("Panel", body, UiSprite("panels/panel-result"), Center, Vector2.zero, new Vector2(760, 420), sliced: true);
            var sparkle = Img("Sparkle", body, UiSprite("panels/sparkle"), Center, new Vector2(-268, 96), new Vector2(166, 140));

            var ribbon = Img("Ribbon", panel.transform, UiSprite("panels/ribbon-victory"), Top, new Vector2(0, 40), new Vector2(600, 80), sliced: true);
            var title = Label("Value", ribbon.transform, "", 40, Center, new Vector2(0, 2), new Vector2(560, 80));

            var rewardRow = Img("Reward", panel.transform, UiSprite("panels/reward-row"), Center, new Vector2(0, 16), new Vector2(440, 76), sliced: true);
            Img("Icon", rewardRow.transform, UiSprite("icons/icon-gold"), MidLeft, new Vector2(46, 0), new Vector2(56, 56));
            var gold = Label("Value", rewardRow.transform, "0", 36, Center, new Vector2(26, 0), new Vector2(320, 76));

            var hint = Label("Hint", panel.transform, "", 26, Center, new Vector2(0, -56), new Vector2(660, 40));
            var close = MakeButton("Close", panel.transform, "Tiếp tục", Bottom, new Vector2(0, 26), new Vector2(260, 76), out _);

            SetRef(popup, "root", body.gameObject);
            SetRef(popup, "dim", dim.gameObject);
            SetRef(popup, "rays", rays.gameObject);
            SetRef(popup, "sparkle", sparkle.gameObject);
            SetRef(popup, "ribbon", ribbon);
            SetRef(popup, "victoryRibbon", UiSprite("panels/ribbon-victory"));
            SetRef(popup, "defeatRibbon", UiSprite("panels/ribbon-defeat"));
            SetRef(popup, "titleText", title);
            SetRef(popup, "rewardRow", rewardRow.gameObject);
            SetRef(popup, "goldText", gold);
            SetRef(popup, "hintText", hint);
            SetRef(popup, "closeButton", close);
            return popup;
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
            SetRef(board, "economy", game.GetComponent<Economy>());
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
        /// Bang chon phao: cung skin voi bang chon hero (`panels/`). Moi dong: emblem + ten + dong chi so (CannonBoard dien
        /// luc mo) + nut nang cap. Duoi cung: go phao / huy. Dat TRUOC ResultPopup de bang ket qua luon de len tren.
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
            var upgrades = new Object[roster.Length];
            var upgradeLabels = new Object[roster.Length];
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

                var upgrade = MakeButton("Upgrade", row.transform, "", MidLeft, new Vector2(460, 0), new Vector2(136, 64), out var upgradeLabel);
                upgradeLabel.fontSize = 24;

                buttons[i] = row;
                infos[i] = info;
                upgrades[i] = upgrade;
                upgradeLabels[i] = upgradeLabel;
            }
            var remove = MakeButton("Remove", picker.transform, "Gỡ pháo", Bottom, new Vector2(-120, 24), new Vector2(200, 72), out _);
            var cancel = MakeButton("Cancel", picker.transform, "Huỷ", Bottom, new Vector2(120, 24), new Vector2(200, 72), out _);

            SetRef(board, "pickerPanel", picker.gameObject);
            SetRefs(board, "roster", roster);
            SetRefs(board, "rosterButtons", buttons);
            SetRefs(board, "rosterInfo", infos);
            SetRefs(board, "upgradeButtons", upgrades);
            SetRefs(board, "upgradeLabels", upgradeLabels);
            SetRef(board, "removeButton", remove);
            SetRef(board, "cancelButton", cancel);

            var popup = canvas.Find("ResultPopup");
            if (popup != null) popup.SetAsLastSibling();
        }

        // ---------------------------------------------------------------- scene Gacha

        static void BuildGachaScene(GachaConfig config)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            config = AssetDatabase.LoadAssetAtPath<GachaConfig>(DataDir + "/GachaConfig.asset");   // ly do: xem BuildBattleScene
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

            Label("Title", canvas, "Triệu hồi", 56, Center, new Vector2(0, -24), new Vector2(800, 80), Top);
            SetRef(screen, "backButton", MakeButton("BackButton", canvas, "Quay lại", TopLeft, new Vector2(24, -24), new Vector2(200, 72), out _));
            SetRef(screen, "goldText", GoldPill(canvas));

            var info = Img("Info", canvas, UiSprite("chrome/panel-wood"), Top, new Vector2(0, -120), new Vector2(1100, 120), sliced: true);
            SetRef(screen, "ratesText", Label("Rates", info.transform, "", 30, Center, new Vector2(0, -14), new Vector2(1060, 44), Top));
            SetRef(screen, "pityText", Label("Pity", info.transform, "", 26, Center, new Vector2(0, 14), new Vector2(1060, 44), Bottom));

            // 10 the = 2 hang x 5 cot, moi the 96x128 hien o 2x.
            var cell = Native(96, 128);
            var gridRect = Rect("Results", canvas, Center, new Vector2(0, -30), new Vector2(5 * cell.x + 4 * 16, 2 * cell.y + 16));
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
            EditorUtility.SetDirty(def);
            return def;
        }

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
