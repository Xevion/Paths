namespace Algorithms {
    /// <summary>
    /// Greedy best-first: orders purely by h, the estimate to the goal. Fast and direct, but the
    /// path isn't guaranteed shortest - it just charges at the target.
    /// </summary>
    public class Greedy : BestFirst {
        private readonly Heuristic _heuristic;

        public Greedy(NodeGrid nodeGrid, Heuristic heuristic = Heuristic.Manhattan, bool diagonal = false)
            : base(nodeGrid, diagonal) {
            _heuristic = heuristic;
        }

        protected override float Estimate(Node node, Node goal) => Heuristics.Of(_heuristic, node, goal);
        protected override float Priority(Node node) => node.DistanceToTarget.Value;
    }
}
