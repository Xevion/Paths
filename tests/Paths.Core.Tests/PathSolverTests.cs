using System;
using System.Collections.Generic;
using NUnit.Framework;
using Paths.Core;
using Paths.Core.Generation;
using Paths.Core.Search;

namespace Paths.Tests {
    /// <summary>
    /// The face PathSolver turns to the app: the generators behind the picker, the pictures the
    /// renderer reads, and the search each setting actually selects. Coverage found this to be the
    /// least exercised file in the core, which fits - it is the part only the interface calls.
    /// </summary>
    public class PathSolverTests {
        private static PathSolver Solver(Generator generator, float density = 0.25f, int size = 24) =>
            new PathSolver(size, size, 7, generator, density);

        public static IEnumerable<TestCaseData> EachGenerator() {
            foreach (Generator generator in Enum.GetValues(typeof(Generator)))
                yield return new TestCaseData(generator).SetName($"{{m}}({generator})");
        }

        [TestCaseSource(nameof(EachGenerator))]
        public void EveryGeneratorLeavesSomewhereToStandAndSomewhereToGo(Generator generator) {
            PathSolver solver = Solver(generator);

            Assert.IsTrue(solver.IsWalkable(solver.Start), $"{generator} put the start in a wall");
            Assert.IsTrue(solver.IsWalkable(solver.End), $"{generator} put the end in a wall");
            Assert.AreNotEqual(solver.Start, solver.End, $"{generator} stacked both markers on one cell");
        }

        /// <summary>
        /// Rooms and Walk carve out of a solid grid while Random adds to an open one, so density means
        /// the opposite thing to each. What has to hold either way is that a grid asked for walls has
        /// some, and still has somewhere to walk.
        /// </summary>
        [TestCaseSource(nameof(EachGenerator))]
        public void EveryGeneratorLeavesBothWallsAndOpenGround(Generator generator) {
            PathSolver solver = Solver(generator);

            int walls = 0, open = 0;
            for (int i = 0; i < solver.Grid.CellCount; i++)
                if (solver.Grid.IsWalkable(i)) open++;
                else walls++;

            Assert.Greater(open, 0, $"{generator} left nowhere to walk");
            if (generator != Generator.Empty)
                Assert.Greater(walls, 0, $"{generator} was asked for walls and drew none");
            else
                Assert.AreEqual(0, walls, "Empty is supposed to leave every cell walkable");
        }

        [Test]
        public void TheSameSeedLaysOutTheSameGrid() {
            var first = new PathSolver(20, 20, 3, Generator.Rooms, 0.3f);
            var second = new PathSolver(20, 20, 3, Generator.Rooms, 0.3f);

            Assert.AreEqual(first.Start, second.Start);
            Assert.AreEqual(first.End, second.End);
            for (int i = 0; i < first.Grid.CellCount; i++)
                Assert.AreEqual(first.Grid.IsWalkable(i), second.Grid.IsWalkable(i), $"cell {i} differs");
        }

        [Test]
        public void AnUnseededSolverStillPutsItsMarkersSomewhereWalkable() {
            // the plain constructor is what a caller uses when it wants an open grid and its own rng
            var solver = new PathSolver(12, 9, new Random(4));

            Assert.IsTrue(solver.IsWalkable(solver.Start));
            Assert.IsTrue(solver.IsWalkable(solver.End));
            Assert.AreEqual(12 * 9, solver.Grid.CellCount, "no generator ran, so nothing should be walled");
        }

        /// <summary>The picture drawn when nothing is animating: walls, plus the two markers.</summary>
        [Test]
        public void TheEditableGridDrawsWallsAndBothMarkers() {
            PathSolver solver = Solver(Generator.Random);
            GridNodeType[,] drawn = solver.RenderEditable();

            Assert.AreEqual(GridNodeType.Start, drawn[solver.Start.X, solver.Start.Y]);
            Assert.AreEqual(GridNodeType.End, drawn[solver.End.X, solver.End.Y]);

            for (int i = 0; i < solver.Grid.CellCount; i++) {
                Coord at = solver.Grid.At(i);
                if (at == solver.Start || at == solver.End)
                    continue;
                GridNodeType expected = solver.Grid.IsWalkable(i) ? GridNodeType.Empty : GridNodeType.Wall;
                Assert.AreEqual(expected, drawn[at.X, at.Y], $"cell {at} drawn wrong");
            }
        }

        [Test]
        public void TheEditableGridFollowsAnEdit() {
            PathSolver solver = Solver(Generator.Empty);
            var at = new Coord(3, 4);

            solver.SetWalkable(at, false);
            Assert.AreEqual(GridNodeType.Wall, solver.RenderEditable()[at.X, at.Y]);

            solver.SetWalkable(at, true);
            Assert.AreEqual(GridNodeType.Empty, solver.RenderEditable()[at.X, at.Y]);
        }

        /// <summary>
        /// The heatmap reads this. An unreached cell and a cell at distance zero have to stay apart,
        /// so nothing solved yet reads as unreached rather than as a field of cold zeroes.
        /// </summary>
        [Test]
        public void TheCostFieldIsEmptyUntilSomethingHasBeenSolved() {
            PathSolver solver = Solver(Generator.Random);
            float[,] cost = solver.CostField();

            Assert.AreEqual(solver.Grid.Width, cost.GetLength(0));
            Assert.AreEqual(solver.Grid.Height, cost.GetLength(1));
            foreach (float reading in cost)
                Assert.AreEqual(PathSolver.Unreached, reading);
        }

        [Test]
        public void TheCostFieldRisesWithDistanceFromTheStart() {
            PathSolver solver = Solvers.Cornered();
            solver.Settings.Algorithm = Algorithm.Dijkstra; // reaches everywhere, so the field fills in
            solver.Solve();

            float[,] cost = solver.CostField();
            Assert.AreEqual(0f, cost[solver.Start.X, solver.Start.Y], "the start is zero cells from itself");

            IReadOnlyList<int> path = solver.Path;
            Assert.Greater(path.Count, 2, "need a route with something along it to read");
            float previous = -1f;
            foreach (int cell in path) {
                Coord at = solver.Grid.At(cell);
                float reading = cost[at.X, at.Y];
                Assert.Greater(reading, previous - 1e-4f, $"cost went backwards along the route at {at}");
                previous = reading;
            }
        }

        [Test]
        public void TerrainPaintedOnAGridComesBackInItsField() {
            PathSolver solver = Solver(Generator.Empty);
            var at = new Coord(5, 2);

            Assert.IsFalse(solver.HasTerrain, "an untouched grid should read as plain");
            solver.SetTerrain(at, Terrain.Water);

            Assert.IsTrue(solver.HasTerrain);
            Assert.AreEqual(Terrain.Water, solver.TerrainAt(at));
            Assert.AreEqual(Terrain.Water, solver.TerrainField()[at.X, at.Y]);
            Assert.AreEqual(Terrain.Plain, solver.TerrainField()[at.X + 1, at.Y]);
        }

        [Test]
        public void OffTheGridIsNotAValidCell() {
            PathSolver solver = Solver(Generator.Empty, size: 10);

            Assert.IsTrue(solver.IsValid(new Coord(0, 0)));
            Assert.IsTrue(solver.IsValid(new Coord(9, 9)));
            Assert.IsFalse(solver.IsValid(new Coord(-1, 0)));
            Assert.IsFalse(solver.IsValid(new Coord(10, 0)));
            Assert.IsFalse(solver.IsValid(new Coord(0, 10)));
        }

        /// <summary>
        /// Every entry in the picker has to reach a search that runs. A missing case would fall through
        /// to A* and quietly pretend, which is the shape of bug this suite exists for.
        /// </summary>
        public static IEnumerable<TestCaseData> EachAlgorithm() {
            foreach (Algorithm algorithm in Enum.GetValues(typeof(Algorithm)))
                yield return new TestCaseData(algorithm).SetName($"{{m}}({algorithm})");
        }

        [TestCaseSource(nameof(EachAlgorithm))]
        public void EveryAlgorithmInThePickerSolvesAndPaints(Algorithm algorithm) {
            PathSolver solver = Solvers.Cornered(density: 0.15f);
            solver.Settings.Algorithm = algorithm;

            ChangeController trace = solver.Solve();

            Assert.Greater(trace.Count, 0, $"{algorithm} drew nothing to play back");
            Assert.Greater(solver.Path.Count, 0, $"{algorithm} found no route on an open grid");
            Assert.AreEqual(solver.Grid.Index(solver.Start), solver.Path[0]);
            Assert.AreEqual(solver.Grid.Index(solver.End), solver.Path[solver.Path.Count - 1]);
        }

        /// <summary>
        /// The pick is the pick. Neither switch has a depth-first version, so both are dropped and the
        /// route is depth-first's own rather than the shortest one a stand-in search would have found.
        /// A switch answering past the picker is what makes changing the algorithm look like it does
        /// nothing, which is the whole reason the table decides this.
        /// </summary>
        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void AModifierWithNoVersionForThePickedAlgorithmIsDropped(bool bidirectional, bool incremental) {
            PathSolver solver = Solvers.Cornered(density: 0.15f);
            solver.Settings.Algorithm = Algorithm.DepthFirst; // the one whose route is nothing like the rest
            solver.Settings.Bidirectional = bidirectional;
            solver.Settings.Incremental = incremental;
            solver.Solve();

            PathSolver bare = Solvers.Cornered(density: 0.15f);
            bare.Settings.Algorithm = Algorithm.DepthFirst;
            bare.Solve();

            Assert.AreEqual(Solvers.RouteCost(bare), Solvers.RouteCost(solver), 1e-3f,
                "a switch with no depth-first version changed the route anyway");

            PathSolver shortest = Solvers.Cornered(density: 0.15f);
            shortest.Settings.Algorithm = Algorithm.Dijkstra;
            shortest.Solve();
            Assert.Greater(Solvers.RouteCost(solver), Solvers.RouteCost(shortest),
                "depth-first is not supposed to be shortest here, so this proves nothing");
        }

        /// <summary>
        /// Where a modifier does have a version, applying it must not cost the shortest route. That it
        /// runs at all is held to the capability table next door, which sweeps each switch over several
        /// grids; asserting it here off one solve would not hold, since a first incremental plan sweeps
        /// exactly as much as a fresh search and only pays off on the edit after.
        /// </summary>
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void AModifierThePickedAlgorithmSupportsKeepsTheRouteOptimal(bool bidirectional, bool incremental) {
            PathSolver solver = Solvers.Cornered(density: 0.15f);
            solver.Settings.Algorithm = Algorithm.Dijkstra; // the one both switches have a version of
            solver.Settings.Bidirectional = bidirectional;
            solver.Settings.Incremental = incremental;
            // with both on the repair takes precedence, so that is the one that has to be armed
            Solvers.AssertArmed(solver, incremental ? Control.Incremental : Control.Bidirectional);
            solver.Solve();

            PathSolver plain = Solvers.Cornered(density: 0.15f);
            plain.Settings.Algorithm = Algorithm.Dijkstra;
            plain.Solve();

            Assert.AreEqual(Solvers.RouteCost(plain), Solvers.RouteCost(solver), 1e-3f,
                "the modified search stopped finding the shortest route");
        }

        /// <summary>
        /// The sidebar warns when terrain has been painted under a search that ignores it, so this flag
        /// has to keep telling the truth about whichever search the settings actually select.
        /// </summary>
        [TestCase(Algorithm.AStar, true)]
        [TestCase(Algorithm.Dijkstra, true)]
        [TestCase(Algorithm.Greedy, false)]
        [TestCase(Algorithm.BreadthFirst, false)]
        [TestCase(Algorithm.DepthFirst, false)]
        [TestCase(Algorithm.JumpPoint, false)]
        public void WeightsAreHonouredByTheSearchesThatPriceSteps(Algorithm algorithm, bool honours) {
            PathSolver solver = Solver(Generator.Empty);
            solver.Settings.Algorithm = algorithm;

            Assert.AreEqual(honours, solver.HonorsWeights);
        }

        [Test]
        public void BothModifiersPriceTerrainWhereTheyApply() {
            PathSolver solver = Solver(Generator.Empty);
            solver.Settings.Algorithm = Algorithm.Dijkstra;

            solver.Settings.Bidirectional = true;
            Assert.IsTrue(solver.HonorsWeights, "the two-ended search prices steps");

            solver.Settings.Bidirectional = false;
            solver.Settings.Incremental = true;
            Assert.IsTrue(solver.HonorsWeights, "a repair prices steps");
        }

        /// <summary>
        /// The terrain warning reads this, so a dropped modifier must not answer for the search that
        /// actually runs. Greedy has no incremental version, so the switch cannot make it price steps.
        /// </summary>
        [Test]
        public void ADroppedModifierDoesNotAnswerForWhatPricesTerrain() {
            PathSolver solver = Solver(Generator.Empty);
            solver.Settings.Algorithm = Algorithm.Greedy; // ignores weights on its own

            solver.Settings.Incremental = true;
            Assert.IsFalse(solver.HonorsWeights,
                "greedy still runs, so the terrain warning has to keep showing");

            solver.Settings.Incremental = false;
            solver.Settings.Bidirectional = true;
            Assert.IsFalse(solver.HonorsWeights, "greedy still runs, so it still ignores terrain");
        }
    }
}
