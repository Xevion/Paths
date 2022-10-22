using System;
using NUnit.Framework;
using Paths.Core;
using Paths.Core.Search;

namespace Paths.Tests {
    /// <summary>
    /// The app drives the incremental search through PathSolver, which decides on its behalf whether
    /// to repair the plan, move the start, or throw it away. Those decisions are easy to get subtly
    /// wrong, so this holds the incremental route against the ordinary one over the same edits.
    /// </summary>
    public class PathSolverIncrementalTests {
        private static PathSolver Build(bool incremental) {
            PathSolver solver = Solvers.Cornered();
            solver.Settings.Incremental = incremental;
            return solver;
        }

        [Test]
        public void RepairedRoutesMatchFreshlySolvedOnes() {
            PathSolver incremental = Build(true);
            PathSolver plain = Build(false);

            incremental.Solve();
            plain.Solve();
            Solvers.AssertSameRoute(incremental, plain, "on the first plan");

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

                Solvers.AssertSameRoute(incremental, plain, $"after edit {edit} at {at}");
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

                Solvers.AssertSameRoute(incremental, plain, $"after move {move} to {at}");
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

                Solvers.AssertSameRoute(incremental, plain, $"after painting {tier} at {at}");
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
