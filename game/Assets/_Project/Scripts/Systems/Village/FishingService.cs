using System;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Farm;
using NinjaVillage.Systems.Save;

namespace NinjaVillage.Systems.Village
{
    /// <summary>Raised when a cast is used or a fish is landed.</summary>
    public readonly struct FishingChangedEvent : IGameEvent { }

    /// <summary>
    /// The fishing pond (EPIC 24 Phase 5): each cast uses one of <see cref="PondMineRules.MaxCasts"/> casts (they
    /// refill over real time), a fish bites, and the timing mini-game's result lands it — fish go to the storehouse
    /// (for selling or Sushi), a Golden Koi becomes a koi-pond decoration to place for free. State in
    /// <c>SaveService.Data.Fishing</c>; rules in <see cref="PondMineRules"/>.
    /// </summary>
    public static class FishingService
    {
        private static FishingSaveData Data => SaveService.Data.Fishing;

        /// <summary>Casts left right now (refilled first).</summary>
        public static int Casts
        {
            get
            {
                Refill();
                return Data.Casts;
            }
        }

        public static TimeSpan UntilNextCast
        {
            get
            {
                Refill();
                return PondMineRules.UntilNextCast(Data.Casts, Data.RegenTicks, GameClock.UtcNow);
            }
        }

        public static int TotalCaught => Data.TotalCaught;

        private static void Refill()
        {
            var data = Data;
            var (casts, regen) = PondMineRules.Casts(data.Casts, data.RegenTicks, GameClock.UtcNow);
            if (casts == data.Casts && regen == data.RegenTicks) return;
            data.Casts = casts;
            data.RegenTicks = regen;
            SaveService.MarkDirty();
        }

        /// <summary>Uses a cast and picks what bites. False when there are no casts left.</summary>
        public static bool TryCast(out FishKind fish, Random rng = null)
        {
            fish = default;
            Refill();
            var data = Data;
            if (data.Casts <= 0) return false;
            if (data.Casts >= PondMineRules.MaxCasts) data.RegenTicks = GameClock.UtcNow.Ticks; // the refill clock starts now
            data.Casts--;
            fish = PondMineRules.PickFish((rng ?? new Random()).NextDouble(), VillageService.CastleLevel);
            Changed();
            return true;
        }

        /// <summary>Lands (or loses) the fish from a cast; returns how many were caught (0, 1, or 2 for a perfect catch).</summary>
        public static int Land(FishKind fish, CatchResult result)
        {
            if (result == CatchResult.Miss) return 0;
            int amount = result == CatchResult.Perfect ? 2 : 1;
            if (fish.GoodsId != null) GoodsService.Add(fish.GoodsId, amount);
            else DecorationService.Gift(DecorationService.Get(PondMineRules.KoiDecorationId), 1); // one koi pond, however perfect
            Data.TotalCaught += amount;
            Progress.Report(ProgressStatIds.FishCaught, amount, fish.Id);
            Changed();
            return amount;
        }

        private static void Changed()
        {
            SaveService.MarkDirty();
            EventBus<FishingChangedEvent>.Raise(new FishingChangedEvent());
        }
    }
}
