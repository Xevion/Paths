using System.Runtime.CompilerServices;

// the interface tests read the solver, the playback and the settings back off UIController to check
// that a control did something. internal beats the reflection they used to do it with
[assembly: InternalsVisibleTo("Paths.Game.Tests")]
