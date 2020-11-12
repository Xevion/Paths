using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Algorithms {
    public class NodeGrid {
        private List<List<Node>> grid;
        public readonly int Width;
        public readonly int Height;
        public int CellCount => this.Width * this.Height;

        public NodeGrid(int width, int height) {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width),
                    "The width of the grid must be a positive non-zero integer.");
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height),
                    "The height of the grid must be a positive non-zero integer.");

            grid = new List<List<Node>>(width);
            // Fill grid with width*height nodes, zero-indexed
            foreach (int x in Enumerable.Range(0, width - 1)) {
                List<Node> list = new List<Node>(height);
                foreach (int y in Enumerable.Range(0, height - 1))
                    list.Add(new Node(new Vector2Int(x, y), true));

                grid.Add(list);
            }

            Width = this.grid.Count;
            Height = this.grid[0].Count;
        }

        public NodeGrid(List<List<Node>> grid) {
            this.grid = grid;

            Height = this.grid[0].Count;
            Width = this.grid.Count;
        }

        public List<Node> GetAdjacentNodes(Node node) {
            List<Node> temp = new List<Node>();

            int col = node.Position.x;
            int row = node.Position.y;

            if (row + 1 < Height) temp.Add(grid[col][row + 1]);
            if (row - 1 >= 0) temp.Add(grid[col][row - 1]);
            if (col - 1 >= 0) temp.Add(grid[col - 1][row]);
            if (col + 1 < Width) temp.Add(grid[col + 1][row]);

            return temp;
        }
        
        public bool IsValid(int x, int y) {
            return x > 0 && x < Width && y > 0 && y < Height;
        }

        /// <summary>
        /// Retrieves a node at a given coordinate.
        /// </summary>
        /// <param name="x">the X (column) coordinate</param>
        /// <param name="y">the Y (row) coordinate</param>
        /// <returns>A Node object</returns>
        /// <exception cref="ArgumentOutOfRangeException">when the coordinate given does not exist on the grid</exception>
        public Node GetNode(int x, int y) {
            // if(!IsValid(x, y))
            // throw new ArgumentOutOfRangeException();

            return grid[x][y];
        }

        public Node GetNode(Vector2Int position) {
            return GetNode(position.x, position.y);
        }

        /// <summary>
        /// Finds one random walkable cell and turns it into a wall.
        /// </summary>
        public void AddRandomWall() {
            while (true) {
                int x = Random.Range(0, Width - 1);
                int y = Random.Range(0, Height - 1);

                if (grid[x][y].Walkable) {
                    grid[x][y].Walkable = false;
                    return;
                }
            }
        }

        public static float Manhattan(Node a, Node b) {
            return Math.Abs(a.Position.x - b.Position.x) + Math.Abs(a.Position.y - b.Position.y);
        }

        public static float Manhattan(Vector2Int a, Vector2Int b) {
            return Manhattan(new Node(a, false), new Node(b, false));
        }

        /// <summary>
        /// Returns a random Vector2Int position within the grid.
        /// </summary>
        /// <returns>a valid Vector2Int position within the grid</returns>
        public Vector2Int RandomPosition() {
            return new Vector2Int(Random.Range(0, Width - 1), Random.Range(0, Height - 1));
        }

        /// <summary>
        /// Applies a ILevelGenerators's generate function to a NodeGrid
        /// </summary>
        /// <param name="generator">A instantiated level generator (ILevelGenerator) object</param>
        public void ApplyGenerator(ILevelGenerator generator) {
        }

        public IEnumerable<Node> Iterator() {
            for (int x = 0; x < this.grid.Count; x++)
                for (int y = 0; y < this.grid[0].Count; y++)
                    yield return this.grid[x][y];
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
            return grid[Random.Range(0, Width - 1)][Random.Range(0, Height - 1)];
        }
    }
}