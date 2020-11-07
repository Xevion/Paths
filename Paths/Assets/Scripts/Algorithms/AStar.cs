using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class AStar : Pathfinding {
    private Grid grid;

    private Stack<Node> path;
    private List<Node> openList;
    private List<Node> closedList;

    public AStar(Grid grid) {
        this.grid = grid;
    }

    public Tuple<List<Grid>, Stack<Node>> FindPath(Vector2 Start, Vector2 End) {
        Node start = new Node(new Vector2((int) (Start.X / Node.NODE_SIZE), (int) (Start.Y / Node.NODE_SIZE)), true);
        Node end = new Node(new Vector2((int) (End.X / Node.NODE_SIZE), (int) (End.Y / Node.NODE_SIZE)), true);

        path = new Stack<Node>();
        openList = new List<Node>();
        closedList = new List<Node>();
        Node current = start;

        // add start node to Open List
        openList.Add(start);

        while (openList.Count != 0 && !closedList.Exists(x => x.Position == end.Position)) {
            current = openList[0];
            openList.Remove(current);
            closedList.Add(current);
            List<Node> adjacencies = this.grid.GetAdjacentNodes(current);


            foreach (Node n in adjacencies) {
                if (!closedList.Contains(n) && n.Walkable) {
                    if (!openList.Contains(n)) {
                        n.Parent = current;
                        n.DistanceToTarget = Math.Abs(n.Position.X - end.Position.X) +
                                             Math.Abs(n.Position.Y - end.Position.Y);
                        n.Cost = n.Weight + n.Parent.Cost;
                        openList.Add(n);
                        openList = openList.OrderBy(node => node.F).ToList<Node>();
                    }
                }
            }
        }

        // construct path, if end was not closed return null
        if (!closedList.Exists(x => x.Position == end.Position)) {
            return null;
        }

        // if all good, return path
        Node temp = closedList[closedList.IndexOf(current)];
        if (temp == null) return null;
        do {
            path.Push(temp);
            temp = temp.Parent;
        } while (temp != start && temp != null);

        return path;
    }
    
    private StateGrid compileState() {}
}