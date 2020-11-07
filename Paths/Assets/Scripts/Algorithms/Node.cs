using System.Numerics;

public class Node {
    // Change this depending on what the desired size is for each element in the grid
    public static int NODE_SIZE = 32;
    public Node Parent;
    public Vector2 Position;

    public Vector2 Center => new Vector2(Position.X + NODE_SIZE / 2, Position.Y + NODE_SIZE / 2);

    public float DistanceToTarget;
    public float Cost;
    public float Weight;

    public float F {
        get {
            if (DistanceToTarget != -1 && Cost != -1)
                return DistanceToTarget + Cost;
            else
                return -1;
        }
    }

    public bool Walkable;

    public Node(Vector2 pos, bool walkable, float weight = 1) {
        Parent = null;
        Position = pos;
        DistanceToTarget = -1;
        Cost = 1;
        Weight = weight;
        Walkable = walkable;
    }
}