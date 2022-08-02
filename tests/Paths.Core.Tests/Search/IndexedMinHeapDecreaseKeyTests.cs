using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Paths.Core.Search;

namespace Paths.Tests.Search {
    public class IndexedMinHeapDecreaseKeyTests {
        [Test]
        public void LoweringAKeyMovesTheCellToTheFront() {
            var heap = new IndexedMinHeap(8);
            heap.Push(0, 1f);
            heap.Push(1, 2f);
            heap.Push(2, 3f);

            heap.DecreaseKey(2, 0.5f);

            Assert.AreEqual(2, heap.Peek());
            Assert.AreEqual(0.5f, heap.KeyOf(2));
        }

        [Test]
        public void LoweringAKeyKeepsTheRestInOrder() {
            var heap = new IndexedMinHeap(8);
            for (int cell = 0; cell < 6; cell++)
                heap.Push(cell, cell + 1f);

            heap.DecreaseKey(5, 0.5f);
            heap.DecreaseKey(4, 1.5f);

            var order = new List<int>();
            while (heap.Count > 0)
                order.Add(heap.Pop());

            CollectionAssert.AreEqual(new[] { 5, 0, 4, 1, 2, 3 }, order);
        }

        [Test]
        public void LoweringToAnEqualKeyIsHarmless() {
            var heap = new IndexedMinHeap(8);
            heap.Push(0, 1f);
            heap.Push(1, 2f);

            heap.DecreaseKey(1, 2f);

            CollectionAssert.AreEqual(new[] { 0, 1 }, new[] { heap.Pop(), heap.Pop() });
        }

        [Test]
        public void RejectsRaisingAKey() {
            var heap = new IndexedMinHeap(8);
            heap.Push(0, 1f);
            Assert.Throws<ArgumentOutOfRangeException>(() => heap.DecreaseKey(0, 2f));
        }

        [Test]
        public void RejectsACellThatIsNotQueued() {
            var heap = new IndexedMinHeap(8);
            heap.Push(0, 1f);
            Assert.Throws<InvalidOperationException>(() => heap.DecreaseKey(1, 0.5f));
        }

        [Test]
        public void RejectsACellThatWasAlreadyPopped() {
            var heap = new IndexedMinHeap(8);
            heap.Push(0, 1f);
            heap.Pop();
            Assert.Throws<InvalidOperationException>(() => heap.DecreaseKey(0, 0.5f));
        }

        /// <summary>
        /// What a relaxing search actually does to the frontier: push a pile of cells, then keep
        /// lowering random queued ones while draining it. Pop order still has to be nondecreasing,
        /// which is the invariant the whole search leans on.
        /// </summary>
        [Test]
        public void StaysOrderedUnderInterleavedPushesPopsAndDecreases() {
            const int count = 256;
            var rng = new Random(11);
            var heap = new IndexedMinHeap(count);
            var keys = new float[count];
            var queued = new List<int>();

            for (int cell = 0; cell < count; cell++) {
                keys[cell] = (float) rng.NextDouble() * 100f;
                heap.Push(cell, keys[cell]);
                queued.Add(cell);
            }

            float previous = float.NegativeInfinity;
            while (heap.Count > 0) {
                // lower a few still-queued cells, but never below what has already come out
                for (int i = 0; i < 3 && queued.Count > 0; i++) {
                    int cell = queued[rng.Next(queued.Count)];
                    float lowered = Math.Max(previous, keys[cell] - (float) rng.NextDouble() * 10f);
                    if (lowered > keys[cell])
                        continue;
                    keys[cell] = lowered;
                    heap.DecreaseKey(cell, lowered);
                }

                int popped = heap.Pop();
                queued.Remove(popped);

                Assert.GreaterOrEqual(keys[popped], previous,
                    $"cell {popped} came out at {keys[popped]} after {previous}");
                previous = keys[popped];
            }

            Assert.AreEqual(0, queued.Count);
        }

        [Test]
        public void SurvivesEveryCellBeingLoweredToTheSameKey() {
            const int count = 64;
            var heap = new IndexedMinHeap(count);
            for (int cell = 0; cell < count; cell++)
                heap.Push(cell, cell + 1f);
            for (int cell = 0; cell < count; cell++)
                heap.DecreaseKey(cell, 0f);

            var seen = new HashSet<int>();
            while (heap.Count > 0)
                Assert.IsTrue(seen.Add(heap.Pop()), "every cell should come out exactly once");

            Assert.AreEqual(count, seen.Count);
        }
    }
}
