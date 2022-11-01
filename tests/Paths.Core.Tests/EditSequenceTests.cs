using System;
using System.Collections.Generic;
using NUnit.Framework;
using Paths.Core;
using Paths.Core.Search;

namespace Paths.Tests {
    /// <summary>
    /// The incremental tests next door each do one kind of edit at a time. What the app actually hands
    /// the solver is a mixture - walls drawn, terrain painted, both markers dragged, in whatever order
    /// the user felt like - and the routing inside Repair between Touch, MoveStart and giving up and
    /// replanning is precisely what an interleaved run leans on. A repaired route has to match a fresh
    /// one after every single step, on every grid.
    /// </summary>
    public class EditSequenceTests {
        private const int Size = 16;
        private const int Edits = 80;

        public static IEnumerable<TestCaseData> Grids() {
            foreach (bool diagonal in new[] { false, true })
                for (int seed = 1; seed <= 10; seed++)
                    yield return new TestCaseData(seed, diagonal)
                        .SetName($"{{m}}(seed {seed},{(diagonal ? "8" : "4")}way)");
        }

        private static PathSolver Build(int seed, bool diagonal, bool incremental) {
            PathSolver solver = Solvers.Cornered(Size, seed);
            solver.Settings.Diagonal = diagonal;
            solver.Settings.Heuristic = diagonal ? Heuristic.Octile : Heuristic.Manhattan;
            // the repair stands in for Dijkstra, so both sides of the comparison run that
            solver.Settings.Algorithm = Algorithm.Dijkstra;
            solver.Settings.Incremental = incremental;
            if (incremental)
                Solvers.AssertArmed(solver, Control.Incremental);
            return solver;
        }

        /// <summary>
        /// One edit, applied to both solvers so they stay the same grid. Returns false when the draw
        /// landed somewhere it cannot be applied, which just means this step is skipped.
        /// </summary>
        private static bool Edit(int kind, Coord at, Terrain tier, PathSolver a, PathSolver b) {
            switch (kind) {
                case 0: // draw or erase a wall
                    if (at == a.Start || at == a.End)
                        return false;
                    bool walkable = !a.IsWalkable(at);
                    a.SetWalkable(at, walkable);
                    b.SetWalkable(at, walkable);
                    return true;
                case 1: // paint a terrain tier
                    if (!a.IsWalkable(at))
                        return false;
                    a.SetTerrain(at, tier);
                    b.SetTerrain(at, tier);
                    return true;
                case 2: // drag the start
                    if (!a.IsWalkable(at) || at == a.End)
                        return false;
                    a.Start = at;
                    b.Start = at;
                    return true;
                // drag the goal, the one a plan cannot be repaired through
                default:
                    if (!a.IsWalkable(at) || at == a.Start)
                        return false;
                    a.End = at;
                    b.End = at;
                    return true;
            }
        }

        [TestCaseSource(nameof(Grids))]
        public void AMixedRunOfEditsTracksAFreshSolveThroughout(int seed, bool diagonal) {
            PathSolver incremental = Build(seed, diagonal, true);
            PathSolver plain = Build(seed, diagonal, false);

            incremental.Solve();
            plain.Solve();
            Solvers.AssertSameRoute(incremental, plain, "on the first plan");

            var rng = new Random(seed * 31 + (diagonal ? 1 : 0));
            Terrain[] tiers = Terrains.All;
            float last = Solvers.RouteCost(plain);
            int moved = 0; // steps that actually changed the answer

            for (int step = 0; step < Edits; step++) {
                // several edits before a solve, because that is how the app arrives: a drag piles up
                // cells across a frame and solves once, so a marker move and a wall land together
                var batch = new List<string>();
                int count = 1 + rng.Next(3);
                for (int i = 0; i < count; i++) {
                    int kind = rng.Next(4);
                    var at = new Coord(rng.Next(Size), rng.Next(Size));
                    if (Edit(kind, at, tiers[rng.Next(tiers.Length)], incremental, plain))
                        batch.Add($"kind {kind} at {at}");
                }
                if (batch.Count == 0)
                    continue;

                incremental.Solve();
                plain.Solve();
                Solvers.AssertSameRoute(incremental, plain, $"after step {step} [{string.Join(", ", batch)}]");

                float now = Solvers.RouteCost(plain);
                if (Math.Abs(now - last) > 1e-3f)
                    moved++;
                last = now;
            }

            // a run whose edits never reach the route agrees with a fresh solve for the dullest of
            // reasons. measured at 36 to 53 across these grids, so this floor catches a run gone
            // slack rather than ordinary variation
            Assert.Greater(moved, 25, $"only {moved} of {Edits} steps changed the route");
        }
    }
}
