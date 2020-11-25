using System.Collections.Generic;
using UnityEngine;

namespace Algorithms {
    public class AStar : IPathfinding {
        public NodeGrid NodeGrid { get; private set; }

        private Stack<Node> _path;
        private List<Node> _openList;
        private List<Node> _closedList;
        public ChangeController ChangeController { get; private set; }

        public Vector2Int Start { get; private set; }
        public Vector2Int End { get; private set; }

        public AStar(NodeGrid nodeGrid) {
            NodeGrid = nodeGrid;
            ChangeController = new ChangeController(nodeGrid.RenderNodeTypes());
        }

        public Stack<Node> FindPath(Vector2Int start, Vector2Int end) {
            this.Start = start;
            this.End = end;

            ChangeController.AddChange(new Change(start.x, start.y, GridNodeType.Start, GridNodeType.Empty));
            ChangeController.AddChange(new Change(end.x, end.y, GridNodeType.End, GridNodeType.Empty));

            Node startNode = NodeGrid.Grid[start.x, start.y];
            Node endNode = NodeGrid.Grid[end.x, end.y];

            _path = new Stack<Node>();
            _openList = new List<Node>();
            _closedList = new List<Node>();

            Node current = startNode;

            // add start node to Open List
            startNode.State = NodeState.Open;
            _openList.Add(startNode);

            while (_openList.Count != 0) {
                current = _openList[0];
                _openList.RemoveAt(0);

                current.State = NodeState.Closed;
                ChangeController.AddChange(new Change(
                    current.Position.x, current.Position.y,
                    GridNodeType.Expanded, GridNodeType.Seen));
                _closedList.Add(current);

                if (current.Position == endNode.Position)
                    break;

                Node[] adjacentNodes = this.NodeGrid.GetAdjacentNodesArray(current);
                for (int i = 0; i < adjacentNodes.Length; i++) {
                    Node node = adjacentNodes[i];
                    if (node != null && node.State == NodeState.None && node.Walkable) {
                        // Setup node & calculate new costs
                        node.Parent = current;
                        node.DistanceToTarget = NodeGrid.Manhattan(node, endNode);
                        node.Cost = node.Weight + node.Parent.Cost;

                        node.State = NodeState.Open;
                        ChangeController.AddChange(new Change(node.Position.x, node.Position.y, GridNodeType.Seen,
                            GridNodeType.Empty));

                        // Insert the new node into the sorted open list in ascending order
                        int index = _openList.BinarySearch(node);
                        if (index < 0) index = ~index;
                        _openList.Insert(index, node);
                    }
                }
            }

            // construct path, if end was not closed return null
            if (!_closedList.Exists(node => node.Position == endNode.Position)) {
                return null;
            }

            // Fix start and end position being overriden
            ChangeController.RemovePositions(start, 1);

            // if all good, return path
            Node temp = _closedList[_closedList.IndexOf(current)];
            if (temp == null) return null;
            do {
                _path.Push(temp);
                ChangeController.AddChange(new Change(temp.Position.x, temp.Position.y, GridNodeType.Path,
                    GridNodeType.Expanded));
                temp = temp.Parent;
            } while (temp != null && !temp.Equals(startNode));

            return _path;
        }

        /// <summary>
        /// Attempts to clean the NodeGrid of all edits made to heuristic values and such, fast.
        /// </summary>
        public void Cleanup() {
            while (_openList.Count > 0) {
                Node node = _openList[0];
                _openList.RemoveAt(0);

                node.Reset();
            }

            while (_closedList.Count > 0) {
                Node node = _closedList[0];
                _closedList.RemoveAt(0);

                node.Reset();
            }
        }
    }
}