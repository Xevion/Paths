using System;
using System.Collections;
using System.Collections.Generic;
using Game.UI;
using NUnit.Framework;
using Paths.Core;
using Paths.Core.Generation;
using Paths.Core.Search;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Terrain = Paths.Core.Search.Terrain;

namespace Game.Tests {
    /// <summary>
    /// Every control the toolbar, sidebar and dialog offer, driven through the callbacks AppShell
    /// hands out, each asserted to reach the thing it is supposed to change.
    ///
    /// What the settings then do is Core's business and tested there; what this catches is a control
    /// wired to nothing, which this project has shipped three times - a whole dialog of no-op
    /// handlers, an interface scale slider that moved nothing, and a generator that was a stub.
    /// </summary>
    public class ShellTests {
        private UIController _ui;
        private AppShell _shell;
        private GridController _grid;

        private static readonly int Gap = Shader.PropertyToID("_Gap");
        private static readonly int ShowDistances = Shader.PropertyToID("_ShowDistances");
        private static readonly int TransitionTime = Shader.PropertyToID("_TransitionTime");
        private static readonly int EmptyColor = Shader.PropertyToID("_EmptyColor");
        private static readonly int BrushColor = Shader.PropertyToID("_BrushColor");

        [UnitySetUp]
        public IEnumerator SetUp() {
            PointerInput.Reset();
            yield return AppScene.Load();

            _ui = UnityEngine.Object.FindObjectOfType<UIController>();
            _shell = UnityEngine.Object.FindObjectOfType<AppShell>();
            _grid = UnityEngine.Object.FindObjectOfType<GridController>();
        }

        [UnityTearDown]
        public IEnumerator TearDown() {
            PointerInput.Reset();
            yield return AppScene.Unload();
        }

        /// <summary>Let the app run a few frames, since most controls land through Update.</summary>
        private IEnumerator Settle(int frames = 3) {
            for (int i = 0; i < frames; i++)
                yield return null;
        }

        private IEnumerator Run(int frames = 40) {
            _shell.onPlayPause();
            yield return Settle(frames);
        }

        // a snapshot of which cells are walls, to tell one generated layout from another
        private static bool[] Layout(PathSolver solver) {
            var walls = new bool[solver.Grid.CellCount];
            for (int i = 0; i < walls.Length; i++)
                walls[i] = !solver.Grid.IsWalkable(i);
            return walls;
        }

        private static bool Same(bool[] a, bool[] b) {
            if (a.Length != b.Length)
                return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i])
                    return false;
            return true;
        }

        [UnityTest]
        public IEnumerator TheAlgorithmPickerSelectsTheSearch() {
            foreach (Algorithm algorithm in Enum.GetValues(typeof(Algorithm))) {
                _shell.onAlgorithm(algorithm);
                yield return Settle();
                Assert.AreEqual(algorithm, _ui._settings.Algorithm, $"picking {algorithm} did not reach the solver");
            }
        }

        [UnityTest]
        public IEnumerator TheHeuristicPickerSelectsTheEstimate() {
            foreach (Heuristic heuristic in Enum.GetValues(typeof(Heuristic))) {
                _shell.onHeuristic(heuristic);
                yield return Settle();
                Assert.AreEqual(heuristic, _ui._settings.Heuristic, $"picking {heuristic} did not reach the solver");
            }
        }

        [UnityTest]
        public IEnumerator TheModeSwitchesReachTheSolver() {
            _shell.onDiagonal(true);
            _shell.onBidirectional(true);
            _shell.onIncremental(true);
            _shell.onWeight(2.5f);
            yield return Settle();

            Assert.IsTrue(_ui._settings.Diagonal, "the diagonal switch did nothing");
            Assert.IsTrue(_ui._settings.Bidirectional, "the bidirectional switch did nothing");
            Assert.IsTrue(_ui._settings.Incremental, "the incremental switch did nothing");
            Assert.AreEqual(2.5f, _ui._settings.Weight, 1e-4f, "the weight slider did nothing");

            _shell.onDiagonal(false);
            _shell.onBidirectional(false);
            _shell.onIncremental(false);
            yield return Settle();

            Assert.IsFalse(_ui._settings.Diagonal, "the diagonal switch would not go back");
            Assert.IsFalse(_ui._settings.Bidirectional, "the bidirectional switch would not go back");
            Assert.IsFalse(_ui._settings.Incremental, "the incremental switch would not go back");
        }

        /// <summary>
        /// The invariant behind holding the settings as one object: a regen throws the solver away and
        /// builds another, and every setting has to make it across. The old code copied six fields by
        /// hand here, so a seventh would have been dropped without a word.
        /// </summary>
        [UnityTest]
        public IEnumerator RegeneratingTheGridKeepsEverySetting() {
            _shell.onAlgorithm(Algorithm.JumpPoint);
            _shell.onHeuristic(Heuristic.Octile);
            _shell.onDiagonal(true);
            _shell.onWeight(1.75f);
            _shell.onBidirectional(true);
            _shell.onIncremental(true);
            yield return Settle();

            SolverSettings before = _ui._settings.Clone();
            // without this the test passes on settings nobody managed to change, which is exactly how
            // it read when every control was stubbed out to check this suite bites
            Assert.IsFalse(before.SameAs(new SolverSettings()),
                "the controls left the settings at their defaults, so a regen has nothing to lose");

            PathSolver old = _ui._solver;
            _shell.onGenerate();
            yield return Settle();

            Assert.AreNotSame(old, _ui._solver, "the grid was never regenerated");
            Assert.IsTrue(before.SameAs(_ui._settings), "a setting was lost regenerating the grid");
            Assert.IsTrue(before.SameAs(_ui._solver.Settings), "the rebuilt solver did not get the settings");
        }

        [UnityTest]
        public IEnumerator TheSizeFieldsResizeTheBoard() {
            _shell.onWidth(21);
            yield return Settle();
            Assert.AreEqual(21, _grid.width, "the width field did not resize the board");

            _shell.onHeight(17);
            yield return Settle();
            Assert.AreEqual(17, _grid.height, "the height field did not resize the board");
            Assert.AreEqual(21 * 17, _ui._solver.Grid.CellCount, "the solver kept the old size");
            // the numbers agreeing is not the same as the texture having been reallocated, and the
            // far corner is off the end of the old pixel array if it was not
            Assert.DoesNotThrow(() => _grid.StateAt(20, 16), "the state texture was not resized with the board");
        }

        [UnityTest]
        public IEnumerator TheSeedFieldRedrawsTheSameGridForTheSameNumber() {
            _shell.onSeed(101);
            yield return Settle();
            bool[] first = Layout(_ui._solver);

            _shell.onSeed(202);
            yield return Settle();
            Assert.IsFalse(Same(first, Layout(_ui._solver)), "a different seed drew the same grid");

            _shell.onSeed(101);
            yield return Settle();
            Assert.IsTrue(Same(first, Layout(_ui._solver)), "the same seed drew a different grid");
        }

        [UnityTest]
        public IEnumerator TheGeneratorPickerChangesTheLayout() {
            var seen = new List<bool[]>();
            foreach (Generator generator in Enum.GetValues(typeof(Generator))) {
                _shell.onGenerator(generator);
                yield return Settle();
                Assert.AreEqual(generator, _ui._generator, $"picking {generator} did not reach the solver");

                bool[] layout = Layout(_ui._solver);
                foreach (bool[] other in seen)
                    Assert.IsFalse(Same(layout, other), $"{generator} drew the same grid as another generator");
                seen.Add(layout);
            }
        }

        [UnityTest]
        public IEnumerator TheDensitySliderChangesHowManyWalls() {
            _shell.onGenerator(Generator.Random);
            _shell.onDensity(0.05f);
            yield return Settle();
            int sparse = Walls(_ui._solver);

            _shell.onDensity(0.45f);
            yield return Settle();
            int dense = Walls(_ui._solver);

            Assert.Greater(dense, sparse, "raising the density did not add walls");
        }

        private static int Walls(PathSolver solver) {
            int count = 0;
            for (int i = 0; i < solver.Grid.CellCount; i++)
                if (!solver.Grid.IsWalkable(i))
                    count++;
            return count;
        }

        [UnityTest]
        public IEnumerator TheBrushPickerArmsTheBrushAndRecolorsThePreview() {
            _shell.onBrush(0);
            yield return Settle();
            Assert.AreEqual(BrushKind.Wall, _ui._brush, "the wall brush did not arm");
            Color wall = _grid.Material.GetColor(BrushColor);

            _shell.onBrush(3); // the tiers run cheapest first, so 3 is Water
            yield return Settle();
            Assert.AreEqual(BrushKind.Terrain, _ui._brush, "the terrain brush did not arm");
            Assert.AreEqual(Terrain.Water, _ui._tier, "the picker armed the wrong tier");
            Assert.AreNotEqual(wall, _grid.Material.GetColor(BrushColor),
                "the hover preview still paints the wall colour, so it would lie about the brush");
        }

        [UnityTest]
        public IEnumerator ThePlayButtonStartsAndStopsTheSearch() {
            yield return Run();
            Assert.IsNotNull(_ui._playback.State, "play did not start a search");
            Assert.Greater(_ui._playback.Fraction, 0f, "play did not advance the timeline");

            _shell.onPlayPause();
            yield return Settle();
            float parked = _ui._playback.Fraction;
            yield return Settle(10);
            Assert.AreEqual(parked, _ui._playback.Fraction, 1e-4f, "pause did not stop the timeline");
        }

        [UnityTest]
        public IEnumerator TheScrubberMovesThePlayhead() {
            yield return Run();

            _shell.onScrub(0.25f);
            yield return Settle();
            Assert.AreEqual(0.25f, _ui._playback.Fraction, 0.05f, "scrubbing did not move the playhead");

            _shell.onScrub(0.9f);
            yield return Settle();
            Assert.AreEqual(0.9f, _ui._playback.Fraction, 0.05f, "scrubbing back did not move the playhead");
        }

        // how much of the board the search has painted, which is nothing until one has run
        private int SearchedCells() {
            int count = 0;
            for (int x = 0; x < _grid.width; x++)
                for (int y = 0; y < _grid.height; y++)
                    switch (_grid.StateAt(x, y)) {
                        case GridNodeType.Seen:
                        case GridNodeType.Expanded:
                        case GridNodeType.Path:
                            count++;
                            break;
                    }
            return count;
        }

        /// <summary>
        /// Scrubbing as the very first thing done to a fresh grid. There is no timeline to seek into
        /// yet, and a control that moves under the cursor while the board ignores it reads as broken.
        /// </summary>
        [UnityTest]
        public IEnumerator TheScrubberWorksBeforeTheFirstSearch() {
            yield return Settle();
            Assert.IsNull(_ui._playback.State, "something already searched, so this proves nothing");
            Assert.AreEqual(0, SearchedCells(), "the board is already painted, so this proves nothing");

            _shell.onScrub(0.5f);
            yield return Settle();

            Assert.IsNotNull(_ui._playback.State, "scrubbing a fresh grid did not produce a timeline");
            Assert.AreEqual(0.5f, _ui._playback.Fraction, 0.05f, "scrubbing did not move the playhead");
            Assert.Greater(SearchedCells(), 0, "the scrub moved the playhead but not the board");

            // parked, not playing: a drag on the transport is not a press of the play button
            Assert.AreEqual(AnimationState.Paused, _ui.State, "scrubbing left the grid somewhere else");
            float parked = _ui._playback.Fraction;
            yield return Settle(10);
            Assert.AreEqual(parked, _ui._playback.Fraction, 1e-4f, "scrubbing started the search playing");
        }

        [UnityTest]
        public IEnumerator ThePaceControlsReachThePlayback() {
            yield return Run(5);

            _shell.onSpeed(0.2f);
            yield return Settle();
            float slow = _ui._playback.Speed;

            _shell.onSpeed(0.9f);
            yield return Settle();
            Assert.Greater(_ui._playback.Speed, slow, "the speed slider did not change the pace");

            _shell.onStepDelay(120f);
            yield return Settle();
            Assert.AreEqual(120f, _ui._playback.StepDelay, 1e-4f, "the step delay did not reach the playback");

            _shell.onLoop(true);
            yield return Settle();
            Assert.IsTrue(_ui._loop, "the loop switch did nothing");
        }

        /// <summary>
        /// The two Display toggles nothing else can see: both land as shader uniforms rather than in
        /// the state texture, so the material is where they have to be read back from.
        /// </summary>
        [UnityTest]
        public IEnumerator TheTileGapAndAnimationTogglesReachTheShader() {
            _shell.onTileGap(true);
            yield return Settle();
            float lines = _grid.Material.GetFloat(Gap);
            Assert.Greater(lines, 0f, "the gap should be open between the tiles");

            _shell.onTileGap(false);
            yield return Settle();
            Assert.AreEqual(0f, _grid.Material.GetFloat(Gap), 1e-5f, "the gap off should close the tiles up");

            yield return Run(5);
            _shell.onAnimate(true);
            yield return Settle();
            Assert.Greater(_grid.Material.GetFloat(TransitionTime), 0f, "animation on should ease the cells");

            _shell.onAnimate(false);
            yield return Settle();
            Assert.AreEqual(0f, _grid.Material.GetFloat(TransitionTime), 1e-5f,
                "animation off should snap the cells instead of easing them");
        }

        /// <summary>
        /// The two overlays a Display checkbox hides directly, with no callback out of the shell.
        /// Reached by label, which is the only handle a closure leaves to take hold of.
        /// </summary>
        [UnityTest]
        [TestCase("Show legend", "Legend", ExpectedResult = null)]
        [TestCase("Show statistics", "Stats", ExpectedResult = null)]
        public IEnumerator ADisplayToggleHidesItsOverlay(string label, string overlay) {
            yield return Settle();
            Transform target = Named(overlay);
            Assert.IsNotNull(target, $"nothing under the shell is named {overlay}");
            Assert.IsTrue(target.gameObject.activeSelf, $"{overlay} should start shown");

            Toggle toggle = Checkbox(label);
            Assert.IsNotNull(toggle, $"no Display checkbox reads {label}");

            toggle.isOn = false;
            yield return Settle();
            Assert.IsFalse(target.gameObject.activeSelf, $"unchecking {label} left {overlay} on screen");

            toggle.isOn = true;
            yield return Settle();
            Assert.IsTrue(target.gameObject.activeSelf, $"rechecking {label} did not bring {overlay} back");
        }

        private Transform Named(string name) {
            foreach (Transform t in _shell.GetComponentsInChildren<Transform>(true))
                if (t.name == name)
                    return t;
            return null;
        }

        private Toggle Checkbox(string label) {
            foreach (Toggle toggle in _shell.GetComponentsInChildren<Toggle>(true))
                foreach (TextMeshProUGUI text in toggle.GetComponentsInChildren<TextMeshProUGUI>(true))
                    if (text.text == label)
                        return toggle;
            return null;
        }

        [UnityTest]
        public IEnumerator TheHeatmapToggleTurnsItOnAndFillsIt() {
            _shell.onAlgorithm(Algorithm.Dijkstra); // reaches everywhere, so the field has readings
            yield return Run();

            _shell.onShowDistances(true);
            yield return Settle();
            Assert.AreEqual(1f, _grid.Material.GetFloat(ShowDistances), 1e-5f, "the heatmap did not switch on");

            int painted = 0;
            for (int x = 0; x < _grid.width; x++)
                for (int y = 0; y < _grid.height; y++)
                    if (_grid.DistanceAt(x, y) > 0)
                        painted++;
            Assert.Greater(painted, 0, "the heatmap switched on but nothing was written into it");

            _shell.onShowDistances(false);
            yield return Settle();
            Assert.AreEqual(0f, _grid.Material.GetFloat(ShowDistances), 1e-5f, "the heatmap did not switch off");
        }

        /// <summary>
        /// Switched on before anything has run, which is the order it actually gets used in. There is
        /// no solve to push a field from at that point, so it has to be filled by the next one.
        /// </summary>
        [UnityTest]
        public IEnumerator TheHeatmapFillsInWhenItWasOnBeforeTheSearchRan() {
            _shell.onAlgorithm(Algorithm.Dijkstra);
            _shell.onShowDistances(true);
            yield return Settle();

            yield return Run();

            int painted = 0;
            for (int x = 0; x < _grid.width; x++)
                for (int y = 0; y < _grid.height; y++)
                    if (_grid.DistanceAt(x, y) > 0)
                        painted++;
            Assert.Greater(painted, 0, "the heatmap was on before the search and never got filled");
        }

        [UnityTest]
        public IEnumerator TheThemePickerRepaintsTheGrid() {
            _shell.onTheme(0);
            yield return Settle();
            Color dark = _grid.Material.GetColor(EmptyColor);

            _shell.onTheme(2);
            yield return Settle();
            Assert.AreNotEqual(dark, _grid.Material.GetColor(EmptyColor), "the theme picker did not repaint the grid");

            _shell.onTheme(0);
            yield return Settle();
            Assert.AreEqual(dark, _grid.Material.GetColor(EmptyColor), "the theme picker would not go back");
        }
    }
}
