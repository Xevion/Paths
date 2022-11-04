using Paths.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests {
    /// <summary>
    /// The playhead against runs of every length. A repair can be a handful of steps or none at all,
    /// which is far shorter than the full search the timeline was written against.
    /// </summary>
    public class PlaybackTests {
        private const int Size = 4;

        private static ChangeController Trace(int changes) {
            var initial = new GridNodeType[Size, Size];
            var controller = new ChangeController(initial);
            for (int i = 0; i < changes; i++)
                controller.AddChange(new Change(i % Size, i / Size, GridNodeType.Expanded, GridNodeType.Empty));
            return controller;
        }

        [Test]
        public void ShortRunsDoNotThrowWhereverThePlayheadIs([Values(0, 1, 2, 3, 12)] int changes,
                                                             [Values(0f, 0.5f, 1f, 40f)] float runtime) {
            var playback = new Playback(10f, 0f);
            playback.Load(Trace(changes));
            playback.Advance(runtime); // speed 10, so a short run is left behind

            Assert.DoesNotThrow(() => playback.SyncState(),
                $"{changes} changes with the playhead at {playback.Runtime}");
        }

        [Test]
        public void PlayingToTheEndAppliesEveryChange([Values(1, 2, 12)] int changes) {
            var playback = new Playback(1000f, 0f);
            playback.Load(Trace(changes));
            playback.Advance(1f);
            playback.SyncState();

            Assert.AreEqual(changes - 1, playback.State.CurrentChangeIndex);
        }

        /// <summary>A run with nothing in it still shows its opening picture rather than blanking.</summary>
        [Test]
        public void AnEmptyRunKeepsItsOpeningPicture() {
            var initial = new GridNodeType[Size, Size];
            initial[1, 1] = GridNodeType.Wall;
            var playback = new Playback(10f, 0f);
            playback.Load(new ChangeController(initial));
            playback.SyncState();

            Assert.AreEqual(GridNodeType.Wall, playback.State.Current[1, 1]);
        }

        /// <summary>
        /// An edit that shortens the run past the playhead must not capture it. Inferring "was at the
        /// end" from the pin's position self-sustains: reading the pin against a shorter run says yes,
        /// and parking it on that end says yes again for every edit after.
        /// </summary>
        [Test]
        public void AnEditThatShortensTheRunDoesNotCaptureThePlayhead() {
            var playback = new Playback(100f, 0f);
            playback.Load(Trace(400));
            playback.Advance(1f); // speed 100, so the pin lands at 100 of 400
            float pinned = playback.Runtime;
            Assert.Less(pinned, 400f, "the playhead played to the end, so this proves nothing");

            playback.Load(Trace(6));
            playback.Load(Trace(400));

            Assert.AreEqual(pinned, playback.Runtime, 1e-3f,
                "a run that came up short took the playhead with it");
        }

        /// <summary>
        /// The other half: a playhead put at the end on purpose still follows a run that grows. The
        /// overshoot is small on purpose, so a fix that only leaves the pin alone cannot pass this.
        /// </summary>
        [Test]
        public void APlayheadPlayedToTheEndFollowsALongerRun() {
            var playback = new Playback(13f, 0f);
            playback.Load(Trace(12));
            playback.Advance(1f);
            Assert.IsTrue(playback.AtEnd, "the playhead did not reach the end, so this proves nothing");
            Assert.Less(playback.Runtime, 400f, "the overshoot already covers the longer run");

            playback.Load(Trace(400));

            Assert.IsTrue(playback.AtEnd, "watching the finish stopped following it");
        }

        /// <summary>A scrub to the top of the range is the same choice, and follows too.</summary>
        [Test]
        public void AScrubToTheEndFollowsALongerRunAndAScrubBackDoesNot() {
            var playback = new Playback(10f, 0f);
            playback.Load(Trace(12));
            playback.SeekFraction(1f);
            playback.Load(Trace(400));
            Assert.IsTrue(playback.AtEnd, "scrubbing to the end did not park there");

            playback.SeekFraction(0.5f);
            playback.Load(Trace(40));
            Assert.AreEqual(200f, playback.Runtime, 1e-3f, "scrubbing back still followed the end");
        }

        // a shorter run has to pull the slider back with it, not leave it off the end
        [Test]
        public void TheSliderStaysInRangeWhenTheRunShortens() {
            var playback = new Playback(1000f, 0f);
            playback.Load(Trace(400));
            playback.Advance(1f);
            playback.Load(Trace(6));

            Assert.LessOrEqual(playback.Fraction, 1f);
            Assert.GreaterOrEqual(playback.Fraction, 0f);
            Assert.DoesNotThrow(() => playback.SyncState());
        }
    }
}
