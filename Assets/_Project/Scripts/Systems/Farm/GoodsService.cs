using System.Collections.Generic;
using NinjaVillage.Core.Data;
using NinjaVillage.Core.Events;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Save;

namespace NinjaVillage.Systems.Farm
{
    /// <summary>Raised whenever the storehouse changes.</summary>
    public readonly struct GoodsChangedEvent : IGameEvent { }

    /// <summary>
    /// The village storehouse (Phase A): counts of every kind of goods, adding harvests, spending
    /// ingredients (the Kitchen), and selling for coins. State in <c>SaveService.Data.Goods</c>.
    /// </summary>
    public static class GoodsService
    {
        public static GoodsCatalog Catalog => CatalogLoader.Load<GoodsCatalog>();

        private static GoodsSaveData Data => SaveService.Data.Goods;

        public static GoodsDefinition Get(string id)
        {
            var catalog = Catalog;
            return catalog != null ? catalog.Get(id) : null;
        }

        public static int Count(string id) => Data.Items.GetLevel(id);
        public static int Count(GoodsDefinition goods) => goods != null ? Count(goods.Id) : 0;

        /// <summary>Everything in stock, in catalog order.</summary>
        public static List<(GoodsDefinition goods, int count)> InStock()
        {
            var list = new List<(GoodsDefinition, int)>();
            var catalog = Catalog;
            if (catalog == null) return list;
            foreach (var goods in catalog.All)
            {
                int count = Count(goods);
                if (goods != null && count > 0) list.Add((goods, count));
            }
            list.Sort((a, b) => a.Item1.SortOrder.CompareTo(b.Item1.SortOrder));
            return list;
        }

        public static void Add(string id, int amount)
        {
            if (string.IsNullOrEmpty(id) || amount <= 0) return;
            Data.Items.SetLevel(id, Count(id) + amount);
            Changed();
        }

        public static bool TrySpend(string id, int amount)
        {
            if (amount <= 0) return true;
            int have = Count(id);
            if (have < amount) return false;
            Data.Items.SetLevel(id, have - amount);
            Changed();
            return true;
        }

        /// <summary>Sells <paramref name="amount"/> (clamped to what's in stock) for coins; returns the coins earned.</summary>
        public static int Sell(GoodsDefinition goods, int amount)
        {
            if (goods == null) return 0;
            amount = System.Math.Min(amount, Count(goods));
            if (amount <= 0 || !TrySpend(goods.Id, amount)) return 0;
            int coins = amount * goods.SellPrice;
            CurrencyService.Grant(CurrencyType.Coins, coins, $"sell_{goods.Id}");
            return coins;
        }

        private static void Changed()
        {
            SaveService.MarkDirty();
            EventBus<GoodsChangedEvent>.Raise(new GoodsChangedEvent());
        }
    }
}
