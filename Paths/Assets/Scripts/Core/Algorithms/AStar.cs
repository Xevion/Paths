namespace Algorithms {
    /// <summary>A*: orders the frontier by f = g + h. The classic, and the default.</summary>
    public class AStar : BestFirst {
        private readonly Heuristic _heuristic;

        public AStar(NodeGrid nodeGrid, Heuristic heuristic = Heuristic.Manhattan, bool diagonal = false)
            : base(nodeGrid, diagonal) {
            _heuristic = heuristic;
        }

        protected override float Estimate(Node node, Node goal) => Heuristics.Of(_heuristic, node, goal);
        protected override float Priority(Node node) => node.Cost.Value + node.DistanceToTarget.Value;
    }
}
