using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Algorithms {
    public class AStar : IPathfinding {
        private NodeGrid _nodeGrid;

        private Stack<Node> _path;
        private List<Node> _openList;
        private List<Node> _closedList;
        private List<GridState> _states;
        public ChangeController ChangeController { get; private set; }

        public Vector2Int Start { get; private set; }
        public Vector2Int End { get; private set; }

        public AStar(NodeGrid nodeGrid) {
            this._nodeGrid = nodeGrid;
            _states = new List<GridState>();
            ChangeController = new ChangeController(nodeGrid.RenderNodeTypes());
        }

        public Stack<Node> FindPath(Vector2Int start, Vector2Int end) {
            this.Start = start;
            this.End = end;

            ChangeController.AddChange(new Change(start.x, start.y, GridNodeType.Start, GridNodeType.Empty));
            ChangeController.AddChange(new Change(end.x, end.y, GridNodeType.End, GridNodeType.Empty));

            Node startNode = _nodeGrid.Grid[start.x, start.y];
            Node endNode = _nodeGrid.Grid[end.x, end.y];

            _path = new Stack<Node>();
            _openList = new List<Node>();
            _closedList = new List<Node>();

            RecordState();
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

                Node[] adjacentNodes = this._nodeGrid.GetAdjacentNodesArray(current);
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

            // Fix start position being overriden
            ChangeController.RemovePositions(start, 1);

            // if all good, return path
            Node temp = _closedList[_closedList.IndexOf(current)];
            if (temp == null) return null;
            do {
                _path.Push(temp);
                temp = temp.Parent;
            } while (temp != null && !temp.Equals(startNode));

            return _path;
        }

        /// <summary>
        /// Records the current state of the pathfinding algorithm in the grid.
        /// </summary>
        public void RecordState() {
            // TODO: Record pathfinding state information (stages, heuristic, statistical info)
            this._states.Add(
                new GridState(this._nodeGrid, this._openList.ToList(), this._closedList, Start, End, _path)
            );
        }

        /// <summary>
        /// Returns the current list of grid states of the pathfinding algorithm.
        /// </summary>
        /// <returns>A list of GridState objects representing the pathfinding algorithm's progress</returns>
        public List<GridState> GetStates() {
            return this._states;
        }
    }
}