namespace NinjaVillage.Core.Utilities
{
    /// <summary>
    /// Centralized tag/layer name constants. Keep these in sync with the Editor's
    /// Tags and Layers settings (Edit → Project Settings → Tags and Layers) —
    /// see EDITOR_SETUP.md for the exact layers this project expects.
    /// </summary>
    public static class Tags
    {
        public const string Player = "Player";
        public const string Enemy = "Enemy";
        public const string Boss = "Boss";
    }

    public static class Layers
    {
        public const string Player = "Player";
        public const string Enemy = "Enemy";
        public const string PlayerProjectile = "PlayerProjectile";
        public const string EnemyProjectile = "EnemyProjectile";
        public const string Pickup = "Pickup";
    }
}
