using System;
using Paths.Core;
using UnityEngine;

/// <summary>
/// The pathfinding playback timeline: the recorded ChangeController and the playhead (runtime) we
/// scrub through it with, including where the playhead sits when a re-solve replaces the run.
/// </summary>
public class Playback {
    public float Speed { get; set; }     // steps/sec the playhead advances at
    // ms; above zero it caps the pace to one step per delay
    public float StepDelay { get; set; }
    private readonly float _clampIncrement;
    private float _runtime;
    // whether the end was chosen rather than arrived at, which is what Load follows
    private bool _followEnd;

    public ChangeController State { get; private set; }

    // runtime is a float so movement can be smooth/sub-step; the index is what we actually render
    public float Runtime => _runtime;
    public int CurrentIndex => (int) Math.Ceiling(_runtime);
    public int Count => State?.Count ?? 0;
    // clamp01 so a playhead pinned past a now-shorter run doesn't shove the slider off the end
    public float Fraction => Count > 0 ? Mathf.Clamp01(_runtime / Count) : 0f;
    // what we actually show - the pin can sit past a shortened run, so cap it at the end
    public int DisplayIndex => Math.Min(CurrentIndex, Count);

    // true once the playhead has reached (or passed) the final recorded step
    public bool AtEnd => CurrentIndex >= Count;
    // the Started/Paused loops only bother rendering while there's still timeline left
    public bool HasFramesLeft => _runtime < Count;

    public Playback(float speed, float clampIncrement) {
        Speed = speed;
        _clampIncrement = clampIncrement;
    }

    /// <summary>
    /// Attach a freshly generated run, keeping the playhead parked on the step it was watching. Only
    /// a playhead put at the end on purpose follows a reshaped run to its new end; one the run shrank
    /// past stays where it is, and DisplayIndex clamps what it renders as.
    /// </summary>
    public void Load(ChangeController state) {
        State = state;
        if (_followEnd)
            _runtime = Count;
    }

    public void Rewind() {
        _runtime = 0f;
        _followEnd = false;
    }

    /// <summary>Scrub to a 0..1 position (the progress slider).</summary>
    public void SeekFraction(float t) {
        if (State == null)
            return;
        _runtime = t * Count;
        // scrubbing to the top of the range chooses the end
        _followEnd = t >= 1f;
    }

    /// <summary>Step the playhead forward by a frame's worth of time.</summary>
    public void Advance(float deltaTime) {
        var increment = deltaTime * Speed * CurrentMultiplier();
        // step delay caps the pace: never more than one step per StepDelay milliseconds
        if (StepDelay > 0f)
            increment = Mathf.Min(increment, deltaTime * 1000f / StepDelay);
        if (_clampIncrement > 0)
            increment = Mathf.Clamp(increment, 0, Count * deltaTime / _clampIncrement);
        _runtime += increment;
        // playing off the end is the other way of choosing it
        _followEnd = _runtime >= Count;
    }

    /// <summary>Point the ChangeController at the current index, clamped to the steps it has.</summary>
    public void SyncState() {
        // a repair can land with nothing to step through - the opening picture is already the answer
        if (Count == 0)
            return;
        State.MoveTo(Mathf.Clamp(CurrentIndex, 0, Count - 1));
    }

    // path tiles linger ~5x longer so you can actually watch the final route draw itself in
    private float CurrentMultiplier() {
        if (State.CurrentChangeIndex == -1)
            return 1;
        return State.CurrentChange.New == GridNodeType.Path ? 1 / 5f : 1;
    }
}
