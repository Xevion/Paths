using System;
using System.Collections.Generic;
using System.Linq;
using Algorithms;
using NUnit.Framework.Constraints;
using UnityEngine.SocialPlatforms;

public class NodeGrid {
    private List<List<Node>> grid;
    private readonly int _width;
    private readonly int _height;

    public NodeGrid(int width, int height) {
        if(width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), $"The width of the grid must be a positive non-zero integer.");
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), $"The height of the grid must be a positive non-zero integer.");

        this.grid = new List<List<Node>>(width);
        for(int _ in Enumerable.Range(0, height - 1)) {}
        
        this._width = width;
        this._height = height;
    }

    public NodeGrid(List<List<Node>> grid) {
        this.grid = grid;

        this._height = this.grid[0].Count;
        this._width = this.grid.Count;
    }

    public List<Node> GetAdjacentNodes(Node node) {
        List<Node> temp = new List<Node>();

        int row = (int) node.Position.Y;
        int col = (int) node.Position.X;

        if (row + 1 < _height) temp.Add(this.grid[col][row + 1]);
        if (row - 1 >= 0) temp.Add(this.grid[col][row - 1]);
        if (col - 1 >= 0) temp.Add(this.grid[col - 1][row]);
        if (col + 1 < _width) temp.Add(this.grid[col + 1][row]);

        return temp;
    }

    public static float Manhattan(Node first, Node second) {
        return Math.Abs(first.Position.X - second.Position.X) + Math.Abs(first.Position.Y - second.Position.Y);
    }
}