using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Algorithms {
    public class NodeGrid {
        public readonly Node[,] Grid;
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


            // Fill grid with width*height nodes, zero-indexed
            Grid = new Node[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    Grid[x, y] = new Node(new Vector2Int(x, y), true);

            Width = width;
            Height = height;
        }

        public NodeGrid(Node[,] grid) {
            this.Grid = grid;

            Height = this.Grid.GetLength(0);
            Width = this.Grid.GetLength(1);
        }

        public List<Node> GetAdjacentNodesList(Node node) {
            List<Node> temp = new List<Node>();

            int col = node.Position.x;
            int row = node.Position.y;

            if (row + 1 < Height) temp.Add(Grid[col, row + 1]);
            if (row - 1 >= 0) temp.Add(Grid[col, row - 1]);
            if (col - 1 >= 0) temp.Add(Grid[col - 1, row]);
            if (col + 1 < Width) temp.Add(Grid[col + 1, row]);

            return temp;
        }

        public Node[] GetAdjacentNodesArray(Node node) {
            int col = node.Position.x;
            int row = node.Position.y;

            return new Node[] {
                row + 1 < Height ? Grid[col, row + 1] : null,
                row - 1 >= 0 ? Grid[col, row - 1] : null,
                col - 1 >= 0 ? Grid[col - 1, row] : null,
                col + 1 < Width ? Grid[col + 1, row] : null
            };
        }

        public Node GetNode(Vector2Int position) {
            return Grid[position.x, position.y];
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
            return new Vector2Int(Random.Range(0, Width), Random.Range(0, Height));
        }

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
            return Grid[Random.Range(0, Width), Random.Range(0, Height)];
        }
    }
}