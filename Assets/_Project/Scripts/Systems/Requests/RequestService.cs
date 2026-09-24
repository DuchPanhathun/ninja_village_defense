using System.Collections.Generic;
using System.Text;
using NinjaVillage.Core.Data;
using NinjaVillage.Core.Events;
using NinjaVillage.Core.Utilities;
using NinjaVillage.Systems.Chapters;
using NinjaVillage.Systems.Daily;
using NinjaVillage.Systems.Economy;
using NinjaVillage.Systems.Farm;
using NinjaVillage.Systems.Save;
using NinjaVillage.Systems.Village;

namespace NinjaVillage.Systems.Requests
{
    /// <summary>Raised when requests are posted, make progress or are delivered.</summary>
    public readonly struct RequestsChangedEvent : IGameEvent { }

    /// <summary>What delivering a request paid out.</summary>
    public readonly struct RequestReward
    {
        public readonly int Coins;
        public readonly int Gems;
        public readonly DecorationDefinition Decoration;

        public RequestReward(int coins, int gems, DecorationDefinition decoration)
        {
            Coins = coins;
            Gems = gems;
            Decoration = decoration;
        }

        public override string ToString()
        {
            var parts = new List<string>();
            if (Coins > 0) parts.Add($"{Coins} coins");
            if (Gems > 0) parts.Add($"{Gems} gems");
            if (Decoration != null) parts.Add(Decoration.NameOrId);
            return string.Join(", ", parts);
        }
    }

    /// <summary>
    /// Villager requests (EPIC 24 Phase 3): every day three villagers each ask for a favour — bring goods from
    /// the storehouse, do something in battle or around the village, or clear the next chapter. Stat requests
    /// count from the day they're posted through the same progress feed as quests (<see cref="QuestTracker"/>
    /// calls <see cref="Record"/>). Delivering pays coins (more with a bigger castle), sometimes gems or a
    /// decoration to place for free. New requests replace the old ones at the daily reset.
    /// </summary>
    public static class RequestService
    {
        public static VillagerRequestCatalog Catalog => CatalogLoader.Load<VillagerRequestCatalog>();

        private static RequestSaveData Data => SaveService.Data.Requests;

        public static VillagerRequestDefinition Get(string id)
        {
            var catalog = Catalog;
            return catalog != null ? catalog.Get(id) : null;
        }

        /// <summary>Today's requests (posting them first if the day changed).</summary>
        public static IReadOnlyList<VillagerRequestState> Active
        {
            get
            {
                EnsureRolled();
                return Data.Active;
            }
        }

        /// <summary>The next chapter still to clear (0 once every chapter is cleared).</summary>
        public static int NextChapter()
        {
            int next = ChapterService.HighestCleared + 1;
            return ChapterService.GetByNumber(next) != null ? next : 0;
        }

        /// <summary>Posts today's requests if the day changed. Returns true if it did.</summary>
        public static bool EnsureRolled()
        {
            var data = Data;
            int today = GameClock.Today;
            if (data.Day == today) return false;
            data.Day = today;
            data.Active.Clear();

            var catalog = Catalog;
            if (catalog != null)
            {
                int castle = VillageService.CastleLevel;
                int kitchen = VillageService.GetLevel(BuildingIds.Kitchen);
                int nextChapter = NextChapter();
                var pool = new List<(string, float)>();
                foreach (var request in catalog.All)
                    if (request != null && RequestRules.IsEligible(request.Kind, request.RequiredCastleLevel, request.RequiredKitchenLevel, castle, kitchen, nextChapter,
                            string.IsNullOrEmpty(request.RequiredBuilding) || VillageService.GetLevel(request.RequiredBuilding) > 0))
                        pool.Add((request.Id, request.Weight));

                int seed = DailyRules.SeedFor(today, SaveService.Data.Profile.PlayerId, RequestRules.Salt);
                var picked = new List<string>();
                DailyRules.RollQuests(pool, RequestRules.ActiveCount, seed, picked);

                var everyone = new List<string>();
                foreach (var (key, _) in RequestRules.Villagers) everyone.Add(key);
                var taken = new HashSet<string>();
                var rng = new System.Random(seed);
                foreach (var id in picked)
                {
                    var request = catalog.Get(id);
                    string villager = RequestRules.PickVillager(request.Villagers, everyone, taken, rng);
                    taken.Add(villager);
                    data.Active.Add(new VillagerRequestState
                    {
                        Id = id, VillagerKey = villager, Target = request.Amount,
                        Chapter = request.Kind == RequestKind.Chapter ? nextChapter : 0,
                    });
                }
            }
            Changed();
            return true;
        }

        // ------------------------------------------------------------------ progress

        public static int Progress(VillagerRequestState state)
        {
            var request = state != null ? Get(state.Id) : null;
            if (request == null) return 0;
            if (state.Delivered) return RequestRules.ShownTarget(request.Kind, state.Target);
            return RequestRules.Progress(request.Kind, state.Target, state.Progress, Stored(request),
                ChapterService.HighestCleared, state.Chapter);
        }

        public static int Target(VillagerRequestState state)
        {
            var request = state != null ? Get(state.Id) : null;
            return request == null ? 1 : RequestRules.ShownTarget(request.Kind, state.Target);
        }

        private static int Stored(VillagerRequestDefinition request) =>
            request != null && request.Kind == RequestKind.Deliver ? GoodsService.Count(request.Goods) : 0;

        public static bool IsComplete(VillagerRequestState state) => state != null && Progress(state) >= Target(state);

        public static bool CanDeliver(VillagerRequestState state) => state != null && !state.Delivered && IsComplete(state);

        /// <summary>Requests done and waiting to be handed in (for "!" badges).</summary>
        public static int DeliverableCount()
        {
            int count = 0;
            foreach (var state in Active)
                if (CanDeliver(state)) count++;
            return count;
        }

        /// <summary>Counts a stat report towards today's stat requests (called by <see cref="QuestTracker"/>).</summary>
        public static void Record(string statId, int amount, bool isMax)
        {
            if (string.IsNullOrEmpty(statId) || amount <= 0) return;
            bool changed = false;
            foreach (var state in Active)
            {
                if (state == null || state.Delivered) continue;
                var request = Get(state.Id);
                if (request == null || request.Kind != RequestKind.Stat || request.StatId != statId) continue;
                int updated = System.Math.Min(state.Target, DailyRules.ApplyProgress(state.Progress, amount, isMax));
                if (updated == state.Progress) continue;
                state.Progress = updated;
                changed = true;
            }
            if (changed) Changed();
        }

        // ------------------------------------------------------------------ delivering

        public static RequestReward RewardFor(VillagerRequestDefinition request) => request == null
            ? default
            : new RequestReward(RequestRules.ScaledCoins(request.RewardCoins, VillageService.CastleLevel), request.RewardGems, request.RewardDecoration);

        /// <summary>Hands the request in: uses up delivered goods and pays the reward. False if it isn't done (or already delivered).</summary>
        public static bool Deliver(VillagerRequestState state, out RequestReward reward)
        {
            reward = default;
            if (!CanDeliver(state)) return false;
            var request = Get(state.Id);
            if (request == null) return false;
            if (request.Kind == RequestKind.Deliver && !GoodsService.TrySpend(request.Goods.Id, state.Target)) return false;

            reward = RewardFor(request);
            if (reward.Coins > 0) CurrencyService.Grant(CurrencyType.Coins, reward.Coins, $"request_{request.Id}");
            if (reward.Gems > 0) CurrencyService.Grant(CurrencyType.Gems, reward.Gems, $"request_{request.Id}");
            if (reward.Decoration != null) DecorationService.Gift(reward.Decoration, 1);
            state.Delivered = true;
            Core.Events.Progress.Report(ProgressStatIds.RequestDelivered, 1, request.Id);
            Changed();
            return true;
        }

        // ------------------------------------------------------------------ text

        public static string VillagerName(VillagerRequestState state) => RequestRules.VillagerName(state?.VillagerKey);

        /// <summary>"Bring 5 Carrots", "Defeat 150 enemies", "Clear Chapter 3".</summary>
        public static string Title(VillagerRequestState state)
        {
            var request = state != null ? Get(state.Id) : null;
            if (request == null) return "A favour";
            int amount = request.Kind == RequestKind.Chapter ? state.Chapter : state.Target;
            return RequestRules.Title(request.DisplayName, amount, request.Goods != null ? request.Goods.NameOrId : null);
        }

        public static string DescribeReward(VillagerRequestDefinition request)
        {
            var reward = RewardFor(request);
            var text = new StringBuilder();
            if (reward.Coins > 0) text.Append($"<color=#FFD24D>{reward.Coins} coins</color>");
            if (reward.Gems > 0) text.Append(text.Length > 0 ? "  ·  " : "").Append($"<color=#7FD8FF>{reward.Gems} gems</color>");
            if (reward.Decoration != null) text.Append(text.Length > 0 ? "  ·  " : "").Append($"<color=#9CFF8A>{reward.Decoration.NameOrId}</color>");
            return text.ToString();
        }

        private static void Changed()
        {
            SaveService.MarkDirty();
            EventBus<RequestsChangedEvent>.Raise(new RequestsChangedEvent());
        }
    }
}
