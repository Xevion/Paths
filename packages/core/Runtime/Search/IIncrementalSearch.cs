#nullable enable
using System.Collections.Generic;

namespace Paths.Core.Search {
    /// <summary>
    /// A search that keeps what it worked out and repairs it when the grid changes, rather than
    /// starting over. Deliberately not an <see cref="ISearch"/>: that one promises to hold nothing
    /// between calls, which is the exact opposite of the point here.
    ///
    /// Use it as Start once, then Replan after every batch of edits. The result of a Replan is the
    /// same route a fresh search would have found; what differs is how much work it took to get it.
    /// </summary>
    public interface IIncrementalSearch {
        /// <summary>Plan from scratch over this grid, and hold on to the working state.</summary>
        SearchResult Start(Grid grid, int start, int goal, SearchOptions options);

        /// <summary>
        /// Tell the search which cells changed since the last plan. Cheaper than making it find out,
        /// and the caller already knows: an edit is a click on a known cell.
        /// </summary>
        void Touch(IReadOnlyList<int> cells);

        /// <summary>Repair the plan and hand back the current route.</summary>
        SearchResult Replan();
    }
}
