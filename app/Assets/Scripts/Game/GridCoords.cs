using Paths.Core.Search;
using UnityEngine;

namespace Game {
    /// <summary>
    /// Unity speaks Vector2Int and the pathfinding core speaks Coord. Nothing but the boundary
    /// between them should care, so the conversion lives here rather than in either side.
    /// </summary>
    public static class GridCoords {
        public static Coord ToCoord(this Vector2Int position) => new Coord(position.x, position.y);

        public static Vector2Int ToVector(this Coord position) => new Vector2Int(position.X, position.Y);
    }
}
