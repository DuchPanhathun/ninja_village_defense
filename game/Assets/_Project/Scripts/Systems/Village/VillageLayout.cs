using System.Collections.Generic;
using UnityEngine;

namespace NinjaVillage.Systems.Village
{
    /// <summary>
    /// Where things live on the village map, shared by the map (drawing) and the decoration rules
    /// (what's free to build on): the buildable area, the paths, and the spots of the fixed displays
    /// next to their buildings — the hero yard by the Dojo, the Armory rack by the Forge, the Talent Tree
    /// by the Shrine, the pet meadow by the Pet House. The ground reaches well past the buildings on every
    /// side so there's open grass to decorate; the Kitchen sits at the west end of the main road, past the farm,
    /// and the house plots line a lane at its east end that runs north to the mine. The fishing pond lies south-west.
    /// </summary>
    public static class VillageLayout
    {
        /// <summary>The village ground (a forest ring is drawn around it).</summary>
        public static readonly Rect Bounds = new(-22f, -16f, 44f, 32f);

        public static readonly Vector2 Plaza = new(0f, -1f);
        public static readonly Vector2 PlazaSize = new(5f, 3.6f);

        /// <summary>Straight dirt paths as (center, size) — the plaza cross plus spurs to each building.</summary>
        public static readonly (Vector2 center, Vector2 size)[] Paths =
        {
            (new Vector2(-0.375f, -1f), new Vector2(36.45f, 1.2f)), // east-west road: the kitchen in the west, the houses in the east
            (new Vector2(0f, 1.1f), new Vector2(1.2f, 4.2f)),   // up to the castle
            (new Vector2(0f, -4.3f), new Vector2(1.2f, 3.4f)),  // down to the market
            (new Vector2(-7.5f, 0f), new Vector2(1.2f, 1.6f)),  // dojo
            (new Vector2(7.5f, 0f), new Vector2(1.2f, 1.6f)),   // forge
            (new Vector2(-8f, -3.2f), new Vector2(1.2f, 3.4f)), // shrine
            (new Vector2(8f, -3.2f), new Vector2(1.2f, 3.4f)),  // pet house
            (new Vector2(-17.4f, -0.1f), new Vector2(1.2f, 1.2f)), // kitchen (its door is right of centre)
            (new Vector2(17.25f, 1.9f), new Vector2(1.2f, 18.2f)),   // the residential lane, up to the mine
            (new Vector2(15.225f, 0.65f), new Vector2(2.85f, 0.9f)),  // house doors off the lane: west / east, three rows
            (new Vector2(19.175f, 0.65f), new Vector2(2.65f, 0.9f)),
            (new Vector2(15.225f, -6.75f), new Vector2(2.85f, 0.9f)),
            (new Vector2(19.175f, -6.75f), new Vector2(2.65f, 0.9f)),
            (new Vector2(15.225f, 5.25f), new Vector2(2.85f, 0.9f)),
            (new Vector2(19.175f, 5.25f), new Vector2(2.65f, 0.9f)),
            (new Vector2(-15.5f, -4.9f), new Vector2(1.2f, 6.6f)),    // down to the fishing pond
        };

        /// <summary>The fishing pond, south-west past the Talent Tree (water inside a grassy bank).</summary>
        public static readonly Rect Pond = new(-19.75f, -12.8f, 7.5f, 4.6f);

        /// <summary>Where the wooden dock reaches into the pond from its north bank (the end of the path).</summary>
        public static readonly Vector2 PondDock = new(-15.5f, -9.3f);

        public static readonly Vector2 Armory = new(10.6f, 0.9f);
        public static readonly Vector2 ProfileBoard = new(3.8f, 1.1f);
        public static readonly Vector2 TalentTree = new(-11.6f, -5f);
        public static readonly Rect HeroYard = new(-12.5f, -3.2f, 8.5f, 1.7f);

        /// <summary>The farm field, top-left beside the Dojo: 2 columns × 3 rows of beds, the sign below.</summary>
        public static readonly Rect FarmField = new(-15f, 0.7f, 4.9f, 9.9f);
        public static readonly Vector2 FarmSign = new(-12.55f, 0.9f);
        public static Vector2 FarmCenter => FarmField.center;

        public static Vector2 FarmPlot(int plot) => new(-13.8f + (plot % 2) * 2.5f, 4.4f + (plot / 2) * 2.4f);
        public static readonly Rect PetMeadow = new(4.8f, -9.6f, 7.6f, 2.4f);

        /// <summary>Where the villagers with today's requests hang around: the east half of the plaza.</summary>
        public static readonly Rect RequestSquare = new(-0.6f, -2.6f, 3.0f, 2.6f);

        /// <summary>The treasury chest, on the west side of the plaza (clear of the profile board's label).</summary>
        public static readonly Vector2 Treasury = new(-1.7f, -0.2f);

        /// <summary>
        /// House plots along the residential lane east of the Forge, in unlock order: the pair by the main road,
        /// the pair south of it, then the pair to the north. Positions are where the front wall stands.
        /// </summary>
        public static Vector2 HousePlot(int plot) => new(plot % 2 == 0 ? 14.3f : 20.0f, (plot / 2) switch { 0 => 1.0f, 1 => -6.4f, _ => 5.6f });

        /// <summary>The ground a house plot takes up (decorations can't go there).</summary>
        public static Rect HouseLot(int plot)
        {
            var p = HousePlot(plot);
            return new Rect(p.x - 1.9f, p.y - 0.5f, 3.8f, 3.4f);
        }

        /// <summary>Round spots decorations can't cover: the displays.</summary>
        public static IEnumerable<(Vector2 center, float radius)> ReservedSpots()
        {
            yield return (Armory, 1.4f);
            yield return (TalentTree, 2.2f);
            yield return (ProfileBoard + new Vector2(0f, 0.6f), 1.8f);
            yield return (Treasury + new Vector2(0f, 0.4f), 1.4f);
        }
    }
}
