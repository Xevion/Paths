using System.Collections;
using Game;
using Game.UI;
using NUnit.Framework;
using Paths.Core;
using Paths.Core.Search;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

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
            _editor = new GridEditor(_camera, _grid, _solver);
            _rig = new CameraRig(_camera);

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
            if (_canvas != null) Object.Destroy(_canvas);
            if (_grid != null) Object.Destroy(_grid.gameObject);
            if (_camera != null) Object.Destroy(_camera.gameObject);
            if (EventSystem.current != null) Object.Destroy(EventSystem.current.gameObject);
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

        private int WallCount() {
            int walls = 0;
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    if (!_solver.IsWalkable(new Coord(x, y)))
                        walls++;
            return walls;
        }

        [UnityTest]
        public IEnumerator PressOnTheGridDrawsWalls() {
            int before = WallCount();

            _pointer.Position = OverGrid;
            _pointer.SetButton(0, true);
            yield return Step();
            _pointer.Position = OverGridNextCell;
            yield return Step();

            Assert.Greater(WallCount(), before, "a press that started on the grid should draw");
        }

        [UnityTest]
        public IEnumerator PressOnTheInterfaceNeverDrawsWalls() {
            int before = WallCount();

            _pointer.Position = OverUi;
            _pointer.SetButton(0, true);
            yield return Step();

            // the control has the cursor now - drag it clean across the board
            _pointer.Position = OverGrid;
            yield return Step();
            _pointer.Position = OverGridNextCell;
            yield return Step();

            Assert.AreEqual(before, WallCount(), "a press that started on the interface must not draw");
        }

        [UnityTest]
        public IEnumerator DraggingAScrubFieldHoldsThePointerStill() {
            int before = WallCount();

            _pointer.Position = OverScrub;
            _pointer.SetButton(0, true);
            yield return Step();

            Assert.IsTrue(PointerInput.Pinned, "the press should have pinned the pointer");

            // the hardware cursor is parked but the OS keeps reporting movement. nothing should see it.
            _pointer.Position = OverGrid;
            yield return Step();

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
            _pointer.Position = OverUi;
            _pointer.SetButton(0, true);
            yield return Step();
            _pointer.SetButton(0, false);
            yield return Step();

            int before = WallCount();
            _pointer.Position = OverGrid;
            _pointer.SetButton(0, true);
            yield return Step();

            Assert.Greater(WallCount(), before, "a fresh press on the grid should draw again");
        }

        [UnityTest]
        public IEnumerator WheelOverTheInterfaceDoesNotZoom() {
            float size = _camera.orthographicSize;

            _pointer.Position = OverUi;
            yield return null;
            yield return null;
            _pointer.Wheel(1f);
            PointerInput.Update();
            _rig.Update();
            _pointer.NextFrame();

            Assert.AreEqual(size, _camera.orthographicSize, "the wheel over a panel scrolls it, not the board");

            _pointer.Position = OverGrid;
            yield return null;
            yield return null;
            _pointer.Wheel(1f);
            PointerInput.Update();
            _rig.Update();
            _pointer.NextFrame();

            Assert.AreNotEqual(size, _camera.orthographicSize, "the wheel over the board should zoom it");
        }
    }
}
