#nullable enable
using System.Collections.Generic;

namespace Paths.Core.Search {
    public enum CellState : byte {
        Unseen,
        Open,
        Closed
    }

    /// <summary>
    /// One search's working memory: the cost to reach each cell, the cell it was reached from, and
    /// whether it is queued or already expanded.
    ///
    /// This used to live on the node objects themselves, which meant a search scribbled on the map
    /// and had to hand-undo it afterwards. Keeping it here instead means two searches can run over
    /// one grid without trampling each other, and there is nothing to clean up when one finishes.
    /// </summary>
    public sealed class SearchState {
        public const int NoParent = -1;

        private readonly float[] _cost;
        private readonly int[] _parent;
        private readonly CellState[] _state;

        public SearchState(int cellCount) {
            _cost = new float[cellCount];
            _parent = new int[cellCount];
            _state = new CellState[cellCount];
            Clear();
        }

        public int CellCount => _cost.Length;

        /// <summary>Back to nothing reached and nothing queued, ready to be searched over again.</summary>
        public void Clear() {
            for (int i = 0; i < _cost.Length; i++) {
                _cost[i] = float.PositiveInfinity;
                _parent[i] = NoParent;
                _state[i] = CellState.Unseen;
            }
        }

        // infinity rather than a nullable float: it compares correctly against any real cost, so the
        // relax check needs no special case for "not reached yet"
        public float Cost(int index) => _cost[index];

        public void SetCost(int index, float cost) => _cost[index] = cost;

        public int Parent(int index) => _parent[index];

        public void SetParent(int index, int parent) => _parent[index] = parent;

        public CellState State(int index) => _state[index];

        public void SetState(int index, CellState state) => _state[index] = state;

        /// <summary>
        /// Walks the parent chain back from a cell and returns it start-first. Empty if the cell was
        /// never reached, so a caller can tell "no path" from "a path of one cell".
        /// </summary>
        public List<int> PathTo(int index) {
            var path = new List<int>();
            if (_state[index] == CellState.Unseen)
                return path;

            for (int at = index; at != NoParent; at = _parent[at])
                path.Add(at);
            path.Reverse();
            return path;
        }
    }
}
