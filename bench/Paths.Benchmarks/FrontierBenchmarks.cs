using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Paths.Core.Search;

namespace Paths.Benchmarks {
    /// <summary>
    /// The heap against the sorted list it is meant to replace: fill the frontier, then drain it.
    /// Counts are cell counts of a 32x32 and a 128x128 grid.
    ///
    /// The sorted-list side here reads keys straight out of an array, where the real frontier calls
    /// a virtual Priority per comparison. So this understates how far behind the old one actually is.
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 2, targetCount: 5)]
    public class FrontierBenchmarks {
        [Params(1_024, 16_384)]
        public int Count;

        private float[] _keys;

        [GlobalSetup]
        public void Setup() {
            var rng = new System.Random(7);
            _keys = new float[Count];
            for (int i = 0; i < Count; i++)
                _keys[i] = (float) rng.NextDouble();
        }

        [Benchmark(Baseline = true)]
        public int SortedList() {
            var frontier = new List<int>();
            for (int cell = 0; cell < Count; cell++) {
                float key = _keys[cell];
                int at = 0;
                while (at < frontier.Count && _keys[frontier[at]] <= key)
                    at++;
                frontier.Insert(at, cell);
            }

            int last = 0;
            while (frontier.Count > 0) {
                last = frontier[0];
                frontier.RemoveAt(0);
            }
            return last;
        }

        [Benchmark]
        public int Heap() {
            var heap = new IndexedMinHeap(Count);
            for (int cell = 0; cell < Count; cell++)
                heap.Push(cell, _keys[cell]);

            int last = 0;
            while (heap.Count > 0)
                last = heap.Pop();
            return last;
        }
    }
}
