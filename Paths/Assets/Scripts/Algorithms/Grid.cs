using System.Collections.Generic;
using NUnit.Framework.Constraints;

public class Grid {
    private List<List<Node>> grid;

    public Grid(int width, int height) {
        this.grid = new List<List<Node>>();
    }

    public int GridRows => this.grid[0].Count;

    public int GridCols => this.grid.Count;

    public List<Node> GetAdjacentNodes(Node node) {
        List<Node> temp = new List<Node>();

        int row = (int) node.Position.Y;
        int col = (int) node.Position.X;
        
        if (row + 1 < GridRows) temp.Add(this.grid[col][row + 1]);
        if (row - 1 >= 0) temp.Add(this.grid[col][row - 1]);
        if (col - 1 >= 0) temp.Add(this.grid[col - 1][row]);
        if (col + 1 < GridCols) temp.Add(this.grid[col + 1][row]);

        return temp;
    }
}