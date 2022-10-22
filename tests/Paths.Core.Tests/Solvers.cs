using NUnit.Framework;
using Paths.Core;
using Paths.Core.Generation;
using Paths.Core.Search;

namespace Paths.Tests {
    /// <summary>
    /// Solvers set up the way the app drives them, and what their routes actually cost.
    /// </summary>
    internal static class Solvers {
        /// <summary>
        /// A solver with its ends pinned to opposite corners. The generator picks random ends, and on
        /// a grid this size they landed close enough together that random edits missed the route
        /// almost every time, which reads as a passing test proving nothing.
        /// </summary>
        public static PathSolver Cornered(int size = 16, int seed = 5, float density = 0.2f) {
            var solver = new PathSolver(size, size, seed, Generator.Random, density);
            solver.Settings.Diagonal = true;
            solver.Settings.Heuristic = Heuristic.Octile;
            solver.Start = NearestOpen(solver, new Coord(0, 0));
            solver.End = NearestOpen(solver, new Coord(size - 1, size - 1));
            return solver;
        }

        /// <summary>The closest walkable cell to a target, searched outwards in rings.</summary>
        public static Coord NearestOpen(PathSolver solver, Coord target) {
            for (int ring = 0; ring < 64; ring++)
                for (int dx = -ring; dx <= ring; dx++)
                    for (int dy = -ring; dy <= ring; dy++) {
                        var at = new Coord(target.X + dx, target.Y + dy);
                        if (solver.IsValid(at) && solver.IsWalkable(at))
                            return at;
                    }
            return target;
        }

        /// <summary>What the route costs, since two different routes can share a length.</summary>
        public static float RouteCost(PathSolver solver) {
            float total = 0f;
            for (int i = 1; i < solver.Path.Count; i++)
                total += solver.Grid.StepCost(solver.Path[i - 1], solver.Path[i]);
            return total;
        }

        public static void AssertSameRoute(PathSolver actual, PathSolver expected, string where) {
            Assert.AreEqual(expected.Path.Count > 0, actual.Path.Count > 0, $"reachability differs {where}");
            Assert.AreEqual(RouteCost(expected), RouteCost(actual), 1e-3f, $"route cost differs {where}");
        }
    }
}
