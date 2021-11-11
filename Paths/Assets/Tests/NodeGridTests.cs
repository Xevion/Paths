using Algorithms;
using NUnit.Framework;
using UnityEngine;

namespace Paths.Tests {
    public class NodeGridTests {
        // the (int,int) ctor lays the backing array out as [width, height], so a grid built straight
        // from a Node[,] should read its size off the same axes. it doesn't - hence this test.
        [Test]
        public void NodeArrayCtorReadsWidthThenHeight() {
            var nodes = new Node[3, 5]; // 3 wide, 5 tall
            for (int x = 0; x < 3; x++)
                for (int y = 0; y < 5; y++)
                    nodes[x, y] = new Node(new Vector2Int(x, y), true);

            var grid = new NodeGrid(nodes);

            Assert.AreEqual(3, grid.Width);
            Assert.AreEqual(5, grid.Height);
        }

        [Test]
        public void CellCountIsWidthTimesHeight() {
            var grid = new NodeGrid(6, 4);
            Assert.AreEqual(24, grid.CellCount);
        }

        [Test]
        public void IsValidRejectsOutOfBounds() {
            var grid = new NodeGrid(4, 4);
            Assert.IsTrue(grid.IsValid(new Vector2Int(0, 0)));
            Assert.IsTrue(grid.IsValid(new Vector2Int(3, 3)));
            Assert.IsFalse(grid.IsValid(new Vector2Int(4, 0)));
            Assert.IsFalse(grid.IsValid(new Vector2Int(-1, 2)));
        }
    }
}
