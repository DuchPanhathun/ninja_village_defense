namespace NinjaVillage.Gameplay.Pets
{
    /// <summary>
    /// Which behaviour component a pet gets when it spawns (see <see cref="PetFactory"/>). Lets a
    /// designer make a new pet by picking an existing ability and tuning numbers — no code.
    /// How each ability reads <see cref="PetRuntimeStats"/> is documented on its component.
    /// </summary>
    public enum PetAbilityType
    {
        /// <summary>Follows the player only (cosmetic / passive owner bonus).</summary>
        None,
        /// <summary>Fox — pulls nearby XP orbs to the player.</summary>
        XpCollector,
        /// <summary>Wolf — runs at enemies near the player and bites them.</summary>
        Bite,
        /// <summary>Hawk — periodically reveals a coin treasure near the player.</summary>
        TreasureHunter,
        /// <summary>Monkey — lobs bananas that damage and stun.</summary>
        BananaThrow,
        /// <summary>Dragon — breathes a cone of fire that burns.</summary>
        FireBreath
    }
}
