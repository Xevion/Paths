using System;
using System.Collections.Generic;
using NUnit.Framework;
using Paths.Core;
using Paths.Core.Generation;
using Paths.Core.Search;
using static Paths.Tests.Search.SearchCorpus;

namespace Paths.Tests {
    /// <summary>
    /// Editing the grid while a run is on screen, which is the app's main interaction. Each edit
    /// re-solves and hands back a fresh timeline, and the picture that timeline opens on is what the
    /// user sees, so it has to agree with the grid they just drew on.
    /// </summary>
    public class EditedTraceTests {
        private static IEnumerable<TestCaseData> Modes() {
            foreach (bool incremental in new[] { false, true })
                foreach (bool diagonal in new[] { false, true })
                    foreach (Algorithm algorithm in new[] { Algorithm.AStar, Algorithm.Dijkstra, Algorithm.JumpPoint })
                        yield return new TestCaseData(incremental, diagonal, algorithm)
                            .SetName($"{{m}}({(incremental ? "incremental" : algorithm.ToString())},{(diagonal ? "8" : "4")}way)");
        }

        private static PathSolver Solver(bool incremental, bool diagonal, Algorithm algorithm, int seed) =>
            new PathSolver(20, 16, seed, Generator.Random, 0.2f) {
                Incremental = incremental,
                Diagonal = diagonal,
                Algorithm = algorithm,
                Heuristic = Heuristic.Octile
            };

        /// <summary>
        /// Toggling a cell has to change what is drawn there. This is the bug that started the suite:
        /// a repair reconsiders the cell just walled off and used to paint its own search state over
        /// the wall, so clicking the same spot twice looked like nothing happening at all.
        /// </summary>
        [TestCaseSource(nameof(Modes))]
        public void TogglingACellChangesWhatIsDrawn(bool incremental, bool diagonal, Algorithm algorithm) {
            PathSolver solver = Solver(incremental, diagonal, algorithm, 4);
            solver.Solve();

            var rng = new Random(17);
            int checkedCells = 0;
            for (int t = 0; t < 60; t++) {
                Coord at = solver.Grid.At(rng.Next(solver.Grid.CellCount));
                if (at == solver.Start || at == solver.End)
                    continue;

                // the playhead sits wherever the user left it, so compare at each end of the run and
                // in the middle. reading only the opening picture proves nothing - nothing is drawn
                // over it yet, so it always matches the grid.
                bool walkable = solver.IsWalkable(at);
                foreach (float where in new[] { 0f, 0.5f, 1f }) {
                    solver.SetWalkable(at, !walkable);
                    GridNodeType walled = DrawnAt(solver, at, where);
                    solver.SetWalkable(at, walkable);
                    GridNodeType cleared = DrawnAt(solver, at, where);

                    Assert.AreNotEqual(walled, cleared,
                        $"cell {at} looks the same walled and cleared, {where:P0} through the run");
                }
                checkedCells++;
            }
            Assert.Greater(checkedCells, 40, "the sweep barely touched any cells");
        }

        /// <summary>
        /// The whole picture, not just the cell that moved: wherever the playhead is parked after an
        /// edit, the walls and both markers it shows are the ones the grid actually holds.
        /// </summary>
        [TestCaseSource(nameof(Modes))]
        public void TheDrawnGridMatchesTheRealOneAtEveryStep(bool incremental, bool diagonal, Algorithm algorithm) {
            PathSolver solver = Solver(incremental, diagonal, algorithm, 9);
            solver.Solve();

            var rng = new Random(23);
            for (int t = 0; t < 40; t++) {
                Coord at = solver.Grid.At(rng.Next(solver.Grid.CellCount));
                if (at != solver.Start && at != solver.End)
                    solver.SetWalkable(at, !solver.IsWalkable(at));

                ChangeController trace = solver.Solve();
                int start = solver.Grid.Index(solver.Start), goal = solver.Grid.Index(solver.End);
                foreach (float where in new[] { 0f, 0.5f, 1f }) {
                    Seek(trace, where);
                    SearchTraceTests.AssertStructureIntact(solver.Grid, trace, start, goal,
                        $"edit {t} at {at}, {where:P0} through the run");
                }
            }
        }

        /// <summary>Painting terrain is the other brush, and it must not disturb the structure either.</summary>
        [TestCaseSource(nameof(Modes))]
        public void PaintingTerrainLeavesTheStructureAlone(bool incremental, bool diagonal, Algorithm algorithm) {
            PathSolver solver = Solver(incremental, diagonal, algorithm, 12);
            solver.Solve();

            var tiers = new[] { Terrain.Plain, Terrain.Road, Terrain.Mud, Terrain.Water };
            var rng = new Random(31);
            for (int t = 0; t < 30; t++) {
                Coord at = solver.Grid.At(rng.Next(solver.Grid.CellCount));
                if (solver.IsWalkable(at) && at != solver.Start && at != solver.End)
                    solver.SetTerrain(at, tiers[rng.Next(tiers.Length)]);

                ChangeController trace = solver.Solve();
                Seek(trace, 1f);
                SearchTraceTests.AssertStructureIntact(solver.Grid, trace,
                    solver.Grid.Index(solver.Start), solver.Grid.Index(solver.End), $"paint {t} at {at}");
            }
        }

        /// <summary>
        /// Sealing the route off is the case that makes a repair trivial: nothing is reachable, so it
        /// can come back with no steps at all. The picture still has to be right.
        /// </summary>
        [TestCaseSource(nameof(Modes))]
        public void SealingTheRouteStillDrawsTheWalls(bool incremental, bool diagonal, Algorithm algorithm) {
            PathSolver solver = Solver(incremental, diagonal, algorithm, 6);
            solver.Solve();

            // wall off everything around the start, so there is certainly no way out
            var sealedOff = new List<Coord>();
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++) {
                    var at = new Coord(solver.Start.X + dx, solver.Start.Y + dy);
                    if ((dx != 0 || dy != 0) && solver.IsValid(at) && at != solver.End) {
                        solver.SetWalkable(at, false);
                        sealedOff.Add(at);
                    }
                }

            ChangeController trace = solver.Solve();
            Assert.IsEmpty(solver.Path, "the start should be walled in");
            Seek(trace, 1f);
            foreach (Coord at in sealedOff)
                Assert.AreEqual(GridNodeType.Wall, trace.Current[at.X, at.Y], $"the wall drawn at {at} is not shown");
        }

        // what a cell is drawn as with the playhead parked this far along
        private static GridNodeType DrawnAt(PathSolver solver, Coord at, float where) {
            ChangeController trace = solver.Solve();
            Seek(trace, where);
            return trace.Current[at.X, at.Y];
        }

        // park the playhead a fraction of the way along, the way the scrubber does
        private static void Seek(ChangeController trace, float where) {
            if (trace.Count == 0)
                return;
            trace.MoveTo(Math.Min((int) Math.Round(trace.Count * where), trace.Count - 1));
        }
    }
}
