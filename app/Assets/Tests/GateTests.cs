using System;
using System.Collections;
using System.Collections.Generic;
using Game.UI;
using NUnit.Framework;
using Paths.Core;
using Paths.Core.Search;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace Game.Tests {
    /// <summary>
    /// The sidebar's account of which search settings are live, held to the table Core answers from.
    /// Whether the table is right about the solver is settled in Core; what is checked here is that the
    /// interface asks it at all, asks for the right control, and greys the right subtree.
    ///
    /// ShellTests covers the other half of this: that a control reaches its sink. Neither test alone
    /// notices a control wired correctly to a setting the search then ignores.
    /// </summary>
    public class GateTests {
        private UIController _ui;
        private AppShell _shell;

        [UnitySetUp]
        public IEnumerator SetUp() {
            PointerInput.Reset();
            yield return AppScene.Load();
            _ui = UnityEngine.Object.FindObjectOfType<UIController>();
            _shell = UnityEngine.Object.FindObjectOfType<AppShell>();
        }

        [UnityTearDown]
        public IEnumerator TearDown() {
            PointerInput.Reset();
            yield return AppScene.Unload();
        }

        private ControlGate[] Gates() => _shell.GetComponentsInChildren<ControlGate>(true);

        private ControlGate Gate(Control control) {
            foreach (ControlGate gate in Gates())
                if (gate.control == control)
                    return gate;
            throw new AssertionException(control + " has no gate");
        }

        private IEnumerator Settle(int frames) {
            for (int i = 0; i < frames; i++)
                yield return null;
        }

        private IEnumerator Configure(Algorithm algorithm, bool bidirectional, bool incremental) {
            _shell.onAlgorithm(algorithm);
            _shell.onBidirectional(bidirectional);
            _shell.onIncremental(incremental);
            yield return Settle(2);
        }

        // named so the fixture can assert the picker is reachable and never greyed
        private SegmentedControl Picker() =>
            _shell.transform.Find("Root/Body/Sidebar/Content/Algorithm")
                .GetComponentInChildren<SegmentedControl>(true);

        /// <summary>
        /// The algorithm picker is the choice the rest of the sidebar modifies, so it has no gate at
        /// all: there is nothing that may take it away. Everything a pick can rule out has exactly one,
        /// since two gates over one setting would fight and none would never grey.
        /// </summary>
        [Test]
        public void EverySettingExceptThePickerIsGatedExactlyOnce() {
            var seen = new List<Control>();
            foreach (ControlGate gate in Gates()) {
                Assert.AreNotEqual(Control.Algorithm, gate.control,
                    "the algorithm picker has a gate, so something can grey out the primary choice");
                Assert.IsFalse(seen.Contains(gate.control), gate.control + " is gated twice");
                seen.Add(gate.control);
            }
            foreach (Control control in Enum.GetValues(typeof(Control))) {
                if (control == Control.Algorithm)
                    continue;
                Assert.Contains(control, seen, control + " has no gate, so it can never be greyed");
            }
        }

        /// <summary>
        /// The one rule the rest of this hangs off. Whatever the switches are set to, the picker keeps
        /// taking input and keeps its own hover text, because picking a search is what this whole
        /// screen is for. A modifier the pick has no version of gives way instead, which is what the
        /// sweep below covers.
        /// </summary>
        [UnityTest]
        public IEnumerator ThePickerIsNeverGreyedWhateverElseIsSet() {
            SegmentedControl picker = Picker();
            string described = picker.GetComponentInChildren<Tooltip>(true).text;
            int checked_ = 0;

            foreach (Algorithm algorithm in Enum.GetValues(typeof(Algorithm)))
            foreach (bool bidirectional in new[] { false, true })
            foreach (bool incremental in new[] { false, true }) {
                yield return Configure(algorithm, bidirectional, incremental);
                string where = $"{algorithm}, bidirectional={bidirectional}, incremental={incremental}";

                Assert.IsTrue(Capabilities.Reads(_ui._settings, Control.Algorithm),
                    $"{where}: the table took the picker out of play");
                Assert.IsTrue(picker.gameObject.activeInHierarchy, $"{where}: the picker was hidden");
                Assert.IsTrue(picker.IsInteractable(), $"{where}: the picker stopped taking input");
                Assert.IsTrue(KeyboardFocus.IsFocusable(picker), $"{where}: the picker left the Tab order");
                Assert.AreEqual(described, picker.GetComponentInChildren<Tooltip>(true).text,
                    $"{where}: the picker is explaining why it is greyed");
                checked_++;
            }
            Assert.AreEqual(24, checked_, "not every configuration was reached");
        }

        [UnityTest]
        public IEnumerator EveryConfigurationGreysWhatItsSearchIgnores() {
            int live = 0, greyed = 0;
            foreach (Algorithm algorithm in Enum.GetValues(typeof(Algorithm)))
            foreach (bool bidirectional in new[] { false, true })
            foreach (bool incremental in new[] { false, true }) {
                yield return Configure(algorithm, bidirectional, incremental);
                foreach (ControlGate gate in Gates()) {
                    bool reads = Capabilities.Reads(_ui._settings, gate.control);
                    Assert.AreEqual(reads, gate.Live,
                        $"{algorithm}, bidirectional={bidirectional}, incremental={incremental}: {gate.control} " +
                        (reads ? "is greyed but the search reads it" : "is offered but the search ignores it"));
                    if (reads) live++;
                    else greyed++;
                }
            }
            Assert.Greater(greyed, 0, "nothing was greyed anywhere, so the sweep proves nothing");
            Assert.Greater(live, 0, "everything was greyed everywhere, so the sweep proves nothing");
        }

        /// <summary>
        /// Greying has to reach what Unity reads, not just the gate's own flag, and has to say why. Jump
        /// Point is the case the old hardcoded visibility got backwards: it showed the heuristic block
        /// for a search that reads nothing from it.
        /// </summary>
        [UnityTest]
        public IEnumerator AGreyedControlTakesNoInputAndSaysWhatIsIgnoringIt() {
            yield return Configure(Algorithm.AStar, false, false);
            ControlGate heuristic = Gate(Control.Heuristic);
            Selectable picker = heuristic.GetComponentInChildren<Selectable>(true);
            Tooltip tip = heuristic.GetComponentInChildren<Tooltip>(true);
            Assert.IsTrue(picker.IsInteractable(), "A* reads the heuristic, so its picker should take input");
            string described = tip.text;

            yield return Configure(Algorithm.JumpPoint, false, false);
            Assert.IsTrue(heuristic.gameObject.activeInHierarchy,
                "the heuristic block is hidden for Jump Point rather than greyed, which says nothing about why");
            Assert.IsFalse(heuristic.Live, "Jump Point is octile-scored, so the heuristic picker does nothing");
            Assert.IsFalse(picker.IsInteractable(), "the greyed picker still takes input");
            Assert.IsFalse(KeyboardFocus.IsFocusable(picker), "the greyed picker is still a Tab stop");
            Assert.AreNotEqual(described, tip.text, "the greyed picker still offers to do what it cannot");

            yield return Configure(Algorithm.AStar, false, false);
            Assert.IsTrue(picker.IsInteractable(), "the picker would not come back");
            Assert.AreEqual(described, tip.text, "the picker came back still explaining why it had been greyed");
        }

        /// <summary>
        /// The split nobody predicted, and the reason Heuristic and Weight are gated apart: Greedy picks
        /// by the estimate, so the picker matters, but its priority is that estimate and nothing else, so
        /// scaling every cell by the same factor reorders none of them.
        /// </summary>
        [UnityTest]
        public IEnumerator GreedyOffersTheEstimateButNotTheWeightOnIt() {
            yield return Configure(Algorithm.Greedy, false, false);

            Assert.IsTrue(Gate(Control.Heuristic).Live, "Greedy picks by the estimate, so the picker is live");
            Assert.IsFalse(Gate(Control.Weight).Live, "scaling a greedy priority reorders nothing");
        }

        private static bool Same(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < 0.02f && Mathf.Abs(a.g - b.g) < 0.02f && Mathf.Abs(a.b - b.b) < 0.02f;

        private static bool ShowsAccent(Component root) {
            foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
                if (Same(graphic.color, Theme.Accent))
                    return true;
            return false;
        }

        /// <summary>
        /// A greyed switch has to read as unavailable rather than as a faint version of a live one, so
        /// it gives up the accent and draws as the empty groove the kit uses for a surface that cannot
        /// be acted on. The knob stays put: the setting is still worth reading while it is held back,
        /// and it comes back to the colour that matches it rather than to whatever it was built as.
        /// </summary>
        [UnityTest]
        public IEnumerator AGreyedSwitchGivesUpItsColourAndKeepsItsState() {
            yield return Configure(Algorithm.Dijkstra, false, false);
            ControlGate gate = Gate(Control.Bidirectional);
            UISwitch anim = gate.GetComponentInChildren<UISwitch>(true);
            gate.GetComponentInChildren<Toggle>(true).isOn = true;
            yield return Settle(30); // the track crossfades, so let it arrive

            Assert.IsTrue(Same(anim.track.color, Theme.Accent), "an on switch should carry the accent");
            Assert.AreEqual(0f, anim.track.innerShadow, 1e-4f, "a live switch should read as a raised pill");
            float knob = anim.knob.anchoredPosition.x;

            yield return Configure(Algorithm.DepthFirst, false, false);
            Assert.IsFalse(gate.Live, "there is no two-ended depth-first search");
            Assert.IsFalse(Same(anim.track.color, Theme.Accent), "the greyed switch still looks active");
            Assert.IsTrue(Same(anim.track.color, Theme.Track), "the greyed switch is not the inert colour");
            Assert.Greater(anim.track.innerShadow, 0f, "the greyed switch still draws as a raised pill");
            Assert.AreEqual(knob, anim.knob.anchoredPosition.x, 0.5f,
                "greying moved the knob, so what the switch is holding is no longer readable");

            yield return Configure(Algorithm.Dijkstra, false, false);
            yield return Settle(30);
            Assert.IsTrue(Same(anim.track.color, Theme.Accent),
                "the switch came back the colour of a setting it is not holding");
            Assert.AreEqual(0f, anim.track.innerShadow, 1e-4f, "the switch came back still recessed");
        }

        /// <summary>
        /// The same rule everywhere else in the section: nothing greyed keeps an accent, so a selection
        /// under a search that ignores it reads as a grey selection rather than a live one.
        /// </summary>
        [UnityTest]
        public IEnumerator NothingGreyedKeepsAnAccent() {
            yield return Configure(Algorithm.AStar, false, false);
            ControlGate heuristic = Gate(Control.Heuristic);
            Assert.IsTrue(ShowsAccent(heuristic), "A* reads the heuristic, so its picker shows a selection");

            yield return Configure(Algorithm.Dijkstra, false, false);
            Assert.IsFalse(heuristic.Live, "Dijkstra works without an estimate");
            Assert.IsFalse(ShowsAccent(heuristic), "the greyed picker still shows a live-looking selection");

            yield return Configure(Algorithm.AStar, false, false);
            Assert.IsTrue(ShowsAccent(heuristic), "the picker came back without its selection");
        }

        /// <summary>
        /// The switches are modifiers, so they follow the pick rather than replacing it. Dijkstra has a
        /// version of each; depth-first has neither, and offering them over it would be offering a
        /// different search under the name of the one that is selected.
        /// </summary>
        [UnityTest]
        public IEnumerator TheModifiersFollowThePickRatherThanReplacingIt() {
            yield return Configure(Algorithm.Dijkstra, false, false);
            Assert.IsTrue(Gate(Control.Bidirectional).Live, "Dijkstra runs from both ends");
            Assert.IsTrue(Gate(Control.Incremental).Live, "Dijkstra is what the repair stands in for");

            yield return Configure(Algorithm.DepthFirst, false, false);
            Assert.IsFalse(Gate(Control.Bidirectional).Live, "there is no two-ended depth-first search");
            Assert.IsFalse(Gate(Control.Incremental).Live, "there is no incremental depth-first search");

            // both on at once: the repair runs one wave, so it takes the two-ended switch with it
            yield return Configure(Algorithm.Dijkstra, true, true);
            Assert.IsTrue(Gate(Control.Incremental).Live, "the repair is what runs when both are on");
            Assert.IsFalse(Gate(Control.Bidirectional).Live, "a repaired plan has no second wave");
        }

        /// <summary>
        /// What the greying is for, end to end through the real solver: a modifier being on must not
        /// stop the picker choosing the search. Depth-first wanders where Dijkstra goes straight, so if
        /// the switch were still answering for the picker the route would not budge.
        /// </summary>
        [UnityTest]
        public IEnumerator ChangingTheAlgorithmStillChangesTheSearchWithAModifierOn() {
            yield return Configure(Algorithm.Dijkstra, true, false);
            _shell.onPlayPause();
            yield return Settle(40);

            int shortest = _ui._solver.Path.Count;
            Assert.Greater(shortest, 0, "nothing was solved, so there is no route to compare");

            yield return Configure(Algorithm.DepthFirst, true, false);
            Assert.Greater(_ui._solver.Path.Count, shortest,
                "the two-ended switch answered for the picker, so changing the algorithm did nothing");
        }
    }
}
