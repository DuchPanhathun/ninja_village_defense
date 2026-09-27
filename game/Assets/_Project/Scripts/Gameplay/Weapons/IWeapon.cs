using NinjaVillage.Gameplay.Combat;
using NinjaVillage.Gameplay.Player;
using UnityEngine;

namespace NinjaVillage.Gameplay.Weapons
{
    /// <summary>
    /// The contract every weapon fulfils (EPIC 2 "Weapon interface"): how far it reaches, how it sounds,
    /// and how it attacks. <see cref="WeaponDefinition"/> implements it for data-driven weapons; a weapon
    /// with a completely different code path (e.g. a channelled beam) can implement it directly and still
    /// be driven by <see cref="AutoAttackController"/>'s targeting and cooldowns.
    /// </summary>
    public interface IWeapon
    {
        string Id { get; }
        float Range { get; }
        string FireSoundId { get; }
        void Fire(AutoAttackController controller, Transform origin, Transform target, PlayerStats stats);
    }
}
