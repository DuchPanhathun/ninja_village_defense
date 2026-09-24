using System.Collections.Generic;
using UnityEngine;

namespace NinjaVillage.Systems.Village
{
    /// <summary>
    /// Where things live on the village map, shared by the map (drawing) and the decoration rules
    /// (what's free to build on): the buildable area, the paths, and the spots of the fixed displays
    /// next to their buildings — the hero yard by the Dojo, the Armory rack by the Forge, the Talent Tree
    /// by the Shrine, the pet meadow by the Pet House.
    /// </summary>
    public static class VillageLayout
    {
        /// <summary>The village ground (a forest ring is drawn around it).</summary>
        public static readonly Rect Bounds = new(-15f, -11f, 30f, 22f);

        public static readonly Vector2 Plaza = new(0f, -1f);
        public static readonly Vector2 PlazaSize = new(5f, 3.6f);

        /// <summary>Straight dirt paths as (center, size) — the plaza cross plus spurs to each building.</summary>
        public static readonly (Vector2 center, Vector2 size)[] Paths =
        {
            (new Vector2(0f, -1f), new Vector2(24f, 1.2f)),     // east-west road
            (new Vector2(0f, 1.1f), new Vector2(1.2f, 4.2f)),   // up to the castle
            (new Vector2(0f, -4.3f), new Vector2(1.2f, 3.4f)),  // down to the market
            (new Vector2(-7.5f, 0f), new Vector2(1.2f, 1.6f)),  // dojo
            (new Vector2(7.5f, 0f), new Vector2(1.2f, 1.6f)),   // forge
            (new Vector2(-8f, -3.2f), new Vector2(1.2f, 3.4f)), // shrine
            (new Vector2(8f, -3.2f), new Vector2(1.2f, 3.4f)),  // pet house
        };

        public static readonly Vector2 Armory = new(10.6f, 0.9f);
        public static readonly Vector2 TalentTree = new(-11.6f, -5f);
        public static readonly Rect HeroYard = new(-12.5f, -3.2f, 8.5f, 1.7f);
        public static readonly Rect PetMeadow = new(4.8f, -9.6f, 7.6f, 2.4f);

        /// <summary>Round spots decorations can't cover: the displays.</summary>
        public static IEnumerable<(Vector2 center, float radius)> ReservedSpots()
        {
            yield return (Armory, 1.4f);
            yield return (TalentTree, 2.2f);
        }
    }
}
