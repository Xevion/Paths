using System.Collections;
using System.Collections.Generic;
using Game.UI;
using NUnit.Framework;
using Paths.Core;
using Paths.Core.Search;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests {
    /// <summary>
    /// The whole app, driven by a scripted pointer and read back off the state texture. The texture
    /// is what the shader samples, so it is what ends up on screen; the solver's grid would only say
    /// what the app meant to draw, and the failures worth catching are the two disagreeing.
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

            yield return SceneManager.LoadSceneAsync("Default", LoadSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName("Default"));
            yield return null;
            yield return null;

            _grid = Object.FindObjectOfType<GridController>();
            _shell = Object.FindObjectOfType<AppShell>();
            _camera = Camera.main;
        }

        // take the app back down, or whatever runs next inherits a live board and a live event system
        [UnityTearDown]
        public IEnumerator TearDown() {
            PointerInput.Reset();
            Scene scene = SceneManager.GetSceneByName("Default");
            if (scene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(scene);

            // the unload's destroys are deferred, and an event system still standing next frame is one
            // the next fixture will adopt instead of building its own
            foreach (EventSystem system in Object.FindObjectsOfType<EventSystem>())
                Object.DestroyImmediate(system.gameObject);
            yield return null;
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
        /// A cell well inside the board that no panel covers, so a press on it reaches the grid. The
        /// runner's window is small, which is exactly why this is found rather than hardcoded.
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
        /// The bug this suite was written for: with a repair running, clicking a cell drew the search
        /// state the repair had just recorded for it instead of the wall, so clicking the same spot
        /// over and over looked like nothing happening.
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

        /// <summary>
        /// Wider than the one cell: every wall the solver holds is a wall on screen, and nothing else
        /// is. This is the invariant that catches the data and the render drifting apart at all.
        /// </summary>
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

        private static PathSolver Solver() {
            var ui = Object.FindObjectOfType<UIController>();
            return (PathSolver) typeof(UIController)
                .GetField("_solver", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(ui);
        }
    }
}
