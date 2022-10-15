using System.Runtime.InteropServices;

namespace Game {
    /// <summary>
    /// The browser's cursor grab. Unity asks for it from the animation frame loop, outside the press
    /// the browser insists on, so the request is deferred and then refused. Arming hands the job to a
    /// native mousedown listener instead, which runs inside the gesture.
    /// </summary>
    public static class PointerLock {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void PathsArmPointerLock(int armed);

        [DllImport("__Internal")]
        private static extern void PathsExitPointerLock();
#endif

        /// <summary>Take the cursor on the next press, for a control that is about to want it.</summary>
        public static void Arm(bool armed) {
#if UNITY_WEBGL && !UNITY_EDITOR
            PathsArmPointerLock(armed ? 1 : 0);
#endif
        }

        /// <summary>Give the cursor back.</summary>
        public static void Exit() {
#if UNITY_WEBGL && !UNITY_EDITOR
            PathsExitPointerLock();
#endif
        }
    }
}
