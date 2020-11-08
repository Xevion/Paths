using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Algorithms;

public class AStar : IPathfinding {
    private NodeGrid _nodeGrid;

    private Stack<Node> _path;
    private List<Node> _openList;
    private List<Node> _closedList;
    
    public AStar(NodeGrid nodeGrid) {
        this._nodeGrid = nodeGrid;
    }

    public Tuple<List<NodeGrid>, Stack<Node>> FindPath(Vector2 Start, Vector2 End) {
        var start = new Node(Start, true);
        var end = new Node(End, true);

        _path = new Stack<Node>();
        _openList = new List<Node>();
        _closedList = new List<Node>();
        Node current = start;

        // add start node to Open List
        _openList.Add(start);

        while (_openList.Count != 0 && !_closedList.Exists(x => x.Position == end.Position)) {
            current = _openList[0];
            _openList.Remove(current);
            _closedList.Add(current);
            List<Node> adjacentNodes = this._nodeGrid.GetAdjacentNodes(current);


            foreach (Node n in adjacentNodes) {
                if (!_closedList.Contains(n) && n.Walkable) {
                    if (!_openList.Contains(n)) {
                        n.Parent = current;
                        n.DistanceToTarget = NodeGrid.Manhattan(n, end);
                        n.Cost = n.Weight + n.Parent.Cost;
                        _openList.Add(n);
                        _openList = _openList.OrderBy(node => node.F).ToList<Node>();
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
            temp = temp.Parent;
        } while (temp != start && temp != null);

        return _path;
    }

    private StateGrid compileState() {
    }
}