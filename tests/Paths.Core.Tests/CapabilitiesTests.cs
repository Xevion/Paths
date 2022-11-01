using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Paths.Core;
using Paths.Core.Generation;
using Paths.Core.Search;

namespace Paths.Tests {
    /// <summary>
    /// Holds the capability table to the solver, both ways round. A control the table calls live has
    /// to be able to change what comes out, and a control it calls ignored must never change it. Only
    /// checking one direction leaves the table free to grey out a control that still works, or to
    /// leave one enabled that does nothing, which is the bug this table exists to stop.
    /// </summary>
    [TestFixture]
    internal class CapabilitiesTests {
        private const int Seeds = 6;

        // enough of a run to tell two searches apart: how much was swept, and what it settled on
        private static string Signature(PathSolver solver) {
            ChangeController trace = solver.Solve();
            float cost = 0f;
            IReadOnlyList<int> path = solver.Path;
            for (int i = 1; i < path.Count; i++)
                cost += solver.Grid.StepCost(path[i - 1], path[i]);
            return $"{trace.Count}/{path.Count}/{cost:0.000}";
        }

        private static readonly Action<SolverSettings>[] Algorithms = {
            s => s.Algorithm = Algorithm.AStar,
            s => s.Algorithm = Algorithm.Dijkstra,
            s => s.Algorithm = Algorithm.Greedy,
            s => s.Algorithm = Algorithm.BreadthFirst,
            s => s.Algorithm = Algorithm.DepthFirst,
            s => s.Algorithm = Algorithm.JumpPoint,
        };

        private static Action<SolverSettings>[] Values(Control control) {
            switch (control) {
                case Control.Algorithm: return Algorithms;
                case Control.Heuristic:
                    return new Action<SolverSettings>[] {
                        s => s.Heuristic = Heuristic.Manhattan, s => s.Heuristic = Heuristic.Euclidean,
                        s => s.Heuristic = Heuristic.Chebyshev, s => s.Heuristic = Heuristic.Octile,
                    };
                case Control.Weight:
                    return new Action<SolverSettings>[] {
                        s => s.Weight = 1f, s => s.Weight = 1.8f, s => s.Weight = 3f,
                    };
                case Control.Diagonal:
                    return new Action<SolverSettings>[] { s => s.Diagonal = false, s => s.Diagonal = true };
                case Control.Bidirectional:
                    return new Action<SolverSettings>[] {
                        s => s.Bidirectional = false, s => s.Bidirectional = true,
                    };
                case Control.Incremental:
                    return new Action<SolverSettings>[] {
                        s => s.Incremental = false, s => s.Incremental = true,
                    };
                default:
                    // falling through to another control's values would sweep the wrong setting and
                    // assert the wrong thing about it, quietly and in both directions
                    throw new ArgumentOutOfRangeException(nameof(control), control, "no values to sweep");
            }
        }

        /// <summary>Every configuration the picker and the two switches can be left in.</summary>
        private static IEnumerable<TestCaseData> Configurations() {
            string[] names = { "A*", "Dijkstra", "Greedy", "BFS", "DFS", "JPS" };
            for (int i = 0; i < Algorithms.Length; i++)
                foreach (bool bidirectional in new[] { false, true })
                    foreach (bool incremental in new[] { false, true }) {
                        int pick = i;
                        Action<SolverSettings> setup = s => {
                            Algorithms[pick](s);
                            s.Bidirectional = bidirectional;
                            s.Incremental = incremental;
                        };
                        string label = names[i] + (bidirectional ? "+bidi" : "") + (incremental ? "+incr" : "");
                        yield return new TestCaseData(label, setup).SetName($"{{m}}({label})");
                    }
        }

        private static PathSolver Solver(int seed, Action<SolverSettings> setup) {
            var solver = new PathSolver(20, 20, seed, Generator.Random, 0.2f);
            solver.Start = Solvers.NearestOpen(solver, new Coord(0, 0));
            solver.End = Solvers.NearestOpen(solver, new Coord(19, 19));
            setup(solver.Settings);
            return solver;
        }

        // how many seeds see a different result as this control runs through its values
        private static int SeedsThatMove(Action<SolverSettings> setup, Control control) {
            Action<SolverSettings>[] values = Values(control);
            int moved = 0;
            for (int seed = 1; seed <= Seeds; seed++) {
                var seen = new HashSet<string>();
                foreach (Action<SolverSettings> value in values)
                    seen.Add(Signature(Solver(seed, s => { setup(s); value(s); })));
                if (seen.Count > 1)
                    moved++;
            }
            return moved;
        }

        [TestCaseSource(nameof(Configurations))]
        public void AnIgnoredControlChangesNothing(string label, Action<SolverSettings> setup) {
            var settings = new SolverSettings();
            setup(settings);

            foreach (Control control in Enum.GetValues(typeof(Control)).Cast<Control>()) {
                if (Capabilities.Reads(settings, control))
                    continue;
                Assert.AreEqual(0, SeedsThatMove(setup, control),
                    $"{label}: the table says {control} is ignored ({Capabilities.Ignoring(settings, control)}), " +
                    "but changing it changes the result");
            }
        }

        [TestCaseSource(nameof(Configurations))]
        public void A_LiveControlChangesSomething(string label, Action<SolverSettings> setup) {
            var settings = new SolverSettings();
            setup(settings);

            foreach (Control control in Enum.GetValues(typeof(Control)).Cast<Control>()) {
                if (!Capabilities.Reads(settings, control))
                    continue;
                Assert.Greater(SeedsThatMove(setup, control), 0,
                    $"{label}: the table says {control} is live, but changing it changes nothing " +
                    $"on any of {Seeds} grids");
            }
        }

        /// <summary>
        /// Both directions above are satisfied by a table that answers the same way every time, so
        /// this pins that it does not. Without it a table stuck on "everything is live" passes the
        /// ignored-control test by having nothing to check, and vice versa.
        /// </summary>
        [Test]
        public void TheTableBothAllowsAndIgnoresControls() {
            int live = 0, ignored = 0;
            foreach (TestCaseData configuration in Configurations()) {
                var settings = new SolverSettings();
                ((Action<SolverSettings>) configuration.Arguments[1])(settings);
                foreach (Control control in Enum.GetValues(typeof(Control)).Cast<Control>())
                    if (Capabilities.Reads(settings, control)) live++;
                    else ignored++;
            }

            Assert.Greater(live, 0, "the table ignores everything, so the live-control test checks nothing");
            Assert.Greater(ignored, 0, "the table allows everything, so the ignored-control test checks nothing");
        }

        /// <summary>
        /// The picker is the choice, not one of the things a choice can take away. Nothing may put it
        /// out of play, in any combination, so that is asserted over the whole table rather than left
        /// to the two sweeps above to notice one configuration at a time.
        /// </summary>
        [Test]
        public void ThePickerIsLiveInEveryConfiguration() {
            foreach (TestCaseData configuration in Configurations()) {
                var settings = new SolverSettings();
                ((Action<SolverSettings>) configuration.Arguments[1])(settings);
                Assert.IsTrue(Capabilities.Reads(settings, Control.Algorithm),
                    $"{configuration.Arguments[0]}: {Capabilities.Ignoring(settings, Control.Algorithm)} " +
                    "greyed out the algorithm picker");
            }
        }

        /// <summary>Every control has to be reachable, or the interface would grey one out forever.</summary>
        [Test]
        public void EveryControlIsLiveInSomeConfiguration() {
            foreach (Control control in Enum.GetValues(typeof(Control)).Cast<Control>()) {
                bool reachable = Configurations().Any(configuration => {
                    var settings = new SolverSettings();
                    ((Action<SolverSettings>) configuration.Arguments[1])(settings);
                    return Capabilities.Reads(settings, control);
                });
                Assert.IsTrue(reachable, $"{control} is ignored in every configuration");
            }
        }
    }
}
