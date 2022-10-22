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
