using System;
using System.Collections.Generic;
using BuildBuild.Data;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuildBuild.Meta
{
    /// <summary>
    /// Tien do he nhiem vu trong save (`SaveData.quests`): id da nhan thuong, ve chon tuong dang cho, va ba bo dem
    /// KHONG suy ra duoc tu trang thai khac (quay gacha / thang Chinh Phat / nhan ruong AFK).
    /// Khong luu tien do tung nhiem vu — xem `QuestGoal`.
    /// </summary>
    [Serializable]
    public class QuestSave
    {
        public List<string> claimed = new List<string>();
        /// <summary>Ve chon tuong chua dung ("sr" / "ssr"); phan tu dau la ve dang cho nguoi choi bam chon.</summary>
        public List<string> vouchers = new List<string>();
        public int gachaRolls;
        public int conquestWins;
        public int afkClaims;
        /// <summary>Hero vua duoc Hanh trinh tang — o thuong "manh" cong vao con nay.</summary>
        public string lastHero = "";
    }

    /// <summary>
    /// Ket qua mot lan nhan thuong: du de UI hien popup ma khong phai doc lai save.
    /// La CLASS (khong phai struct) de `heroName` luon la "" chu khong phai null — struct default de nguyen null, va
    /// moi cho doc `.Length` se no NullReferenceException giua Play (da dinh 2026-10-15: bam Claim la no).
    /// Ve chon tuong KHONG nam o day: no la thuoc tinh cua chinh nhiem vu (`Quest.reward.voucher`), khong phai ket qua.
    /// </summary>
    public class QuestClaim
    {
        public int gold;
        public string heroName = "";
        public bool heroIsNew;
        public int shards;
    }

    /// <summary>
    /// Luat cua he nhiem vu (GAME_DESIGN.md muc 11). Noi dung + so o `QuestCatalog`; o day chi co LUAT:
    ///   - Moc N+1 mo khi da NHAN HET thuong cua moc N (ke ca o thuong cuoi moc) — khong the bo qua moc;
    ///   - Tien do doc thang tu save nen LUON hoi to, khong bao gio bat nguoi choi lam lai;
    ///   - Tang tuong: chua so huu -> nguyen con; da so huu -> quy ra manh theo bac (khong co phan thuong vo dung);
    ///   - Ve chon tuong nam trong save toi khi nguoi choi bam chon, khong het han.
    /// Khong cham vao `Economy` (Core) hay UI: VANG duoc tra ve cho nguoi goi cong, dung khuon `AfkChest.Claim`.
    /// </summary>
    public static class QuestLog
    {
        static QuestSave Data => SaveSystem.Data.quests ??= new QuestSave();
        static List<string> Claimed => Data.claimed ??= new List<string>();
        static List<string> Vouchers => Data.vouchers ??= new List<string>();

        /// <summary>Ve chon tuong dang cho ("" = khong co).</summary>
        public static string PendingVoucher => Vouchers.Count > 0 ? Vouchers[0] : "";

        public static bool IsClaimed(string id) => Claimed.Contains(id);

        /// <summary>Moc dang mo = moc dau tien con dong chua nhan; xong het thi tra moc cuoi.</summary>
        public static int Stage
        {
            get
            {
                for (int stage = 1; stage <= QuestCatalog.StageCount; stage++)
                    foreach (var quest in QuestCatalog.All)
                        if (quest.stage == stage && !IsClaimed(quest.id)) return stage;
                return QuestCatalog.StageCount;
            }
        }

        /// <summary>Moc 1 luon mo; moc sau doi nhan het thuong cua moc truoc.</summary>
        static bool IsUnlocked(Quest quest)
        {
            if (quest == null || quest.stage <= 1) return true;
            foreach (var other in QuestCatalog.All)
                if (other.stage == quest.stage - 1 && !IsClaimed(other.id)) return false;
            return true;
        }

        public static int Progress(Quest quest, GachaConfig gacha)
        {
            if (quest == null) return 0;
            switch (quest.goal)
            {
                case QuestGoal.ClearWave: return Mathf.Max(0, SaveSystem.Data.nextWave - 1);
                case QuestGoal.OwnHeroes: return OwnedHeroes(gacha);
                case QuestGoal.MaxHeroLevel: return MaxHeroLevel(gacha);
                case QuestGoal.GachaRolls: return Data.gachaRolls;
                case QuestGoal.ConquestWins: return Data.conquestWins;
                case QuestGoal.AfkClaims: return Data.afkClaims;
                case QuestGoal.Chapter: return ClaimedInStage(quest.stage);
            }
            return 0;
        }

        static bool IsComplete(Quest quest, GachaConfig gacha) =>
            quest != null && Progress(quest, gacha) >= Mathf.Max(1, quest.target);

        public static bool CanClaim(Quest quest, GachaConfig gacha) =>
            quest != null && !IsClaimed(quest.id) && IsUnlocked(quest) && IsComplete(quest, gacha);

        /// <summary>So o dang bam nhan duoc — dung cho cham do tren nut HUD.</summary>
        public static int ClaimableCount(GachaConfig gacha)
        {
            int count = 0;
            foreach (var quest in QuestCatalog.All)
                if (CanClaim(quest, gacha)) count++;
            return count;
        }

        /// <summary>
        /// Danh dau da nhan + giao thuong (tuong / manh / ve ghi thang vao save). VANG khong cong o day — tra ve cho
        /// nguoi goi cong qua `Economy`, nho vay HUD nghe duoc `Economy.Changed`.
        /// </summary>
        public static QuestClaim Claim(Quest quest, GachaConfig gacha)
        {
            var claim = new QuestClaim();
            // Dieu kien nam o DAY chu khong o man hinh: `Claim` la API cong khai cua tang luat, nen caller thu
            // hai (nut "nhan het", auto-claim sau tran, admin) khong duoc phat thuong cua o chua mo / chua xong.
            if (!CanClaim(quest, gacha)) return claim;

            Claimed.Add(quest.id);
            claim.gold = quest.reward.gold;

            if (!string.IsNullOrEmpty(quest.reward.hero))
            {
                var granted = GrantHero(quest.reward.hero, gacha);
                claim.heroName = granted.heroName;
                claim.heroIsNew = granted.heroIsNew;
                claim.shards += granted.shards;
            }
            if (!string.IsNullOrEmpty(quest.reward.voucher)) Vouchers.Add(quest.reward.voucher);
            if (quest.reward.shards > 0) claim.shards += GrantShards(quest.reward.shards);

            SaveSystem.Save();
            return claim;
        }

        /// <summary>Nguoi choi bam chon tuong trong ve: giao hero (hoac manh neu da co) roi tieu ve.</summary>
        public static QuestClaim UseVoucher(string unitId, GachaConfig gacha)
        {
            string voucher = PendingVoucher;
            if (voucher.Length == 0 || Array.IndexOf(QuestCatalog.VoucherPool(voucher), unitId) < 0) return new QuestClaim();
            var claim = GrantHero(unitId, gacha);
            Vouchers.RemoveAt(0);
            SaveSystem.Save();
            return claim;
        }

        // ---- Bo dem: noi goi (GachaSystem / ConquestMap / AfkChest) tu Save() ngay sau do, nen o day khong Save. ----
        public static void CountGacha(int rolls) => Data.gachaRolls += Mathf.Max(0, rolls);
        public static void CountConquestWin() => Data.conquestWins++;
        public static void CountAfkClaim() => Data.afkClaims++;

        /// <summary>Admin (F1): xoa sach tien do nhiem vu de thu lai tu moc 1.</summary>
        public static void DevReset()
        {
            SaveSystem.Data.quests = new QuestSave();
            SaveSystem.Save();
        }

        /// <summary>Tang nguyen con, hoac +manh neu da so huu. `heroId` la `UnitDefinition.id`.</summary>
        static QuestClaim GrantHero(string heroId, GachaConfig gacha)
        {
            var claim = new QuestClaim();
            var definition = FindUnit(gacha, heroId);
            claim.heroName = definition != null ? definition.displayName : heroId;

            var owned = SaveSystem.Data.owned.Find(o => o.id == heroId);
            if (owned == null)
            {
                SaveSystem.Data.owned.Add(new OwnedUnit { id = heroId });
                claim.heroIsNew = true;
            }
            else
            {
                claim.shards = QuestCatalog.ShardsFor(definition != null ? definition.rarity : Rarity.N);
                owned.shards += claim.shards;
            }
            Data.lastHero = heroId;
            return claim;
        }

        /// <summary>Manh roi vao hero vua duoc Hanh trinh tang; chua tang con nao thi vao mot unit dang so huu (ngau nhien).</summary>
        static int GrantShards(int amount)
        {
            var owned = SaveSystem.Data.owned;
            if (owned.Count == 0) return 0;
            var target = !string.IsNullOrEmpty(Data.lastHero) ? owned.Find(o => o.id == Data.lastHero) : null;
            target ??= owned[Random.Range(0, owned.Count)];
            target.shards += amount;
            return amount;
        }

        static int ClaimedInStage(int stage)
        {
            int done = 0;
            foreach (var quest in QuestCatalog.All)
                if (quest.stage == stage && !quest.IsChapterReward && IsClaimed(quest.id)) done++;
            return done;
        }

        static int OwnedHeroes(GachaConfig gacha)
        {
            int count = 0;
            foreach (var owned in SaveSystem.Data.owned)
                if (IsHero(gacha, owned.id)) count++;
            return count;
        }

        static int MaxHeroLevel(GachaConfig gacha)
        {
            int best = 0;
            foreach (var owned in SaveSystem.Data.owned)
                if (IsHero(gacha, owned.id)) best = Mathf.Max(best, owned.level);
            return best;
        }

        static bool IsHero(GachaConfig gacha, string unitId)
        {
            var definition = FindUnit(gacha, unitId);
            return definition != null && definition.category == UnitCategory.Hero;
        }

        static UnitDefinition FindUnit(GachaConfig gacha, string unitId)
        {
            if (gacha == null || string.IsNullOrEmpty(unitId)) return null;
            foreach (var unit in gacha.pool)
                if (unit != null && unit.id == unitId) return unit;
            return null;
        }
    }
}
