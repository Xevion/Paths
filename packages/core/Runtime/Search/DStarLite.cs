#nullable enable
using System.Collections.Generic;

namespace Paths.Core.Search {
    /// <summary>
    /// D* Lite: plans once, then repairs that plan when the grid changes instead of searching again.
    ///
    /// It searches backward, from the goal outward, so what it works out is each cell's cost to reach
    /// the goal. That is what makes it repairable: an edit invalidates the cells around it, and
    /// everything outside the affected region keeps the answer it already had.
    ///
    /// Each cell carries g, the cost it has settled on, and rhs, the cost implied by its neighbours
    /// one step on. Where the two agree the cell is consistent and nothing needs doing. An edit makes
    /// some cells disagree, and the queue holds exactly those until they are reconciled.
    /// </summary>
    public sealed class DStarLite : IIncrementalSearch {
        private Grid _grid = null!;
        private SearchOptions _options;
        private IndexedMinHeap _queue = null!;

        private float[] _g = null!;
        private float[] _rhs = null!;
        private int[] _neighbors = null!;

        // BestNeighborCost is called from inside loops that are themselves walking _neighbors, so it
        // needs a buffer of its own rather than trampling the one its caller is mid-way through
        private int[] _scratch = null!;

        private int _start;
        private int _goal;

        // the heuristic is measured from the start, so moving the start would invalidate every key
        // already queued. km carries the shift instead, which is the whole reason the queue survives.
        private float _km;
        private int _lastStart;
        private float _scale;

        private readonly List<int> _touched = new List<int>();
        private List<Visit> _visits = new List<Visit>();
        private int _expanded;

        public SearchResult Start(Grid grid, int start, int goal, SearchOptions options) {
            _grid = grid;
            _options = options;
            _start = start;
            _goal = goal;
            _lastStart = start;
            _km = 0f;
            _scale = grid.MinWeight;

            _queue = new IndexedMinHeap(grid.CellCount);
            _g = new float[grid.CellCount];
            _rhs = new float[grid.CellCount];
            _neighbors = new int[Grid.MaxNeighbors];
            _scratch = new int[Grid.MaxNeighbors];
            for (int i = 0; i < grid.CellCount; i++) {
                _g[i] = float.PositiveInfinity;
                _rhs[i] = float.PositiveInfinity;
            }

            _visits = new List<Visit>(grid.CellCount / 4);
            _expanded = 0;
            _rhs[goal] = 0f;
            Enqueue(goal);

            return Plan();
        }

        public void Touch(IReadOnlyList<int> cells) {
            for (int i = 0; i < cells.Count; i++)
                _touched.Add(cells[i]);
        }

        /// <summary>Move the start without discarding the plan, for dragging the marker around.</summary>
        public void MoveStart(int start) {
            if (start == _start)
                return;

            // every queued key was measured from the old start; km absorbs the difference so they
            // stay comparable rather than all needing to be recomputed
            _km += Heuristics.Estimate(_options.Heuristic, _grid.At(_lastStart), _grid.At(start)) * _scale;
            _lastStart = start;
            _start = start;
        }

        public SearchResult Replan() {
            // each replan reports only the work it actually did, so playback shows the repair rather
            // than replaying the whole original search again
            _visits = new List<Visit>();
            _expanded = 0;

            // a changed cell moves what it costs to enter, which is what its neighbours priced their
            // own rhs on, so both the cell and everything adjacent has to be reconsidered
            for (int i = 0; i < _touched.Count; i++) {
                int cell = _touched[i];
                UpdateVertex(cell);
                int count = _grid.Neighbors(cell, _options.Diagonal, _neighbors);
                for (int n = 0; n < count; n++)
                    UpdateVertex(_neighbors[n]);

                // a cell that just became a wall keeps no neighbours of its own, so the loop above
                // cannot reach the cells that used to reach it. sweep its eight surrounds directly.
                if (!_grid.IsWalkable(cell))
                    UpdateAround(cell);
            }
            _touched.Clear();

            // weights may have moved, and the heuristic is only admissible when scaled by the
            // cheapest cell on the board
            _scale = _grid.MinWeight;

            return Plan();
        }

        private void UpdateAround(int cell) {
            Coord at = _grid.At(cell);
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++) {
                    if (dx == 0 && dy == 0)
                        continue;
                    var to = new Coord(at.X + dx, at.Y + dy);
                    if (_grid.InBounds(to))
                        UpdateVertex(_grid.Index(to));
                }
        }

        private SearchResult Plan() {
            ComputeShortestPath();

            List<int> path = ExtractPath();
            foreach (int cell in path)
                _visits.Add(new Visit(cell, VisitKind.Path));

            return new SearchResult(path, _visits, Snapshot(), _expanded);
        }

        private void ComputeShortestPath() {
            // the textbook stops the moment the start's own figure is settled, because there the
            // caller is an agent about to take one step. this one reads the whole route out and
            // paints a heatmap of every cell, so it drains the queue and leaves the field consistent
            // throughout. that is still only the cells an edit disturbed, which is the point; it just
            // is not the smaller set that answering "where do I step next" would need.
            while (_queue.Count > 0) {
                int top = _queue.Peek();
                float topKey = _queue.KeyOf(top), topTie = _queue.TiebreakOf(top);

                CalcKey(top, out float freshKey, out float freshTie);
                // its key was set before km moved, so it is stale rather than actually next
                if (topKey < freshKey || (topKey == freshKey && topTie < freshTie)) {
                    _queue.Update(top, freshKey, freshTie);
                    continue;
                }

                _visits.Add(new Visit(top, VisitKind.Expanded));
                _expanded++;

                if (_g[top] > _rhs[top]) {
                    // overpriced: accept the cheaper figure and let the neighbours reprice off it
                    _g[top] = _rhs[top];
                    _queue.Remove(top);
                    UpdateNeighbors(top);
                } else {
                    // underpriced, which an edit can cause: drop it and let it settle again
                    _g[top] = float.PositiveInfinity;
                    UpdateVertex(top);
                    UpdateNeighbors(top);
                }
            }
        }

        private void UpdateNeighbors(int cell) {
            int count = _grid.Neighbors(cell, _options.Diagonal, _neighbors);
            for (int i = 0; i < count; i++)
                UpdateVertex(_neighbors[i]);
        }

        private void UpdateVertex(int cell) {
            if (cell != _goal)
                _rhs[cell] = BestNeighborCost(cell, out int _);

            if (_queue.Contains(cell))
                _queue.Remove(cell);

            if (_g[cell] != _rhs[cell])
                Enqueue(cell);
        }

        private void Enqueue(int cell) {
            CalcKey(cell, out float key, out float tie);
            _queue.Push(cell, key, tie);
            _visits.Add(new Visit(cell, VisitKind.Seen));
        }

        /// <summary>
        /// The cheapest way on from this cell: what it costs to step into a neighbour plus what that
        /// neighbour has settled on. Weights price the cell being entered, so the step is charged in
        /// the direction of travel even though the search runs the other way.
        /// </summary>
        private float BestNeighborCost(int cell, out int via) {
            via = SearchState.NoParent;
            if (!_grid.IsWalkable(cell))
                return float.PositiveInfinity;

            int count = _grid.Neighbors(cell, _options.Diagonal, _scratch);
            float best = float.PositiveInfinity;

            for (int i = 0; i < count; i++) {
                int next = _scratch[i];
                if (float.IsPositiveInfinity(_g[next]))
                    continue;
                float total = _grid.StepCost(cell, next) + _g[next];
                if (total < best) {
                    best = total;
                    via = next;
                }
            }
            return best;
        }

        private void CalcKey(int cell, out float key, out float tiebreak) {
            float settled = _g[cell] < _rhs[cell] ? _g[cell] : _rhs[cell];
            tiebreak = settled;
            key = float.IsPositiveInfinity(settled)
                ? float.PositiveInfinity
                : settled + Heuristics.Estimate(_options.Heuristic, _grid.At(_start), _grid.At(cell)) * _scale + _km;
        }

        /// <summary>
        /// What the route from the start costs. The search settles the start's neighbours rather than
        /// the start itself, so it is rhs that holds the answer here; g can still be infinite at the
        /// moment the queue is exhausted, and reading it was the first thing to go wrong.
        /// </summary>
        private float RouteCost => _g[_start] < _rhs[_start] ? _g[_start] : _rhs[_start];

        // walk the settled costs downhill from the start
        private List<int> ExtractPath() {
            var path = new List<int>();
            if (float.IsPositiveInfinity(RouteCost) && _start != _goal)
                return path;

            int at = _start;
            path.Add(at);
            // a route can only be as long as the board; anything more means the costs disagree
            for (int step = 0; step < _grid.CellCount && at != _goal; step++) {
                BestNeighborCost(at, out int next);
                if (next == SearchState.NoParent)
                    return new List<int>();
                at = next;
                path.Add(at);
            }

            return at == _goal ? path : new List<int>();
        }

        /// <summary>
        /// The timeline and the heatmap both want a SearchState, keyed on distance from the start.
        /// This search knows the opposite, each cell's distance to the goal, so the field is flipped:
        /// the route's total less what a cell has left. On the route that is exactly the distance
        /// covered so far; off it, it reads as how much progress reaching that cell would represent,
        /// and cells further from the goal than the start is come out at zero.
        /// </summary>
        private SearchState Snapshot() {
            var state = new SearchState(_grid.CellCount);
            float total = RouteCost;
            if (float.IsPositiveInfinity(total))
                return state;

            for (int i = 0; i < _grid.CellCount; i++) {
                if (float.IsPositiveInfinity(_g[i]))
                    continue;
                float covered = total - _g[i];
                state.SetCost(i, covered > 0f ? covered : 0f);
                state.SetState(i, CellState.Closed);
            }
            return state;
        }
    }
}
