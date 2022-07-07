namespace Game.UI {
    /// <summary>
    /// Global switch for the UI's chrome animations (panel slides, pops, sheens). Off, they snap to their
    /// end state instead. The grid's own cell animation is separate - that's the Display sidebar's toggle.
    /// </summary>
    public static class Motion {
        public static bool Reduced;
    }
}
