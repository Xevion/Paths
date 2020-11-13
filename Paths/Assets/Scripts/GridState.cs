using System.Collections.Generic;
using System.Linq;
using Algorithms;
using UnityEngine;

public class GridState {
    public List<List<GridNodeType>> Grid;
    public float time;

    public GridState(NodeGrid grid, IEnumerable<Node> seen, IEnumerable<Node> expanded, Vector2Int start,
        Vector2Int end, IReadOnlyCollection<Node> path) {
        time = Time.realtimeSinceStartup;
        Grid = new List<List<GridNodeType>>(grid.Width);

        // Add walls and empty tiles
        foreach (var x in Enumerable.Range(0, grid.Width - 1)) {
            Grid.Add(new List<GridNodeType>(grid.Height));
            foreach (var y in Enumerable.Range(0, grid.Height - 1)) {
                Node node = grid.GetNode(x, y);
                Grid[x].Add(!node.Walkable ? GridNodeType.Wall : GridNodeType.Empty);
            }
        }

        // Add 'seen' tiles
        foreach (Node seenNode in seen)
            Grid[seenNode.Position.x][seenNode.Position.y] = GridNodeType.Seen;

        // Add 'expanded' tiles
        foreach (Node expandedNode in expanded)
            Grid[expandedNode.Position.x][expandedNode.Position.y] = GridNodeType.Expanded;


        // Add 'path' tiles
        if (path != null)
            foreach (Node pathNode in path)
                Grid[pathNode.Position.y][pathNode.Position.y] = GridNodeType.Path;

        // Set start and end tiles
        Grid[start.x][start.y] = GridNodeType.Start;
        Grid[end.x][end.y] = GridNodeType.End;
    }

    public IEnumerable<GridNodeType> GetNodes() {
        return Grid.SelectMany(nodeList => nodeList).ToList();
    }

    public string RenderGrid() {
        string result = "";
        foreach (List<GridNodeType> nodeTypes in Grid) {
            result = nodeTypes.Aggregate(result, (current, nodeType) => current + $"{(int) nodeType}") + "\n";
        }

        return result;
    }
}