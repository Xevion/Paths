using NUnit.Framework;
using Paths.Core.Search;

namespace Paths.Tests.Search {
    public class HeuristicTests {
        private const float Eps = 1e-4f;

        [Test]
        public void ZeroWhenAlreadyThere() {
            var at = new Coord(3, 4);
            foreach (Heuristic kind in new[] { Heuristic.Manhattan, Heuristic.Euclidean, Heuristic.Chebyshev, Heuristic.Octile })
                Assert.AreEqual(0f, Heuristics.Estimate(kind, at, at), Eps, kind.ToString());
        }

        [Test]
        public void MeasuresAKnownOffset() {
            var from = new Coord(0, 0);
            var to = new Coord(3, 4);

            Assert.AreEqual(7f, Heuristics.Estimate(Heuristic.Manhattan, from, to), Eps);
            Assert.AreEqual(5f, Heuristics.Estimate(Heuristic.Euclidean, from, to), Eps);
            Assert.AreEqual(4f, Heuristics.Estimate(Heuristic.Chebyshev, from, to), Eps);
            Assert.AreEqual(3f + 4f + (Grid.DiagonalCost - 2f) * 3f,
                Heuristics.Estimate(Heuristic.Octile, from, to), Eps);
        }

        [Test]
        public void IsSymmetric() {
            var a = new Coord(1, 7);
            var b = new Coord(6, 2);
            foreach (Heuristic kind in new[] { Heuristic.Manhattan, Heuristic.Euclidean, Heuristic.Chebyshev, Heuristic.Octile })
                Assert.AreEqual(Heuristics.Estimate(kind, a, b), Heuristics.Estimate(kind, b, a), Eps, kind.ToString());
        }

        /// <summary>
        /// Octile is the exact cost of walking an open 8-connected grid, so on an empty grid it is
        /// what the shortest path actually costs. Everything cheaper is still admissible; Manhattan
        /// coming out above it is precisely why A* stops finding shortest paths with diagonals on.
        /// </summary>
        [Test]
        public void OctileIsTheTrueOpenGridCostAndManhattanOvershootsIt() {
            var from = new Coord(0, 0);
            var to = new Coord(5, 3);

            float octile = Heuristics.Estimate(Heuristic.Octile, from, to);
            // three diagonals then two straight
            Assert.AreEqual(3f * Grid.DiagonalCost + 2f, octile, Eps);

            Assert.Greater(Heuristics.Estimate(Heuristic.Manhattan, from, to), octile);
            Assert.LessOrEqual(Heuristics.Estimate(Heuristic.Euclidean, from, to), octile + Eps);
            Assert.LessOrEqual(Heuristics.Estimate(Heuristic.Chebyshev, from, to), octile + Eps);
        }

        // with only 4-direction movement the true cost is Manhattan, and nothing may exceed it
        [Test]
        public void NothingOvershootsManhattanOnOrthogonalMovement() {
            var from = new Coord(0, 0);
            var to = new Coord(5, 3);
            float manhattan = Heuristics.Estimate(Heuristic.Manhattan, from, to);

            foreach (Heuristic kind in new[] { Heuristic.Euclidean, Heuristic.Chebyshev, Heuristic.Octile })
                Assert.LessOrEqual(Heuristics.Estimate(kind, from, to), manhattan + Eps, kind.ToString());
        }
    }
}
