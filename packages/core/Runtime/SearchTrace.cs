#nullable enable
using Paths.Core.Search;

/// <summary>
/// Turns what a search did into the change timeline playback scrubs through.
///
/// This is the seam between the two models. The searches record plain cell visits and know nothing
/// about rendering; the timeline needs an initial picture plus a before-and-after for every step.
/// </summary>
public static class SearchTrace {
    public static ChangeController Build(Grid grid, SearchResult result, int start, int goal, double duration = 0d) {
        GridNodeType[,] initial = Render(grid, start, goal);

        // walk a copy alongside so each change carries the value it actually replaced
        var current = (GridNodeType[,]) initial.Clone();
        var controller = new ChangeController(initial, duration);

        foreach (Visit visit in result.Visits) {
            // structural cells are painted up front and never scribbled over. a repair re-examines
            // the cell just walled off, and a wall drawn as expanded is a wall you cannot see.
            if (visit.Cell == start || visit.Cell == goal || !grid.IsWalkable(visit.Cell))
                continue;

            Coord at = grid.At(visit.Cell);
            GridNodeType become = Kind(visit.Kind);
            GridNodeType was = current[at.X, at.Y];
            if (was == become)
                continue;

            controller.AddChange(new Change(at.X, at.Y, become, was));
            current[at.X, at.Y] = become;
        }

        return controller;
    }

    public static GridNodeType[,] Render(Grid grid, int start, int goal) {
        var rendered = new GridNodeType[grid.Width, grid.Height];
        for (int i = 0; i < grid.CellCount; i++) {
            Coord at = grid.At(i);
            rendered[at.X, at.Y] = grid.IsWalkable(i) ? GridNodeType.Empty : GridNodeType.Wall;
        }

        Coord startAt = grid.At(start);
        Coord goalAt = grid.At(goal);
        rendered[startAt.X, startAt.Y] = GridNodeType.Start;
        rendered[goalAt.X, goalAt.Y] = GridNodeType.End;
        return rendered;
    }

    private static GridNodeType Kind(VisitKind kind) {
        switch (kind) {
            case VisitKind.Seen: return GridNodeType.Seen;
            case VisitKind.Expanded: return GridNodeType.Expanded;
            default: return GridNodeType.Path;
        }
    }
}
