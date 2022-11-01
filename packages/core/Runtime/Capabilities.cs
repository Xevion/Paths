#nullable enable
using Paths.Core.Search;

namespace Paths.Core {
    /// <summary>A setting the interface exposes, so it can be asked about one at a time.</summary>
    public enum Control {
        Algorithm,
        Heuristic,
        Weight,
        Diagonal,
        Bidirectional,
        Incremental
    }

    /// <summary>What is taking a setting out of play, for the interface to say so in words.</summary>
    public enum Override {
        None,
        /// <summary>There is no version of this modifier for the search that is picked.</summary>
        Algorithm,
        /// <summary>A repaired plan runs one wave, so there is no second one to send from the goal.</summary>
        Incremental,
        /// <summary>Jump Point Search is octile-scored and 8-connected by construction.</summary>
        JumpPoint,
        /// <summary>An uninformed search has no estimate to consult.</summary>
        Uninformed,
        /// <summary>Greedy orders by the estimate and nothing else, so scaling it reorders nothing.</summary>
        Greedy
    }

    /// <summary>
    /// Which settings the configured search actually reads. One table, because the interface needs it
    /// to know what to grey out, the solver needs it to know what to apply, and the tests need it to
    /// know what to hold the solver to; three copies of this would drift.
    ///
    /// The algorithm is the choice and everything else modifies it. A modifier with no version for the
    /// picked search is dropped, never applied in its place, so picking a search always gets that
    /// search. The reverse, letting a switch stand in for the picker, is what this replaced.
    /// </summary>
    public static class Capabilities {
        /// <summary>Whether changing this control can change what the next Solve produces.</summary>
        public static bool Reads(SolverSettings settings, Control control) =>
            Ignoring(settings, control) == Override.None;

        /// <summary>What is ignoring this control, or None when it is live.</summary>
        public static Override Ignoring(SolverSettings settings, Control control) {
            switch (control) {
                case Control.Algorithm:
                    // never taken out of play, whatever else is set. the modifiers below give way
                    // to it rather than the other way round.
                    return Override.None;
                case Control.Heuristic:
                    return Estimating(settings.Algorithm);
                case Control.Weight:
                    Override estimate = Estimating(settings.Algorithm);
                    if (estimate != Override.None)
                        return estimate;
                    // greedy is the odd one: it reads the estimate but weighs nothing against it, so
                    // scaling every cell by the same amount leaves the order it expands in unchanged
                    return settings.Algorithm == Algorithm.Greedy ? Override.Greedy : Override.None;
                case Control.Diagonal:
                    return settings.Algorithm == Algorithm.JumpPoint ? Override.JumpPoint : Override.None;
                case Control.Bidirectional:
                    Override twoEnded = TwoEnded(settings.Algorithm);
                    if (twoEnded != Override.None)
                        return twoEnded;
                    return settings.Incremental ? Override.Incremental : Override.None;
                case Control.Incremental:
                    return Repairable(settings.Algorithm);
                default:
                    return Override.None;
            }
        }

        // the searches there is a two-ended version of. BidirectionalSearch is Dijkstra from both
        // ends; sending an informed wave each way needs a termination test that is not written yet,
        // and the one here is only sound where the estimate is zero.
        private static Override TwoEnded(Algorithm algorithm) =>
            algorithm == Algorithm.Dijkstra ? Override.None : Override.Algorithm;

        // the searches there is an incremental version of. DStarLite settles the cells a Dijkstra
        // would whatever estimate it is handed, so it stands in for that one and no other.
        private static Override Repairable(Algorithm algorithm) =>
            algorithm == Algorithm.Dijkstra ? Override.None : Override.Algorithm;

        // whether the search consults an estimate at all
        private static Override Estimating(Algorithm algorithm) {
            switch (algorithm) {
                case Algorithm.Dijkstra:
                case Algorithm.BreadthFirst:
                case Algorithm.DepthFirst:
                    return Override.Uninformed;
                case Algorithm.JumpPoint:
                    return Override.JumpPoint;
                default:
                    return Override.None;
            }
        }
    }
}
