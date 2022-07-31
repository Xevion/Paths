using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Paths.Core.Search;

namespace Paths.Tests.Search {
    public class IndexedMinHeapTests {
        private static List<int> Drain(IndexedMinHeap heap) {
            var order = new List<int>();
            while (heap.Count > 0)
                order.Add(heap.Pop());
            return order;
        }

        [Test]
        public void StartsEmpty() {
            var heap = new IndexedMinHeap(8);
            Assert.AreEqual(0, heap.Count);
            Assert.IsFalse(heap.Contains(3));
            Assert.Throws<InvalidOperationException>(() => heap.Pop());
            Assert.Throws<InvalidOperationException>(() => heap.Peek());
        }

        [Test]
        public void PopsInKeyOrder() {
            var heap = new IndexedMinHeap(8);
            heap.Push(0, 5f);
            heap.Push(1, 1f);
            heap.Push(2, 4f);
            heap.Push(3, 2f);
            heap.Push(4, 3f);

            CollectionAssert.AreEqual(new[] { 1, 3, 4, 2, 0 }, Drain(heap));
        }

        [Test]
        public void PeekIsTheSmallestWithoutRemovingIt() {
            var heap = new IndexedMinHeap(8);
            heap.Push(0, 5f);
            heap.Push(1, 2f);

            Assert.AreEqual(1, heap.Peek());
            Assert.AreEqual(2, heap.Count);
        }

        [Test]
        public void TracksWhichCellsAreQueued() {
            var heap = new IndexedMinHeap(8);
            heap.Push(3, 1f);

            Assert.IsTrue(heap.Contains(3));
            Assert.IsFalse(heap.Contains(4));

            heap.Pop();
            Assert.IsFalse(heap.Contains(3), "a popped cell is no longer queued");
        }

        [Test]
        public void RejectsPushingAQueuedCellTwice() {
            var heap = new IndexedMinHeap(8);
            heap.Push(3, 1f);
            Assert.Throws<InvalidOperationException>(() => heap.Push(3, 0.5f));
        }

        [Test]
        public void RequeuingAPoppedCellIsFine() {
            var heap = new IndexedMinHeap(8);
            heap.Push(3, 1f);
            heap.Pop();
            heap.Push(3, 0.5f);

            Assert.AreEqual(3, heap.Pop());
        }

        // one element is where an off-by-one in the pop path shows up, since there is no last
        // element to move into the hole
        [Test]
        public void HandlesASingleElement() {
            var heap = new IndexedMinHeap(4);
            heap.Push(2, 7f);

            Assert.AreEqual(2, heap.Pop());
            Assert.AreEqual(0, heap.Count);
            Assert.IsFalse(heap.Contains(2));
        }

        [Test]
        public void ClearEmptiesEverything() {
            var heap = new IndexedMinHeap(8);
            heap.Push(1, 1f);
            heap.Push(2, 2f);

            heap.Clear();

            Assert.AreEqual(0, heap.Count);
            Assert.IsFalse(heap.Contains(1));
            Assert.IsFalse(heap.Contains(2));
        }

        // the frontier sees keys in no particular order, so shuffle a full heap's worth and make
        // sure the sift paths hold the invariant whatever order they arrive in
        [Test]
        public void PopsInOrderForManyShuffledKeys() {
            const int count = 512;
            var rng = new Random(4);
            var keys = Enumerable.Range(0, count).Select(_ => (float) rng.NextDouble()).ToArray();

            var heap = new IndexedMinHeap(count);
            foreach (int cell in Enumerable.Range(0, count).OrderBy(_ => rng.Next()))
                heap.Push(cell, keys[cell]);

            List<int> order = Drain(heap);

            Assert.AreEqual(count, order.Count);
            CollectionAssert.AreEqual(order.OrderBy(cell => keys[cell]).ToList(), order);
        }

        [Test]
        public void RejectsAZeroCapacityHeap() {
            Assert.Throws<ArgumentOutOfRangeException>(() => new IndexedMinHeap(0));
        }
    }
}
