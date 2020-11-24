﻿using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Helps manage Change value types by rendering forward/backward movements to a Enum array.
/// </summary>
public class ChangeController {
    private readonly GridNodeType[,] _initial;
    public GridNodeType[,] Current { get; private set; }

    public int Index { get; private set; }
    private readonly List<Change> _changes;
    public int Count => _changes.Count;
    public Change CurrentChange => _changes[Index];

    public ChangeController(GridNodeType[,] initial) {
        _initial = initial;
        Current = initial;
        Index = 0;
        _changes = new List<Change>();
    }

    /// <summary>
    /// Resets the ChangeController back to the initial state.
    /// </summary>
    public void Reset() {
        Current = _initial;
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
                Change cur = _changes[--Index];
                Current[cur.X, cur.Y] = cur.Old;
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
}