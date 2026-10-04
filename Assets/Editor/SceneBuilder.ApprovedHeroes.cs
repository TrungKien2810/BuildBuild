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
            HeroN("PetalSprite", "petalsprite", "Hoa yêu bắn hạt", d =>
            {
                d.maxHealth = 220; d.damage = 24; d.attackRate = 1; d.range = 4.5f; d.splashRadius = 0.1f;
                d.passiveKind = PassiveKind.SeedStacks;
                d.projectileFrames = ApprovedFrames("PetalSprite", "seed-bolt-fx");
                d.passiveFrames = ApprovedFrames("PetalSprite", "seed-stuck-fx");
                d.impactFrames = ApprovedFrames("PetalSprite", "sprout-burst-fx");
                d.passiveText = "Hạt Bám: mỗi đòn cắm 1 hạt trong 4s; đủ 3 hạt nổ 2× đòn thường lên mục tiêu.";
            }, SkillKind.SeedRain, d => { d.manaMax = 5; d.ultimateText = "Mưa Hạt: bắn 8 hạt trong 1,6s vào cụm đông nhất; áp dụng Hạt Bám."; });
            HeroN("KoboldTrapper", "koboldtrapper", "Kobold gài bẫy", d =>
            {
                d.maxHealth = 300; d.damage = 15; d.attackRate = 0.8f; d.range = 3.5f; d.splashRadius = 0.1f;
                d.passiveKind = PassiveKind.Traps;
                d.projectileFrames = ApprovedFrames("KoboldTrapper", "knife-bolt-fx");
                d.passiveFrames = ApprovedFrames("KoboldTrapper", "trap-open-fx");
                d.passiveText = "Đặt Bẫy: mỗi 8s đặt 1 bẫy, tối đa 3; giẫm gây 1× và trói 1,5s (tinh anh 0,75s, boss miễn trói).";
            }, SkillKind.TrapDetonation, d =>
            {
                d.manaMax = 6; d.skillRadius = 0.8f;
                d.skillIntroFrames = ApprovedFrames("KoboldTrapper", "trap-snap-fx");
                d.skillFrames = ApprovedFrames("KoboldTrapper", "trap-blast-fx");
                d.ultimateText = "Kích Nổ: mọi bẫy nổ 3× quanh nó và trói 1s (tinh anh 0,5s), rồi đặt lại 3 bẫy.";
            });
            HeroN("SigilMage", "sigilmage", "Thầy bùa ấn sao", d =>
            {
                d.maxHealth = 240; d.damage = 18; d.attackRate = 0.8f; d.range = 4.5f; d.splashRadius = 0.1f;
                d.passiveKind = PassiveKind.SigilWeakness;
                d.projectileFrames = ApprovedFrames("SigilMage", "sigil-bolt-fx");
                d.passiveFrames = ApprovedFrames("SigilMage", "sigil-ring-fx");
                d.passiveText = "Ấn Yếu: địch trong bán kính 2 nhận thêm 15% sát thương; không cộng với phá giáp cao hơn.";
            }, SkillKind.SigilBombs, d =>
            {
                d.manaMax = 6; d.skillIntroFrames = ApprovedFrames("SigilMage", "sigil-mark-fx");
                d.skillFrames = ApprovedFrames("SigilMage", "star-blast-fx");
                d.ultimateText = "Bùa Nổ: gắn ấn lên 3 địch nhiều máu nhất; sau 2s nổ 3× trong bán kính 0,6.";
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
            ApprovedSR("Bloodswordsman", "bloodswordsman", "Huyết kiếm khách", "blood-shatter-cast", SkillKind.BloodShatter, d =>
            {
                d.maxHealth = 900; d.damage = 32; d.attackRate = 0.8f; d.range = 0.8f; d.bloodswordsman = b;
                d.impactFrames = ApprovedFrames("Bloodswordsman", "blood-hit-fx");
                d.healFrames = ApprovedFrames("Bloodswordsman", "blood-heal-fx");
                d.passiveText = "Huyết Ý: mỗi 10% máu mất +5% sát thương (tối đa 35%); tự hạ địch hồi 6% máu, hồi chiêu 2s.";
                d.minorText = "Hiến Huyết: tiêu 15% máu hiện tại; 6s +40% công, chém tối đa 2 địch và hồi 4% máu mỗi đòn trúng. Hồi 10s.";
                d.secondMinorText = "Phản Huyết: dưới 40% máu, đỡ 50% đòn trực tiếp đầu tiên trong 1,5s rồi phản chém 2×, hồi 8% máu. Hồi 8s.";
                d.ultimateText = "Toái Kiếm Quy Huyết: tiêu 20% máu hiện tại, găm tối đa 3 địch 2×; sau 0,8s thu mảnh 3× và hồi 8% máu mỗi mục tiêu.";
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
            ApprovedSR("Soulbinder", "soulbinder", "Thuật sĩ kết hồn", "great-pact-cast", SkillKind.SoulPact, d =>
            {
                d.maxHealth = 320; d.damage = 24; d.attackRate = 0.8f; d.range = 4.5f; d.soulbinder = s; d.splashRadius = 0.1f;
                d.projectileFrames = ApprovedFrames("Soulbinder", "soul-bolt-fx");
                d.impactFrames = ApprovedFrames("Soulbinder", "soul-hit-fx");
                d.passiveText = "Chuyển Kết: địch liên kết chết thì dây chuyển sang địch gần nhất trong vùng chọn, giữ thời gian còn lại.";
                d.minorText = "Tam Hồn Kết: nối tối đa 3 địch trong 6s; sát thương trực tiếp lan 25% HP thực mất lên mỗi địch khác. Hồi 10s.";
                d.secondMinorText = "Giật Hồn: choáng các địch đang nối, mỗi hồn gây 1,5× lên gốc; tổng sát thương lan đúng một lần. Hồi 10s.";
                d.ultimateText = "Đại Hồn Khế: nối tối đa 5 địch trong 8s, lan 40%; Hồn Chấn 2× vào gốc. Sát thương sao không lan tiếp.";
            });
        }

        static UnityEngine.Sprite[] ApprovedFrames(string id, string action) => Frames(ArtRoot + id + "/" + action, action);
        static void ApprovedSR(string id, string unitId, string name, string cast, SkillKind kind, Action<HeroDefinition> stats)
        {
            var anim = AnimSet(id, id + "/", ("attack", "attack"), (cast, cast), hit: 4, skillHit: 2);
            anim.framesPerSecond = 1000f / 140f;
            EditorUtility.SetDirty(anim);
            var hero = HeroDef(id, null, anim, d => { stats(d); d.displayName = name; }, kind, d => d.manaMax = 6f);
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
