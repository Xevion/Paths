using UnityEngine;

namespace Game {
    /// <summary>Where key state comes from. Swappable so tests can drive the app without a keyboard.</summary>
    public interface IKeySource {
        bool Held(KeyCode key);
        bool Pressed(KeyCode key);
    }

    public class KeyboardSource : IKeySource {
        public bool Held(KeyCode key) => Input.GetKey(key);
        public bool Pressed(KeyCode key) => Input.GetKeyDown(key);
    }

    /// <summary>
    /// The app's keyboard, the pointer seam's opposite number. Reading a key here says nothing about who
    /// should act on it - KeyboardFocus.UiOwns answers that, and the app's shortcuts go through both.
    /// </summary>
    public static class KeyInput {
        public static IKeySource Source = new KeyboardSource();

        public static bool Held(KeyCode key) => Source.Held(key);
        public static bool Pressed(KeyCode key) => Source.Pressed(key);

        /// <summary>Go back to the real keyboard.</summary>
        public static void Reset() => Source = new KeyboardSource();
    }
}
