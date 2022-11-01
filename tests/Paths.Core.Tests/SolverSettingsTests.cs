using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Paths.Core;
using Paths.Core.Search;

namespace Paths.Tests {
    /// <summary>
    /// A standing incremental plan is built under one set of settings and cannot be repaired into an
    /// answer for different ones. Both tests here walk the fields by reflection rather than naming
    /// them, so a setting added later is covered without anyone remembering to come back.
    /// </summary>
    public class SolverSettingsTests {
        private static FieldInfo[] Fields =>
            typeof(SolverSettings).GetFields(BindingFlags.Public | BindingFlags.Instance);

        public static IEnumerable<TestCaseData> EachSetting() {
            foreach (FieldInfo field in Fields)
                yield return new TestCaseData(field.Name).SetName($"{{m}}({field.Name})");
        }

        // several layouts per setting: whether a stale plan can be told from a fresh one depends on
        // the grid having somewhere else worth going, and one grid is a thin thing to rest that on
        public static IEnumerable<TestCaseData> EachSettingAndGrid() {
            foreach (FieldInfo field in Fields)
                for (int seed = 1; seed <= 8; seed++)
                    yield return new TestCaseData(field.Name, seed).SetName($"{{m}}({field.Name},seed {seed})");
        }

        /// <summary>A value of the right type that is definitely not the one handed in.</summary>
        private static object Other(object value) {
            switch (value) {
                case bool flag:
                    return !flag;
                case float number:
                    return number + 1f;
                case Enum choice:
                    foreach (object candidate in Enum.GetValues(choice.GetType()))
                        if (!candidate.Equals(choice))
                            return candidate;
                    throw new InvalidOperationException($"{choice.GetType().Name} has only one value");
                default:
                    throw new InvalidOperationException($"no other value for a {value.GetType().Name}");
            }
        }

        private static void Change(SolverSettings settings, string name) {
            FieldInfo field = typeof(SolverSettings).GetField(name);
            field.SetValue(settings, Other(field.GetValue(settings)));
        }

        [Test]
        public void ThereAreSettingsToCheck() {
            // the reflection both tests run on would quietly cover nothing if these became properties
            Assert.IsNotEmpty(Fields, "SolverSettings exposes no public fields to walk");
        }

        [TestCaseSource(nameof(EachSetting))]
        public void ChangingASettingMakesItStopMatching(string name) {
            var settings = new SolverSettings();
            SolverSettings changed = settings.Clone();
            Change(changed, name);

            Assert.IsFalse(settings.SameAs(changed), $"{name} changed but SameAs still calls them equal");
        }

        /// <summary>
        /// The behaviour behind SameAs: a plan standing from before the change has to give the same
        /// answer as one built from scratch afterwards. Not every setting can pull the two apart -
        /// changing the algorithm drops the repair rather than repairing differently, for one - but
        /// the invariant holds for all of them, and the ones that can diverge are what makes this bite.
        /// </summary>
        [TestCaseSource(nameof(EachSettingAndGrid))]
        public void APlanIsRebuiltWhenASettingChanges(string name, int seed) {
            PathSolver repaired = Solvers.Cornered(seed: seed);
            repaired.Settings.Algorithm = Algorithm.Dijkstra; // the search the repair stands in for
            repaired.Settings.Incremental = true;
            Solvers.AssertArmed(repaired, Control.Incremental);
            // a plan stands from here on, built under the settings as they read now
            repaired.Solve();

            PathSolver fresh = Solvers.Cornered(seed: seed);
            fresh.Settings.Algorithm = Algorithm.Dijkstra;
            fresh.Settings.Incremental = true;

            Change(repaired.Settings, name);
            Change(fresh.Settings, name);

            repaired.Solve();
            fresh.Solve();

            Solvers.AssertSameRoute(repaired, fresh, $"after changing {name} on seed {seed}");
        }
    }
}
