using System.Collections.Generic;

namespace Algorithms {
    /// <summary>BFS: FIFO frontier, expands in rings out from the start. Shortest path on an unweighted grid.</summary>
    public class BreadthFirst : Pathfinder {
        private readonly Queue<Node> _frontier = new Queue<Node>();

        public BreadthFirst(NodeGrid nodeGrid, bool diagonal = false) : base(nodeGrid, diagonal) { }

        protected override void ClearFrontier() => _frontier.Clear();
        protected override bool FrontierEmpty => _frontier.Count == 0;
        protected override void Add(Node node) => _frontier.Enqueue(node);
        protected override Node Take() => _frontier.Dequeue();
    }
}
