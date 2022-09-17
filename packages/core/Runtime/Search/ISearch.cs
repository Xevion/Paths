#nullable enable
namespace Paths.Core.Search {
    /// <summary>
    /// A one-shot search over a grid.
    ///
    /// Implementations hold no state between calls: everything a run needs lives in the SearchState
    /// it builds and hands back inside the result. That is what lets the same instance be reused,
    /// and two searches run over one grid at once.
    /// </summary>
    public interface ISearch {
        SearchResult Search(Grid grid, int start, int goal, SearchOptions options);

        /// <summary>
        /// Whether this one prices cells by what they cost to enter. The ones that do not are not
        /// broken, they answer a different question: breadth-first counts steps and Jump Point Search
        /// has uniform costs built into the rules it skips cells by. Either way the route they return
        /// is not the cheapest one once terrain is on the grid, so the app says as much rather than
        /// quietly showing a worse path.
        /// </summary>
        bool HonorsWeights { get; }
    }
}
