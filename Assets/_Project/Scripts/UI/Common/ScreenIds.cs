namespace NinjaVillage.UI.Common
{
    /// <summary>
    /// Shared <see cref="UIScreen.ScreenId"/>s so any screen can open any other by id without a
    /// type reference (e.g. the Dojo opens the Hero screen). A screen type may be placed in more
    /// than one scene (Main Menu and Village) — ids are unique per scene, not globally.
    /// </summary>
    public static class ScreenIds
    {
        // Main Menu
        public const string Home = "home";
        public const string Chapters = "chapters";
        public const string Settings = "settings";
        public const string Profile = "profile";

        // Meta progression
        public const string Heroes = "heroes";
        public const string Pets = "pets";
        public const string Talents = "talents";
        public const string Inventory = "inventory";       // weapons + equipment
        public const string Collection = "collection";     // evolution journal

        // Village
        public const string VillageHud = "village_hud";
        public const string Decorations = "decorations";   // village decoration shop
        public const string Neighbours = "neighbours";     // other players' villages to visit
        public const string Building = "building";         // generic building detail/upgrade menu
        public const string Forge = "forge";
        public const string Shrine = "shrine";
        public const string Market = "market";
        public const string Castle = "castle";
        public const string Dojo = "dojo";
        public const string PetHouse = "pet_house";

        // Daily content
        public const string DailyLogin = "daily_login";
        public const string Quests = "quests";
        public const string Achievements = "achievements";

        // Live ops / store
        public const string Store = "store";
        public const string BattlePass = "battle_pass";
        public const string Events = "events";
    }
}
