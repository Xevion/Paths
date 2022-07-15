using Algorithms;

namespace Paths.Benchmarks {
    internal static class Grids {
        // fixed seed so a run is comparable against the one before it
        public static NodeGrid Seeded(int size, double wallChance, int seed = 7) {
            var rng = new System.Random(seed);
            var grid = new NodeGrid(size, size, new System.Random(seed));
            foreach (Node node in grid.Iterator())
                if (rng.NextDouble() < wallChance)
                    node.Walkable = false;
            grid.Grid[0, 0].Walkable = true;
            grid.Grid[size - 1, size - 1].Walkable = true;
            return grid;
        }
    }
}
