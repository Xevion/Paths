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
                foreach (int y in Enumerable.Range(0, height))
                    list.Add(new Node(new Vector2Int(x, y), true));

                grid.Add(list);
            }

            Width = width;
            Height = height;
        }

        public NodeGrid(List<List<Node>> grid) {
            this.grid = grid;

            Height = this.grid[0].Count;
            Width = this.grid.Count;
        }

        public List<Node> GetAdjacentNodes(Node node) {
            List<Node> temp = new List<Node>();

            int row = node.Position.y;
            int col = node.Position.x;

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

        public void FlipRandomWall() {
            grid[Random.Range(0, Width - 1)][Random.Range(0, Height - 1)].Walkable = false;
        }

        public static float Manhattan(Node first, Node second) {
            return Math.Abs(first.Position.x - second.Position.x) + Math.Abs(first.Position.y - second.Position.y);
        }

        public static float Manhattan(Vector2Int algorithmStart, Vector2Int algorithmEnd) {
            return Manhattan(new Node(algorithmStart, false), new Node(algorithmEnd, false));
        }

        /// <summary>
        /// Returns a random Vector2Int position within the grid.
        /// </summary>
        /// <returns>a valid Vector2Int position within the grid</returns>
        public Vector2Int RandomPosition() {
            return new Vector2Int(Random.Range(0, Width - 1), Random.Range(0, Height - 1));
        }
    }
}