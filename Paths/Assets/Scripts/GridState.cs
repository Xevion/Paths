﻿using System.Collections.Generic;
using Algorithms;
using UnityEngine;

public class GridState {
    public readonly GridNodeType[,] Grid;
    public readonly float Time;

    public GridState(NodeGrid grid, IEnumerable<Node> seen, IEnumerable<Node> expanded, Vector2Int start,
        Vector2Int end, IReadOnlyCollection<Node> path) {
        this.Time = UnityEngine.Time.realtimeSinceStartup;

        Grid = new GridNodeType[grid.Width, grid.Height];

        // Add walls and empty tiles
        for (int x = 0; x < grid.Width; x++) {
            for (int y = 0; y < grid.Height; y++) {
                Node node = grid.GetNode(x, y);
                Grid[x, y] = node.Walkable ? GridNodeType.Empty : GridNodeType.Wall;
            }
        }

        // Add 'seen' tiles
        foreach (Node seenNode in seen)
            Grid[seenNode.Position.x, seenNode.Position.y] = GridNodeType.Seen;

        // Add 'expanded' tiles
        foreach (Node expandedNode in expanded)
            Grid[expandedNode.Position.x, expandedNode.Position.y] = GridNodeType.Expanded;


        // Add 'path' tiles
        if (path != null)
            foreach (Node pathNode in path)
                Grid[pathNode.Position.x, pathNode.Position.y] = GridNodeType.Path;

        // Set start and end tiles
        Grid[start.x, start.y] = GridNodeType.Start;
        Grid[end.x, end.y] = GridNodeType.End;
    }
}