using Paths.Core.Search;

namespace Paths.Core {
    /// <summary>
    /// Everything the interface can change about how a solve runs, in one object so a rebuilt solver
    /// can be handed the current set in a single assignment instead of a list of copies somebody has
    /// to remember to extend. Wider than the per-call SearchOptions: this also picks which search
    /// runs and whether a plan is kept between solves.
    /// </summary>
    public sealed class SolverSettings {
        public Algorithm Algorithm = Algorithm.AStar;
        public Heuristic Heuristic = Heuristic.Manhattan;
        public bool Diagonal;
        public float Weight = 1f; // A* heuristic weight, 1 keeps it shortest
        // both modify the picked search where there is a version of them for it, and are dropped
        // where there is not. Capabilities is what knows which is which.
        public bool Bidirectional; // run the search from both ends at once
        public bool Incremental; // repair a kept plan rather than search again

        /// <summary>The part of this one search call receives.</summary>
        public SearchOptions ForSearch() => new SearchOptions(Diagonal, Heuristic, Weight);

        public SolverSettings Clone() => (SolverSettings) MemberwiseClone();

        /// <summary>
        /// Whether a plan built under those settings still answers for these. Every field counts, so
        /// a new one belongs here too; SolverSettingsTests walks the fields and fails until it is.
        /// </summary>
        public bool SameAs(SolverSettings other) =>
            Algorithm == other.Algorithm && Heuristic == other.Heuristic && Diagonal == other.Diagonal &&
            Weight.Equals(other.Weight) && Bidirectional == other.Bidirectional &&
            Incremental == other.Incremental;
    }
}
