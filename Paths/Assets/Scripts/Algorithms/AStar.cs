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

        public Vector2Int Start { get; private set; }
        public Vector2Int End { get; private set; }

        public AStar(NodeGrid nodeGrid) {
            this._nodeGrid = nodeGrid;
            _states = new List<GridState>();
        }

        public Stack<Node> FindPath(Vector2Int start, Vector2Int end) {
            this.Start = start;
            this.End = end;

            var startNode = new Node(start, true);
            var endNode = new Node(end, true);


            _path = new Stack<Node>();
            _openList = new List<Node>();
            _closedList = new List<Node>();

            RecordState();
            Node current = startNode;

            // add start node to Open List
            _openList.Add(startNode);

            while (_openList.Count != 0 && !_closedList.Exists(x => x.Position == endNode.Position)) {
                current = _openList[0];
                _openList.Remove(current);
                _closedList.Add(current);
                RecordState();

                Node[] adjacentNodes = this._nodeGrid.GetAdjacentNodesArray(current);
                if (true) {
                    for (int i = 0; i < adjacentNodes.Length; i++) {
                        Node n = adjacentNodes[i];
                        if (n == null) continue;

                        if (!_closedList.Contains(n) && n.Walkable) {
                            if (!_openList.Contains(n)) {
                                n.Parent = current;
                                n.DistanceToTarget = NodeGrid.Manhattan(n, endNode);
                                n.Cost = n.Weight + n.Parent.Cost;

                                _openList.Add(n);
                                _openList = _openList.OrderBy(node => node.F).ToList();
                            }
                        }
                    }
                }
            }

            // construct path, if end was not closed return null
            if (!_closedList.Exists(node => node.Position == endNode.Position)) {
                return null;
            }

            // if all good, return path
            Node temp = _closedList[_closedList.IndexOf(current)];
            if (temp == null) return null;
            do {
                _path.Push(temp);
                RecordState();
                temp = temp.Parent;
            } while (temp != startNode && temp != null);

            return _path;
        }

        /// <summary>
        /// Records the current state of the pathfinding algorithm in the grid.
        /// </summary>
        public void RecordState() {
            // TODO: Record pathfinding state information (stages, heuristic, statistical info)
            this._states.Add(
                new GridState(this._nodeGrid, this._openList, this._closedList, Start, End, _path)
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