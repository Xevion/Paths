﻿/// <summary>
/// A Change struct represents a Change in the grid's rendering.
/// This struct remembers the original and new GridNodeType, the time it was recorded and of course the relevant position.
/// </summary>
public readonly struct Change {
    public readonly int X;
    public readonly int Y;
    public readonly GridNodeType New;
    public readonly GridNodeType Old;
    public readonly float Time;

    public Change(int x, int y, GridNodeType newType, GridNodeType oldType) {
        this.X = x;
        this.Y = y;
        this.New = newType;
        this.Old = oldType;
        this.Time = UnityEngine.Time.realtimeSinceStartup;
    }

    public override string ToString() {
        return $"Change({X}, {Y}, {Old} -> {New})";
    }
}