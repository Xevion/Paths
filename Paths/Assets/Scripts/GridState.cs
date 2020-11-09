using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Algorithms;

public class GridState {
    private List<List<GridNodeType>> _grid;

    public GridState(NodeGrid grid, IEnumerable<Node> seen, IEnumerable<Node> expanded, Vector2 start, Vector2 end, IReadOnlyCollection<Node> path) {
        this._grid = new List<List<GridNodeType>>(grid.Width);

        // Add walls and empty tiles
        foreach (var x in Enumerable.Range(0, grid.Width - 1)) {
            this._grid.Add(new List<GridNodeType>(grid.Height));
            foreach (var y in Enumerable.Range(0, grid.Height - 1)) {
                Node node = grid.GetNode(x, y);
                this._grid[x].Add(!node.Walkable ? GridNodeType.Wall : GridNodeType.Empty);
            }
        }

        // Add 'seen' tiles
        foreach (Node seenNode in seen) {
            this._grid[(int) seenNode.Position.X][(int) seenNode.Position.Y] = GridNodeType.Seen;
        }

        // Add 'expanded' tiles
        foreach (Node expandedNode in expanded) {
            this._grid[(int) expandedNode.Position.X][(int) expandedNode.Position.Y] = GridNodeType.Expanded;
        }

        // Set start and end tiles
        this._grid[(int) start.X][(int) start.Y] = GridNodeType.Start;
        this._grid[(int) end.X][(int) end.Y] = GridNodeType.End;

        // Add 'path' tiles
        if (path != null)
            foreach (Node pathNode in path)
                this._grid[(int) pathNode.Position.X][(int) pathNode.Position.Y] = GridNodeType.Path;
    }
}