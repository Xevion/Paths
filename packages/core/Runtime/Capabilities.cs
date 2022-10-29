#nullable enable
using Paths.Core.Search;

namespace Paths.Core {
    /// <summary>A setting the interface exposes, so it can be asked about one at a time.</summary>
    public enum Control {
        Algorithm,
        Heuristic,
        Weight,
        Diagonal,
        Bidirectional
    }

    /// <summary>What is taking a setting out of play, for the interface to say so in words.</summary>
    public enum Override {
        None,
        /// <summary>The plan is kept and repaired, which is D* Lite's job whatever else is picked.</summary>
        Incremental,
        /// <summary>Searching from both ends replaces the picked search rather than modifying it.</summary>
        Bidirectional,
        /// <summary>Jump Point Search is octile-scored and 8-connected by construction.</summary>
        JumpPoint,
        /// <summary>An uninformed search has no estimate to scale.</summary>
        Uninformed,
        /// <summary>Greedy orders by the estimate and nothing else, so scaling it reorders nothing.</summary>
        Greedy
    }

    /// <summary>
    /// Which settings the configured search actually reads. One table, because the interface needs it
    /// to know what to grey out and the tests need it to know what to hold the solver to; two copies
    /// of this would drift and the greyed-out control would start lying again.
    /// </summary>
    public static class Capabilities {
        /// <summary>Whether changing this control can change what the next Solve produces.</summary>
        public static bool Reads(SolverSettings settings, Control control) =>
            Ignoring(settings, control) == Override.None;

        /// <summary>What is ignoring this control, or None when it is live.</summary>
        public static Override Ignoring(SolverSettings settings, Control control) {
            // a kept plan is D* Lite's, so the picker and everything feeding an estimate sit it out.
            // it walks the grid the connectivity says, so Diagonal still counts.
            if (settings.Incremental)
                return control == Control.Diagonal ? Override.None : Override.Incremental;

            if (settings.Bidirectional)
                return control == Control.Algorithm || control == Control.Heuristic ||
                       control == Control.Weight
                    ? Override.Bidirectional
                    : Override.None;

            switch (control) {
                case Control.Heuristic:
                    return Estimating(settings.Algorithm);
                case Control.Weight:
                    // greedy is the odd one: it reads the estimate but weighs nothing against it, so
                    // scaling every cell by the same amount leaves the order it expands in unchanged
                    Override estimate = Estimating(settings.Algorithm);
                    if (estimate != Override.None)
                        return estimate;
                    return settings.Algorithm == Algorithm.Greedy ? Override.Greedy : Override.None;
                case Control.Diagonal:
                    return settings.Algorithm == Algorithm.JumpPoint ? Override.JumpPoint : Override.None;
                default:
                    return Override.None;
            }
        }

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
