using System.Collections.Generic;
using Algorithms;
using UnityEngine;

/// <summary>
/// A general interface for implementing pathfinding algorithms.
/// </summary>
public interface IPathfinding {
    /// <summary>
    /// Finds a path between two positions on the grid. The search it took to get there is recorded
    /// separately, on ChangeController, for playback.
    /// </summary>
    /// <param name="start">The position from which pathfinding begins</param>
    /// <param name="end">The position trying to be found via pathfinding</param>
    /// <returns>The path from end back to start, or null if there isn't one.</returns>
    Stack<Node> FindPath(Vector2Int start, Vector2Int end);
    NodeGrid NodeGrid { get; }
    Vector2Int Start { get; }
    Vector2Int End { get; }
    ChangeController ChangeController { get; }
    void Cleanup();
}