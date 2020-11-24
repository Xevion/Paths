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

            while (_openList.Count != 0) {
                current = _openList.First();
                _openList.Remove(current);

                current.State = NodeState.Closed;
                _closedList.Add(current);
                
                if (current.Position == endNode.Position)
                    break;
                
                RecordState();

                Node[] adjacentNodes = this._nodeGrid.GetAdjacentNodesArray(current);
                for (int i = 0; i < adjacentNodes.Length; i++) {
                    Node node = adjacentNodes[i];
                    if (node != null && node.State == NodeState.None && node.Walkable) {
                        // Setup node & calculate new costs
                        node.Parent = current;
                        node.DistanceToTarget = NodeGrid.Manhattan(node, endNode);
                        node.Cost = node.Weight + node.Parent.Cost;
                        
                        // Set to open and add to open list (sorted)
                        node.State = NodeState.Open;
                        // _openList.Add(node);

                        int index = _openList.BinarySearch(node);
                        if (index < 0) index = ~index;
                        _openList.Insert(index, node);
                        // _openList = _openList.OrderBy(n => n.F).ToList();
                        // _openList.Sort((n, o) => n.F.CompareTo(o.F));
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