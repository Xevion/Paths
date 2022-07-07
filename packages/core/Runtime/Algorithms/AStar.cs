namespace Algorithms {
    /// <summary>A*: orders the frontier by f = g + h. The classic, and the default.</summary>
    public class AStar : BestFirst {
        private readonly Heuristic _heuristic;
        private readonly float _weight;

        public AStar(NodeGrid nodeGrid, Heuristic heuristic = Heuristic.Manhattan, bool diagonal = false,
            float weight = 1f) : base(nodeGrid, diagonal) {
            _heuristic = heuristic;
            _weight = weight;
        }

        protected override float Estimate(Node node, Node goal) => Heuristics.Of(_heuristic, node, goal);
        // weighted A*: w>1 leans on the heuristic, expanding fewer nodes for a possibly-suboptimal path
        protected override float Priority(Node node) => node.Cost.Value + _weight * node.DistanceToTarget.Value;
    }
}
