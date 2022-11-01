using System;
using System.Collections.Generic;
using NUnit.Framework;
using Paths.Core;
using Paths.Core.Generation;
using Paths.Core.Search;

namespace Paths.Tests {
    /// <summary>
    /// The heatmap reads two things together: the cost field and the rendered picture it tints. They
    /// are uploaded as a pair, so they are asserted as a pair here rather than one at a time.
    /// </summary>
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    internal class CostFieldTests {
        private static IEnumerable<TestCaseData> Configurations() {
            yield return Case("A*", s => { });
            yield return Case("Dijkstra", s => s.Algorithm = Algorithm.Dijkstra);
            yield return Case("Greedy", s => s.Algorithm = Algorithm.Greedy);
            yield return Case("BFS", s => s.Algorithm = Algorithm.BreadthFirst);
            yield return Case("DFS", s => s.Algorithm = Algorithm.DepthFirst);
            yield return Case("JPS", s => s.Algorithm = Algorithm.JumpPoint);
            // both switches have a Dijkstra version and no other, so that is what they run over.
            // left on the default pick they would be dropped and this would test A* twice more.
            yield return Case("Bidirectional",
                s => { s.Algorithm = Algorithm.Dijkstra; s.Bidirectional = true; });
            yield return Case("Incremental",
                s => { s.Algorithm = Algorithm.Dijkstra; s.Incremental = true; });
        }

        private static TestCaseData Case(string name, Action<SolverSettings> setup) =>
            new TestCaseData(name, setup).SetName($"{{m}}({name})");

        /// <summary>
        /// The finished picture plus the field that tints it, which is the pair GridController
        /// uploads. Seeds that lay out an unsolvable grid are skipped rather than asserted over: a
        /// search that gives up immediately has nothing to say about how the heatmap reads.
        /// </summary>
        private static (GridNodeType[,] cells, float[,] field) Finished(Action<SolverSettings> setup, int seed) {
            var solver = new PathSolver(24, 24, seed, Generator.Random, 0.2f);
            solver.Start = Solvers.NearestOpen(solver, new Coord(0, 0));
            solver.End = Solvers.NearestOpen(solver, new Coord(23, 23));
            setup(solver.Settings);

            ChangeController trace = solver.Solve();
            if (solver.Path.Count == 0)
                return (null, null);

            trace.MoveTo(Math.Max(0, trace.Count - 1));
            return (trace.Current, solver.CostField());
        }

        // cells drawn as the search layer are the only ones the shader tints, so they are the ones
        // that need a reading. walls, start and end draw as themselves.
        private static bool Tinted(GridNodeType cell) =>
            cell == GridNodeType.Seen || cell == GridNodeType.Expanded;

        /// <summary>
        /// Every cell the shader tints has to have been reached by the search that tinted it. A cell
        /// can legitimately read zero, since anything level with the start does, so this asks for a
        /// reading at all rather than a positive one.
        /// </summary>
        [TestCaseSource(nameof(Configurations))]
        public void EveryTintedCellHasACostReading(string name, Action<SolverSettings> setup) {
            int checked_ = 0;
            for (int seed = 1; seed <= 12; seed++) {
                (GridNodeType[,] cells, float[,] field) = Finished(setup, seed);
                if (cells == null)
                    continue;

                int tinted = 0, unreached = 0;
                for (int x = 0; x < cells.GetLength(0); x++)
                    for (int y = 0; y < cells.GetLength(1); y++) {
                        if (!Tinted(cells[x, y]))
                            continue;
                        tinted++;
                        if (field[x, y] < 0f)
                            unreached++;
                    }

                Assert.Greater(tinted, 20, $"{name} seed {seed} barely searched, so this proves nothing");
                Assert.AreEqual(0, unreached,
                    $"{name} seed {seed}: {unreached} of {tinted} tinted cells were never reached");
                checked_++;
            }
            Assert.Greater(checked_, 6, $"{name} had too few solvable grids to prove anything");
        }

        /// <summary>
        /// The field is normalized by its own maximum, so if the tinted cells only ever reach a
        /// fraction of it the gradient is squashed into a slice of the ramp and reads as one flat
        /// colour. The floor sits well below what a healthy search manages (0.9 and up) and well
        /// above the half-ramp a single published wave gives.
        /// </summary>
        [TestCaseSource(nameof(Configurations))]
        public void TintedCellsSpanMostOfTheGradient(string name, Action<SolverSettings> setup) {
            int checked_ = 0;
            for (int seed = 1; seed <= 12; seed++) {
                (GridNodeType[,] cells, float[,] field) = Finished(setup, seed);
                if (cells == null)
                    continue;

                float max = 0f, tintedMax = 0f;
                for (int x = 0; x < cells.GetLength(0); x++)
                    for (int y = 0; y < cells.GetLength(1); y++) {
                        if (field[x, y] > max) max = field[x, y];
                        if (Tinted(cells[x, y]) && field[x, y] > tintedMax) tintedMax = field[x, y];
                    }

                Assert.Greater(max, 0f, $"{name} seed {seed} produced an empty field");
                Assert.GreaterOrEqual(tintedMax / max, 0.8f,
                    $"{name} seed {seed}: gradient only spans t=0..{tintedMax / max:0.00}");
                checked_++;
            }
            Assert.Greater(checked_, 6, $"{name} had too few solvable grids to prove anything");
        }
    }
}
