using System.Collections.Generic;
using NUnit.Framework;
using Paths.Core.Generation;
using Paths.Core.Search;

namespace Paths.Tests.Generation {
    public class LevelGeneratorTests {
        private static Grid Solid(int width, int height) {
            var grid = new Grid(width, height);
            for (int i = 0; i < grid.CellCount; i++)
                grid.SetWalkable(i, false);
            return grid;
        }

        private static int CountWalkable(Grid grid) {
            int open = 0;
            for (int i = 0; i < grid.CellCount; i++)
                if (grid.IsWalkable(i))
                    open++;
            return open;
        }

        /// <summary>Flood fill from the first open cell, so islands show up as a shortfall.</summary>
        private static bool AllOpenSpaceIsConnected(Grid grid) {
            int first = -1;
            for (int i = 0; i < grid.CellCount && first < 0; i++)
                if (grid.IsWalkable(i))
                    first = i;
            if (first < 0)
                return false;

            var seen = new HashSet<int> { first };
            var queue = new Queue<int>();
            queue.Enqueue(first);
            var buffer = new int[Grid.MaxNeighbors];

            while (queue.Count > 0) {
                int count = grid.Neighbors(queue.Dequeue(), false, buffer);
                for (int i = 0; i < count; i++)
                    if (seen.Add(buffer[i]))
                        queue.Enqueue(buffer[i]);
            }

            return seen.Count == CountWalkable(grid);
        }

        [Test]
        public void RandomPlacementFillsToTheTargetShare() {
            for (int seed = 0; seed < 20; seed++) {
                var grid = new Grid(20, 20);
                new RandomPlacement(0.25f, true, true).Generate(grid, new System.Random(seed));

                Assert.AreEqual(400 - 100, CountWalkable(grid), $"seed {seed}");
            }
        }

        // asking for more walls than there are open cells used to spin looking for candidates
        [Test]
        public void RandomPlacementStopsWhenTheGridIsFull() {
            var grid = new Grid(8, 8);
            new RandomPlacement(2f, false, true).Generate(grid, new System.Random(1));

            Assert.AreEqual(0, CountWalkable(grid));
        }

        [Test]
        public void RandomPlacementIsSeedDeterministic() {
            var a = new Grid(16, 16);
            var b = new Grid(16, 16);
            new RandomPlacement(0.3f, true, true).Generate(a, new System.Random(99));
            new RandomPlacement(0.3f, true, true).Generate(b, new System.Random(99));

            for (int i = 0; i < a.CellCount; i++)
                Assert.AreEqual(a.IsWalkable(i), b.IsWalkable(i), $"cell {i}");
        }

        [Test]
        public void RoomCarveOpensSpaceOutOfASolidGrid() {
            for (int seed = 0; seed < 20; seed++) {
                Grid grid = Solid(30, 30);
                new RoomCarve(4, 8, 3, 8).Generate(grid, new System.Random(seed));

                Assert.Greater(CountWalkable(grid), 0, $"seed {seed} carved nothing");
                Assert.Less(CountWalkable(grid), grid.CellCount, $"seed {seed} carved everything");
            }
        }

        // rooms are carved not to overlap, so without corridors they are sealed islands and a path
        // between two of them can never exist
        [Test]
        public void RoomCarveConnectsEveryRoom() {
            for (int seed = 0; seed < 24; seed++) {
                Grid grid = Solid(30, 30);
                new RoomCarve(4, 8, 3, 8).Generate(grid, new System.Random(seed));

                Assert.IsTrue(AllOpenSpaceIsConnected(grid), $"seed {seed} left an unreachable room");
            }
        }

        [Test]
        public void DrunkardWalkCarvesOneConnectedCave() {
            for (int seed = 0; seed < 24; seed++) {
                Grid grid = Solid(24, 24);
                new DrunkardWalk(1, 400, false).Generate(grid, new System.Random(seed));

                Assert.Greater(CountWalkable(grid), 0, $"seed {seed} carved nothing");
                Assert.IsTrue(AllOpenSpaceIsConnected(grid), $"seed {seed} left a disconnected pocket");
            }
        }

        [Test]
        public void DrunkardWalkIsSeedDeterministic() {
            Grid a = Solid(20, 20);
            Grid b = Solid(20, 20);
            new DrunkardWalk(1, 300, false).Generate(a, new System.Random(7));
            new DrunkardWalk(1, 300, false).Generate(b, new System.Random(7));

            for (int i = 0; i < a.CellCount; i++)
                Assert.AreEqual(a.IsWalkable(i), b.IsWalkable(i), $"cell {i}");
        }

        [Test]
        public void DrunkardWalkStaysInsideTheGrid() {
            Grid grid = Solid(10, 10);
            Assert.DoesNotThrow(() => new DrunkardWalk(3, 2000, false).Generate(grid, new System.Random(3)));
        }
    }
}
