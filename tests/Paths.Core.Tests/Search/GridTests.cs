using System;
using NUnit.Framework;
using Paths.Core.Search;

namespace Paths.Tests.Search {
    public class GridTests {
        // the old NodeGrid stored a Node[x, y] and read its size off whichever axis it happened to
        // grab, which is exactly the bug that made this worth pinning down. width is x, height is y.
        [Test]
        public void IndexIsRowMajorAndRoundTrips() {
            var grid = new Grid(3, 5);

            Assert.AreEqual(3, grid.Width);
            Assert.AreEqual(5, grid.Height);
            Assert.AreEqual(15, grid.CellCount);

            Assert.AreEqual(0, grid.Index(new Coord(0, 0)));
            Assert.AreEqual(2, grid.Index(new Coord(2, 0)));
            Assert.AreEqual(3, grid.Index(new Coord(0, 1)));
            Assert.AreEqual(14, grid.Index(new Coord(2, 4)));

            for (int i = 0; i < grid.CellCount; i++)
                Assert.AreEqual(i, grid.Index(grid.At(i)));
        }

        [Test]
        public void InBoundsRejectsEveryEdge() {
            var grid = new Grid(4, 3);

            Assert.IsTrue(grid.InBounds(new Coord(0, 0)));
            Assert.IsTrue(grid.InBounds(new Coord(3, 2)));
            Assert.IsFalse(grid.InBounds(new Coord(4, 0)));
            Assert.IsFalse(grid.InBounds(new Coord(0, 3)));
            Assert.IsFalse(grid.InBounds(new Coord(-1, 0)));
            Assert.IsFalse(grid.InBounds(new Coord(0, -1)));
        }

        [Test]
        public void CellsStartWalkableAtWeightOne() {
            var grid = new Grid(4, 4);
            for (int i = 0; i < grid.CellCount; i++) {
                Assert.IsTrue(grid.IsWalkable(i));
                Assert.AreEqual(1f, grid.Weight(i));
            }
        }

        [Test]
        public void WalkableAndWeightRoundTrip() {
            var grid = new Grid(4, 4);
            int index = grid.Index(new Coord(2, 1));

            grid.SetWalkable(index, false);
            grid.SetWeight(index, 2.5f);

            Assert.IsFalse(grid.IsWalkable(index));
            Assert.AreEqual(2.5f, grid.Weight(index));
            Assert.IsTrue(grid.IsWalkable(grid.Index(new Coord(1, 2))), "neighbouring cell should be untouched");
        }

        // a zero or negative weight would make a cell free or profitable to walk through, which
        // breaks every shortest-path guarantee the searches rely on
        [Test]
        public void RejectsNonPositiveWeight() {
            var grid = new Grid(2, 2);
            Assert.Throws<ArgumentOutOfRangeException>(() => grid.SetWeight(0, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => grid.SetWeight(0, -1f));
        }

        [Test]
        public void StepCostChargesMoreForADiagonal() {
            var grid = new Grid(5, 5);
            int from = grid.Index(new Coord(2, 2));

            Assert.AreEqual(1f, grid.StepCost(from, grid.Index(new Coord(3, 2))), 1e-5f);
            Assert.AreEqual(1f, grid.StepCost(from, grid.Index(new Coord(2, 3))), 1e-5f);
            Assert.AreEqual(Grid.DiagonalCost, grid.StepCost(from, grid.Index(new Coord(3, 3))), 1e-5f);
            Assert.AreEqual(Grid.DiagonalCost, grid.StepCost(from, grid.Index(new Coord(1, 3))), 1e-5f);
        }

        [Test]
        public void StepCostScalesByWhatTheDestinationCharges() {
            var grid = new Grid(5, 5);
            int from = grid.Index(new Coord(2, 2));
            int to = grid.Index(new Coord(3, 2));
            grid.SetWeight(to, 3f);

            Assert.AreEqual(3f, grid.StepCost(from, to), 1e-5f);
            Assert.AreEqual(1f, grid.StepCost(to, from), 1e-5f,
                "the weight belongs to the cell being entered, not the one being left");
        }

        [Test]
        public void RejectsAnEmptyGrid() {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Grid(0, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Grid(4, -1));
        }
    }
}
