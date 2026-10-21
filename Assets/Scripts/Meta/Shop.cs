using System;
using System.Collections.Generic;
using BuildBuild.Data;

namespace BuildBuild.Meta
{
    /// <summary>Loai quay cua cua hang — moi loai co chu ky lam moi va so o rieng.</summary>
    public enum ShopKind { Daily, Weekly, Monthly, Offer }

    /// <summary>Mot o trong cua hang: manh cua mot unit DANG so huu, hoac mot mon trang bi.</summary>
    [Serializable]
    public struct ShopOffer
    {
        /// <summary>unitId cua hero duoc ban manh; RONG = o nay ban trang bi.</summary>
        public string heroId;
        public int shards;
        public EquipmentSlot slot;
        public Rarity rarity;
        public int price;

        public bool IsGear => string.IsNullOrEmpty(heroId);
    }

    /// <summary>Mot ky ban hang: `bought` chi co nghia khi `key` con la ky hien tai.</summary>
    [Serializable]
    public class ShopPeriod
    {
        public string kind = "";
        public string key = "";
        public List<int> bought = new List<int>();
        public List<ShopOffer> stock = new List<ShopOffer>();
    }

    [Serializable]
    public class ShopSave
    {
        /// <summary>Moi loai cua hang mot ky (xem `Shop.KeyOf`) — chi giu ky hien tai.</summary>
        public List<ShopPeriod> periods = new List<ShopPeriod>();

        // Truong CU thoi chi co lo Daily — doc MOT lan roi chuyen sang `periods` (xem `FindPeriod`).
        public string day = "";
        public List<int> bought = new List<int>();
    }

    /// <summary>
    /// Cua hang (GDD 10.4): bon loai ban hang, moi loai mot chu ky lam moi — `Daily` (ngay), `Weekly` (thu Hai),
    /// `Monthly` (ngay 1), `Offer` (mot lan, khong lam moi). Man hinh la scene rieng (`UI/ShopScene.cs`), khong
    /// con la bang phu trong scene Battle nhu truoc 2026-10-20.
    ///
    /// Luat chon hang: manh chi ban cho unit NGUOI CHOI DA SO HUU (manh cua unit chua co thi chua dung duoc
    /// vao viec gi); trang bi chi ban tu R tro len. Moi o mua mot lan trong ky.
    ///
    /// So o / so manh / gia deu la TAM (giong GDD 7.2) — doi o day, khong rai trong UI.
    /// </summary>
    public static class Shop
    {
        public const int MaxSlots = 4;

        const string DailyKey = "daily";
        const string WeeklyKey = "weekly";
        const string MonthlyKey = "monthly";
        const string OfferKey = "offer";

        public static int Slots(ShopKind kind)
        {
            switch (kind)
            {
                case ShopKind.Weekly: return 3;
                case ShopKind.Monthly: return 2;
                case ShopKind.Offer: return 3;
                default: return MaxSlots;
            }
        }

        static int HeroSlotsOf(ShopKind kind)
        {
            switch (kind)
            {
                case ShopKind.Weekly: return 2;
                case ShopKind.Monthly: return 1;
                case ShopKind.Offer: return 3;
                default: return 3;
            }
        }

        static int ShardsOf(ShopKind kind)
        {
            switch (kind)
            {
                case ShopKind.Weekly: return 15;
                case ShopKind.Monthly: return 30;
                case ShopKind.Offer: return 25;
                default: return 5;
            }
        }

        /// <summary>
        /// Lo hang cua mot loai. `rarityOf` tra bac cua mot unitId — do lop UI (co `UnitDefinition[]`) cung
        /// cap, vi `SaveData.owned` khong luu bac.
        /// </summary>
        public static ShopOffer[] Offers(ShopKind kind, Func<string, Rarity> rarityOf)
        {
            var save = SaveSystem.Data;
            var period = FindPeriod(kind);
            string key = KeyOf(kind);
            if (period == null)
            {
                period = new ShopPeriod { kind = KindName(kind), key = key };
                save.shop.periods.Add(period);
            }
            if (period.key != key)
            {
                period.key = key;
                period.bought.Clear();
                period.stock.Clear();
            }
            if (period.stock.Count > 0) return period.stock.ToArray();
            var result = new ShopOffer[MaxSlots];
            int n = 0;
            int seed = SeedOf(kind);
            int shards = ShardsOf(kind);

            int heroSlots = HeroSlotsOf(kind);
            var used = new List<int>();
            for (int k = 0; k < heroSlots && save.owned.Count > 0; k++)
            {
                // Acc it unit: van lay vong qua danh sach nhung KHONG ban trung mot hero hai o — truoc day acc
                // co 1 unit thi ca ba o Daily la cung mot con, gia nhu nhau.
                int at = (seed + k) % save.owned.Count;
                for (int step = 0; step < save.owned.Count && used.Contains(at); step++) at = (at + 1) % save.owned.Count;
                if (used.Contains(at)) break;
                used.Add(at);
                var unit = save.owned[at];
                var rarity = rarityOf != null ? rarityOf(unit.id) : Rarity.N;
                result[n++] = new ShopOffer
                {
                    heroId = unit.id,
                    shards = shards,
                    price = BundlePrice(rarity, shards),
                };
            }

            // `Offer` chi ban manh (goi uu dai mot lan), ba loai kia co them o trang bi.
            if (kind != ShopKind.Offer)
            {
                var rarity = kind == ShopKind.Monthly ? Rarity.SSR : GearRarity(kind, seed);
                result[n++] = new ShopOffer
                {
                    slot = (EquipmentSlot)(seed % 4),
                    rarity = rarity,
                    price = GearPrice(rarity),
                };
            }
            period.stock.AddRange(result);
            SaveSystem.Save();
            return result;
        }

        /// <summary>Cac o con mua duoc trong ky hien tai — UI hien o dong duoi tieu de.</summary>
        public static int Remaining(ShopKind kind, Func<string, Rarity> rarityOf)
        {
            var offers = Offers(kind, rarityOf);
            int left = 0;
            for (int i = 0; i < offers.Length; i++)
                if (offers[i].price > 0 && !Bought(kind, i)) left++;
            return left;
        }

        public static bool Bought(ShopKind kind, int index)
        {
            var period = FindPeriod(kind);
            return period != null && period.key == KeyOf(kind) && period.bought.Contains(index);
        }

        /// <summary>
        /// Mua mot o. `spend` tra false khi khong du vang (noi goi quyet dinh bao loi the nao) — cung luat voi
        /// `GachaSystem` (khong tu tru vang).
        /// </summary>
        public static bool TryBuy(ShopKind kind, int index, ShopOffer offer, Func<int, bool> spend)
        {
            if (index < 0 || index >= Slots(kind) || offer.price <= 0) return false;
            if (Bought(kind, index)) return false;
            if (!offer.IsGear && SaveSystem.Data.Find(offer.heroId) == null) return false;
            if (spend == null || !spend(offer.price)) return false;

            if (offer.IsGear)
            {
                Armory.Add(offer.slot, offer.rarity);
            }
            else
            {
                var unit = SaveSystem.Data.Find(offer.heroId);
                if (unit != null) unit.shards += offer.shards;
            }

            MarkBought(kind, index);
            SaveSystem.Save();
            return true;
        }

        /// <summary>Chu thich duoi tieu de: loai nay lam moi khi nao + con bao lau.</summary>
        public static string RestockLabel(ShopKind kind)
        {
            switch (kind)
            {
                case ShopKind.Weekly: return $"Restocks Monday · {TimeLeft(NextMonday())}";
                case ShopKind.Monthly: return $"Restocks on the 1st · {TimeLeft(NextMonth())}";
                case ShopKind.Offer: return "One-time offers — no restock";
                default: return $"Restocks daily · {TimeLeft(DateTime.Now.Date.AddDays(1))}";
            }
        }

        /// <summary>Khoa cua ky hien tai — doi khoa la ca lo hang coi nhu moi.</summary>
        public static string KeyOf(ShopKind kind)
        {
            switch (kind)
            {
                case ShopKind.Weekly: return "W" + NextMonday().AddDays(-7).ToString("yyyy-MM-dd");
                case ShopKind.Monthly: return "M" + DateTime.Now.ToString("yyyy-MM");
                case ShopKind.Offer: return "O";
                default: return "D" + DateTime.Now.ToString("yyyy-MM-dd");
            }
        }

        public static int ShardPrice(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.SSR: return 1400;
                case Rarity.SR: return 700;
                case Rarity.R: return 300;
                default: return 150;
            }
        }

        /// <summary>Gia cho goi `shards` manh — tinh thang tu gia 5 manh (khong giam gia), so TAM.</summary>
        public static int BundlePrice(Rarity rarity, int shards) => ShardPrice(rarity) * shards / 5;

        public static int GearPrice(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.SSR: return 3200;
                case Rarity.SR: return 1600;
                default: return 800;
            }
        }

        // Seeded theo ky: cung mot ky thi ca ba/ bon o deu on dinh, sang ky moi thi doi.
        static int SeedOf(ShopKind kind)
        {
            switch (kind)
            {
                case ShopKind.Weekly: return int.Parse(KeyOf(ShopKind.Weekly).Substring(1).Replace("-", ""));
                case ShopKind.Monthly: return DateTime.Now.Year * 12 + DateTime.Now.Month;
                case ShopKind.Offer: return 0;   // khong xoay: goi uu dai luon la cua acc nay
                default: return int.Parse(DateTime.Now.ToString("yyyyMMdd"));
            }
        }

        static Rarity GearRarity(ShopKind kind, int seed) =>
            kind == ShopKind.Weekly
                ? (Rarity)(1 + seed / 7 % 3)      // R / SR / SSR xoay theo tuan
                : (Rarity)(1 + seed / 4 % 3);     // R / SR / SSR xoay theo ngay

        static string KindName(ShopKind kind)
        {
            switch (kind)
            {
                case ShopKind.Weekly: return WeeklyKey;
                case ShopKind.Monthly: return MonthlyKey;
                case ShopKind.Offer: return OfferKey;
                default: return DailyKey;
            }
        }

        static DateTime NextMonday()
        {
            var now = DateTime.Now.Date;
            int days = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
            return now.AddDays(days == 0 ? 7 : days);
        }

        static DateTime NextMonth() => new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);

        static string TimeLeft(DateTime target)
        {
            var span = target - DateTime.Now;
            if (span.TotalDays >= 1) return $"{(int)span.TotalDays}d {span.Hours}h";
            return $"{span.Hours}h {span.Minutes:00}m";
        }

        /// <summary>
        /// Ky cua mot loai (co the chua co). Doc luon chuyen lo Daily cua ban cu sang `periods` mot lan —
        /// khong chuyen thi ngay cap nhat, nguoi choi mua lai duoc cac o da mua hom nay.
        /// </summary>
        static ShopPeriod FindPeriod(ShopKind kind)
        {
            var save = SaveSystem.Data.shop;
            if (!string.IsNullOrEmpty(save.day))
            {
                string today = KeyOf(ShopKind.Daily);
                bool hasDaily = save.periods.Exists(p => p.kind == DailyKey);
                if (!hasDaily && save.day == today)
                    save.periods.Add(new ShopPeriod { kind = DailyKey, key = save.day, bought = new List<int>(save.bought) });
                save.day = "";
                save.bought.Clear();
            }
            return save.periods.Find(p => p.kind == KindName(kind));
        }

        static void MarkBought(ShopKind kind, int index)
        {
            string key = KeyOf(kind);
            var period = FindPeriod(kind);
            if (period == null)
            {
                period = new ShopPeriod { kind = KindName(kind) };
                SaveSystem.Data.shop.periods.Add(period);
            }
            if (period.key != key)
            {
                period.key = key;
                period.bought.Clear();
                // Doi ky ma khong xoa snapshot thi sang ngay moi van ban hang hom qua (`Offers` thay `stock`
                // khac rong nen tuong da co lo cho ky nay roi tra ve luon).
                period.stock.Clear();
            }
            if (!period.bought.Contains(index)) period.bought.Add(index);
        }
    }
}
