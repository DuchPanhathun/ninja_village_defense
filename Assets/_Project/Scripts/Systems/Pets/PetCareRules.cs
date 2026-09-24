namespace NinjaVillage.Systems.Pets
{
    /// <summary>
    /// Pet care (EPIC 24 Phase 7) as pure functions: pet each pet once a day and feed it once a day (a fish, some
    /// shrimp or a crop from the storehouse). A happy pet is stronger in today's battles — petted +5%, fed +10%.
    /// </summary>
    public static class PetCareRules
    {
        public const float PettedBonus = 0.05f;
        public const float FedBonus = 0.10f;

        /// <summary>What a pet can be fed, favourite first; feeding uses one of the first one in stock.</summary>
        public static readonly string[] Treats = { "fish", "shrimp", "carrot", "radish", "rice" };

        public static bool CanPet(int pettedDay, int today) => pettedDay != today;
        public static bool CanFeed(int fedDay, int today) => fedDay != today;

        /// <summary>Multiplier on the pet's power in battle today.</summary>
        public static float PowerScale(int pettedDay, int fedDay, int today) =>
            1f + (pettedDay == today ? PettedBonus : 0f) + (fedDay == today ? FedBonus : 0f);

        /// <summary>The first treat with any in stock, or null.</summary>
        public static string PickTreat(System.Func<string, int> stock)
        {
            foreach (var treat in Treats)
                if (stock(treat) > 0) return treat;
            return null;
        }
    }
}
