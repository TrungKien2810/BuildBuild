using System;
using System.Collections.Generic;
using BuildBuild.Data;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuildBuild.Meta
{
    /// <summary>Mot thanh NPC tren ban do Chinh Phat — dung luc Do Tham, luu vao save de xem lai / danh sau.</summary>
    [Serializable]
    public class ConquestTarget
    {
        /// <summary>0 = De, 1 = Ngang, 2 = Kho.</summary>
        public int tier;
        public int lord;
        /// <summary>O dat tren ban do — chi so vao `ConquestMap.Spots`; -1 = save cu (khong co o), xem `SpotFor`.</summary>
        public int spot = -1;
        /// <summary>9 o doi hinh, ten asset HeroDefinition, "" = o trong (cung thu tu o cua minh, dat doi xung).</summary>
        public List<string> heroes = new List<string>();
        public List<string> cannons = new List<string>();
        public List<string> soldiers = new List<string>();
        /// <summary>
        /// Quan tiep vien: hero ra tu thanh NPC giua tran theo dot (`HeroBoard`). KHONG tinh vao `power` — phe ta cung co
        /// mot o chi vien khong tinh vao luc chien minh, va moi phe chi dung `HeroBoard.ReinforceLimit` hero.
        /// </summary>
        public List<string> reserves = new List<string>();
        public int cannonLevel = 1;
        /// <summary>He so nhan mau + sat thuong cho hero / linh NPC (thay level + sao + trang bi cua phe ta).</summary>
        public float strength = 1f;
        /// <summary>Sao cua hero NPC — mo noi tai / chieu phu nhu luat hero minh.</summary>
        public int stars;
        public int power;
        /// <summary>He so mau thanh NPC so voi thanh minh.</summary>
        public float castleScale = 1f;
        public int shards;
        public bool done;
    }

    [Serializable]
    public class ConquestSave
    {
        public List<ConquestTarget> targets = new List<ConquestTarget>();
        public int streak;
        public long scannedAt;
        /// <summary>Ngay (yyyymmdd, gio may) cua `attacksUsed`.</summary>
        public int day;
        public int attacksUsed;
    }

    /// <summary>
    /// Ban do Chinh Phat (GAME_DESIGN.md muc 8): Do Tham lo 3 thanh NPC De / Ngang / Kho quanh thanh minh, luc chien NPC =
    /// luc chien minh x he so do kho x (1 + 0,06 x chuoi thang). Luat luot + so thuong la TAM (Claude, chua can) — o hang
    /// so duoi day.
    /// </summary>
    public static class ConquestMap
    {
        public const int AttacksPerDay = 5;
        public const float FreeScanSeconds = 30f * 60f;
        public const float StreakBonus = 0.06f;
        public static readonly string[] TierNames = { "Easy", "Ngang", "Hard" };
        public static readonly float[] TierPower = { 0.85f, 1f, 1.2f };
        static readonly float[] TierGold = { 0.8f, 1f, 1.4f };
        static readonly int[] TierShards = { 0, 1, 2 };
        /// <summary>So hero tiep vien moi thanh = so tran tha duoc (`HeroBoard.ReinforceLimit`), moi do kho nhu nhau.</summary>
        const int Reserves = 1;
        /// <summary>So o hero (`HeroBoard.slots`), cho dung linh (`SoldierCamp.postCount`) trong scene Battle. So be phao
        /// truyen vao (`CannonBoard.MountCount`) vi se mo them be.</summary>
        const int HeroSlots = 9, SoldierPosts = 3;
        /// <summary>
        /// He so pham N / R / SR / SSR (TAM): chi so goc khong do het bo chieu — Banneret, Vespera mau / sat thuong thap
        /// nhung gia tri nam o buff / hoi mau; SR / SSR co noi tai + chieu manh hon.
        /// </summary>
        public static readonly float[] RarityPower = { 1f, 1.15f, 1.3f, 1.5f };
        public static readonly string[] LordNames = { "Baron Grimhold", "Countess Velmora", "Young Lord Aldric" };

        // Tam cac bai dat trong tren `UI/raid/world-map.png`, chuan hoa 0..1 goc duoi-trai. Ban do (bo ui-v3) ve dung 3
        // bai dat cho thanh NPC — trai-tren / phai-tren / phai-duoi — nen moi lan quet dung ca ba, chi doi do kho <-> bai.
        // Do bang mask mau dat roi lay TAM VUNG PHU KIN: quet moi tam dat vao khung token 64x62 px, lay trung binh cac tam
        // ma khung phu kin >= 99% dat — bai thu 4 (thanh minh) cung the, do ra (0,502 · 0,525) = token thanh minh trong
        // scene (Center + y 29 px * UiScale 2 tren ban do 1920x1080).
        static readonly Vector2[] Spots =
        {
            new Vector2(0.187f, 0.766f), new Vector2(0.804f, 0.806f), new Vector2(0.836f, 0.259f),
        };

        /// <summary>
        /// Tam o dat cua token thanh NPC tren ban do. Uu tien `spot` da luu luc quet; save cu (truoc khi ban do doi bo
        /// cuc, `spot` = -1) roi ve thu tu o nen ba thanh khong chong nhau — KHONG can quet lai.
        /// </summary>
        public static Vector2 SpotFor(ConquestTarget target, int rank) =>
            Spots[target.spot >= 0 && target.spot < Spots.Length ? target.spot : rank % Spots.Length];

        static ConquestSave Data => SaveSystem.Data.conquest ?? (SaveSystem.Data.conquest = new ConquestSave());

        public static IReadOnlyList<ConquestTarget> Targets => Data.targets;
        public static int Streak => Data.streak;

        public static int AttacksLeft
        {
            get
            {
                if (Data.day != Today) return AttacksPerDay;
                return Mathf.Max(0, AttacksPerDay - Data.attacksUsed);
            }
        }

        /// <summary>Giay con lai toi lan quet mien phi; 0 = quet mien phi duoc.</summary>
        public static float SecondsToFreeScan =>
            Data.targets.Count == 0 ? 0f : Mathf.Max(0f, FreeScanSeconds - (Now - Data.scannedAt));

        /// <summary>Gia quet ngay khi chua toi gio mien phi: TAM = vang thang wave dang danh.</summary>
        public static int ScanCost(WaveTuning tuning) =>
            SecondsToFreeScan <= 0f ? 0 : tuning != null ? tuning.WaveGold(Mathf.Max(1, SaveSystem.Data.nextWave)) : 100;

        /// <summary>Phao khong mat mau: luc chien = DPS x he so nay (TAM) — phao lv1 goc ngang mot hero lv1 goc.</summary>
        const float CannonWeight = 3f;

        /// <summary>
        /// Luc chien (TAM, sua 2026-10-02): hero = can(mau x DPS), DPS = sat thuong x toc danh, ca ba da nhan he so
        /// level/sao/trang bi; linh = nhu hero x so con trong top. Trung binh nhan chu khong cong: hai doi danh nhau thang
        /// thua theo mau x DPS, va nhan ca mau lan sat thuong cua NPC voi `strength` thi luc chien tang DUNG `strength` lan
        /// (cong tuyen tinh cu: x1,2 tren nhan thi thuc te manh x1,44).
        /// </summary>
        public static float UnitPower(HeroDefinition hero, GachaConfig gacha, float healthMultiplier, float damageMultiplier, float rateMultiplier = 1f)
        {
            float squad = hero is SoldierDefinition soldier ? soldier.squadSize : 1f;
            float dps = hero.damage * damageMultiplier * hero.attackRate * rateMultiplier;
            return Mathf.Sqrt(hero.maxHealth * healthMultiplier * dps) * squad * RarityPower[(int)RarityOf(gacha, hero.unitId)];
        }

        public static float CannonPower(CannonDefinition cannon, GachaConfig gacha, int level) =>
            Progression.CannonDamage(cannon, level) * cannon.fireRate * CannonWeight * RarityPower[(int)RarityOf(gacha, cannon.unitId)];

        /// <summary>Pham gan co dinh vao unit (`UnitDefinition` trong pool gacha); khong tim thay = N.</summary>
        public static Rarity RarityOf(GachaConfig gacha, string unitId)
        {
            if (gacha != null && gacha.pool != null)
                foreach (var unit in gacha.pool)
                    if (unit != null && unit.id == unitId) return unit.rarity;
            return Rarity.N;
        }

        /// <summary>
        /// Luc chien minh = doi MANH NHAT co the xep tu unit da so huu (9 hero + 3 linh + phao bang so be dang co), khong phai doi dang xep —
        /// xep thieu o khong lam ha luc chien / do kho thanh NPC.
        /// </summary>
        public static float PlayerPower(HeroDefinition[] heroes, CannonDefinition[] cannons, int cannonSlots, SoldierDefinition[] soldiers,
            EquipmentConfig equipment, GachaConfig gacha)
        {
            float power = 0f;
            foreach (var hero in BestHeroes(heroes, equipment, gacha)) power += HeroPower(hero, equipment, gacha);
            foreach (var soldier in BestSoldiers(soldiers, gacha)) power += SoldierPower(soldier, gacha);
            foreach (var cannon in BestCannons(cannons, cannonSlots, gacha)) power += CannonPower(cannon, gacha, CannonLevel(cannon));
            return power;
        }

        static float HeroPower(HeroDefinition hero, EquipmentConfig equipment, GachaConfig gacha)
        {
            Progression.Multipliers(hero.unitId, equipment, out float health, out float damage, out float rate);
            return UnitPower(hero, gacha, health, damage, rate);
        }

        static float SoldierPower(SoldierDefinition soldier, GachaConfig gacha)
        {
            float progress = Progression.StatMultiplier(soldier.unitId);
            return UnitPower(soldier, gacha, progress, progress);
        }

        static List<HeroDefinition> BestHeroes(HeroDefinition[] heroes, EquipmentConfig equipment, GachaConfig gacha) =>
            Best(heroes, h => h.unitId, HeroSlots, h => HeroPower(h, equipment, gacha));

        static List<SoldierDefinition> BestSoldiers(SoldierDefinition[] soldiers, GachaConfig gacha) =>
            Best(soldiers, x => x.unitId, SoldierPosts, x => SoldierPower(x, gacha));

        static List<CannonDefinition> BestCannons(CannonDefinition[] cannons, int slots, GachaConfig gacha) =>
            Best(cannons, c => c.unitId, slots, c => CannonPower(c, gacha, CannonLevel(c)));

        /// <summary><paramref name="count"/> unit da so huu co luc chien cao nhat, manh truoc.</summary>
        static List<T> Best<T>(T[] pool, Func<T, string> unitId, int count, Func<T, float> power) where T : UnityEngine.Object
        {
            var owned = new List<T>(Array.FindAll(pool, u => u != null && !string.IsNullOrEmpty(unitId(u)) && SaveSystem.Data.Find(unitId(u)) != null));
            owned.Sort((a, b) => power(b).CompareTo(power(a)));
            if (owned.Count > count) owned.RemoveRange(count, owned.Count - count);
            return owned;
        }

        /// <summary>
        /// Do Tham: dung lai 3 thanh. Cost &gt; 0 thi nguoi goi da tru vang. Doi hinh NPC soi doi manh nhat cua minh (doi dem
        /// vao `PlayerPower`): bang so hero / phao / linh (it nhat 3 hero), moi unit NPC CUNG PHAM voi unit tuong ung cua minh (het loai cung pham thi lay bat ky) —
        /// doi N gap doi N, doi SSR gap doi SSR; them `Reserves` hero tiep vien. He so `strength` giai sao cho luc chien
        /// NPC (doi hinh tren san + linh + phao, khong tinh tiep vien — doi xung voi luc chien minh) dung bang muc tieu.
        /// </summary>
        public static void Scan(HeroDefinition[] heroes, CannonDefinition[] cannons, int cannonSlots, SoldierDefinition[] soldiers,
            EquipmentConfig equipment, GachaConfig gacha)
        {
            var data = Data;
            float mine = Mathf.Max(1f, PlayerPower(heroes, cannons, cannonSlots, soldiers, equipment, gacha));
            var myHeroes = BestHeroes(heroes, equipment, gacha);
            var myCannons = BestCannons(cannons, cannonSlots, gacha);
            // Toi thieu 3 hero / 1 loai linh: minh so huu it hon thi o thieu lay pham N.
            var heroRarities = Rarities(myHeroes, h => h.unitId, gacha, 3);
            var soldierRarities = Rarities(BestSoldiers(soldiers, gacha), x => x.unitId, gacha, 1);
            var cannonRarities = Rarities(myCannons, c => c.unitId, gacha, 0);
            int stars = AverageStars(myHeroes);
            float cannonLevelSum = 0f;
            foreach (var cannon in myCannons) cannonLevelSum += CannonLevel(cannon);
            float myCannonLevel = myCannons.Count > 0 ? cannonLevelSum / myCannons.Count : 1f;

            var spots = new List<int>();
            for (int i = 0; i < Spots.Length; i++) spots.Add(i);
            var lords = new List<int> { 0, 1, 2 };
            data.targets.Clear();
            for (int tier = 0; tier < 3; tier++)
            {
                float scale = TierPower[tier] * (1f + StreakBonus * data.streak);
                var target = new ConquestTarget { tier = tier, stars = stars, castleScale = scale };
                int spot = Random.Range(0, spots.Count);
                target.spot = spots[spot];
                spots.RemoveAt(spot);
                int lord = Random.Range(0, lords.Count);
                target.lord = lords[lord];
                lords.RemoveAt(lord);

                var cells = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
                for (int i = 0; i < 9; i++) target.heroes.Add("");
                var pool = new List<HeroDefinition>(Array.FindAll(heroes, h => h != null));
                foreach (var rarity in heroRarities)
                {
                    if (pool.Count == 0 || cells.Count == 0) break;
                    int cell = Random.Range(0, cells.Count);
                    target.heroes[cells[cell]] = TakeSameRarity(pool, rarity, gacha, h => h.unitId).name;
                    cells.RemoveAt(cell);
                }
                // Tiep vien: uu tien hero chua co trong doi hinh; roster het thi lap lai loai da dung.
                for (int i = 0; i < Reserves && heroes.Length > 0; i++)
                {
                    if (pool.Count == 0) pool.AddRange(Array.FindAll(heroes, h => h != null));
                    var rarity = heroRarities[Random.Range(0, heroRarities.Count)];
                    target.reserves.Add(TakeSameRarity(pool, rarity, gacha, h => h.unitId).name);
                }
                foreach (var rarity in soldierRarities)
                {
                    var soldierPool = new List<SoldierDefinition>(Array.FindAll(soldiers, x => x != null));
                    if (soldierPool.Count > 0) target.soldiers.Add(TakeSameRarity(soldierPool, rarity, gacha, x => x.unitId).name);
                }
                target.cannonLevel = Mathf.Max(1, Mathf.RoundToInt(myCannonLevel * scale));
                float cannonPower = 0f;
                // Bang so phao minh dang lap (CannonBoard dung dung so nay, be thua de trong).
                foreach (var rarity in cannonRarities)
                {
                    var cannonPool = new List<CannonDefinition>(Array.FindAll(cannons, c => c != null));
                    if (cannonPool.Count == 0) break;
                    var cannon = TakeSameRarity(cannonPool, rarity, gacha, c => c.unitId);
                    target.cannons.Add(cannon.name);
                    cannonPower += CannonPower(cannon, gacha, target.cannonLevel);
                }

                // Hero tren san + linh NPC ganh phan luc chien con lai sau phao. Tiep vien khong tinh: tung chia `strength`
                // cho ca 2-4 tiep vien trong khi tran chi tha 1 -> doi tren san yeu hon so hien, Kho ~ Ngang ~ De.
                float basePower = 0f;
                foreach (var name in target.heroes)
                {
                    var hero = Find(heroes, name);
                    if (hero != null) basePower += UnitPower(hero, gacha, 1f, 1f);
                }
                foreach (var name in target.soldiers)
                {
                    var soldier = Find(soldiers, name);
                    if (soldier != null) basePower += UnitPower(soldier, gacha, 1f, 1f);
                }
                float goal = mine * scale;
                target.strength = basePower > 0f ? Mathf.Max(0.5f, (goal - cannonPower) / basePower) : 1f;
                target.power = Mathf.RoundToInt(cannonPower + basePower * target.strength);
                target.shards = TierShards[tier];
                data.targets.Add(target);
            }
            data.scannedAt = Now;
            SaveSystem.Save();
        }

        static List<Rarity> Rarities<T>(List<T> units, Func<T, string> unitId, GachaConfig gacha, int minimum)
        {
            var rarities = units.ConvertAll(u => RarityOf(gacha, unitId(u)));
            while (rarities.Count < minimum) rarities.Add(Rarity.N);
            return rarities;
        }

        /// <summary>Boc ngau nhien (va bo khoi pool) mot unit pham <paramref name="rarity"/>; khong co thi bat ky.</summary>
        static T TakeSameRarity<T>(List<T> pool, Rarity rarity, GachaConfig gacha, Func<T, string> unitId)
        {
            var same = pool.FindAll(u => RarityOf(gacha, unitId(u)) == rarity);
            var from = same.Count > 0 ? same : pool;
            var pick = from[Random.Range(0, from.Count)];
            pool.Remove(pick);
            return pick;
        }

        /// <summary>Vang thuong ha thanh: vang thang wave dang danh x he so do kho x chuoi thang (TAM).</summary>
        public static int Gold(ConquestTarget target, WaveTuning tuning)
        {
            int baseGold = tuning != null ? tuning.WaveGold(Mathf.Max(1, SaveSystem.Data.nextWave)) : 100;
            return Mathf.RoundToInt(baseGold * TierGold[target.tier] * (1f + StreakBonus * Data.streak));
        }

        public static bool CanAttack(ConquestTarget target) => target != null && !target.done && AttacksLeft > 0;

        /// <summary>Admin: tra lai du luot trong ngay (thanh da danh van "da danh" — Do Tham lai de co muc tieu moi).</summary>
        public static void DevResetAttacks()
        {
            Data.attacksUsed = 0;
            SaveSystem.Save();
        }

        /// <summary>Bam bat dau tran: tru 1 luot, thanh nay khong danh lai duoc nua (thang hay thua).</summary>
        public static void ConsumeAttack(ConquestTarget target)
        {
            var data = Data;
            if (data.day != Today)
            {
                data.day = Today;
                data.attacksUsed = 0;
            }
            data.attacksUsed++;
            target.done = true;
            SaveSystem.Save();
        }

        /// <summary>Ket thuc tran: thang thi chuoi +1 va cong manh cho unit so huu ngau nhien; thua thi chuoi ve 0.</summary>
        public static int Finish(ConquestTarget target, bool won)
        {
            var data = Data;
            int shards = 0;
            if (won)
            {
                data.streak++;
                QuestLog.CountConquestWin();   // nhiem vu "Win a Conquest battle"
                var owned = SaveSystem.Data.owned;
                for (int i = 0; i < target.shards && owned.Count > 0; i++, shards++)
                    owned[Random.Range(0, owned.Count)].shards++;
            }
            else data.streak = 0;
            SaveSystem.Save();
            return shards;
        }

        public static T Find<T>(T[] pool, string name) where T : UnityEngine.Object =>
            string.IsNullOrEmpty(name) ? null : Array.Find(pool, d => d != null && d.name == name);

        static int CannonLevel(CannonDefinition cannon) => Mathf.Max(1, Progression.Owned(cannon.unitId)?.level ?? 1);

        static int AverageStars(List<HeroDefinition> heroes)
        {
            int sum = 0;
            foreach (var hero in heroes) sum += Progression.Stars(hero.unitId);
            return heroes.Count > 0 ? Mathf.RoundToInt((float)sum / heroes.Count) : 0;
        }

        static int Today => int.Parse(DateTime.Now.ToString("yyyyMMdd"));
        static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
