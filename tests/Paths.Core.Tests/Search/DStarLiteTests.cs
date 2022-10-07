using System;
using System.Collections.Generic;
using NUnit.Framework;
using Paths.Core.Search;
using static Paths.Tests.Search.SearchCorpus;

namespace Paths.Tests.Search {
    /// <summary>
    /// The whole claim of an incremental search is that repairing a plan gives the same answer as
    /// planning again. So that is what these check, over long runs of edits: cost against the same
    /// reference every other search is held to, after every change.
    /// </summary>
    public class DStarLiteTests {
        [Test]
        public void OpenGridMatchesTheReference() {
            var grid = new Grid(10, 10);
            int start = grid.Index(new Coord(0, 0));
            int goal = grid.Index(new Coord(9, 9));

            SearchResult result = new DStarLite().Start(grid, start, goal, new SearchOptions());

            Assert.IsTrue(result.Found);
            Assert.AreEqual(ReferenceCost(grid, start, goal, false), result.Cost, Eps);
        }

        [Test]
        public void StartEqualsGoalIsAPathOfOne() {
            var grid = new Grid(5, 5);
            int cell = grid.Index(new Coord(2, 2));

            SearchResult result = new DStarLite().Start(grid, cell, cell, new SearchOptions());

            Assert.IsTrue(result.Found);
            CollectionAssert.AreEqual(new[] { cell }, result.Path);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FirstPlanMatchesTheReference(bool diagonal) {
            foreach (int size in new[] { 10, 16 })
                for (int seed = 0; seed < 40; seed++) {
                    Grid grid = Seeded(seed, size, size, 0.24);
                    int start = grid.Index(new Coord(0, 0));
                    int goal = grid.Index(new Coord(size - 1, size - 1));
                    float reference = ReferenceCost(grid, start, goal, diagonal);

                    SearchResult result = new DStarLite()
                        .Start(grid, start, goal, new SearchOptions(diagonal, Heuristic.Octile));

                    string where = $"diagonal {diagonal}, size {size}, seed {seed}{Render(grid, start, goal)}";
                    Assert.AreEqual(reference >= 0, result.Found, $"reachability mismatch on {where}");
                    if (reference >= 0)
                        Assert.AreEqual(reference, result.Cost, Eps, $"cost mismatch on {where}");
                    AssertPathIsWalkable(grid, result, start, goal, diagonal, where);
                }
        }

        /// <summary>
        /// Walls toggled one at a time, replanning after each. This is the case the whole design is
        /// for, and the one where a missed vertex update shows up as a stale cost several edits later.
        /// </summary>
        [TestCase(false)]
        [TestCase(true)]
        public void ReplanAfterEachWallEditMatchesTheReference(bool diagonal) {
            for (int seed = 0; seed < 30; seed++) {
                Grid grid = Seeded(seed, 12, 12, 0.18);
                int start = grid.Index(new Coord(0, 0));
                int goal = grid.Index(new Coord(11, 11));
                var options = new SearchOptions(diagonal, Heuristic.Octile);

                var search = new DStarLite();
                search.Start(grid, start, goal, options);

                var rng = new Random(seed + 31);
                for (int edit = 0; edit < 25; edit++) {
                    int cell = rng.Next(grid.CellCount);
                    if (cell == start || cell == goal)
                        continue;

                    grid.SetWalkable(cell, !grid.IsWalkable(cell));
                    search.Touch(new[] { cell });
                    SearchResult result = search.Replan();

                    float reference = ReferenceCost(grid, start, goal, diagonal);
                    string where = $"diagonal {diagonal}, seed {seed}, edit {edit}{Render(grid, start, goal)}";
                    Assert.AreEqual(reference >= 0, result.Found, $"reachability mismatch on {where}");
                    if (reference >= 0)
                        Assert.AreEqual(reference, result.Cost, Eps, $"cost mismatch on {where}");
                    AssertPathIsWalkable(grid, result, start, goal, diagonal, where);
                }
            }
        }

        /// <summary>Terrain repainted under the plan, which moves costs without moving walls.</summary>
        [Test]
        public void ReplanAfterTerrainEditsMatchesTheReference() {
            float[] tiers = { 0.5f, 1f, 2f, 4f };

            for (int seed = 0; seed < 25; seed++) {
                Grid grid = Seeded(seed, 12, 12, 0.16);
                int start = grid.Index(new Coord(0, 0));
                int goal = grid.Index(new Coord(11, 11));
                var options = new SearchOptions(true, Heuristic.Octile);

                var search = new DStarLite();
                search.Start(grid, start, goal, options);

                var rng = new Random(seed + 91);
                for (int edit = 0; edit < 20; edit++) {
                    int cell = rng.Next(grid.CellCount);
                    if (!grid.IsWalkable(cell))
                        continue;

                    grid.SetWeight(cell, tiers[rng.Next(tiers.Length)]);
                    search.Touch(new[] { cell });
                    SearchResult result = search.Replan();

                    float reference = ReferenceCost(grid, start, goal, true);
                    string where = $"seed {seed}, edit {edit}{Render(grid, start, goal)}";
                    Assert.AreEqual(reference >= 0, result.Found, $"reachability mismatch on {where}");
                    if (reference >= 0)
                        Assert.AreEqual(reference, result.Cost, Eps, $"cost mismatch on {where}");
                }
            }
        }

        /// <summary>A batch of edits landing together, the way a mouse drag delivers them.</summary>
        [Test]
        public void ReplanHandlesABatchOfEdits() {
            for (int seed = 0; seed < 25; seed++) {
                Grid grid = Seeded(seed, 14, 14, 0.15);
                int start = grid.Index(new Coord(0, 0));
                int goal = grid.Index(new Coord(13, 13));
                var options = new SearchOptions(true, Heuristic.Octile);

                var search = new DStarLite();
                search.Start(grid, start, goal, options);

                var rng = new Random(seed + 404);
                for (int batch = 0; batch < 8; batch++) {
                    var changed = new List<int>();
                    for (int i = 0; i < 6; i++) {
                        int cell = rng.Next(grid.CellCount);
                        if (cell == start || cell == goal)
                            continue;
                        grid.SetWalkable(cell, !grid.IsWalkable(cell));
                        changed.Add(cell);
                    }

                    search.Touch(changed);
                    SearchResult result = search.Replan();

                    float reference = ReferenceCost(grid, start, goal, true);
                    string where = $"seed {seed}, batch {batch}{Render(grid, start, goal)}";
                    Assert.AreEqual(reference >= 0, result.Found, $"reachability mismatch on {where}");
                    if (reference >= 0)
                        Assert.AreEqual(reference, result.Cost, Eps, $"cost mismatch on {where}");
                }
            }
        }

        /// <summary>
        /// The start moved, which shifts every queued key. km is meant to absorb that, so the plan
        /// has to stay correct without being thrown away.
        /// </summary>
        [Test]
        public void MovingTheStartKeepsThePlanCorrect() {
            for (int seed = 0; seed < 25; seed++) {
                Grid grid = Seeded(seed, 12, 12, 0.18);
                int goal = grid.Index(new Coord(11, 11));
                var options = new SearchOptions(true, Heuristic.Octile);

                var search = new DStarLite();
                int start = grid.Index(new Coord(0, 0));
                search.Start(grid, start, goal, options);

                var rng = new Random(seed + 77);
                for (int move = 0; move < 15; move++) {
                    int next = rng.Next(grid.CellCount);
                    if (!grid.IsWalkable(next) || next == goal)
                        continue;

                    search.MoveStart(next);
                    SearchResult result = search.Replan();

                    float reference = ReferenceCost(grid, next, goal, true);
                    string where = $"seed {seed}, move {move}, start {grid.At(next)}{Render(grid, next, goal)}";
                    Assert.AreEqual(reference >= 0, result.Found, $"reachability mismatch on {where}");
                    if (reference >= 0)
                        Assert.AreEqual(reference, result.Cost, Eps, $"cost mismatch on {where}");
                    AssertPathIsWalkable(grid, result, next, goal, true, where);
                }
            }
        }

        [Test]
        public void WallingOffTheGoalLosesThePathAndOpeningItBack() {
            var grid = new Grid(6, 6);
            int start = grid.Index(new Coord(0, 0));
            int goal = grid.Index(new Coord(5, 5));
            var options = new SearchOptions();

            var search = new DStarLite();
            Assert.IsTrue(search.Start(grid, start, goal, options).Found);

            var wall = new[] { grid.Index(new Coord(4, 5)), grid.Index(new Coord(5, 4)) };
            foreach (int cell in wall)
                grid.SetWalkable(cell, false);
            search.Touch(wall);
            Assert.IsFalse(search.Replan().Found, "the goal is sealed off");

            grid.SetWalkable(wall[0], true);
            search.Touch(new[] { wall[0] });
            SearchResult reopened = search.Replan();
            Assert.IsTrue(reopened.Found, "the route should come back when the wall does");
            Assert.AreEqual(ReferenceCost(grid, start, goal, false), reopened.Cost, Eps);
        }
    }
}
