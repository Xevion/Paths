#nullable enable
using System.Collections.Generic;

namespace Paths.Core.Search {
    /// <summary>
    /// Breadth- and depth-first, which pick the next cell by when it was found rather than by what
    /// it costs. Neither relaxes: a cell is scored once when it is discovered and left alone after,
    /// because neither is ordering by cost in the first place.
    ///
    /// That makes breadth-first shortest in number of steps, which is only the cheapest path when
    /// every step costs the same. Turn diagonals on, or weight a cell, and the two part ways.
    /// </summary>
    public abstract class UninformedSearch : ISearch {
        /// <summary>Depth-first takes the cell it found most recently, breadth-first the oldest.</summary>
        protected abstract bool TakeNewest { get; }

        public SearchResult Search(Grid grid, int start, int goal, SearchOptions options) {
            var state = new SearchState(grid.CellCount);
            var visits = new List<Visit>();
            var neighbors = new int[Grid.MaxNeighbors];

            // one list serving as both stack and queue: depth-first pops the tail, breadth-first
            // walks a head index forward, so neither pays to shuffle the front of a list
            var frontier = new List<int>();
            int head = 0;
            int expanded = 0;

            state.SetCost(start, 0f);
            state.SetState(start, CellState.Open);
            frontier.Add(start);

            while (head < frontier.Count) {
                int current;
                if (TakeNewest) {
                    current = frontier[frontier.Count - 1];
                    frontier.RemoveAt(frontier.Count - 1);
                } else {
                    current = frontier[head++];
                }

                if (state.State(current) == CellState.Closed)
                    continue;

                state.SetState(current, CellState.Closed);
                visits.Add(new Visit(current, VisitKind.Expanded));
                expanded++;

                if (current == goal)
                    break;

                int count = grid.Neighbors(current, options.Diagonal, neighbors);
                for (int i = 0; i < count; i++) {
                    int next = neighbors[i];
                    if (state.State(next) != CellState.Unseen)
                        continue;

                    state.SetCost(next, state.Cost(current) + grid.StepCost(current, next));
                    state.SetParent(next, current);
                    state.SetState(next, CellState.Open);
                    visits.Add(new Visit(next, VisitKind.Seen));
                    frontier.Add(next);
                }
            }

            List<int> path = state.State(goal) == CellState.Closed ? state.PathTo(goal) : new List<int>();
            foreach (int cell in path)
                visits.Add(new Visit(cell, VisitKind.Path));

            return new SearchResult(path, visits, state, expanded);
        }
    }

    /// <summary>Expands in rings outward, so it finds the path with the fewest steps.</summary>
    public sealed class BreadthFirstSearch : UninformedSearch {
        protected override bool TakeNewest => false;
    }

    /// <summary>
    /// Follows one direction as far as it goes before backing up. Rarely the sensible choice, but
    /// it draws the difference between the strategies better than anything else here.
    /// </summary>
    public sealed class DepthFirstSearch : UninformedSearch {
        protected override bool TakeNewest => true;
    }
}
