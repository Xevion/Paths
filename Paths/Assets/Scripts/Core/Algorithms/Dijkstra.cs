namespace Algorithms {
    /// <summary>Dijkstra: A* with no heuristic - orders purely by g, the cost so far.</summary>
    public class Dijkstra : BestFirst {
        public Dijkstra(NodeGrid nodeGrid, bool diagonal = false) : base(nodeGrid, diagonal) { }

        // no Estimate override -> heuristic is 0
        protected override float Priority(Node node) => node.Cost.Value;
    }
}
