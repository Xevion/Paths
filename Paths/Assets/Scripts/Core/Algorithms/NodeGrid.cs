using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace Algorithms {
    public class NodeGrid {
        public readonly Node[,] Grid;
        public readonly int Width;
        public readonly int Height;
        public int CellCount => this.Width * this.Height;

        // its own RNG instead of the global UnityEngine.Random - pass a seeded one for reproducible
        // grids (the tests rely on this), leave it null and you get a different layout each run.
        private readonly Random _random;

        public NodeGrid(int width, int height, Random random = null) {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width),
                    "The width of the grid must be a positive non-zero integer.");
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height),
                    "The height of the grid must be a positive non-zero integer.");

            _random = random ?? new Random();

            // Fill grid with width*height nodes, zero-indexed
            Grid = new Node[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    Grid[x, y] = new Node(new Vector2Int(x, y), true);

            Width = width;
            Height = height;
        }

        public NodeGrid(Node[,] grid, Random random = null) {
            this.Grid = grid;
            _random = random ?? new Random();

            Width = this.Grid.GetLength(0);
            Height = this.Grid.GetLength(1);
        }

        /// <summary>
        /// Returns adjacent Node objects in each of the cardinal directions.
        /// Only valid nodes will be included. Invalid positions will return 0 nodes.
        /// </summary>
        /// <param name="node">The node from which adjacents will be found.</param>
        /// <returns>A length 4 or less list containing nodes.</returns>
        /// <seealso cref="GetAdjacentNodesArray"/>
        public List<Node> GetAdjacentNodesList(Node node) {
            List<Node> temp = new List<Node>(4);

            int col = node.Position.x;
            int row = node.Position.y;

            if (row + 1 < Height) temp.Add(Grid[col, row + 1]);
            if (row - 1 >= 0) temp.Add(Grid[col, row - 1]);
            if (col - 1 >= 0) temp.Add(Grid[col - 1, row]);
            if (col + 1 < Width) temp.Add(Grid[col + 1, row]);

            return temp;
        }

        /// <summary>
        /// Returns True if a Vector2Int position is within the grid's boundaries.
        /// </summary>
        /// <param name="position">A Vector2Int coordinate position.</param>
        /// <returns>True if valid and real (a node with the same position exists).</returns>
        public bool IsValid(Vector2Int position) {
            return position.x >= 0 && position.y >= 0 && position.x < Width && position.y < Height;
        }

        /// <summary>
        /// Returns a 1D Array describing the valid nearby nodes.
        /// Invalid nodes will be represented by null.
        /// Currently only returns nodes in cardinal directions (no diagonals).
        /// Only valid positions within the grid are returned.
        /// </summary>
        /// <param name="node">A valid position on the grid.</param>
        /// <returns>A 4 length array containing valid nodes in each of the cardinal directions.</returns>
        /// <seealso cref="GetAdjacentNodesList"/>
        public Node[] GetAdjacentNodesArray(Node node) {
            int col = node.Position.x;
            int row = node.Position.y;

            return new[] {
                row + 1 < Height ? Grid[col, row + 1] : null,
                row - 1 >= 0 ? Grid[col, row - 1] : null,
                col - 1 >= 0 ? Grid[col - 1, row] : null,
                col + 1 < Width ? Grid[col + 1, row] : null
            };
        }

        public Node GetNode(Vector2Int position) {
            return Grid[position.x, position.y];
        }

        /// <summary>
        /// Neighbours used by the searches: the 4 cardinal cells, plus the 4 diagonals when diagonal
        /// is on. Diagonals don't cut corners - both shared orthogonal cells have to be open, else
        /// you get paths squeezing through wall corners which looks wrong.
        /// </summary>
        public List<Node> Neighbors(Node node, bool diagonal) {
            int x = node.Position.x, y = node.Position.y;
            bool up = y + 1 < Height, down = y - 1 >= 0, left = x - 1 >= 0, right = x + 1 < Width;

            var result = new List<Node>(diagonal ? 8 : 4);
            if (up) result.Add(Grid[x, y + 1]);
            if (down) result.Add(Grid[x, y - 1]);
            if (left) result.Add(Grid[x - 1, y]);
            if (right) result.Add(Grid[x + 1, y]);

            if (diagonal) {
                if (up && right && Grid[x, y + 1].Walkable && Grid[x + 1, y].Walkable) result.Add(Grid[x + 1, y + 1]);
                if (up && left && Grid[x, y + 1].Walkable && Grid[x - 1, y].Walkable) result.Add(Grid[x - 1, y + 1]);
                if (down && right && Grid[x, y - 1].Walkable && Grid[x + 1, y].Walkable) result.Add(Grid[x + 1, y - 1]);
                if (down && left && Grid[x, y - 1].Walkable && Grid[x - 1, y].Walkable) result.Add(Grid[x - 1, y - 1]);
            }
            return result;
        }

        public static int Manhattan(Node a, Node b) {
            return Manhattan(a.Position, b.Position);
        }

        public static float Euclidean(Vector2Int a, Vector2Int b) {
            float dx = a.x - b.x, dy = a.y - b.y;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        public static int Chebyshev(Vector2Int a, Vector2Int b) {
            return Math.Max(Math.Abs(a.x - b.x), Math.Abs(a.y - b.y));
        }

        public static int SignedManhattan(Vector2Int a, Vector2Int b) {
            return a.x - b.x + (a.y - b.y);
        }

        public static int Manhattan(Vector2Int a, Vector2Int b) {
            return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);
        }

        /// <summary>
        /// Returns a random Vector2Int position within the grid.
        /// </summary>
        /// <returns>a valid Vector2Int position within the grid</returns>
        public Vector2Int RandomPosition() {
            return new Vector2Int(_random.Next(0, Width), _random.Next(0, Height));
        }

        /// <summary>A random int in [min, max) off the grid's own RNG - generators draw through this.</summary>
        public int Next(int min, int max) => _random.Next(min, max);

        /// <summary>
        /// Applies a ILevelGenerators's generate function to a NodeGrid
        /// </summary>
        /// <param name="generator">A instantiated level generator (ILevelGenerator) object</param>
        public void ApplyGenerator(ILevelGenerator generator) {
            generator.Generate(this);
        }

        public IEnumerable<Node> Iterator() {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    yield return this.Grid[x, y];
        }

        public IEnumerable<Node> Walls() {
            return this.Iterator().Where(node => !node.Walkable);
        }

        public IEnumerable<Node> Empty() {
            return this.Iterator().Where(node => node.Walkable);
        }

        /// <summary>
        /// Returns a random valid node on the grid.
        /// </summary>
        /// <returns>A Node object.</returns>
        public Node GetRandomNode() {
            return Grid[_random.Next(0, Width), _random.Next(0, Height)];
        }

        public static NodeGrid GetFilledNodeGrid(int width, int height) {
            var nodeGrid = new NodeGrid(width, height);

            // Set each Node to a Wall
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    nodeGrid.Grid[x, y].Walkable = false;

            return nodeGrid;
        }

        public bool IsEdge(Vector2Int position) {
            return position.x == 0 || position.x == Width - 1 || position.y == 0 || position.y == Height - 1;
        }

        public void ClearRoom(RectInt room) {
            for (int x = room.xMin; x < room.xMax; x++)
                for (int y = room.yMin; y < room.yMax; y++) {
                    Grid[x, y].Walkable = true;
                }
        }

        public GridNodeType[,] RenderNodeTypes(Vector2Int? start = null, Vector2Int? end = null) {
            GridNodeType[,] nodeTypeGrid = new GridNodeType[Grid.GetLength(0), Grid.GetLength(1)];

            for (int x = 0; x < Grid.GetLength(0); x++) {
                for (int y = 0; y < Grid.GetLength(1); y++) {
                    nodeTypeGrid[x, y] = Grid[x, y].Walkable ? GridNodeType.Empty : GridNodeType.Wall;
                }
            }

            // Start / End node addition
            if (start.HasValue)
                nodeTypeGrid[start.Value.x, start.Value.y] = GridNodeType.Start;

            if (end.HasValue)
                nodeTypeGrid[end.Value.x, end.Value.y] = GridNodeType.End;

            return nodeTypeGrid;
        }
    }
}