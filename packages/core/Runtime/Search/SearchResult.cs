#nullable enable
using System.Collections.Generic;

namespace Paths.Core.Search {
    public enum VisitKind : byte {
        Seen,
        Expanded,
        Path
    }

    /// <summary>
    /// One thing a search did, in the order it did it. Playback replays these to animate the search,
    /// so the sequence matters as much as the final answer does.
    /// </summary>
    public readonly struct Visit {
        public readonly int Cell;
        public readonly VisitKind Kind;

        public Visit(int cell, VisitKind kind) {
            Cell = cell;
            Kind = kind;
        }

        public override string ToString() => $"{Kind} {Cell}";
    }

    /// <summary>
    /// What a search found, plus the working state it found it with. Holding onto the state lets
    /// callers read per-cell costs afterwards, which is what the distance heatmap draws.
    /// </summary>
    public sealed class SearchResult {
        public readonly IReadOnlyList<int> Path;
        public readonly IReadOnlyList<Visit> Visits;
        public readonly SearchState State;
        public readonly int Expanded;

        public SearchResult(IReadOnlyList<int> path, IReadOnlyList<Visit> visits, SearchState state, int expanded) {
            Path = path;
            Visits = visits;
            State = state;
            Expanded = expanded;
        }

        public bool Found => Path.Count > 0;

        /// <summary>Cost of the path found, or infinity when there wasn't one.</summary>
        public float Cost => Found ? State.Cost(Path[Path.Count - 1]) : float.PositiveInfinity;
    }
}
