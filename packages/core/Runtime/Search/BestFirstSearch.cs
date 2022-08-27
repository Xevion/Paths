#nullable enable
using System.Collections.Generic;

namespace Paths.Core.Search {
    /// <summary>
    /// The shared expand loop for the searches that order their frontier by some priority. A*,
    /// Dijkstra and Greedy differ only in what that priority means, so that is all a subclass says.
    ///
    /// Cells are relaxed properly: reaching one more cheaply than before lowers its key in place
    /// rather than leaving the worse route standing. Scoring a cell once at discovery is only
    /// correct when cells come out in cost order, which is true of Dijkstra and not of A*.
    /// </summary>
    public abstract class BestFirstSearch : ISearch {
        protected abstract float Priority(float cost, float estimate);

        /// <summary>Dijkstra turns this off so it never pays for an estimate it ignores.</summary>
        protected virtual bool UsesHeuristic => true;

        public SearchResult Search(Grid grid, int start, int goal, SearchOptions options) {
            var state = new SearchState(grid.CellCount);
            var frontier = new IndexedMinHeap(grid.CellCount);
            // most searches see and expand a good share of the grid, so start the trace near that
            var visits = new List<Visit>(grid.CellCount / 4);
            var neighbors = new int[Grid.MaxNeighbors];
            Coord goalAt = grid.At(goal);
            int expanded = 0;

            state.SetCost(start, 0f);
            state.SetState(start, CellState.Open);
            frontier.Push(start, Priority(0f, Estimate(grid, start, goalAt, options)));

            while (frontier.Count > 0) {
                int current = frontier.Pop();
                state.SetState(current, CellState.Closed);
                visits.Add(new Visit(current, VisitKind.Expanded));
                expanded++;

                if (current == goal)
                    break;

                float costHere = state.Cost(current);
                int count = grid.Neighbors(current, options.Diagonal, neighbors);
                for (int i = 0; i < count; i++) {
                    int next = neighbors[i];
                    if (state.State(next) == CellState.Closed)
                        continue;

                    float cost = costHere + grid.StepCost(current, next);
                    if (cost >= state.Cost(next))
                        continue;

                    state.SetCost(next, cost);
                    state.SetParent(next, current);
                    float priority = Priority(cost, Estimate(grid, next, goalAt, options));

                    // Open means queued, so the two cases are push it or move it up
                    if (state.State(next) == CellState.Unseen) {
                        state.SetState(next, CellState.Open);
                        visits.Add(new Visit(next, VisitKind.Seen));
                        frontier.Push(next, priority);
                    } else {
                        frontier.DecreaseKey(next, priority);
                    }
                }
            }

            List<int> path = state.State(goal) == CellState.Closed ? state.PathTo(goal) : new List<int>();
            foreach (int cell in path)
                visits.Add(new Visit(cell, VisitKind.Path));

            return new SearchResult(path, visits, state, expanded);
        }

        private float Estimate(Grid grid, int cell, Coord goal, SearchOptions options) {
            if (!UsesHeuristic)
                return 0f;
            return Heuristics.Estimate(options.Heuristic, grid.At(cell), goal) * options.HeuristicWeight;
        }
    }
}
