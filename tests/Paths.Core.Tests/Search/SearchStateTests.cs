using NUnit.Framework;
using Paths.Core.Search;

namespace Paths.Tests.Search {
    public class SearchStateTests {
        [Test]
        public void StartsUnreachedEverywhere() {
            var state = new SearchState(9);
            for (int i = 0; i < state.CellCount; i++) {
                Assert.AreEqual(float.PositiveInfinity, state.Cost(i));
                Assert.AreEqual(SearchState.NoParent, state.Parent(i));
                Assert.AreEqual(CellState.Unseen, state.State(i));
            }
        }

        // the relax check is "is this cheaper than what we have", so an unreached cell has to lose
        // that comparison against any real cost without needing a special case
        [Test]
        public void UnreachedCostLosesToAnyRealCost() {
            var state = new SearchState(4);
            Assert.IsTrue(12345f < state.Cost(0));
        }

        [Test]
        public void CostParentAndStateRoundTrip() {
            var state = new SearchState(4);

            state.SetCost(2, 3.5f);
            state.SetParent(2, 1);
            state.SetState(2, CellState.Closed);

            Assert.AreEqual(3.5f, state.Cost(2));
            Assert.AreEqual(1, state.Parent(2));
            Assert.AreEqual(CellState.Closed, state.State(2));
            Assert.AreEqual(CellState.Unseen, state.State(3), "neighbouring cell should be untouched");
        }

        [Test]
        public void ClearWipesEverything() {
            var state = new SearchState(4);
            state.SetCost(1, 2f);
            state.SetParent(1, 0);
            state.SetState(1, CellState.Open);

            state.Clear();

            Assert.AreEqual(float.PositiveInfinity, state.Cost(1));
            Assert.AreEqual(SearchState.NoParent, state.Parent(1));
            Assert.AreEqual(CellState.Unseen, state.State(1));
        }

        [Test]
        public void PathToWalksTheParentChainStartFirst() {
            var state = new SearchState(6);
            foreach (int cell in new[] { 0, 1, 2, 3 })
                state.SetState(cell, CellState.Closed);
            state.SetParent(1, 0);
            state.SetParent(2, 1);
            state.SetParent(3, 2);

            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, state.PathTo(3));
        }

        [Test]
        public void PathToIsEmptyForAnUnreachedCell() {
            var state = new SearchState(4);
            CollectionAssert.IsEmpty(state.PathTo(3));
        }

        [Test]
        public void PathToAStartCellIsJustThatCell() {
            var state = new SearchState(4);
            state.SetState(0, CellState.Closed);
            state.SetCost(0, 0f);

            CollectionAssert.AreEqual(new[] { 0 }, state.PathTo(0));
        }
    }
}
