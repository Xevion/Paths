using System.Collections.Generic;

namespace Algorithms {
    /// <summary>
    /// Best-first searches that keep the frontier sorted by some priority - A*, Dijkstra, Greedy.
    /// They only differ in what Priority() means (f = g+h, g, or h). Kept as a plain sorted list:
    /// no decrease-key since we never re-open a node, same as the original A*.
    /// </summary>
    public abstract class BestFirst : Pathfinder {
        private readonly List<Node> _frontier = new List<Node>();

        protected BestFirst(NodeGrid nodeGrid, bool diagonal) : base(nodeGrid, diagonal) { }

        protected abstract float Priority(Node node);

        protected override void ClearFrontier() => _frontier.Clear();
        protected override bool FrontierEmpty => _frontier.Count == 0;

        protected override Node Take() {
            Node node = _frontier[0];
            _frontier.RemoveAt(0);
            return node;
        }

        protected override void Add(Node node) {
            // insert keeping the list ascending by priority, so Take() is just index 0
            float p = Priority(node);
            int i = 0;
            while (i < _frontier.Count && Priority(_frontier[i]) <= p)
                i++;
            _frontier.Insert(i, node);
        }
    }
}
