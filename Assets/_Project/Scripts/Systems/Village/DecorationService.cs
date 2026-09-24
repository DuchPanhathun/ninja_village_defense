using System.Collections.Generic;
using NinjaVillage.Core.Data;
using NinjaVillage.Core.Events;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Save;
using UnityEngine;

namespace NinjaVillage.Systems.Village
{
    /// <summary>Raised whenever the placed decorations change (bought, moved, sold).</summary>
    public readonly struct DecorationsChangedEvent : IGameEvent { }

    /// <summary>
    /// The village decoration shop and layout: buying places a decoration (paid on placement, so
    /// cancelling costs nothing), moving re-checks the spot, selling refunds half. Placement follows
    /// <see cref="DecorationRules"/> against the building plots, paths and displays of
    /// <see cref="VillageLayout"/>. State lives in <c>SaveService.Data.Village.Decorations</c>.
    /// </summary>
    public static class DecorationService
    {
        public static DecorationCatalog Catalog => CatalogLoader.Load<DecorationCatalog>();

        private static VillageSaveData Data => SaveService.Data.Village;

        public static IReadOnlyList<PlacedDecoration> Placed => Data.Decorations;

        public static DecorationDefinition Get(string id)
        {
            var catalog = Catalog;
            return catalog != null ? catalog.Get(id) : null;
        }

        public static PlacedDecoration Find(int uid)
        {
            foreach (var placed in Data.Decorations)
                if (placed.Uid == uid) return placed;
            return null;
        }

        /// <summary>Shop order: by category, then price.</summary>
        public static List<DecorationDefinition> GetShopItems()
        {
            var list = new List<DecorationDefinition>();
            var catalog = Catalog;
            if (catalog == null) return list;
            foreach (var item in catalog.All)
                if (item != null) list.Add(item);
            list.Sort((a, b) => a.Category != b.Category ? a.Category.CompareTo(b.Category) : a.Price.Amount.CompareTo(b.Price.Amount));
            return list;
        }

        public static int CountPlaced(DecorationDefinition definition)
        {
            int count = 0;
            if (definition == null) return 0;
            foreach (var placed in Data.Decorations)
                if (placed.Id == definition.Id) count++;
            return count;
        }

        /// <summary>Whether <paramref name="definition"/> fits at <paramref name="position"/> (ignoring decoration <paramref name="ignoreUid"/>).</summary>
        public static PlacementBlocker CheckPlacement(DecorationDefinition definition, Vector2 position, int ignoreUid = 0)
        {
            if (definition == null) return PlacementBlocker.OutsideVillage;
            var others = new List<(Vector2, float)>();
            foreach (var placed in Data.Decorations)
            {
                if (placed.Uid == ignoreUid) continue;
                var other = Get(placed.Id);
                others.Add((new Vector2(placed.X, placed.Y), other != null ? other.Radius : 0.5f));
            }
            return DecorationRules.Check(position, definition.Radius, VillageLayout.Bounds, BlockedAreas(),
                VillageLayout.ReservedSpots(), others);
        }

        /// <summary>Building plots (their base, where the walls stand) and every path.</summary>
        public static IEnumerable<Rect> BlockedAreas()
        {
            var catalog = VillageService.Catalog;
            if (catalog != null)
                foreach (var building in catalog.All)
                {
                    if (building == null) continue;
                    Vector2 p = building.PlotPosition, f = building.Footprint;
                    yield return new Rect(p.x - f.x * 0.75f, p.y - f.y * 0.6f, f.x * 1.5f, f.y * 1.3f);
                }
            foreach (var (center, size) in VillageLayout.Paths)
                yield return new Rect(center - size * 0.5f, size);
            yield return new Rect(VillageLayout.Plaza - VillageLayout.PlazaSize * 0.5f, VillageLayout.PlazaSize);
        }

        /// <summary>Pays for and places a new decoration. Fails (and charges nothing) if the spot is blocked or it's unaffordable.</summary>
        public static bool TryBuy(DecorationDefinition definition, Vector2 position, bool flip, out string error)
        {
            error = null;
            if (definition == null) { error = "Unknown decoration."; return false; }
            position = DecorationRules.Snap(position);
            var blocker = CheckPlacement(definition, position);
            if (blocker != PlacementBlocker.None) { error = DecorationRules.Describe(blocker); return false; }
            if (!CurrencyService.TrySpend(definition.Price, $"decoration_{definition.Id}"))
            {
                error = $"Not enough {definition.Price.Currency.ToString().ToLowerInvariant()}.";
                return false;
            }

            Data.Decorations.Add(new PlacedDecoration
            {
                Uid = Data.NextDecorationUid++, Id = definition.Id, X = position.x, Y = position.y, Flip = flip,
            });
            Changed();
            return true;
        }

        public static bool TryMove(int uid, Vector2 position, bool flip, out string error)
        {
            error = null;
            var placed = Find(uid);
            var definition = placed != null ? Get(placed.Id) : null;
            if (definition == null) { error = "Unknown decoration."; return false; }
            position = DecorationRules.Snap(position);
            var blocker = CheckPlacement(definition, position, uid);
            if (blocker != PlacementBlocker.None) { error = DecorationRules.Describe(blocker); return false; }
            placed.X = position.x;
            placed.Y = position.y;
            placed.Flip = flip;
            Changed();
            return true;
        }

        /// <summary>Removes a decoration and refunds <see cref="DecorationRules.RefundShare"/> of its price.</summary>
        public static bool TrySell(int uid, out Price refund)
        {
            refund = default;
            var placed = Find(uid);
            if (placed == null) return false;
            var definition = Get(placed.Id);
            Data.Decorations.Remove(placed);
            if (definition != null)
            {
                refund = new Price(definition.Price.Currency, DecorationRules.Refund(definition.Price.Amount));
                CurrencyService.Grant(refund, $"decoration_sell_{definition.Id}");
            }
            Changed();
            return true;
        }

        private static void Changed()
        {
            SaveService.MarkDirty();
            EventBus<DecorationsChangedEvent>.Raise(new DecorationsChangedEvent());
        }
    }
}
