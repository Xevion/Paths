using System.Collections.Generic;
using UnityEngine;

namespace Algorithms {
    /// <summary>
    /// Shared guts of the grid searches - the expand loop, the Seen/Expanded/Path change recording
    /// that playback replays, path reconstruction, and cleanup. Subclasses only decide how the
    /// frontier orders nodes (priority / queue / stack) and, if informed, what heuristic to use.
    /// Pulled out of the old standalone AStar so the others aren't five copies of the same loop.
    /// </summary>
    public abstract class Pathfinder : IPathfinding {
        public NodeGrid NodeGrid { get; }
        public ChangeController ChangeController { get; }
        public Vector2Int Start { get; private set; }
        public Vector2Int End { get; private set; }

        protected readonly bool Diagonal;
        private readonly List<Node> _touched = new List<Node>();
        private Stack<Node> _path;

        private const float DiagonalCost = 1.41421356f; // sqrt(2)

        protected Pathfinder(NodeGrid nodeGrid, bool diagonal) {
            NodeGrid = nodeGrid;
            Diagonal = diagonal;
            ChangeController = new ChangeController(nodeGrid.RenderNodeTypes());
        }

        // frontier hooks - the only thing that actually differs between algorithms
        protected abstract void ClearFrontier();
        protected abstract void Add(Node node);
        protected abstract Node Take();
        protected abstract bool FrontierEmpty { get; }

        // informed searches (A*, Greedy) override this; the rest never look at the goal
        protected virtual float Estimate(Node node, Node goal) => 0f;

        public Stack<Node> FindPath(Vector2Int start, Vector2Int end) {
            Start = start;
            End = end;

            ChangeController.AddChange(new Change(start.x, start.y, GridNodeType.Start, GridNodeType.Empty));
            ChangeController.AddChange(new Change(end.x, end.y, GridNodeType.End, GridNodeType.Empty));

            Node startNode = NodeGrid.Grid[start.x, start.y];
            Node endNode = NodeGrid.Grid[end.x, end.y];

            _path = new Stack<Node>();
            ClearFrontier();

            startNode.Cost = 0;
            startNode.DistanceToTarget = Estimate(startNode, endNode);
            startNode.State = NodeState.Open;
            Touch(startNode);
            Add(startNode);

            Node current = startNode;
            while (!FrontierEmpty) {
                current = Take();
                if (current.State == NodeState.Closed)
                    continue;

                current.State = NodeState.Closed;
                ChangeController.AddChange(new Change(current.Position.x, current.Position.y,
                    GridNodeType.Expanded, GridNodeType.Seen));

                if (current.Position == end)
                    break;

                foreach (Node node in NodeGrid.Neighbors(current, Diagonal)) {
                    if (!node.Walkable || node.State != NodeState.None)
                        continue;

                    node.Parent = current;
                    node.Cost = current.Cost + StepCost(current, node);
                    node.DistanceToTarget = Estimate(node, endNode);
                    node.State = NodeState.Open;
                    ChangeController.AddChange(new Change(node.Position.x, node.Position.y,
                        GridNodeType.Seen, GridNodeType.Empty));
                    Touch(node);
                    Add(node);
                }
            }

            // never reached the goal -> no path
            if (current.Position != end)
                return null;

            Node temp = current;
            do {
                _path.Push(temp);
                ChangeController.AddChange(new Change(temp.Position.x, temp.Position.y,
                    GridNodeType.Path, GridNodeType.Expanded));
                temp = temp.Parent;
            } while (temp != null && !temp.Equals(startNode));

            // patch start/end back over the Seen/Expanded/Path scribbles (same fix A* always used)
            ChangeController.RemovePositions(start, 1);
            ChangeController.RemovePositions(end, 3);

            return _path;
        }

        private static float StepCost(Node from, Node to) {
            bool diagonal = from.Position.x != to.Position.x && from.Position.y != to.Position.y;
            return diagonal ? DiagonalCost : 1f;
        }

        private void Touch(Node node) => _touched.Add(node);

        /// <summary>Undo everything this search scribbled on the shared grid so the next one is clean.</summary>
        public void Cleanup() {
            foreach (Node node in _touched)
                node.Reset();
            _touched.Clear();
            ClearFrontier();
        }
    }
}
