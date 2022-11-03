using System.Collections;
using System.Collections.Generic;
using Game;
using Game.UI;
using NUnit.Framework;
using Paths.Core;
using Paths.Core.Search;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Terrain = Paths.Core.Search.Terrain;

namespace Game.Tests {
    /// <summary>
    /// Drives a real event system and a real grid off a scripted pointer, so what the interface does
    /// to the board can be tested without a mouse.
    /// </summary>
    public class PointerTests {
        private const int Size = 10;

        private Camera _camera;
        private GridController _grid;
        private PathSolver _solver;
        private GridEditor _editor;
        private CameraRig _rig;
        private FakePointer _pointer;
        private AnimationState _state = AnimationState.Stopped;
        private GameObject _canvas;

        // the left half of the screen is covered by an interface panel, the right half is bare grid
        private Vector2 OverUi => new Vector2(Screen.width * 0.25f, Screen.height * 0.5f);
        private Vector2 OverScrub => new Vector2(Screen.width * 0.25f, Screen.height * 0.8f);
        private Vector2 OverGrid => new Vector2(Screen.width * 0.78f, Screen.height * 0.5f);
        private Vector2 OverGridNextCell => new Vector2(Screen.width * 0.78f, Screen.height * 0.72f);

        [SetUp]
        public void SetUp() {
            _pointer = new FakePointer();
            PointerInput.Reset();
            PointerInput.Source = _pointer;

            var cameraObject = new GameObject("Camera");
            _camera = cameraObject.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 5f;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.transform.localScale = new Vector3(Size, Size, 1f);
            quad.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Paths/Grid"));
            _grid = quad.AddComponent<GridController>();
            _grid.width = _grid.height = Size;

            _solver = new PathSolver(Size, Size);
            // the solver drops its markers on random cells from an unseeded Random, and a press on a
            // marker moves it rather than drawing, which reads as a press that did nothing. park them
            // under the panel, at the high-x end, which is the half of the board nothing here presses
            _solver.Start = new Coord(Size - 1, Size - 1);
            _solver.End = new Coord(Size - 1, Size - 2);
            _editor = new GridEditor(_camera, _grid, _solver);
            _rig = new CameraRig(_camera);

            // this fixture builds its own screen, so anything still standing came from a fixture that
            // loads the app. EnsureEventSystem would adopt a stray event system rather than build one
            // reading the scripted pointer, and a stray sidebar would swallow every press on the grid
            Assert.IsEmpty(AppScene.LeftStanding(), "a previous fixture left something on screen");
            PointerBaseInput.EnsureEventSystem();
            BuildPanel();
        }

        // a screen-space panel over the left half, standing in for the toolbar and sidebar
        private void BuildPanel() {
            _canvas = new GameObject("Canvas", typeof(RectTransform));
            Canvas canvas = _canvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Panel", typeof(RectTransform));
            var rt = (RectTransform) panel.transform;
            rt.SetParent(_canvas.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            panel.AddComponent<Image>().raycastTarget = true;

            // a real scrub field in the panel's upper band, the control the bug was reported against
            var field = new GameObject("Scrub", typeof(RectTransform));
            var frt = (RectTransform) field.transform;
            frt.SetParent(rt, false);
            frt.anchorMin = new Vector2(0f, 0.7f);
            frt.anchorMax = new Vector2(1f, 0.9f);
            frt.offsetMin = frt.offsetMax = Vector2.zero;
            field.AddComponent<Image>().raycastTarget = true;
            ScrubControl scrub = field.AddComponent<ScrubControl>();
            scrub.min = 0;
            scrub.max = 100;
            scrub.value = 10;
        }

        [TearDown]
        public void TearDown() {
            PointerInput.Reset();
            // immediate, not deferred: the next SetUp runs before a frame passes, so a Destroy here
            // leaves the screen still standing when it looks
            if (_canvas != null) Object.DestroyImmediate(_canvas);
            if (_grid != null) Object.DestroyImmediate(_grid.gameObject);
            if (_camera != null) Object.DestroyImmediate(_camera.gameObject);
            if (EventSystem.current != null) Object.DestroyImmediate(EventSystem.current.gameObject);
        }

        /// <summary>One app frame. Two yields of slack: the event system and the test runner both
        /// drive off Update and their order between them isn't fixed.</summary>
        private IEnumerator Step() {
            yield return null;
            yield return null;
            PointerInput.Update();
            _state = _editor.Process(_state);
            _pointer.NextFrame();
        }

        /// <summary>
        /// Move, settle, then press - never both in one frame. Whether a press belongs to the
        /// interface is decided from the event system's raycast, and that is refreshed on its own
        /// Update; pressing the same frame the cursor arrives can be judged against where it was.
        /// </summary>
        private IEnumerator MoveTo(Vector2 position) {
            _pointer.Position = position;
            yield return Step();
            yield return Step(); // a second frame: one is not always enough for uGUI to re-raycast
        }

        private IEnumerator PressAt(Vector2 position) {
            yield return MoveTo(position);
            _pointer.SetButton(0, true);
            yield return Step();
        }

        /// <summary>
        /// What the pointer seam saw, for an assertion about a press that went nowhere. The answer is
        /// never in the wall count, it is in who owned the press and what the raycast hit.
        /// </summary>
        private string Seam(Vector2 at) {
            var hits = new List<RaycastResult>();
            if (EventSystem.current != null)
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = at }, hits);
            var under = new List<string>();
            foreach (RaycastResult hit in hits)
                under.Add(hit.gameObject.name);
            string standing = AppScene.LeftStanding();
            return $"\n  at {at}, screen {Screen.width}x{Screen.height}" +
                   $"\n  overUi {PointerInput.OverUi}, uiOwns {PointerInput.UiOwns()}, pinned {PointerInput.Pinned}" +
                   $"\n  eventSystem {(EventSystem.current == null ? "none" : EventSystem.current.gameObject.name)}" +
                   $"\n  raycast hit [{string.Join(", ", under)}]" +
                   $"\n  left standing [{(standing.Length == 0 ? "nothing" : standing)}]";
        }

        private int WallCount() {
            int walls = 0;
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    if (!_solver.IsWalkable(new Coord(x, y)))
                        walls++;
            return walls;
        }

        /// <summary>
        /// Holds the parking above to its purpose. Left where the solver puts them the markers land
        /// on a cell this fixture presses roughly one run in fifty, and the failure that produces is
        /// a wall count that did not move, which says nothing about why.
        /// </summary>
        [UnityTest]
        public IEnumerator TheMarkersSitClearOfEveryCellTheFixturePresses() {
            yield return null;
            var pressed = new List<Vector2Int>(DragLane());
            foreach (Vector2 at in new[] { OverGrid, OverGridNextCell })
                if (_grid.CellAt(_camera.ScreenToWorldPoint(at), out Vector2Int cell))
                    pressed.Add(cell);
            Assert.IsNotEmpty(pressed, "no cell was worked out, so this guard is checking nothing");

            foreach (Vector2Int cell in pressed) {
                Assert.AreNotEqual(_solver.Start, cell.ToCoord(), $"the start sits on {cell}, which is pressed");
                Assert.AreNotEqual(_solver.End, cell.ToCoord(), $"the end sits on {cell}, which is pressed");
            }
        }

        [UnityTest]
        public IEnumerator PressOnTheGridDrawsWalls() {
            int before = WallCount();

            yield return PressAt(OverGrid);
            yield return MoveTo(OverGridNextCell);

            Assert.Greater(WallCount(), before, "a press that started on the grid should draw" + Seam(OverGrid));
        }

        [UnityTest]
        public IEnumerator PressOnTheInterfaceNeverDrawsWalls() {
            int before = WallCount();

            yield return PressAt(OverUi);

            // the control has the cursor now - drag it clean across the board
            yield return MoveTo(OverGrid);
            yield return MoveTo(OverGridNextCell);

            Assert.AreEqual(before, WallCount(), "a press that started on the interface must not draw");
        }

        [UnityTest]
        public IEnumerator DraggingAScrubFieldHoldsThePointerStill() {
            int before = WallCount();

            yield return PressAt(OverScrub);

            Assert.IsTrue(PointerInput.Pinned, "the press should have pinned the pointer");

            // the hardware cursor is parked but the OS keeps reporting movement. nothing should see it.
            yield return MoveTo(OverGrid);

            Assert.AreEqual(OverScrub, PointerInput.Position);
            Assert.IsTrue(EventSystem.current.IsPointerOverGameObject(),
                "the event system should still be looking at the pinned point");
            Assert.AreEqual(before, WallCount());

            _pointer.SetButton(0, false);
            yield return Step();
            yield return Step();

            Assert.IsFalse(PointerInput.Pinned, "release should hand the pointer back");
        }

        [UnityTest]
        public IEnumerator PinnedPositionIgnoresWhereTheCursorWent() {
            _pointer.Position = OverUi;
            PointerInput.Pin(OverUi);
            _pointer.Position = OverGrid;

            Assert.AreEqual(OverUi, PointerInput.Position);

            PointerInput.Unpin();
            Assert.AreEqual(OverGrid, PointerInput.Position);
            yield break;
        }

        [UnityTest]
        public IEnumerator ReleasingHandsTheGridBackItsPresses() {
            yield return PressAt(OverUi);
            _pointer.SetButton(0, false);
            yield return Step();

            int before = WallCount();
            yield return PressAt(OverGrid);

            Assert.Greater(WallCount(), before, "a fresh press on the grid should draw again" + Seam(OverGrid));
        }

        [UnityTest]
        public IEnumerator WheelOverTheInterfaceDoesNotZoom() {
            float size = _camera.orthographicSize;

            yield return MoveTo(OverUi);
            _pointer.Wheel(1f);
            PointerInput.Update();
            _rig.Update();
            _pointer.NextFrame();

            Assert.AreEqual(size, _camera.orthographicSize, "the wheel over a panel scrolls it, not the board");

            yield return MoveTo(OverGrid);
            _pointer.Wheel(1f);
            PointerInput.Update();
            _rig.Update();
            _pointer.NextFrame();

            Assert.AreNotEqual(size, _camera.orthographicSize, "the wheel over the board should zoom it");
        }

        private Vector2 ScreenOf(Vector2Int cell) => _camera.WorldToScreenPoint(_grid.GetWorldPosition(cell));

        /// <summary>
        /// A run of cells in one column that all sit clear of the panel, so a drag down them reaches
        /// the board the whole way. Found rather than hardcoded: the runner's window is small and the
        /// grid only covers the middle of it.
        /// </summary>
        private List<Vector2Int> DragLane() {
            var lane = new List<Vector2Int>();
            for (int x = 0; x < Size; x++) {
                lane.Clear();
                for (int y = 0; y < Size; y++) {
                    Vector2 screen = ScreenOf(new Vector2Int(x, y));
                    if (screen.x > Screen.width * 0.58f && screen.y > 4f && screen.y < Screen.height - 4f)
                        lane.Add(new Vector2Int(x, y));
                }
                if (lane.Count >= 4)
                    return lane;
            }
            return lane;
        }

        private IEnumerator DragDown(List<Vector2Int> lane) {
            yield return PressAt(ScreenOf(lane[0]));
            for (int i = 1; i < lane.Count; i++)
                yield return MoveTo(ScreenOf(lane[i]));
            _pointer.SetButton(0, false);
            yield return Step();
        }

        /// <summary>
        /// A drag commits to one operation on the cell it started from and does that the whole way.
        /// Toggling each cell as it is crossed would mean dragging back over your own line rubbed it
        /// out, and a fast drag skipping about would leave a dotted mess behind it.
        /// </summary>
        [UnityTest]
        public IEnumerator ADragThatStartsOnOpenGroundOnlyDrawsWalls() {
            List<Vector2Int> lane = DragLane();
            Assert.GreaterOrEqual(lane.Count, 4, "no run of cells clear of the panel to drag down");

            // a wall already sitting in the middle of the lane - a toggling drag would rub it out
            _solver.SetWalkable(lane[1].ToCoord(), false);
            Assert.IsTrue(_solver.IsWalkable(lane[0].ToCoord()), "the drag has to start on open ground");

            yield return DragDown(lane);

            foreach (Vector2Int cell in lane)
                Assert.IsFalse(_solver.IsWalkable(cell.ToCoord()), $"cell {cell} was crossed but is not a wall");
        }

        [UnityTest]
        public IEnumerator ADragThatStartsOnAWallOnlyClearsThem() {
            List<Vector2Int> lane = DragLane();
            Assert.GreaterOrEqual(lane.Count, 4, "no run of cells clear of the panel to drag down");

            // start on a wall, and leave the rest of the lane open so a toggling drag would fill it in
            _solver.SetWalkable(lane[0].ToCoord(), false);
            for (int i = 1; i < lane.Count; i++)
                _solver.SetWalkable(lane[i].ToCoord(), true);

            yield return DragDown(lane);

            foreach (Vector2Int cell in lane)
                Assert.IsTrue(_solver.IsWalkable(cell.ToCoord()), $"cell {cell} was crossed but is still a wall");
        }

        /// <summary>
        /// Pressing a cell that already holds the armed tier means erase, and that decision is taken
        /// once, at the press, for the whole drag. Taken per cell instead, a drag over mixed ground
        /// would paint some cells and erase others and come out striped.
        /// </summary>
        [UnityTest]
        public IEnumerator ATerrainDragErasesAllTheWayWhenItStartsOnThatTier() {
            List<Vector2Int> lane = DragLane();
            Assert.GreaterOrEqual(lane.Count, 4, "no run of cells clear of the panel to drag down");

            foreach (Vector2Int cell in lane) {
                _solver.SetWalkable(cell.ToCoord(), true);
                _solver.SetTerrain(cell.ToCoord(), Terrain.Mud);
            }
            // one cell along the way is not the armed tier, so a per-cell decision would paint it
            _solver.SetTerrain(lane[2].ToCoord(), Terrain.Plain);

            _editor.Brush = BrushKind.Terrain;
            _editor.Tier = Terrain.Mud;
            yield return PressAt(ScreenOf(lane[0]));
            // say which half failed: the press never reaching the board reads the same at the end as
            // the drag deciding per cell, and they are very different problems
            Assert.AreEqual(Terrain.Plain, _solver.TerrainAt(lane[0].ToCoord()),
                "the press never reached the board, so the drag never started" + Seam(ScreenOf(lane[0])));
            for (int i = 1; i < lane.Count; i++)
                yield return MoveTo(ScreenOf(lane[i]));
            _pointer.SetButton(0, false);
            yield return Step();

            foreach (Vector2Int cell in lane)
                Assert.AreEqual(Terrain.Plain, _solver.TerrainAt(cell.ToCoord()),
                    $"the drag started as an erase, so cell {cell} should have come out plain");
        }

        [UnityTest]
        public IEnumerator ATerrainDragPaintsOneTierAllTheWay() {
            List<Vector2Int> lane = DragLane();
            Assert.GreaterOrEqual(lane.Count, 4, "no run of cells clear of the panel to drag down");

            foreach (Vector2Int cell in lane) {
                _solver.SetWalkable(cell.ToCoord(), true);
                _solver.SetTerrain(cell.ToCoord(), Terrain.Plain);
            }
            _solver.SetTerrain(lane[2].ToCoord(), Terrain.Water); // a different tier in the way

            _editor.Brush = BrushKind.Terrain;
            _editor.Tier = Terrain.Mud;
            yield return DragDown(lane);

            foreach (Vector2Int cell in lane)
                Assert.AreEqual(Terrain.Mud, _solver.TerrainAt(cell.ToCoord()), $"cell {cell} was crossed but is not Mud");
        }

        /// <summary>
        /// A marker cannot be dropped in a wall. The drag ends on one, so the marker has to stay put
        /// at the last cell that would take it rather than follow the cursor in.
        /// </summary>
        [UnityTest]
        public IEnumerator DraggingTheStartIntoAWallLeavesItOutside() {
            List<Vector2Int> lane = DragLane();
            Assert.GreaterOrEqual(lane.Count, 4, "no run of cells clear of the panel to drag down");

            foreach (Vector2Int cell in lane)
                _solver.SetWalkable(cell.ToCoord(), true);
            _solver.Start = lane[0].ToCoord();
            _solver.End = new Coord(0, 0);

            Vector2Int last = lane[lane.Count - 1];
            _solver.SetWalkable(last.ToCoord(), false); // the drag finishes inside this one

            yield return DragDown(lane);

            Assert.AreNotEqual(last.ToCoord(), _solver.Start, "the start was dropped into a wall");
            Assert.IsTrue(_solver.IsWalkable(_solver.Start), "the start is standing in a wall");
        }

    }
}
