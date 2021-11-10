namespace Algorithms {
    /// <summary>Distance estimates the informed searches (A*, Greedy) can use toward the goal.</summary>
    public enum Heuristic {
        Manhattan,
        Euclidean,
        Chebyshev
    }

    public static class Heuristics {
        public static float Of(Heuristic heuristic, Node a, Node b) {
            switch (heuristic) {
                case Heuristic.Euclidean:
                    return NodeGrid.Euclidean(a.Position, b.Position);
                case Heuristic.Chebyshev:
                    return NodeGrid.Chebyshev(a.Position, b.Position);
                default:
                    return NodeGrid.Manhattan(a.Position, b.Position);
            }
        }
    }
}
