using System;
using System.Collections.Generic;

/// <summary>
/// The recorded timeline of a search, as a picture of the grid plus every change made to it.
/// Playback walks the index back and forth; the picture is kept up to date as it moves.
/// </summary>
public class ChangeController {
    private readonly GridNodeType[,] _initial;
    public GridNodeType[,] Current { get; private set; }
    public bool[,] DirtyFlags { get; private set; }
    public int CurrentChangeIndex { get; private set; }
    private readonly List<Change> _changes;
    public int Count => _changes.Count;
    public Change CurrentChange => _changes[CurrentChangeIndex];

    /// <summary>How long the search took, in seconds.</summary>
    public double Duration { get; }

    /// <summary>
    /// How far into that duration the playhead sits. Changes are recorded after the search has
    /// finished rather than as it runs, so there is no per-change timestamp to read; the elapsed
    /// total is real and this spreads it evenly across the steps.
    /// </summary>
    public double CurrentRuntime => Count == 0 ? 0d : Duration * (CurrentChangeIndex + 1) / Count;

    public ChangeController(GridNodeType[,] initial, double duration = 0d) {
        Duration = duration;
        // keep a pristine copy - Current gets mutated in place as changes are applied, so holding
        // the same array in both meant Reset had nothing left to restore from
        _initial = (GridNodeType[,]) initial.Clone();
        Current = initial;
        CurrentChangeIndex = -1;
        _changes = new List<Change>();
        DirtyFlags = new bool[initial.GetLength(0), initial.GetLength(1)];
        SetDirty();
    }

    /// <summary>
    /// Sets the entire grid as dirty, essentially re-rendering all values in the shader.
    /// </summary>
    public void SetDirty() {
        int width = DirtyFlags.GetLength(0);
        int height = DirtyFlags.GetLength(1);

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                DirtyFlags[x, y] = true;
            }
        }
    }

    /// <summary>
    /// Sets only a specific position as dirty.
    /// </summary>
    /// <param name="position"></param>
    public void SetDirty(int x, int y) {
        DirtyFlags[x, y] = true;
    }

    /// <summary>
    /// Resets the ChangeController back to the initial state.
    /// </summary>
    public void Reset() {
        Current = (GridNodeType[,]) _initial.Clone();
        CurrentChangeIndex = -1;
        SetDirty();
    }

    /// <summary>
    /// Adds a new change to the list.
    /// </summary>
    /// <param name="change">A valid Change value type.</param>
    public void AddChange(Change change) {
        _changes.Add(change);
    }


    /// <summary>
    /// Move the ChangeController's current index forward by a certain number of times.
    /// Positive values only, wil only result in forward movement (if any).
    /// </summary>
    /// <param name="n">The number of times to move forward.</param>
    public void Forward(int n = 1) {
        if (n < 0)
            throw new ArgumentOutOfRangeException(nameof(n));

        for (int i = 0; i < n; i++) {
            Change cur = _changes[++CurrentChangeIndex];
            Current[cur.X, cur.Y] = cur.New;
            SetDirty(cur.X, cur.Y);
        }
    }

    /// <summary>
    /// Move the ChangeController's current index backward by a certain number of times.
    /// Positive values only, will only result in backward movement (if any).
    /// </summary>
    /// <param name="n">The number of times to move backward.</param>
    public void Reverse(int n = 1) {
        if (n < 0)
            throw new ArgumentOutOfRangeException(nameof(n));

        for (int i = 0; i < n; i++) {
            Change cur = _changes[CurrentChangeIndex--]; // post decrement as we apply the current Change's old, not the previous
            Current[cur.X, cur.Y] = cur.Old;
            SetDirty(cur.X, cur.Y);
        }
    }

    /// <summary>
    /// Move the ChangeController's current index by index, forward or backward.
    /// Values can be positive or negative to determine movement. Best used for calculating movement via difference.
    /// </summary>
    /// <param name="n">The number of times to move. Sign determines direction.</param>
    public void Move(int n) {
        if (n > 0)
            Forward(n);
        else if (n < 0)
            Reverse(-n);
    }

    /// <summary>
    /// Move to this specific index in the ChangeController's states.
    /// All changes will be applied to get here, either forward or reverse.
    /// </summary>
    /// <param name="index">The new index to move to.</param>
    /// <exception cref="ArgumentOutOfRangeException">When the index is invalid.</exception>
    public void MoveTo(int index) {
        // check that index is valid at least
        if (index >= _changes.Count || index < 0)
            throw new ArgumentOutOfRangeException(nameof(index),
                $"Cannot move to change index {index}. Only indexes from 0 to {_changes.Count - 1} are valid.");

        Move(index - CurrentChangeIndex);
    }

}