using Algorithms;
using NUnit.Framework;
using UnityEngine;

namespace Paths.Tests {
    public class AStarTests {
        // on a wide-open grid the shortest path is the Manhattan distance. the reconstructed path
        // drops the start node, so its count lands exactly on that distance.
        [Test]
        public void OpenGridPathIsManhattanLength() {
            var grid = new NodeGrid(5, 5);
            var astar = new AStar(grid, Heuristic.Manhattan);

            var path = astar.FindPath(new Vector2Int(0, 0), new Vector2Int(4, 4));

            Assert.IsNotNull(path);
            Assert.AreEqual(8, path.Count); // (4-0) + (4-0)
        }

        [Test]
        public void WalledOffTargetHasNoPath() {
            var grid = new NodeGrid(5, 5);
            // box the end node (4,4) in behind its only two neighbours
            grid.Grid[3, 4].Walkable = false;
            grid.Grid[4, 3].Walkable = false;

            var astar = new AStar(grid, Heuristic.Manhattan);
            var path = astar.FindPath(new Vector2Int(0, 0), new Vector2Int(4, 4));

            Assert.IsNull(path);
        }
    }
}
