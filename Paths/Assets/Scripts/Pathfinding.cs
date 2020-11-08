using System;
using System.Collections.Generic;
using System.Numerics;
using Algorithms;

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
    Tuple<List<NodeGrid>, Stack<Node>> FindPath(Vector2 start, Vector2 end);
}