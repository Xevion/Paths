using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Helps manage Change value types by rendering forward/backward movements to a Enum array.
/// </summary>
public class ChangeController {
    private readonly GridNodeType[,] _initial;
    public GridNodeType[,] Current { get; private set; }
    public bool[,] DirtyFlags { get; private set; }
    public int Index { get; private set; }
    private readonly List<Change> _changes;
    public int Count => _changes.Count;
    public Change CurrentChange => _changes[Index];
    public double CurrentRuntime => _changes[Index].Time - _changes[0].Time;

    public ChangeController(GridNodeType[,] initial) {
        _initial = initial;
        Current = initial;
        Index = -1;
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
    public void SetDirty(Vector2Int position) {
        DirtyFlags[position.x, position.y] = true;
    }

    /// <summary>
    /// Resets the ChangeController back to the initial state.
    /// </summary>
    public void Reset() {
        Current = _initial;
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
    /// Move the ChangeController's current index forward by index.
    /// Positive values only, wil only result in forward movement (if any).
    /// </summary>
    /// <param name="n">The number of times to move forward.</param>
    public void Forward(int n = 1) {
        if (n < 0)
            throw new ArgumentOutOfRangeException(nameof(n));

        for (int i = 0; i < n; i++) {
            Change cur = _changes[++Index];
            Current[cur.X, cur.Y] = cur.New;
            SetDirty(new Vector2Int(cur.X, cur.Y));
        }
    }

    /// <summary>
    /// Move the ChangeController's current index backward by index.
    /// Positive values only, will only result in backward movement (if any).
    /// </summary>
    /// <param name="n">The number of times to move backward.</param>
    public void Reverse(int n = 1) {
        if (n < 0)
            throw new ArgumentOutOfRangeException(nameof(n));

        if (n > 5 && Index - n == 0)
            Reset(); // resetting by copying values instead of mutating might be easier.
        else {
            for (int i = 0; i < n; i++) {
                Change cur = _changes[Index--]; // post decrement as we apply the current Change's old, not the previous
                Current[cur.X, cur.Y] = cur.Old;
                SetDirty(new Vector2Int(cur.X, cur.Y));
            }
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

        // diff & move to
        int diff = index - Index;
        if (diff != 0)
            // prefer resetting if index is 0 and it needs to move at least some.
            if (index == 0 && diff > 5)
                Reset();
            else
                Move(diff);
    }

    /// <summary>
    /// Removes all Change values referencing a specific position.
    /// Intended for fixing start and end positions.
    /// Works in reverse, i.e. count = 1 removes the last position if any.
    /// </summary>
    /// <param name="position">The Vector2Int position to look for.</param>
    /// <param name="count">Maximum number of Change values to remove. -1 for all.</param>
    public void RemovePositions(Vector2Int position, int count = -1) {
        if (count == 0)
            return;

        for (int i = _changes.Count - 1; i >= 0; i--)
            if (_changes[i].X == position.x && _changes[i].Y == position.y) {
                _changes.RemoveAt(i);

                // Return if the count is now zero.
                if (--count == 0)
                    return;
            }
    }
}