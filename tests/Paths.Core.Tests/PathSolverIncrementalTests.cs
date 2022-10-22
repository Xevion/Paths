using System;
using NUnit.Framework;
using Paths.Core;
using Paths.Core.Generation;
using Paths.Core.Search;

namespace Paths.Tests {
    /// <summary>
    /// The app drives the incremental search through PathSolver, which decides on its behalf whether
    /// to repair the plan, move the start, or throw it away. Those decisions are easy to get subtly
    /// wrong, so this holds the incremental route against the ordinary one over the same edits.
    /// </summary>
    public class PathSolverIncrementalTests {
        /// <summary>What the route actually costs, since two different routes can share a length.</summary>
        private static float Cost(PathSolver solver) {
            float total = 0f;
            for (int i = 1; i < solver.Path.Count; i++)
                total += solver.Grid.StepCost(solver.Path[i - 1], solver.Path[i]);
            return total;
        }

        private static void AssertSameRoute(PathSolver incremental, PathSolver plain, string where) {
            Assert.AreEqual(plain.Path.Count > 0, incremental.Path.Count > 0, $"reachability differs {where}");
            Assert.AreEqual(Cost(plain), Cost(incremental), 1e-3f, $"route cost differs {where}");
        }

        private static PathSolver Build(bool incremental) {
            var solver = new PathSolver(16, 16, 5, Generator.Random, 0.2f);
            solver.Diagonal = true;
            solver.Heuristic = Heuristic.Octile;
            solver.Incremental = incremental;
            // opposite corners on purpose: with the generator's own random ends the route came out
            // barely a dozen cells long, and random edits almost never touched it
            solver.Start = Corner(solver, 0, 0);
            solver.End = Corner(solver, 15, 15);
            return solver;
        }

        // the nearest walkable cell to a corner, spiralling out by rings
        private static Coord Corner(PathSolver solver, int x, int y) {
            for (int ring = 0; ring < 16; ring++)
                for (int dx = 0; dx <= ring; dx++)
                    for (int dy = 0; dy <= ring; dy++) {
                        var at = new Coord(x == 0 ? dx : x - dx, y == 0 ? dy : y - dy);
                        if (solver.IsValid(at) && solver.IsWalkable(at))
                            return at;
                    }
            return new Coord(x, y);
        }

        [Test]
        public void RepairedRoutesMatchFreshlySolvedOnes() {
            PathSolver incremental = Build(true);
            PathSolver plain = Build(false);

            incremental.Solve();
            plain.Solve();
            AssertSameRoute(incremental, plain, "on the first plan");

            var rng = new Random(12);
            for (int edit = 0; edit < 60; edit++) {
                var at = new Coord(rng.Next(16), rng.Next(16));
                if (at == incremental.Start || at == incremental.End)
                    continue;

                bool walkable = !incremental.IsWalkable(at);
                incremental.SetWalkable(at, walkable);
                plain.SetWalkable(at, walkable);

                incremental.Solve();
                plain.Solve();

                AssertSameRoute(incremental, plain, $"after edit {edit} at {at}");
            }
        }

        [Test]
        public void MovingTheStartAndTheGoalKeepsTheRouteRight() {
            PathSolver incremental = Build(true);
            PathSolver plain = Build(false);
            incremental.Solve();
            plain.Solve();

            var rng = new Random(3);
            for (int move = 0; move < 30; move++) {
                var at = new Coord(rng.Next(16), rng.Next(16));
                if (!incremental.IsWalkable(at))
                    continue;

                // alternate which end moves: the goal is the one that cannot be repaired through
                if (move % 2 == 0) {
                    if (at == incremental.End) continue;
                    incremental.Start = at;
                    plain.Start = at;
                } else {
                    if (at == incremental.Start) continue;
                    incremental.End = at;
                    plain.End = at;
                }

                incremental.Solve();
                plain.Solve();

                AssertSameRoute(incremental, plain, $"after move {move} to {at}");
            }
        }

        [Test]
        public void TerrainPaintedUnderAPlanIsRepairedThroughToo() {
            PathSolver incremental = Build(true);
            PathSolver plain = Build(false);
            incremental.Solve();
            plain.Solve();

            var rng = new Random(44);
            Terrain[] tiers = Terrains.All;

            for (int edit = 0; edit < 40; edit++) {
                var at = new Coord(rng.Next(16), rng.Next(16));
                if (!incremental.IsWalkable(at))
                    continue;

                Terrain tier = tiers[rng.Next(tiers.Length)];
                incremental.SetTerrain(at, tier);
                plain.SetTerrain(at, tier);

                incremental.Solve();
                plain.Solve();

                AssertSameRoute(incremental, plain, $"after painting {tier} at {at}");
            }
        }

        [Test]
        public void ResetThrowsThePlanAwayWithoutChangingTheAnswer() {
            PathSolver solver = Build(true);
            solver.Solve();
            int before = solver.Path.Count;

            solver.Reset();
            solver.Solve();

            Assert.AreEqual(before, solver.Path.Count);
        }
    }
}
