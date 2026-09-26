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
            BuildHeroData();
            var gacha = BuildGachaData();
            BuildSoldiers();
            BuildChainwarden();
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
                    d.squadSize = 3; d.summonCost = 30; d.summonCooldown = 12f;
                });

            var tortoise = SoldierDef("StoneTortoise", "Rùa đá khiên",
                AnimSet("StoneTortoise", "StoneTortoise/", attack: ("attack", "attack"), hit: 2),
                d =>
                {
                    d.maxHealth = 700f; d.damage = 8f; d.attackRate = 0.5f; d.range = 0.6f; d.moveSpeed = 0.6f;
                    d.projectileSpeed = 20f; d.splashRadius = 0.3f;
                    d.squadSize = 2; d.summonCost = 40; d.summonCooldown = 18f;
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
                    d.squadSize = 3; d.summonCost = 35; d.summonCooldown = 15f;
                });

            return new[] { wolf, tortoise, lizard };
        }

        /// <summary>
        /// Hero SR Chainwarden (plan muc A4, kit chot 2026-09-24). So = GDD muc 3 (Claude de xuat, CHUA can bang); toc chay
        /// GDD khong ghi -> 1,0 (giua Cragfist 0,9 va Ironblade 1,1). Nem Moc chua co art than -> dung clip attack.
        /// Hero nam o Data/ goc nen tu vao bang chon hero.
        /// </summary>
        static HeroDefinition BuildChainwarden()
        {
            var warden = LoadOrCreate<WardenDefinition>(DataDir + "/Warden_Chainwarden.asset", out _);
            if (warden.stunFrames == null || warden.stunFrames.Length == 0)
                warden.stunFrames = Frames(ArtRoot + "Enemy/stunned-status", "stun");
            EditorUtility.SetDirty(warden);

            var hero = HeroDef("Chainwarden", null,
                AnimSet("Chainwarden", "Chainwarden/", attack: ("attack", "attack"), hit: 4),
                d =>
                {
                    d.maxHealth = 1000f; d.damage = 18f; d.attackRate = 0.7f; d.range = 0.6f; d.moveSpeed = 1f;
                    d.projectileSpeed = 20f; d.splashRadius = 0.3f;
                    d.maxHeld = 4; d.heldDamageBonus = 0.2f;
                },
                SkillKind.HookThrow, d => { d.manaMax = 5f; });
            if (hero.warden == null) hero.warden = warden;
            // Chua co avatar trong UI/avatars/: tam dung anh identity.
            if (hero.avatar == null) hero.avatar = LoadSprite(ArtRoot + "Chainwarden/identity/identity-1.png");
            EditorUtility.SetDirty(hero);
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
            // Chua co avatar rieng cho linh: tam dung anh identity (icon gacha + bai cho).
            if (def.avatar == null) def.avatar = LoadSprite($"{ArtRoot}{id}/identity/identity-1.png");
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

            CreateEventSystem();
            BuildBattleHud(CreateCanvas("HUD"), waves, castle, economy, board, roster);
            BuildSoldierCamp(game, waves, heroPrefab);

            var admin = game.AddComponent<AdminPanel>();
            SetRef(admin, "waves", waves);
            SetRef(admin, "castle", castle);
            SetRef(admin, "economy", economy);
            SetRef(admin, "hud", Object.FindObjectOfType<BattleHud>());

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

        // ---------------------------------------------------------------- bai linh (nhanh Linh, plan muc A3)

        // Bai dat SAU thanh: mep alpha trai cua phao dai o px 260 -> linh cho dung o x 150, ba hang cung hang o hero.
        static readonly string[] CampOrder = { "WarWolf", "StoneTortoise", "AcidLizard" };
        const float CampXPx = 150f;
        static readonly float[] CampRowsPx = { 360f, 525f, 690f };

        /// <summary>
        /// `SoldierCamp` tren Game + moi loai linh mot `SoldierPost` o bai sau thanh + thanh tai nguyen tren HUD (ngay
        /// duoi thanh mau lau dai, cung khung 3-slice). SummonConfig chi TAO khi chua co — so chinh tay khong bi ghi de.
        /// </summary>
        static void BuildSoldierCamp(GameObject game, WaveManager waves, Hero heroPrefab)
        {
            var config = LoadOrCreate<SummonConfig>(DataDir + "/SummonConfig.asset", out _);
            var roster = CampOrder
                .Select(id => AssetDatabase.LoadAssetAtPath<SoldierDefinition>($"{SoldierDir}/Soldier_{id}.asset"))
                .Where(def => def != null)
                .Take(CampRowsPx.Length)
                .ToArray();

            var camp = game.AddComponent<SoldierCamp>();
            SetRef(camp, "waves", waves);
            SetRef(camp, "config", config);
            SetRef(camp, "unitPrefab", heroPrefab);
            SetRefs(camp, "roster", roster);

            var root = new GameObject("SoldierPosts").transform;
            for (int i = 0; i < roster.Length; i++)
                BuildSoldierPost(root, camp, i, roster[i], PxToWorld(CampXPx, CampRowsPx[i]));

            var canvas = Object.FindObjectOfType<BattleHud>().transform;
            var frame = Img("SummonResource", canvas, UiSprite("chrome/bar-frame"), TopLeft, new Vector2(80, -100), Native(200, 23), sliced: true);
            var fill = Img("Fill", frame.transform, UiSprite("chrome/bar-fill-xp"), Center, Vector2.zero, Vector2.zero);
            Stretch(fill.rectTransform, 6 * UiScale, 5 * UiScale, 4 * UiScale, 4 * UiScale);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            Img("Icon", frame.transform, UiSprite("style-b/icons/icon-skill-summon-squad"), MidLeft, new Vector2(-56, 0), new Vector2(48, 48));
            var value = Label("Value", frame.transform, "", 26, Center, Vector2.zero, Native(200, 23));
            var hud = frame.gameObject.AddComponent<SummonHud>();
            SetRef(hud, "camp", camp);
            SetRef(hud, "fill", fill);
            SetRef(hud, "valueText", value);
        }

        /// <summary>
        /// Mot con linh dung idle o bai (nhin sang phai), collider de keo, va canvas world-space tren dau: vong hoi chieu
        /// (`ring-fill` Filled Radial) tren nen `ring-bg` + gia tai nguyen. Canvas 1 px = 0,01 unit nhu art.
        /// </summary>
        static void BuildSoldierPost(Transform root, SoldierCamp camp, int index, SoldierDefinition soldier, Vector3 position)
        {
            var go = new GameObject("Post_" + soldier.name);
            go.transform.SetParent(root, false);
            go.transform.position = position;

            Battlefield.MeasureBody(soldier.animations, out float radius, out float height);
            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(Mathf.Max(0.8f, radius * 2f + 0.2f), height + 0.5f);
            collider.offset = new Vector2(0f, (height + 0.5f) * 0.5f);

            var idle = soldier.animations != null && soldier.animations.idle.IsValid ? soldier.animations.idle.frames[0] : null;
            var body = NewSprite("Body", go.transform, idle, 0);
            body.transform.localScale = Vector3.one * Battlefield.UnitScale;
            body.flipX = true;      // art quay trai; o bai linh nhin ve phia san
            body.sortingOrder = SortOrder.ForUnit(body);
            SetRef(body.gameObject.AddComponent<SpriteAnimator>(), "set", soldier.animations);

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
            var cost = Label("Cost", canvasRect, soldier.summonCost.ToString(), 20, Center, new Vector2(0, -24), new Vector2(80, 24));

            var post = go.AddComponent<SoldierPost>();
            SetRef(post, "camp", camp);
            SetInt(post, "index", index);
            SetRef(post, "body", body);
            SetRef(post, "cooldownFill", ring);
            SetRef(post, "costText", cost);
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
