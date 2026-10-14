using System;
using System.Linq;
using BuildBuild.Combat;
using BuildBuild.Data;
using UnityEditor;
using UnityEngine;

namespace BuildBuild.Editor
{
    /// <summary>
    /// Noi 2 hero SSR da duyet art (2026-10-08) vao game: **Hau Nghe** (Xa thu) + **Bach Loc Tien** (Hoi mau) —
    /// bo sung hai vai con thieu SSR trong roster. Moi hero co Kit + component rieng (`Assets/Scripts/Combat/HauNghe.cs`,
    /// `BachLocTien.cs`), so nam o lop Kit (khong ghi vao asset khi chay lai: chi FRAME + hitFrame duoc ghi).
    /// Chay lai bao nhieu lan cung duoc: asset da co thi khong ghi de so nguoi dung da chinh tay.
    ///
    /// Ten thu muc clip + tien to frame do `tempart/deliver-ssr-heroes.py` giao (`art-contract.json` trong tung thu muc
    /// ghi ro diem neo tung clip, importer doc `pipeline-meta.json` de dat pivot). Vi tien to frame khac ten thu muc nen
    /// moi cho goi `SSRFrames` voi ca thu muc + tien to.
    /// </summary>
    public static partial class SceneBuilder
    {
        /// <summary>Nhip clip cua 2 hero nay: 130 ms/frame — dung nhip da duyet o gallery `complete-assets-v3`.</summary>
        const float SsrFramesPerSecond = 1000f / 130f;

        /// <summary>
        /// Frame ra don cua tung action, do tu art (do alpha: dien tich lon nhat + vuon xa nhat ve phia truoc) —
        /// xem `tempart/_inspect_ssr_hitframe.py`. `attack` 3 (tha ten / nghien truong), chieu cuoi Hau Nghe 5
        /// (nha ten khong lo o cuoi clip giuong cung), cac clip cast khac 3-4.
        /// </summary>
        const int SsrAttackHitFrame = 3;
        const int SsrXaNhatHitFrame = 5;
        const int SsrHoiXuanHitFrame = 4;

        static void BuildNewSSRHeroes()
        {
            // ---------------------------------------------------------------- Hau Nghe (SSR Xa thu)
            var hau = LoadOrCreate<HauNgheKit>(DataDir + "/HauNgheKit.asset", out _);
            hau.luanCast.frames = SSRFrames("HauNghe", "skill-luan-cast", "nhat-luan-cast");
            hau.luanCast.hitFrame = 3;
            hau.cuuTienCast.frames = SSRFrames("HauNghe", "skill-cuu-tien-cast", "cuu-tien-cast");
            hau.cuuTienCast.hitFrame = SsrXaNhatHitFrame;
            hau.bocCast.frames = SSRFrames("HauNghe", "skill-boc-cast", "nhat-boc-cast");
            hau.bocCast.hitFrame = 4;
            hau.xaNhatCast.frames = SSRFrames("HauNghe", "skill-cast", "xa-nhat-cast");
            hau.xaNhatCast.hitFrame = SsrXaNhatHitFrame;
            hau.arrow = SSRFrames("HauNghe", "skill-arrow-fx", "solar-arrow");
            hau.hit = SSRFrames("HauNghe", "skill-hit-fx", "solar-hit-impact");
            hau.seal = SSRFrames("HauNghe", "skill-seal-fx", "nhat-an");
            hau.skyBow = SSRFrames("HauNghe", "skill-sky-bow-fx", "thien-cung-wings");
            hau.luanGround = SSRFrames("HauNghe", "skill-zone-fx", "nhat-luan-ground-r2");
            hau.luanEmpowered = SSRFrames("HauNghe", "skill-zone-empowered-fx", "nhat-luan-empowered");
            hau.volley = SSRFrames("HauNghe", "skill-volley-fx", "cuu-tien-arrows");
            hau.volleyEmpowered = SSRFrames("HauNghe", "skill-volley-empowered-fx", "cuu-tien-empowered-arrow");
            hau.chain = SSRFrames("HauNghe", "skill-chain-fx", "nhat-boc-chain-ray");
            hau.burst = SSRFrames("HauNghe", "skill-burst-fx", "nhat-boc-impact");
            hau.aim = SSRFrames("HauNghe", "skill-aim-fx", "xa-nhat-aim");
            hau.sunArrow = SSRFrames("HauNghe", "skill-sun-arrow-fx", "xa-nhat-arrow");
            EditorUtility.SetDirty(hau);
            SSRHero("HauNghe", "haunghe", "Hau Nghe", Rarity.SSR, SkillKind.SolarShot,
                ("skill-cast", "xa-nhat-cast"), d =>
                {
                    d.maxHealth = 520; d.damage = 34; d.attackRate = 0.8f; d.range = 5.5f; d.moveSpeed = 1.1f;
                    d.splashRadius = 0.1f;
                    d.hauNghe = hau;
                    d.projectileFrames = hau.arrow;
                    d.impactFrames = hau.hit;
                    d.ultimateText = "Sun Shot: draw the bow for 1s, then fire a large arrow through a lane segment — each Enemy takes ×3 ATK, "
                        + "the aimed Target takes +×1 ATK per Sun Seal (consume all seals). Interrupted drawing deals no Damage.";
                    d.passiveText = "Heavenly Sun Eye: arrows hitting the aimed Target Stack Sun Seals (up to 3, "
                        + "each seal grants +12% Damage against it); at 3 seals, the next Secondary uses Heavenly Bow form, then consumes the seals.";
                    d.minorText = "Sun Wheel Guard: pin an arrow to create a telegraphed sun wheel zone; Enemies in the zone "
                        + "suffer Slow; staying through the telegraph roots them for 0,9s. Cooldown 10s.";
                    d.secondMinorText = "Nine Arrow Convergence: 3 pulses × 3 arrows converge on one Target (×0,6 ATK per pulse); "
                        + "interruption cancels the remaining pulses. Cooldown 12s.";
                    d.thirdMinorText = "Solar Chain Burst: an arrow explodes for ×1 ATK, then 3 rays deal ×0,6 ATK to up to 3 nearby Enemies. Cooldown 9s.";
                    d.ultimateIcon = UiSprite("icons/icon-skill-haunghe-xa-nhat");
                    d.passiveIcon = UiSprite("icons/icon-passive-haunghe-sun-seal");
                    d.minorIcon = UiSprite("icons/icon-skill-haunghe-luan-line");
                    d.secondMinorIcon = UiSprite("icons/icon-skill-haunghe-nine-arrows");
                    d.thirdMinorIcon = UiSprite("icons/icon-skill-haunghe-sun-burst");
                });

            // ---------------------------------------------------------------- Bach Loc Tien (SSR Hoi mau)
            var loc = LoadOrCreate<BachLocTienKit>(DataDir + "/BachLocTienKit.asset", out _);
            loc.mamCast.frames = SSRFrames("BachLocTien", "skill-mam-cast", "tam-mam-cast");
            loc.mamCast.hitFrame = 4;
            loc.diepCast.frames = SSRFrames("BachLocTien", "skill-diep-cast", "hoan-diep-cast");
            loc.diepCast.hitFrame = 4;
            loc.locAnhCast.frames = SSRFrames("BachLocTien", "skill-deer-cast", "loc-anh-cast");
            loc.locAnhCast.hitFrame = 3;
            loc.hoiXuanCast.frames = SSRFrames("BachLocTien", "skill-cast", "hoi-xuan-cast");
            loc.hoiXuanCast.hitFrame = SsrHoiXuanHitFrame;
            loc.bolt = SSRFrames("BachLocTien", "skill-bolt-fx", "loc-tuc-bolt");
            loc.bloom = SSRFrames("BachLocTien", "skill-bloom-fx", "heal-cleanse-bloom");
            loc.mamGround = SSRFrames("BachLocTien", "skill-mam-fx", "tam-mam-ground");
            loc.transfer = SSRFrames("BachLocTien", "skill-transfer-fx", "hoan-diep-transfer");
            loc.path = SSRFrames("BachLocTien", "skill-path-fx", "loc-anh-path");
            loc.deer = SSRFrames("BachLocTien", "skill-deer-fx", "linh-loc-walk");
            loc.spirit = SSRFrames("BachLocTien", "skill-sinh-khi-fx", "sinh-khi");
            loc.lane = SSRFrames("BachLocTien", "skill-lane-fx", "hoi-xuan-flower-zone");
            EditorUtility.SetDirty(loc);
            SSRHero("BachLocTien", "bachloctien", "Bach Loc Tien", Rarity.SSR, SkillKind.DeerRevival,
                ("skill-cast", "hoi-xuan-cast"), d =>
                {
                    d.maxHealth = 560; d.damage = 26; d.attackRate = 0.9f; d.range = 5f; d.moveSpeed = 1.1f;
                    d.healsAllies = true;
                    d.healFrames = loc.bloom;
                    d.bachLocTien = loc;
                    d.ultimateText = "White Deer Renewal: telegraph for 0,7s; a spirit deer walks along the lane, leaving a flower strip for 5s — "
                        + "Heal living heroes/soldiers in the strip for 70% ATK each second.";
                    d.passiveText = "Sacred Forest Veins: actual HP healed charges Vitality (capped); when full, the next Secondary "
                        + "gains a separate Spirit Deer support pulse, then consumes all Vitality.";
                    d.minorText = "Triple Spring Sprouts: plant up to 3 sprouts beneath allies with the lowest HP ratio in Range; "
                        + "sprouts Heal in pulses, mature after 3 pulses (new sprouts replace old ones). Cooldown 11s.";
                    d.secondMinorText = "Leaf Life Renewal: harvest a mature sprout for a powerful instant Heal on an "
                        + "ally in danger — consume the source sprout, trading a Heal zone for rescue. Cooldown 9s.";
                    d.thirdMinorText = "Deer Forest Patrol: requires at least 2 sprouts — a deer shadow runs from the oldest to the newest sprout, healing one "
                        + "pulse and removing one Poison/Burn from each ally along the path. Cooldown 13s.";
                    d.ultimateIcon = UiSprite("icons/icon-skill-bachloctien-hoi-xuan");
                    d.passiveIcon = UiSprite("icons/icon-passive-bachloctien-sinh-khi");
                    d.minorIcon = UiSprite("icons/icon-skill-bachloctien-three-sprouts");
                    d.secondMinorIcon = UiSprite("icons/icon-skill-bachloctien-leaf-revive");
                    d.thirdMinorIcon = UiSprite("icons/icon-skill-bachloctien-deer-path");
                });
        }

        /// <summary>
        /// Hero SSR co bo 3 chieu phu: nhip clip 130 ms/frame (nhip da duyet), 3 chieu phu mo cung moc sao mac dinh
        /// (`minorStar` / `secondMinorStar` / `thirdMinorStar` deu la 3), avatar lay tu UI da duyet, Unit vao pool gacha
        /// nhu moi hero da xong. VAI TRO khong dat o day — `AssignHeroRoles` la nguon duy nhat cho bang vai.
        /// </summary>
        static void SSRHero(string id, string unitId, string name, Rarity rarity, SkillKind kind,
            (string folder, string action) skill, Action<HeroDefinition> stats)
        {
            var anim = AnimSet(id, id + "/", ("attack", "attack"), skill, hit: SsrAttackHitFrame,
                skillHit: id == "HauNghe" ? SsrXaNhatHitFrame : SsrHoiXuanHitFrame);
            // `AnimSet`/`HeroDef` chi dien du lieu luc TAO asset. Lan build 2026-10-08 de lai Anim_ 0 frame va Hero_HauNghe
            // mac dinh tren dia (asset da tao, du lieu khong duoc luu) -> cac lan build sau bo qua. Thieu thi dien lai o day.
            string dir = ArtRoot + id + "/";
            if (!anim.idle.IsValid)
            {
                anim.idle.frames = Frames(dir + "idle", "idle");
                anim.walk.frames = Frames(dir + "walk", "walk");
                anim.attack.frames = Frames(dir + "attack", "attack");
                anim.hurt.frames = Frames(dir + "hurt", "hurt");
                anim.death.frames = Frames(dir + "death", "death");
                anim.attack.hitFrame = SsrAttackHitFrame;
            }
            anim.framesPerSecond = SsrFramesPerSecond;
            EditorUtility.SetDirty(anim);

            var hero = NamedHero(HeroDef(id, null, anim, stats, kind, d => d.manaMax = 6f), name);
            if (hero.animations == null)
            {
                hero.animations = anim;
                stats(hero);
            }
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

        /// <summary>Frame `prefix-N.png` trong `Assets/Art/Generated/&lt;unit&gt;/&lt;folder&gt;/`.</summary>
        static Sprite[] SSRFrames(string unit, string folder, string prefix) =>
            Frames(ArtRoot + unit + "/" + folder, prefix);
    }
}
