#nullable enable
using System;

namespace Paths.Core.Search {
    /// <summary>
    /// The map a search runs over: which cells can be walked, and what each costs to enter. Stored
    /// as flat row-major arrays so a cell is just an int and its neighbours are index arithmetic.
    ///
    /// Nothing here changes while a search runs. The per-search scratch lives in SearchState, which
    /// is what lets two searches share one grid.
    /// </summary>
    public sealed class Grid {
        public readonly int Width;
        public readonly int Height;

        private readonly bool[] _walkable;
        private readonly float[] _weight;

        private float _minWeight = 1f;
        private float _maxWeight = 1f;
        private bool _statsStale;

        public Grid(int width, int height) {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), width, "grid needs a positive width");
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), height, "grid needs a positive height");

            Width = width;
            Height = height;
            _walkable = new bool[width * height];
            _weight = new float[width * height];
            for (int i = 0; i < _walkable.Length; i++) {
                _walkable[i] = true;
                _weight[i] = 1f;
            }
        }

        public int CellCount => _walkable.Length;

        // row-major: consecutive x values are adjacent in memory, so walking a row is a linear scan
        public int Index(Coord at) => at.Y * Width + at.X;

        public Coord At(int index) => new Coord(index % Width, index / Width);

        public bool InBounds(Coord at) => at.X >= 0 && at.X < Width && at.Y >= 0 && at.Y < Height;

        // unchecked on purpose: these sit in the expand loop, and callers reach them through
        // Neighbors, which only ever hands back indices it has already bounds-checked
        public bool IsWalkable(int index) => _walkable[index];

        public void SetWalkable(int index, bool walkable) => _walkable[index] = walkable;

        /// <summary>What it costs to enter this cell. 1 unless something has said otherwise.</summary>
        public float Weight(int index) => _weight[index];

        public void SetWeight(int index, float weight) {
            if (weight <= 0f)
                throw new ArgumentOutOfRangeException(nameof(weight), weight, "cell weight must be positive");
            _weight[index] = weight;
            _statsStale = true;
        }

        /// <summary>
        /// The cheapest any cell is to enter. A heuristic measures distance, so the least a stretch
        /// of it can possibly cost is that distance times this, which is what keeps an estimate from
        /// overshooting once a cell is allowed to cost less than one.
        /// </summary>
        public float MinWeight {
            get { Refresh(); return _minWeight; }
        }

        public float MaxWeight {
            get { Refresh(); return _maxWeight; }
        }

        /// <summary>Whether anything here costs other than the default to walk over.</summary>
        public bool HasTerrain => MinWeight < 1f || MaxWeight > 1f;

        // painting a brush stroke writes many cells before anything reads back, so the scan is put
        // off until it is asked for rather than run per cell
        private void Refresh() {
            if (!_statsStale)
                return;

            float least = float.PositiveInfinity, most = 0f;
            for (int i = 0; i < _weight.Length; i++) {
                if (_weight[i] < least) least = _weight[i];
                if (_weight[i] > most) most = _weight[i];
            }
            _minWeight = least;
            _maxWeight = most;
            _statsStale = false;
        }

        /// <summary>Most neighbours any cell can have, so callers can size a buffer once.</summary>
        public const int MaxNeighbors = 8;

        /// <summary>What a diagonal step costs, against 1 for an orthogonal one.</summary>
        public const float DiagonalCost = 1.41421356f;

        /// <summary>
        /// What it costs to step between two adjacent cells: the distance covered, scaled by what
        /// the destination charges to enter. Only meaningful for cells that are actually adjacent,
        /// which is all the searches ever ask about.
        /// </summary>
        public float StepCost(int from, int to) {
            bool diagonal = from % Width != to % Width && from / Width != to / Width;
            return (diagonal ? DiagonalCost : 1f) * _weight[to];
        }

        /// <summary>
        /// Writes the walkable neighbours of a cell into the buffer and returns how many there were.
        /// Takes a buffer rather than returning a collection because this runs once per expanded
        /// node, and an allocation there shows up in every search.
        ///
        /// Diagonals never cut corners: both orthogonals a diagonal squeezes between have to be open.
        /// </summary>
        public int Neighbors(int index, bool diagonal, int[] buffer) {
            int x = index % Width;
            int y = index / Width;
            int count = 0;

            // these double as the bounds check and as the corner-cutting test below
            bool west = x > 0 && _walkable[index - 1];
            bool east = x < Width - 1 && _walkable[index + 1];
            bool south = y > 0 && _walkable[index - Width];
            bool north = y < Height - 1 && _walkable[index + Width];

            if (west) buffer[count++] = index - 1;
            if (east) buffer[count++] = index + 1;
            if (south) buffer[count++] = index - Width;
            if (north) buffer[count++] = index + Width;

            if (!diagonal)
                return count;

            if (west && south && _walkable[index - Width - 1]) buffer[count++] = index - Width - 1;
            if (east && south && _walkable[index - Width + 1]) buffer[count++] = index - Width + 1;
            if (west && north && _walkable[index + Width - 1]) buffer[count++] = index + Width - 1;
            if (east && north && _walkable[index + Width + 1]) buffer[count++] = index + Width + 1;

            return count;
        }
    }
}
