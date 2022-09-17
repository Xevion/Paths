#nullable enable
using System;
using System.Collections.Generic;

namespace Paths.Core.Search {
    /// <summary>
    /// Jump Point Search: A* that skips the long runs of equally-good cells between the points where
    /// a route genuinely has to make a decision, so it touches a fraction of what plain A* does.
    ///
    /// Always 8-connected, so <see cref="SearchOptions.Diagonal"/> does not apply to it, and always
    /// octile-scored. Diagonals do not cut corners, matching the rest of the movement here. The jump
    /// and prune rules are the no-corner-cutting variant.
    ///
    /// Uniform step costs are baked into the jump rules, so cell weights do not apply either.
    /// </summary>
    public sealed class JumpPointSearch : ISearch {
        public SearchResult Search(Grid grid, int start, int goal, SearchOptions options) =>
            new Run(grid, goal).Execute(start);

        // skipping over cells assumes they all cost the same, which is the whole trick
        public bool HonorsWeights => false;

        // one of these per call, so the recursion has somewhere to hang its context without the
        // search itself holding state between runs
        private sealed class Run {
            private readonly Grid _grid;
            private readonly Coord _goal;
            private readonly SearchState _state;
            private readonly IndexedMinHeap _frontier;
            private readonly List<Visit> _visits;

            // reused across every expansion. these were allocated fresh per expanded cell, which on a
            // big grid cost more than the jumping they were feeding
            private readonly List<Coord> _successors = new List<Coord>(Grid.MaxNeighbors);
            private readonly List<Coord> _pruned = new List<Coord>(Grid.MaxNeighbors);

            public Run(Grid grid, int goal) {
                _grid = grid;
                _goal = grid.At(goal);
                _state = new SearchState(grid.CellCount);
                _frontier = new IndexedMinHeap(grid.CellCount);
                _visits = new List<Visit>(grid.CellCount / 4);
            }

            public SearchResult Execute(int start) {
                int goal = _grid.Index(_goal);
                int expanded = 0;

                _state.SetCost(start, 0f);
                _state.SetState(start, CellState.Open);
                _frontier.Push(start, Octile(_grid.At(start), _goal));

                while (_frontier.Count > 0) {
                    int current = _frontier.Pop();
                    _state.SetState(current, CellState.Closed);
                    _visits.Add(new Visit(current, VisitKind.Expanded));
                    expanded++;

                    if (current == goal)
                        break;

                    Coord at = _grid.At(current);
                    foreach (Coord jump in Successors(current, at)) {
                        int next = _grid.Index(jump);
                        if (_state.State(next) == CellState.Closed)
                            continue;

                        float cost = _state.Cost(current) + Octile(at, jump);
                        if (cost >= _state.Cost(next))
                            continue;

                        _state.SetCost(next, cost);
                        _state.SetParent(next, current);
                        float priority = cost + Octile(jump, _goal);

                        if (_state.State(next) == CellState.Unseen) {
                            _state.SetState(next, CellState.Open);
                            _visits.Add(new Visit(next, VisitKind.Seen));
                            _frontier.Push(next, priority);
                        } else {
                            _frontier.DecreaseKey(next, priority);
                        }
                    }
                }

                List<int> path = _state.State(goal) == CellState.Closed ? Interpolate(goal) : new List<int>();
                foreach (int cell in path)
                    _visits.Add(new Visit(cell, VisitKind.Path));

                return new SearchResult(path, _visits, _state, expanded);
            }

            /// <summary>Where each pruned direction lands, for the directions that land anywhere.</summary>
            private List<Coord> Successors(int cell, Coord at) {
                Prune(cell, at);
                _successors.Clear();
                for (int i = 0; i < _pruned.Count; i++) {
                    Coord? jump = Jump(_pruned[i].X, _pruned[i].Y, at.X, at.Y);
                    if (jump.HasValue)
                        _successors.Add(jump.Value);
                }
                return _successors;
            }

            /// <summary>
            /// Walk in one direction until something worth stopping for: the goal, a forced
            /// neighbour, or a wall. Diagonals probe horizontally and vertically first, since
            /// anything those would force is a reason to stop on the diagonal too.
            /// </summary>
            private Coord? Jump(int x, int y, int px, int py) {
                if (!Walkable(x, y))
                    return null;
                if (x == _goal.X && y == _goal.Y)
                    return new Coord(x, y);

                int dx = x - px, dy = y - py;

                if (dx != 0 && dy != 0) {
                    if (Jump(x + dx, y, x, y).HasValue || Jump(x, y + dy, x, y).HasValue)
                        return new Coord(x, y);
                } else if (dx != 0) {
                    // forced when a cell beside us is open but the one diagonally behind it is a
                    // wall: there is no cheaper way into it, so it has to be expanded from here
                    if ((Walkable(x, y - 1) && !Walkable(x - dx, y - 1)) ||
                        (Walkable(x, y + 1) && !Walkable(x - dx, y + 1)))
                        return new Coord(x, y);
                } else {
                    if ((Walkable(x - 1, y) && !Walkable(x - 1, y - dy)) ||
                        (Walkable(x + 1, y) && !Walkable(x + 1, y - dy)))
                        return new Coord(x, y);
                }

                // carry on. a diagonal needs both orthogonals open to continue, same no-corner-cutting
                // rule the rest of the movement follows
                if (Walkable(x + dx, y) && Walkable(x, y + dy))
                    return Jump(x + dx, y + dy, x, y);
                return null;
            }

            /// <summary>The directions worth jumping, given how this cell was arrived at.</summary>
            private void Prune(int cell, Coord at) {
                List<Coord> found = _pruned;
                found.Clear();
                int x = at.X, y = at.Y;
                int parent = _state.Parent(cell);

                if (parent == SearchState.NoParent) {
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++) {
                            if (dx == 0 && dy == 0) continue;
                            if (Movable(x, y, dx, dy))
                                found.Add(new Coord(x + dx, y + dy));
                        }
                    return;
                }

                Coord from = _grid.At(parent);
                int hx = Math.Sign(x - from.X), hy = Math.Sign(y - from.Y);

                if (hx != 0 && hy != 0) {
                    bool wy = Walkable(x, y + hy), wx = Walkable(x + hx, y);
                    if (wy) found.Add(new Coord(x, y + hy));
                    if (wx) found.Add(new Coord(x + hx, y));
                    if (wy && wx) found.Add(new Coord(x + hx, y + hy));
                } else if (hx != 0) {
                    bool next = Walkable(x + hx, y), up = Walkable(x, y + 1), down = Walkable(x, y - 1);
                    if (next) {
                        found.Add(new Coord(x + hx, y));
                        if (up) found.Add(new Coord(x + hx, y + 1));
                        if (down) found.Add(new Coord(x + hx, y - 1));
                    }
                    if (up) found.Add(new Coord(x, y + 1));
                    if (down) found.Add(new Coord(x, y - 1));
                } else {
                    bool next = Walkable(x, y + hy), right = Walkable(x + 1, y), left = Walkable(x - 1, y);
                    if (next) {
                        found.Add(new Coord(x, y + hy));
                        if (right) found.Add(new Coord(x + 1, y + hy));
                        if (left) found.Add(new Coord(x - 1, y + hy));
                    }
                    if (right) found.Add(new Coord(x + 1, y));
                    if (left) found.Add(new Coord(x - 1, y));
                }
            }

            /// <summary>
            /// The parent chain only holds jump points, so fill the straight and diagonal runs
            /// between them back in. Comes out start-first, every cell present.
            /// </summary>
            private List<int> Interpolate(int goal) {
                var jumps = new List<Coord>();
                for (int cell = goal; cell != SearchState.NoParent; cell = _state.Parent(cell))
                    jumps.Add(_grid.At(cell));
                jumps.Reverse();

                var full = new List<int> { _grid.Index(jumps[0]) };
                for (int i = 1; i < jumps.Count; i++) {
                    Coord from = jumps[i - 1], to = jumps[i];
                    int sx = Math.Sign(to.X - from.X), sy = Math.Sign(to.Y - from.Y);
                    Coord step = from;
                    while (step != to) {
                        step = new Coord(step.X + sx, step.Y + sy);
                        full.Add(_grid.Index(step));
                    }
                }
                return full;
            }

            private static float Octile(Coord a, Coord b) =>
                Heuristics.Estimate(Heuristic.Octile, a, b);

            private bool Walkable(int x, int y) {
                var at = new Coord(x, y);
                return _grid.InBounds(at) && _grid.IsWalkable(_grid.Index(at));
            }

            // can we step (dx,dy) off (x,y) without cutting a corner
            private bool Movable(int x, int y, int dx, int dy) {
                if (!Walkable(x + dx, y + dy))
                    return false;
                if (dx != 0 && dy != 0)
                    return Walkable(x + dx, y) && Walkable(x, y + dy);
                return true;
            }
        }
    }
}
