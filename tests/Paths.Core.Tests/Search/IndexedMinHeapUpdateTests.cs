using System;
using System.Collections.Generic;
using NUnit.Framework;
using Paths.Core.Search;

namespace Paths.Tests.Search {
    /// <summary>
    /// The operations D* Lite needs on top of push and pop: a key that can move either way, removal
    /// of a cell mid-heap, and a second key that breaks ties on the first.
    /// </summary>
    public class IndexedMinHeapUpdateTests {
        private static List<int> Drain(IndexedMinHeap heap) {
            var order = new List<int>();
            while (heap.Count > 0)
                order.Add(heap.Pop());
            return order;
        }

        [Test]
        public void UpdateCanRaiseAKey() {
            var heap = new IndexedMinHeap(8);
            heap.Push(0, 1f);
            heap.Push(1, 2f);
            heap.Push(2, 3f);

            heap.Update(0, 10f); // was the cheapest, now the dearest

            CollectionAssert.AreEqual(new[] { 1, 2, 0 }, Drain(heap));
        }

        [Test]
        public void UpdateCanLowerAKey() {
            var heap = new IndexedMinHeap(8);
            heap.Push(0, 1f);
            heap.Push(1, 2f);
            heap.Push(2, 3f);

            heap.Update(2, 0.5f);

            CollectionAssert.AreEqual(new[] { 2, 0, 1 }, Drain(heap));
        }

        [Test]
        public void RemoveTakesACellOutOfTheMiddle() {
            var heap = new IndexedMinHeap(8);
            for (int i = 0; i < 6; i++)
                heap.Push(i, i);

            heap.Remove(3);

            Assert.IsFalse(heap.Contains(3));
            Assert.AreEqual(5, heap.Count);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 4, 5 }, Drain(heap));
        }

        [Test]
        public void RemoveHandlesTheLastSlotAndTheRoot() {
            var heap = new IndexedMinHeap(8);
            heap.Push(0, 1f);
            heap.Push(1, 2f);
            heap.Push(2, 3f);

            heap.Remove(2); // whichever slot it landed in, this is the largest
            heap.Remove(0); // the root
            CollectionAssert.AreEqual(new[] { 1 }, Drain(heap));
        }

        [Test]
        public void TheSecondKeyBreaksTiesOnTheFirst() {
            var heap = new IndexedMinHeap(8);
            heap.Push(0, 5f, 9f);
            heap.Push(1, 5f, 1f);
            heap.Push(2, 5f, 4f);
            heap.Push(3, 4f, 100f); // a lower first key wins however big the tiebreak

            CollectionAssert.AreEqual(new[] { 3, 1, 2, 0 }, Drain(heap));
        }

        [Test]
        public void UpdatingOnlyTheTiebreakReorders() {
            var heap = new IndexedMinHeap(8);
            heap.Push(0, 2f, 1f);
            heap.Push(1, 2f, 2f);

            heap.Update(0, 2f, 3f);

            CollectionAssert.AreEqual(new[] { 1, 0 }, Drain(heap));
        }

        [Test]
        public void UpdateAndRemoveRejectAnUnqueuedCell() {
            var heap = new IndexedMinHeap(8);
            heap.Push(0, 1f);

            Assert.Throws<InvalidOperationException>(() => heap.Update(5, 1f));
            Assert.Throws<InvalidOperationException>(() => heap.Remove(5));
        }

        /// <summary>
        /// Random pushes, updates and removals against a plain list doing the same thing. The heap's
        /// invariant is easy to break in a way that only shows on a particular shape of tree.
        /// </summary>
        [Test]
        public void MatchesANaiveQueueUnderRandomTraffic() {
            const int cells = 64;
            var rng = new Random(7);

            for (int round = 0; round < 200; round++) {
                var heap = new IndexedMinHeap(cells);
                var mirror = new Dictionary<int, (float, float)>();

                for (int step = 0; step < 300; step++) {
                    int cell = rng.Next(cells);
                    float k1 = rng.Next(0, 12);
                    float k2 = rng.Next(0, 4);

                    if (!heap.Contains(cell)) {
                        heap.Push(cell, k1, k2);
                        mirror[cell] = (k1, k2);
                    } else if (rng.NextDouble() < 0.5) {
                        heap.Update(cell, k1, k2);
                        mirror[cell] = (k1, k2);
                    } else {
                        heap.Remove(cell);
                        mirror.Remove(cell);
                    }
                }

                var expected = new List<int>(mirror.Keys);
                expected.Sort((a, b) => {
                    int first = mirror[a].Item1.CompareTo(mirror[b].Item1);
                    if (first != 0) return first;
                    return mirror[a].Item2.CompareTo(mirror[b].Item2);
                });

                List<int> actual = Drain(heap);
                Assert.AreEqual(expected.Count, actual.Count, $"count mismatch on round {round}");
                // equal keys can come out in either order, so compare the keys rather than the cells
                for (int i = 0; i < actual.Count; i++)
                    Assert.AreEqual(mirror[expected[i]], mirror[actual[i]], $"key order broke at {i} on round {round}");
            }
        }
    }
}
