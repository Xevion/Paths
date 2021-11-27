using System.Collections.Generic;
using UnityEngine;

namespace Algorithms {
    /// <summary>
    /// Jump Point Search - A* on a uniform-cost 8-connected grid that skips over the long runs of
    /// symmetric, equally-good cells between "jump points", so it touches a fraction of what plain
    /// A* does. Always diagonal, and like the rest of the diagonal movement here it won't cut corners.
    /// </summary>
    public class JumpPoint : IPathfinding {
        public NodeGrid NodeGrid { get; }
        public ChangeController ChangeController { get; }
        public Vector2Int Start { get; private set; }
        public Vector2Int End { get; private set; }

        public JumpPoint(NodeGrid nodeGrid) {
            NodeGrid = nodeGrid;
            ChangeController = new ChangeController(nodeGrid.RenderNodeTypes());
        }

        public Stack<Node> FindPath(Vector2Int start, Vector2Int end) {
            Start = start;
            End = end;
            return null;
        }

        public void Cleanup() { }
    }
}
