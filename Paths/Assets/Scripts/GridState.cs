using System.Collections.Generic;
using System.Linq;
using Algorithms;
using UnityEngine;

public struct GridState {
    public readonly GridNodeType[,] Grid;
    public readonly float Time;

    public GridState(NodeGrid grid, IReadOnlyList<Node> seen, IReadOnlyList<Node> expanded, Vector2Int start,
        Vector2Int end, Stack<Node> path) {
        this.Time = UnityEngine.Time.realtimeSinceStartup;

        Grid = new GridNodeType[grid.Width, grid.Height];

        // Add walls and empty tiles
        for (int x = 0; x < grid.Width; x++) {
            for (int y = 0; y < grid.Height; y++) {
                Grid[x, y] = grid.Grid[x, y].Walkable ? GridNodeType.Empty : GridNodeType.Wall;
            }
        }

        // Add 'seen' tiles
        int length = seen.Count();
        for (int i = 0; i < length; i++) {
            Node seenNode = seen[i];
            Grid[seenNode.Position.x, seenNode.Position.y] = GridNodeType.Seen;
        }

        // Add 'expanded' tiles
        length = expanded.Count();
        for (int i = 0; i < length; i++) {
            Node expandedNode = expanded[i];
            Grid[expandedNode.Position.x, expandedNode.Position.y] = GridNodeType.Expanded;
        }

        // Add 'path' tiles
        if (path != null) {
            Node[] pathArray = path.ToArray();
            length = pathArray.Length;
            for (int i = 0; i < length; i++) {
                Node pathNode = pathArray[i];
                Grid[pathNode.Position.x, pathNode.Position.y] = GridNodeType.Path;
            }
        }

        // Set start and end tiles
        Grid[start.x, start.y] = GridNodeType.Start;
        Grid[end.x, end.y] = GridNodeType.End;
    }
}