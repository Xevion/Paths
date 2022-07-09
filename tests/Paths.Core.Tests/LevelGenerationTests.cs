using System.Collections.Generic;
using System.Linq;
using Algorithms;
using LevelGeneration;
using NUnit.Framework;
using UnityEngine;

namespace Paths.Tests {
    public class LevelGenerationTests {
        // both cave generators are subtractive - they carve out of a solid grid
        private static NodeGrid Solid(int width, int height, int seed) {
            var grid = new NodeGrid(width, height, new System.Random(seed));
            foreach (Node node in grid.Iterator())
                node.Walkable = false;
            return grid;
        }

        private static int Walkable(NodeGrid grid) => grid.Empty().Count();

        // flood fill from the first walkable cell - a generator whose open space isn't one connected
        // region drops start/end into separate pockets, where no path can ever exist
        private static bool FullyConnected(NodeGrid grid) {
            Node first = grid.Empty().FirstOrDefault();
            if (first == null) return false;

            var seen = new HashSet<Vector2Int> { first.Position };
            var queue = new Queue<Node>();
            queue.Enqueue(first);
            while (queue.Count > 0)
                // the cardinal neighbours come back null-padded at the grid edge
                foreach (Node next in grid.GetAdjacentNodesArray(queue.Dequeue()))
                    if (next != null && next.Walkable && seen.Add(next.Position))
                        queue.Enqueue(next);

            return seen.Count == Walkable(grid);
        }

        [Test]
        public void DrunkardWalkCarvesOpenSpace() {
            NodeGrid grid = Solid(40, 40, 7);
            grid.ApplyGenerator(new DrunkardWalk(6, 80, false));

            int open = Walkable(grid);
            Assert.Greater(open, 0, "the walk carved nothing");
            Assert.Less(open, grid.CellCount, "the walk cleared the whole grid");
        }

        [Test]
        public void DrunkardWalkWalksContiguously() {
            NodeGrid grid = Solid(40, 40, 11);
            grid.ApplyGenerator(new DrunkardWalk(1, 120, false));

            // a single walk only ever steps to an adjacent cell, so its trail is one connected region
            Assert.IsTrue(FullyConnected(grid));
        }

        [Test]
        public void DrunkardWalkIsSeedDeterministic() {
            NodeGrid a = Solid(30, 30, 99);
            NodeGrid b = Solid(30, 30, 99);
            a.ApplyGenerator(new DrunkardWalk(4, 60, false));
            b.ApplyGenerator(new DrunkardWalk(4, 60, false));

            CollectionAssert.AreEqual(
                a.Iterator().Select(n => n.Walkable).ToList(),
                b.Iterator().Select(n => n.Walkable).ToList());
        }

        [Test]
        public void RoomCarveOpensRooms() {
            NodeGrid grid = Solid(48, 48, 3);
            grid.ApplyGenerator(new RoomCarve(new Vector2Int(4, 8), new Vector2Int(4, 9), 60));

            int open = Walkable(grid);
            Assert.Greater(open, 0, "no rooms were carved");
            Assert.Less(open, grid.CellCount);
        }

        [Test]
        public void RoomCarveConnectsEveryRoom() {
            // rooms are carved not to overlap, so without corridors each one is its own island
            for (int seed = 0; seed < 12; seed++) {
                NodeGrid grid = Solid(48, 48, seed);
                grid.ApplyGenerator(new RoomCarve(new Vector2Int(4, 8), new Vector2Int(4, 9), 60));
                Assert.IsTrue(FullyConnected(grid), $"seed {seed} left an unreachable room");
            }
        }
    }
}
