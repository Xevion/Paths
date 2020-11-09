using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Algorithms {
    public class NodeGrid {
        private List<List<Node>> grid;
        public readonly int Width;
        public readonly int Height;

        public NodeGrid(int width, int height) {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width),
                    $"The width of the grid must be a positive non-zero integer.");
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height),
                    $"The height of the grid must be a positive non-zero integer.");

            this.grid = new List<List<Node>>(width);
            // Fill grid with width*height nodes, zero-indexed
            foreach (int x in Enumerable.Range(0, width - 1)) {
                List<Node> list = new List<Node>(height);
                foreach (int y in Enumerable.Range(0, height))
                    list.Add(new Node(new Vector2(x, y), true));

                this.grid.Add(list);
            }

            this.Width = width;
            this.Height = height;
        }

        public NodeGrid(List<List<Node>> grid) {
            this.grid = grid;

            this.Height = this.grid[0].Count;
            this.Width = this.grid.Count;
        }

        public IEnumerable<Node> GetAdjacentNodes(Node node) {
            List<Node> temp = new List<Node>();

            int row = (int) node.Position.Y;
            int col = (int) node.Position.X;

            if (row + 1 < Height) temp.Add(this.grid[col][row + 1]);
            if (row - 1 >= 0) temp.Add(this.grid[col][row - 1]);
            if (col - 1 >= 0) temp.Add(this.grid[col - 1][row]);
            if (col + 1 < Width) temp.Add(this.grid[col + 1][row]);

            return temp;
        }

        public bool IsValid(int x, int y) {
            return x > 0 && x < this.Width && y > 0 && y < this.Height;
        }
        
        /// <summary>
        /// Retrieves a node at a given coordinate.
        /// </summary>
        /// <param name="x">the X (column) coordinate</param>
        /// <param name="y">the Y (row) coordinate</param>
        /// <returns>A Node object</returns>
        /// <exception cref="ArgumentOutOfRangeException">when the coordinate given does not exist on the grid</exception>
        public Node GetNode(int x, int y) {
            if(!IsValid(x, y))
                throw new ArgumentOutOfRangeException();
            
            return this.grid[x][y];
        }

        public static float Manhattan(Node first, Node second) {
            return Math.Abs(first.Position.X - second.Position.X) + Math.Abs(first.Position.Y - second.Position.Y);
        }
    }
}