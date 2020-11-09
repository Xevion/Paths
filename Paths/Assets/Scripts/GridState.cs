using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Algorithms;

public class GridState {
    public List<List<GridNodeType>> Grid;

    public GridState(NodeGrid grid, IEnumerable<Node> seen, IEnumerable<Node> expanded, Vector2 start, Vector2 end, IReadOnlyCollection<Node> path) {
        this.Grid = new List<List<GridNodeType>>(grid.Width);

        // Add walls and empty tiles
        foreach (var x in Enumerable.Range(0, grid.Width - 1)) {
            this.Grid.Add(new List<GridNodeType>(grid.Height));
            foreach (var y in Enumerable.Range(0, grid.Height - 1)) {
                Node node = grid.GetNode(x, y);
                this.Grid[x].Add(!node.Walkable ? GridNodeType.Wall : GridNodeType.Empty);
            }
        }

        // Add 'seen' tiles
        foreach (Node seenNode in seen)
            this.Grid[(int) seenNode.Position.X][(int) seenNode.Position.Y] = GridNodeType.Seen;

        // Add 'expanded' tiles
        foreach (Node expandedNode in expanded)
            this.Grid[(int) expandedNode.Position.X][(int) expandedNode.Position.Y] = GridNodeType.Expanded;


        // Add 'path' tiles
        if (path != null)
            foreach (Node pathNode in path)
                this.Grid[(int) pathNode.Position.X][(int) pathNode.Position.Y] = GridNodeType.Path;
        
        // Set start and end tiles
        this.Grid[(int) start.X][(int) start.Y] = GridNodeType.Start;
        this.Grid[(int) end.X][(int) end.Y] = GridNodeType.End;
    }

    public IEnumerable<GridNodeType> GetNodes() {
        return this.Grid.SelectMany(nodeList => nodeList).ToList();
    }

    public string RenderGrid() {
        string result = "";
        foreach (List<GridNodeType> nodeTypes in this.Grid) {
            result = nodeTypes.Aggregate(result, (current, nodeType) => current + $"{(int) nodeType}") + "\n";
        }
        return result;
    }
}