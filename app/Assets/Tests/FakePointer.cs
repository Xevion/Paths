using Game;
using UnityEngine;

namespace Game.Tests {
    /// <summary>A scriptable pointer for tests. Buttons edge-detect against the previous frame, so
    /// a test sets the state, runs a frame, then calls <see cref="NextFrame"/> to close it out.</summary>
    public class FakePointer : IPointerSource {
        public Vector2 Position { get; set; }
        public Vector2 ScrollDelta { get; set; }
        public float ScrollAxis { get; set; }
        public float DragDelta { get; set; }

        private readonly bool[] _held = new bool[3];
        private readonly bool[] _previous = new bool[3];

        public bool Held(int button) => _held[button];
        public bool Pressed(int button) => _held[button] && !_previous[button];
        public bool Released(int button) => !_held[button] && _previous[button];

        public void SetButton(int button, bool down) => _held[button] = down;

        /// <summary>Where the app last asked the cursor to be put.</summary>
        public Vector2 Warped { get; private set; }
        public void WarpTo(Vector2 position) => Warped = position;

        public void Wheel(float notches) {
            ScrollDelta = new Vector2(0f, notches);
            ScrollAxis = notches * 0.1f; // the legacy axis reads smaller than a notch
        }

        public void NextFrame() {
            for (int b = 0; b < _held.Length; b++) _previous[b] = _held[b];
            ScrollDelta = Vector2.zero;
            ScrollAxis = 0f;
            DragDelta = 0f;
        }
    }
}
