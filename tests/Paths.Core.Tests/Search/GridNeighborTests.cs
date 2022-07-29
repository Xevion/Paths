using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Paths.Core.Search;

namespace Paths.Tests.Search {
    public class GridNeighborTests {
        private static List<Coord> Neighbors(Grid grid, Coord at, bool diagonal) {
            var buffer = new int[Grid.MaxNeighbors];
            int count = grid.Neighbors(grid.Index(at), diagonal, buffer);
            return buffer.Take(count).Select(grid.At).ToList();
        }

        [Test]
        public void InteriorCellHasFourOrthogonalNeighbors() {
            var grid = new Grid(5, 5);
            List<Coord> found = Neighbors(grid, new Coord(2, 2), false);

            Assert.AreEqual(4, found.Count);
            CollectionAssert.AreEquivalent(
                new[] { new Coord(1, 2), new Coord(3, 2), new Coord(2, 1), new Coord(2, 3) }, found);
        }

        [Test]
        public void InteriorCellHasEightWhenDiagonal() {
            var grid = new Grid(5, 5);
            Assert.AreEqual(8, Neighbors(grid, new Coord(2, 2), true).Count);
        }

        [Test]
        public void CornerIsClampedToTheGrid() {
            var grid = new Grid(5, 5);
            Assert.AreEqual(2, Neighbors(grid, new Coord(0, 0), false).Count);
            Assert.AreEqual(3, Neighbors(grid, new Coord(0, 0), true).Count);
            Assert.AreEqual(2, Neighbors(grid, new Coord(4, 4), false).Count);
        }

        [Test]
        public void WallsAreNotNeighbors() {
            var grid = new Grid(5, 5);
            grid.SetWalkable(grid.Index(new Coord(1, 2)), false);

            List<Coord> found = Neighbors(grid, new Coord(2, 2), false);

            Assert.AreEqual(3, found.Count);
            CollectionAssert.DoesNotContain(found, new Coord(1, 2));
        }

        // squeezing diagonally between two walls would let a path slip through a sealed corner,
        // so a diagonal is only offered when both orthogonals beside it are open
        [Test]
        public void DiagonalNeedsBothOrthogonalsOpen() {
            var grid = new Grid(5, 5);
            grid.SetWalkable(grid.Index(new Coord(3, 2)), false);

            CollectionAssert.DoesNotContain(Neighbors(grid, new Coord(2, 2), true), new Coord(3, 3));
            CollectionAssert.DoesNotContain(Neighbors(grid, new Coord(2, 2), true), new Coord(3, 1));
            CollectionAssert.Contains(Neighbors(grid, new Coord(2, 2), true), new Coord(1, 1));
        }

        [Test]
        public void SealedCellHasNoNeighbors() {
            var grid = new Grid(5, 5);
            foreach (Coord wall in new[] { new Coord(1, 2), new Coord(3, 2), new Coord(2, 1), new Coord(2, 3) })
                grid.SetWalkable(grid.Index(wall), false);

            Assert.AreEqual(0, Neighbors(grid, new Coord(2, 2), false).Count);
            Assert.AreEqual(0, Neighbors(grid, new Coord(2, 2), true).Count,
                "every diagonal is corner-cutting once all four orthogonals are walls");
        }

        // non-square grids are where a row-major/column-major mixup shows up
        [Test]
        public void NeighborsAreCorrectOnANonSquareGrid() {
            var grid = new Grid(3, 7);
            List<Coord> found = Neighbors(grid, new Coord(1, 3), false);

            CollectionAssert.AreEquivalent(
                new[] { new Coord(0, 3), new Coord(2, 3), new Coord(1, 2), new Coord(1, 4) }, found);
        }
    }
}
