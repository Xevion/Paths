#nullable enable
namespace Paths.Core.Search {
    /// <summary>
    /// The terrain a cell can be painted with. A grid stores plain weights, so a search never hears
    /// about any of this; the tiers exist so the brush and the palette have a fixed set to agree on
    /// rather than each picking numbers of their own.
    /// </summary>
    public enum Terrain {
        Plain,
        Road,
        Mud,
        Water
    }

    public static class Terrains {
        /// <summary>What a cell of this terrain costs to enter.</summary>
        public static float Weight(Terrain terrain) {
            switch (terrain) {
                case Terrain.Road: return 0.5f;
                case Terrain.Mud: return 2f;
                case Terrain.Water: return 4f;
                default: return 1f;
            }
        }

        /// <summary>
        /// The tier a weight came from. Nearest match rather than exact, so a weight that arrived
        /// from somewhere other than the brush still paints as the tier it most resembles.
        /// </summary>
        public static Terrain Of(float weight) {
            Terrain closest = Terrain.Plain;
            float best = float.PositiveInfinity;
            foreach (Terrain terrain in All) {
                float gap = System.Math.Abs(Weight(terrain) - weight);
                if (gap < best) {
                    best = gap;
                    closest = terrain;
                }
            }
            return closest;
        }

        public static readonly Terrain[] All = {
            Terrain.Plain, Terrain.Road, Terrain.Mud, Terrain.Water
        };
    }
}
