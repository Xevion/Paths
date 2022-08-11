#nullable enable
namespace Paths.Core.Search {
    /// <summary>
    /// The knobs a search reads. Not every search reads all of them: the uninformed ones ignore the
    /// heuristic entirely, and Jump Point Search is 8-connected by construction so it ignores
    /// <see cref="Diagonal"/> too.
    /// </summary>
    public readonly struct SearchOptions {
        public readonly bool Diagonal;
        public readonly Heuristic Heuristic;

        /// <summary>
        /// Multiplier on the estimate. Above 1 the search leans on the heuristic and expands fewer
        /// cells, at the price of no longer guaranteeing the shortest path.
        /// </summary>
        public readonly float HeuristicWeight;

        public SearchOptions(bool diagonal = false, Heuristic heuristic = Heuristic.Manhattan,
            float heuristicWeight = 1f) {
            Diagonal = diagonal;
            Heuristic = heuristic;
            HeuristicWeight = heuristicWeight;
        }

        public SearchOptions With(bool diagonal) =>
            new SearchOptions(diagonal, Heuristic, HeuristicWeight);
    }
}
