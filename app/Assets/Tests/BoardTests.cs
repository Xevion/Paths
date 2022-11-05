using System.Collections;
using System.Collections.Generic;
using Game.UI;
using NUnit.Framework;
using Paths.Core;
using Paths.Core.Search;
using Terrain = Paths.Core.Search.Terrain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests {
    /// <summary>
    /// The whole app, driven by a scripted pointer and read back off the textures the shader
    /// samples. The solver's grid would only say what the app meant to draw; the failures worth
    /// catching are the two disagreeing.
    /// </summary>
    public class BoardTests {
        private GridController _grid;
        private AppShell _shell;
        private Camera _camera;
        private FakePointer _pointer;

        // how the board can be searched - a repair and a full search paint very differently.
        // Returns(null) is how a UnityTest takes cases; without it the runner skips them all.
        private static IEnumerable<TestCaseData> Modes() {
            yield return new TestCaseData(false, false).Returns(null).SetName("{m}(plain)");
            yield return new TestCaseData(true, false).Returns(null).SetName("{m}(incremental)");
            yield return new TestCaseData(false, true).Returns(null).SetName("{m}(bidirectional)");
        }

        [UnitySetUp]
        public IEnumerator SetUp() {
            _pointer = new FakePointer();
            PointerInput.Reset();
            PointerInput.Source = _pointer;

            yield return AppScene.Load();

            _grid = Object.FindObjectOfType<GridController>();
            _shell = Object.FindObjectOfType<AppShell>();
            _camera = Camera.main;
        }

        [UnityTearDown]
        public IEnumerator TearDown() {
            PointerInput.Reset();
            yield return AppScene.Unload();
        }

        private IEnumerator Frame() {
            yield return null;
            _pointer.NextFrame();
        }

        private IEnumerator Frames(int count) {
            for (int i = 0; i < count; i++)
                yield return Frame();
        }

        private Vector2 ScreenOf(Vector2Int cell) => _camera.WorldToScreenPoint(_grid.GetWorldPosition(cell));

        private IEnumerator Press(Vector2Int cell) {
            _pointer.Position = ScreenOf(cell);
            yield return Frame();
            _pointer.SetButton(0, true);
            yield return Frame();
        }

        private IEnumerator Release() {
            _pointer.SetButton(0, false);
            yield return Frames(2);
        }

        private IEnumerator Click(Vector2Int cell) {
            yield return Press(cell);
            yield return Release();
        }

        /// <summary>
        /// A cell no panel covers, so a press reaches the grid. Found rather than hardcoded because
        /// the runner's window is small.
        /// </summary>
        private IEnumerator FindClearCell(System.Action<Vector2Int> found) {
            for (int x = 2; x < _grid.width - 2; x++)
                for (int y = 2; y < _grid.height - 2; y++) {
                    _pointer.Position = ScreenOf(new Vector2Int(x, y));
                    yield return Frame();
                    if (!PointerInput.OverUi && _grid.StateAt(x, y) == GridNodeType.Empty) {
                        found(new Vector2Int(x, y));
                        yield break;
                    }
                }
            found(new Vector2Int(-1, -1));
        }

        private IEnumerator StartSearching(bool incremental, bool bidirectional) {
            _shell.onIncremental(incremental);
            _shell.onBidirectional(bidirectional);
            _shell.onPlayPause();
            yield return Frames(60);
        }

        /// <summary>
        /// A repair records a state for the clicked cell, and that must not win over the wall the
        /// click drew, or clicking the same spot repeatedly looks like nothing happening.
        /// </summary>
        [UnityTest]
        [TestCaseSource(nameof(Modes))]
        public IEnumerator ClickingACellTogglesWhatIsDrawn(bool incremental, bool bidirectional) {
            var cell = new Vector2Int(-1, -1);
            yield return FindClearCell(c => cell = c);
            Assert.GreaterOrEqual(cell.x, 0, "no clear cell to click on");

            yield return StartSearching(incremental, bidirectional);

            for (int click = 0; click < 3; click++) {
                yield return Click(cell);
                Assert.AreEqual(GridNodeType.Wall, _grid.StateAt(cell.x, cell.y),
                    $"click {click} drew a wall but the board does not show one");

                yield return Click(cell);
                Assert.AreNotEqual(GridNodeType.Wall, _grid.StateAt(cell.x, cell.y),
                    $"click {click} cleared the wall but the board still shows one");
            }
        }

        /// <summary>Every wall the solver holds is a wall on screen, and nothing else is.</summary>
        [UnityTest]
        [TestCaseSource(nameof(Modes))]
        public IEnumerator TheBoardShowsEveryWallTheSolverHolds(bool incremental, bool bidirectional) {
            var cell = new Vector2Int(-1, -1);
            yield return FindClearCell(c => cell = c);
            Assert.GreaterOrEqual(cell.x, 0, "no clear cell to click on");

            yield return StartSearching(incremental, bidirectional);

            // draw a short streak, the way a drag does, then check the whole board
            yield return Press(cell);
            for (int i = 1; i <= 3; i++) {
                _pointer.Position = ScreenOf(new Vector2Int(cell.x + i, cell.y));
                yield return Frame();
            }
            yield return Release();
            yield return Frames(3);

            PathSolver solver = Solver();
            for (int x = 0; x < _grid.width; x++)
                for (int y = 0; y < _grid.height; y++) {
                    bool solid = !solver.IsWalkable(new Coord(x, y));
                    bool drawn = _grid.StateAt(x, y) == GridNodeType.Wall;
                    Assert.AreEqual(solid, drawn, $"cell ({x}, {y}) is {(solid ? "a wall" : "open")} but is drawn as {_grid.StateAt(x, y)}");
                }
        }

        /// <summary>Both markers stay on the board however the search paints over them.</summary>
        [UnityTest]
        [TestCaseSource(nameof(Modes))]
        public IEnumerator TheMarkersAreAlwaysOnTheBoard(bool incremental, bool bidirectional) {
            var cell = new Vector2Int(-1, -1);
            yield return FindClearCell(c => cell = c);
            Assert.GreaterOrEqual(cell.x, 0, "no clear cell to click on");

            yield return StartSearching(incremental, bidirectional);
            yield return Click(cell);
            yield return Frames(5);

            PathSolver solver = Solver();
            Assert.AreEqual(GridNodeType.Start, _grid.StateAt(solver.Start.X, solver.Start.Y));
            Assert.AreEqual(GridNodeType.End, _grid.StateAt(solver.End.X, solver.End.Y));
        }

        /// <summary>Editing with nothing running renders straight off the grid, so it must track too.</summary>
        [UnityTest]
        public IEnumerator ClickingBeforeAnythingRunsStillDrawsTheWall() {
            var cell = new Vector2Int(-1, -1);
            yield return FindClearCell(c => cell = c);
            Assert.GreaterOrEqual(cell.x, 0, "no clear cell to click on");

            yield return Click(cell);
            yield return Frames(2);
            Assert.AreEqual(GridNodeType.Wall, _grid.StateAt(cell.x, cell.y));

            yield return Click(cell);
            yield return Frames(2);
            Assert.AreNotEqual(GridNodeType.Wall, _grid.StateAt(cell.x, cell.y));
        }

        /// <summary>
        /// Paused is its own branch, and the Modes matrix crosses how the board searches rather
        /// than how it is playing, so this is the only place that branch is exercised.
        /// </summary>
        [UnityTest]
        [TestCaseSource(nameof(Modes))]
        public IEnumerator EditingWhilePausedStillDrawsTheWall(bool incremental, bool bidirectional) {
            var cell = new Vector2Int(-1, -1);
            yield return FindClearCell(c => cell = c);
            Assert.GreaterOrEqual(cell.x, 0, "no clear cell to click on");

            yield return StartSearching(incremental, bidirectional);
            _shell.onPlayPause(); // park it
            yield return Frames(3);

            yield return Click(cell);
            yield return Frames(3);
            Assert.AreEqual(GridNodeType.Wall, _grid.StateAt(cell.x, cell.y),
                "a wall drawn while paused is not on the board");

            yield return Click(cell);
            yield return Frames(3);
            Assert.AreNotEqual(GridNodeType.Wall, _grid.StateAt(cell.x, cell.y),
                "the wall was cleared while paused but the board still shows it");
        }

        /// <summary>
        /// Terrain rides in the heatmap texture rather than the state index so the search can sweep
        /// over it. Both halves: the tier arrives, and the cell's state is left alone.
        /// </summary>
        [UnityTest]
        [TestCaseSource(nameof(Modes))]
        public IEnumerator PaintingTerrainLeavesTheCellsStateAlone(bool incremental, bool bidirectional) {
            var cell = new Vector2Int(-1, -1);
            yield return FindClearCell(c => cell = c);
            Assert.GreaterOrEqual(cell.x, 0, "no clear cell to paint on");

            yield return StartSearching(incremental, bidirectional);
            GridNodeType before = _grid.StateAt(cell.x, cell.y);

            _shell.onBrush(2); // the tiers run cheapest first, so 2 is Mud
            yield return Frames(2);
            yield return Click(cell);
            yield return Frames(3);

            Assert.AreEqual(Terrain.Mud, _grid.TerrainAt(cell.x, cell.y), "the painted tier never reached the board");
            Assert.AreNotEqual(GridNodeType.Wall, _grid.StateAt(cell.x, cell.y), "painting terrain drew a wall");
            if (before != GridNodeType.Empty)
                Assert.AreNotEqual(GridNodeType.Empty, _grid.StateAt(cell.x, cell.y),
                    "painting terrain wiped the search state off the cell");
        }

        /// <summary>Resizing mid-search has to leave a board that is still drawn correctly.</summary>
        [UnityTest]
        [TestCaseSource(nameof(Modes))]
        public IEnumerator ResizingMidSearchRedrawsTheWholeBoard(bool incremental, bool bidirectional) {
            yield return StartSearching(incremental, bidirectional);

            _shell.onWidth(_grid.width + 5);
            yield return Frames(20);

            PathSolver solver = Solver();
            Assert.AreEqual(_grid.width * _grid.height, solver.Grid.CellCount, "the board and the solver disagree on size");
            for (int x = 0; x < _grid.width; x++)
                for (int y = 0; y < _grid.height; y++) {
                    bool solid = !solver.IsWalkable(new Coord(x, y));
                    bool drawn = _grid.StateAt(x, y) == GridNodeType.Wall;
                    Assert.AreEqual(solid, drawn, $"cell ({x}, {y}) drawn as {_grid.StateAt(x, y)} after the resize");
                }
        }

        /// <summary>
        /// The join mask is the last thing this side of the GPU that says what shape the route is,
        /// so it has to say what the solver walked. Not the shader's copy of the bit order.
        /// </summary>
        [UnityTest]
        [TestCaseSource(nameof(Modes))]
        public IEnumerator TheRibbonIsJoinedAlongTheRouteAndNowhereElse(bool incremental, bool bidirectional) {
            yield return StartSearching(incremental, bidirectional);
            AssertRibbonMatchesTheRoute("after the first search");
        }

        /// <summary>
        /// The mask holds the whole route rather than being revealed a step at a time, so a push
        /// that does not happen leaves a ribbon drawn along a route that no longer exists.
        /// </summary>
        [UnityTest]
        [TestCaseSource(nameof(Modes))]
        public IEnumerator EditingTheRouteRedrawsTheRibbon(bool incremental, bool bidirectional) {
            yield return StartSearching(incremental, bidirectional);

            var onRoute = new Vector2Int(-1, -1);
            yield return FindRouteCell(c => onRoute = c);
            Assume.That(onRoute.x >= 0, "no reachable cell on the route to wall off");

            yield return Click(onRoute);
            yield return Frames(5);

            AssertRibbonMatchesTheRoute("after walling off a cell the route ran through");
        }

        /// <summary>A cell the route runs through, clear of the panels so a press reaches it.</summary>
        private IEnumerator FindRouteCell(System.Action<Vector2Int> found) {
            PathSolver solver = Solver();
            for (int i = 1; i < solver.Path.Count - 1; i++) {
                Coord at = solver.Grid.At(solver.Path[i]);
                var cell = new Vector2Int(at.X, at.Y);
                _pointer.Position = ScreenOf(cell);
                yield return Frame();
                if (!PointerInput.OverUi) {
                    found(cell);
                    yield break;
                }
            }
            found(new Vector2Int(-1, -1));
        }

        private void AssertRibbonMatchesTheRoute(string when) {
            PathSolver solver = Solver();
            Assume.That(solver.Path.Count > 1, $"there is no route to draw {when}");

            var steps = new HashSet<(int, int, int, int)>();
            for (int i = 1; i < solver.Path.Count; i++) {
                Coord a = solver.Grid.At(solver.Path[i - 1]);
                Coord b = solver.Grid.At(solver.Path[i]);
                steps.Add((a.X, a.Y, b.X, b.Y));
                steps.Add((b.X, b.Y, a.X, a.Y));
                Assert.IsTrue(PathLinks.Linked(_grid.LinkAt(a.X, a.Y), b.X - a.X, b.Y - a.Y),
                    $"step {i} of the route is not drawn {when}");
            }

            for (int x = 0; x < _grid.width; x++)
                for (int y = 0; y < _grid.height; y++)
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++) {
                            if (!PathLinks.Linked(_grid.LinkAt(x, y), dx, dy))
                                continue;
                            Assert.IsTrue(steps.Contains((x, y, x + dx, y + dy)),
                                $"({x}, {y}) is drawn joined to ({x + dx}, {y + dy}), which the route " +
                                $"never stepped between, {when}");
                        }
        }

        /// <summary>A theme is colour only, so swapping it must not disturb a single cell's state.</summary>
        [UnityTest]
        public IEnumerator SwitchingThemeDoesNotDisturbWhatIsDrawn() {
            yield return StartSearching(false, false);

            var before = new GridNodeType[_grid.width, _grid.height];
            for (int x = 0; x < _grid.width; x++)
                for (int y = 0; y < _grid.height; y++)
                    before[x, y] = _grid.StateAt(x, y);

            _shell.onTheme(1);
            yield return Frames(3);

            for (int x = 0; x < _grid.width; x++)
                for (int y = 0; y < _grid.height; y++)
                    Assert.AreEqual(before[x, y], _grid.StateAt(x, y), $"cell ({x}, {y}) changed state with the theme");
        }

        private static PathSolver Solver() => Object.FindObjectOfType<UIController>()._solver;
    }
}
