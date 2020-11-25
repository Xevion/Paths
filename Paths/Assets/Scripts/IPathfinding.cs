using System.Collections.Generic;
using Algorithms;
using UnityEngine;

/// <summary>
/// A general interface for implementing pathfinding algorithms.
/// </summary>
public interface IPathfinding {
    /// <summary>
    /// Finds a valid path between two Node objects.
    /// </summary>
    /// <param name="start">The position from which pathfinding begins</param>
    /// <param name="end">The position trying to be found via pathfinding</param>
    /// <returns>A List of NodeGridGrid objects representing the timeline of Pathfinding</returns>
    Stack<Node> FindPath(Vector2Int start, Vector2Int end);

    Vector2Int Start { get; }
    Vector2Int End { get; }
    ChangeController ChangeController { get; }
}