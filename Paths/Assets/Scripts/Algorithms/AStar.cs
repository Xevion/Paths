using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vector2 = System.Numerics.Vector2;

namespace Algorithms {
    public class AStar : IPathfinding {
        private NodeGrid _nodeGrid;

        private Stack<Node> _path;
        private List<Node> _openList;
        private List<Node> _closedList;
        private List<GridState> _states;

        private Vector2 _start;
        private Vector2 _end;

        public AStar(NodeGrid nodeGrid) {
            this._nodeGrid = nodeGrid;
            _states = new List<GridState>();
        }

        public Stack<Node> FindPath(Vector2 Start, Vector2 End) {
            this._start = Start;
            this._end = End;

            var start = new Node(Start, true);
            var end = new Node(End, true);
            

            _path = new Stack<Node>();
            _openList = new List<Node>();
            _closedList = new List<Node>();
            
            RecordState();
            Node current = start;

            // add start node to Open List
            _openList.Add(start);

            while (_openList.Count != 0 && !_closedList.Exists(x => x.Position == end.Position)) {
                current = _openList[0];
                _openList.Remove(current);
                _closedList.Add(current);
                RecordState();

                IEnumerable<Node> adjacentNodes = this._nodeGrid.GetAdjacentNodes(current);

                foreach (Node n in adjacentNodes) {
                    if (!_closedList.Contains(n) && n.Walkable) {
                        if (!_openList.Contains(n)) {
                            n.Parent = current;
                            n.DistanceToTarget = NodeGrid.Manhattan(n, end);
                            n.Cost = n.Weight + n.Parent.Cost;

                            _openList.Add(n);
                            _openList = _openList.OrderBy(node => node.F).ToList();

                            RecordState();
                        }
                    }
                }
            }

            // construct path, if end was not closed return null
            if (!_closedList.Exists(x => x.Position == end.Position)) {
                return null;
            }

            // if all good, return path
            Node temp = _closedList[_closedList.IndexOf(current)];
            if (temp == null) return null;
            do {
                _path.Push(temp);
                RecordState();
                temp = temp.Parent;
            } while (temp != start && temp != null);

            return _path;
        }

        /// <summary>
        /// Records the current state of the pathfinding algorithm in the grid.
        /// </summary>
        public void RecordState() {
            // TODO: Record basic grid node types
            // TODO: Record timing information
            // TODO: Record pathfinding state information (stages, heuristic, statistical info)
            this._states.Add(
                new GridState(this._nodeGrid, this._openList, this._closedList, _start, _end, _path)
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