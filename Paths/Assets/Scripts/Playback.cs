using System;
using UnityEngine;

/// <summary>
/// Owns the pathfinding playback timeline - the recorded ChangeController and the playhead
/// (runtime) we scrub through it with. Pulled out of UIController so the play/pause/scrub stuff
/// isn't all tangled up with input and rendering. The pin-on-edit logic will live here too.
/// </summary>
public class Playback {
    private readonly float _speed;
    private readonly float _clampIncrement;
    private float _runtime;

    public ChangeController State { get; private set; }

    // runtime is a float so movement can be smooth/sub-step; the index is what we actually render
    public float Runtime => _runtime;
    public int CurrentIndex => (int) Math.Ceiling(_runtime);
    public int Count => State?.Count ?? 0;
    public float Fraction => Count > 0 ? _runtime / Count : 0f;

    // true once the playhead has reached (or passed) the final recorded step
    public bool AtEnd => CurrentIndex >= Count;
    // the Started/Paused loops only bother rendering while there's still timeline left
    public bool HasFramesLeft => _runtime < Count;

    public Playback(float speed, float clampIncrement) {
        _speed = speed;
        _clampIncrement = clampIncrement;
    }

    /// <summary>Attach a freshly generated run of changes and rewind to the start.</summary>
    public void Load(ChangeController state) {
        State = state;
        _runtime = 0f;
    }

    public void Rewind() => _runtime = 0f;

    /// <summary>Scrub to a 0..1 position (the progress slider).</summary>
    public void SeekFraction(float t) {
        if (State != null)
            _runtime = t * Count;
    }

    /// <summary>Step the playhead forward by a frame's worth of time.</summary>
    public void Advance(float deltaTime) {
        var increment = deltaTime * _speed * CurrentMultiplier();
        if (_clampIncrement > 0)
            increment = Mathf.Clamp(increment, 0, Count * deltaTime / _clampIncrement);
        _runtime += increment;
    }

    /// <summary>Point the ChangeController at the current index, clamped so start/end always render.</summary>
    public void SyncState() {
        State.MoveTo(Mathf.Clamp(CurrentIndex, 1, Count - 1));
    }

    // path tiles linger ~5x longer so you can actually watch the final route draw itself in
    private float CurrentMultiplier() {
        if (State.CurrentChangeIndex == -1)
            return 1;
        return State.CurrentChange.New == GridNodeType.Path ? 1 / 5f : 1;
    }
}
