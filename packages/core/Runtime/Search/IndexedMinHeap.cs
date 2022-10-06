#nullable enable
using System;

namespace Paths.Core.Search {
    /// <summary>
    /// A binary min-heap over cell indices, keyed by a float and a tiebreak float.
    ///
    /// The frontier this replaces was a sorted List that scanned linearly on every insert and
    /// recomputed each element's priority as it went, which is most of why a search cost six times
    /// as much for four times the cells. Indexed means it also tracks where each cell currently
    /// sits, so a cell already queued can have its key lowered in place instead of being pushed
    /// again and shadowing the stale copy.
    ///
    /// The second key exists for D* Lite, whose priorities are a pair compared in order. Everything
    /// else leaves it at zero and compares on the first alone.
    /// </summary>
    public sealed class IndexedMinHeap {
        private const int Absent = -1;

        private readonly int[] _heap;      // heap slot -> cell
        private readonly int[] _slot;      // cell -> heap slot, Absent when not queued
        private readonly float[] _key;     // cell -> its key
        private readonly float[] _key2;    // cell -> its tiebreak, 0 unless someone asked for one
        private int _count;

        public IndexedMinHeap(int capacity) {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "heap needs room for at least one cell");

            _heap = new int[capacity];
            _slot = new int[capacity];
            _key = new float[capacity];
            _key2 = new float[capacity];
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

        public float TiebreakOf(int cell) => _key2[cell];

        public int Peek() {
            if (_count == 0)
                throw new InvalidOperationException("heap is empty");
            return _heap[0];
        }

        public void Push(int cell, float key, float tiebreak = 0f) {
            if (Contains(cell))
                throw new InvalidOperationException($"cell {cell} is already queued, lower its key instead");

            _key[cell] = key;
            _key2[cell] = tiebreak;
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

        /// <summary>
        /// Set a queued cell's key, whichever way it moves. D* Lite raises keys as often as it lowers
        /// them, so it cannot use DecreaseKey and its narrower promise.
        /// </summary>
        public void Update(int cell, float key, float tiebreak = 0f) {
            int at = _slot[cell];
            if (at == Absent)
                throw new InvalidOperationException($"cell {cell} is not queued");

            _key[cell] = key;
            _key2[cell] = tiebreak;
            SiftUp(at);
            SiftDown(_slot[cell]); // only one of the two can move it, the other is a no-op
        }

        /// <summary>Drop a queued cell. D* Lite removes a vertex the moment it becomes consistent.</summary>
        public void Remove(int cell) {
            int at = _slot[cell];
            if (at == Absent)
                throw new InvalidOperationException($"cell {cell} is not queued");

            _slot[cell] = Absent;
            _count--;
            if (at == _count)
                return; // it was the last slot, nothing has to fill the hole

            int moved = _heap[_count];
            _heap[at] = moved;
            _slot[moved] = at;
            SiftUp(at);
            SiftDown(_slot[moved]);
        }

        // lexicographic: first key, then the tiebreak. everything but D* Lite leaves the tiebreak at
        // zero, so this collapses to the plain float comparison it started as.
        private bool Below(int cell, float key, float tiebreak) =>
            _key[cell] < key || (_key[cell] == key && _key2[cell] < tiebreak);

        // both sifts carry the moving cell along rather than swapping pairwise, so each step is one
        // write instead of three
        private void SiftUp(int at) {
            int cell = _heap[at];

            while (at > 0) {
                int above = (at - 1) / 2;
                int parent = _heap[above];
                if (!Below(cell, _key[parent], _key2[parent]))
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
            float tiebreak = _key2[cell];

            while (true) {
                int left = at * 2 + 1;
                if (left >= _count)
                    break;

                int right = left + 1;
                int lower = right < _count && Below(_heap[right], _key[_heap[left]], _key2[_heap[left]])
                    ? right
                    : left;

                int child = _heap[lower];
                if (!Below(child, key, tiebreak))
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
