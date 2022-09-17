#nullable enable
using System.Collections.Generic;

namespace Paths.Core.Search {
    /// <summary>
    /// Dijkstra run from both ends at once, stopping when the two waves have met and neither can
    /// improve on the meeting. Two frontiers each cover a smaller area than one wave crossing the
    /// whole distance, which is where the saving comes from.
    ///
    /// The old version kept the forward search on the shared node fields and the backward search on
    /// its own local arrays, so the two halves were written twice over. Both are SearchStates now.
    /// </summary>
    public sealed class BidirectionalSearch : ISearch {
        public bool HonorsWeights => true;

        public SearchResult Search(Grid grid, int start, int goal, SearchOptions options) {
            var forward = new SearchState(grid.CellCount);
            var backward = new SearchState(grid.CellCount);
            var visits = new List<Visit>(grid.CellCount / 4);
            var neighbors = new int[Grid.MaxNeighbors];
            int expanded = 0;

            if (start == goal) {
                forward.SetCost(start, 0f);
                forward.SetState(start, CellState.Closed);
                var only = new List<int> { start };
                visits.Add(new Visit(start, VisitKind.Expanded));
                visits.Add(new Visit(start, VisitKind.Path));
                return new SearchResult(only, visits, forward, 1);
            }

            var openForward = new IndexedMinHeap(grid.CellCount);
            var openBackward = new IndexedMinHeap(grid.CellCount);

            forward.SetCost(start, 0f);
            forward.SetState(start, CellState.Open);
            openForward.Push(start, 0f);

            backward.SetCost(goal, 0f);
            backward.SetState(goal, CellState.Open);
            openBackward.Push(goal, 0f);

            float best = float.PositiveInfinity;
            int meetForward = SearchState.NoParent;
            int meetBackward = SearchState.NoParent;

            while (openForward.Count > 0 && openBackward.Count > 0) {
                float lowForward = forward.Cost(openForward.Peek());
                float lowBackward = backward.Cost(openBackward.Peek());

                // neither wave can reach anything cheaper than what they have already met at
                if (lowForward + lowBackward >= best)
                    break;

                // expand whichever wave is shallower, so the two stay balanced
                bool goForward = lowForward <= lowBackward;
                SearchState side = goForward ? forward : backward;
                SearchState other = goForward ? backward : forward;
                IndexedMinHeap open = goForward ? openForward : openBackward;

                int current = open.Pop();
                side.SetState(current, CellState.Closed);
                visits.Add(new Visit(current, VisitKind.Expanded));
                expanded++;

                float costHere = side.Cost(current);
                int count = grid.Neighbors(current, options.Diagonal, neighbors);
                for (int i = 0; i < count; i++) {
                    int next = neighbors[i];
                    if (side.State(next) == CellState.Closed)
                        continue;

                    // the backward wave walks edges the wrong way round, and a cell charges to be
                    // entered, so its step is priced as the forward move it stands for
                    float step = goForward ? grid.StepCost(current, next) : grid.StepCost(next, current);
                    float cost = costHere + step;

                    if (cost < side.Cost(next)) {
                        bool discovered = side.State(next) == CellState.Unseen;
                        side.SetCost(next, cost);
                        side.SetParent(next, current);
                        if (discovered) {
                            side.SetState(next, CellState.Open);
                            visits.Add(new Visit(next, VisitKind.Seen));
                            open.Push(next, cost);
                        } else {
                            open.DecreaseKey(next, cost);
                        }
                    }

                    // the other wave has already reached this cell, so the edge closes a route
                    float fromOther = other.Cost(next);
                    if (!float.IsPositiveInfinity(fromOther) && costHere + step + fromOther < best) {
                        best = costHere + step + fromOther;
                        meetForward = goForward ? current : next;
                        meetBackward = goForward ? next : current;
                    }
                }
            }

            if (meetForward == SearchState.NoParent)
                return new SearchResult(new List<int>(), visits, forward, expanded);

            List<int> path = Stitch(forward, backward, meetForward, meetBackward);
            // the forward half never reached the goal, so record what the whole route actually cost
            forward.SetCost(goal, best);
            foreach (int cell in path)
                visits.Add(new Visit(cell, VisitKind.Path));

            return new SearchResult(path, visits, forward, expanded);
        }

        /// <summary>Start to the meeting edge off the forward tree, then across it and out to the goal.</summary>
        private static List<int> Stitch(SearchState forward, SearchState backward, int meetForward, int meetBackward) {
            var path = new List<int>();
            for (int cell = meetForward; cell != SearchState.NoParent; cell = forward.Parent(cell))
                path.Add(cell);
            path.Reverse();
            for (int cell = meetBackward; cell != SearchState.NoParent; cell = backward.Parent(cell))
                path.Add(cell);
            return path;
        }
    }
}
