/// <summary>
/// Describes the type of an individual node on the grid
/// This assists with rendering of the many states of the graph.
/// </summary>
public enum GridNodeType {
    Empty,
    Wall,
    Start,
    End,
    Seen,
    Expanded,
    Path
}