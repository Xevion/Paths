#nullable enable
using System;

namespace Paths.Core.Search {
    /// <summary>
    /// A binary min-heap over cell indices, keyed by float.
    ///
    /// The frontier this replaces was a sorted List that scanned linearly on every insert and
    /// recomputed each element's priority as it went, which is most of why a search cost six times
    /// as much for four times the cells. Indexed means it also tracks where each cell currently
    /// sits, so a cell already queued can have its key lowered in place instead of being pushed
    /// again and shadowing the stale copy.
    /// </summary>
    public sealed class IndexedMinHeap {
        private const int Absent = -1;

        private readonly int[] _heap;      // heap slot -> cell
        private readonly int[] _slot;      // cell -> heap slot, Absent when not queued
        private readonly float[] _key;     // cell -> its key
        private int _count;

        public IndexedMinHeap(int capacity) {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "heap needs room for at least one cell");

            _heap = new int[capacity];
            _slot = new int[capacity];
            _key = new float[capacity];
            Clear();
        }

        public int Count => _count;

        public int Capacity => _heap.Length;

        public void Clear() {
            _count = 0;
            for (int i = 0; i < _slot.Length; i++)
                _slot[i] = Absent;
        }

        public bool Contains(int cell) => _slot[cell] != Absent;

        public float KeyOf(int cell) => _key[cell];

        public int Peek() {
            if (_count == 0)
                throw new InvalidOperationException("heap is empty");
            return _heap[0];
        }

        public void Push(int cell, float key) {
            if (Contains(cell))
                throw new InvalidOperationException($"cell {cell} is already queued, lower its key instead");

            _key[cell] = key;
            _heap[_count] = cell;
            _slot[cell] = _count;
            _count++;
            SiftUp(_count - 1);
        }

        public int Pop() {
            if (_count == 0)
                throw new InvalidOperationException("heap is empty");

            int top = _heap[0];
            _count--;
            // the last cell fills the hole at the root, unless the cell we just took was the last one
            if (_count > 0) {
                int moved = _heap[_count];
                _heap[0] = moved;
                _slot[moved] = 0;
                SiftDown(0);
            }
            _slot[top] = Absent;
            return top;
        }

        /// <summary>
        /// Lowers a queued cell's key and floats it up to where it now belongs. This is what the
        /// slot table is for: relaxing a cell that is already queued costs one sift, instead of a
        /// second entry that later has to be recognised as stale and thrown away.
        /// </summary>
        public void DecreaseKey(int cell, float key) {
            int at = _slot[cell];
            if (at == Absent)
                throw new InvalidOperationException($"cell {cell} is not queued");
            if (key > _key[cell])
                throw new ArgumentOutOfRangeException(nameof(key), key,
                    $"decrease-key cannot raise a key, {cell} is already at {_key[cell]}");

            _key[cell] = key;
            SiftUp(at);
        }

        // both sifts carry the moving cell along rather than swapping pairwise, so each step is one
        // write instead of three
        private void SiftUp(int at) {
            int cell = _heap[at];
            float key = _key[cell];

            while (at > 0) {
                int above = (at - 1) / 2;
                int parent = _heap[above];
                if (_key[parent] <= key)
                    break;
                _heap[at] = parent;
                _slot[parent] = at;
                at = above;
            }

            _heap[at] = cell;
            _slot[cell] = at;
        }

        private void SiftDown(int at) {
            int cell = _heap[at];
            float key = _key[cell];

            while (true) {
                int left = at * 2 + 1;
                if (left >= _count)
                    break;

                int right = left + 1;
                int lower = right < _count && _key[_heap[right]] < _key[_heap[left]] ? right : left;

                int child = _heap[lower];
                if (_key[child] >= key)
                    break;

                _heap[at] = child;
                _slot[child] = at;
                at = lower;
            }

            _heap[at] = cell;
            _slot[cell] = at;
        }
    }
}
