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
    }
}
