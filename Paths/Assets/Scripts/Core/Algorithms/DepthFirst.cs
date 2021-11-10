using System.Collections.Generic;

namespace Algorithms {
    /// <summary>DFS: LIFO frontier, dives as deep as it can before backtracking. Rarely a short path, fun to watch.</summary>
    public class DepthFirst : Pathfinder {
        private readonly Stack<Node> _frontier = new Stack<Node>();

        public DepthFirst(NodeGrid nodeGrid, bool diagonal = false) : base(nodeGrid, diagonal) { }

        protected override void ClearFrontier() => _frontier.Clear();
        protected override bool FrontierEmpty => _frontier.Count == 0;
        protected override void Add(Node node) => _frontier.Push(node);
        protected override Node Take() => _frontier.Pop();
    }
}
