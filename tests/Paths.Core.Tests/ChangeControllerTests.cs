using Paths.Core;
using NUnit.Framework;

namespace Paths.Tests {
    public class ChangeControllerTests {
        private static ChangeController WithChanges(int count) {
            var controller = new ChangeController(new GridNodeType[4, 4]);
            for (int i = 0; i < count; i++)
                controller.AddChange(new Change(i % 4, i / 4, GridNodeType.Seen, GridNodeType.Empty));
            return controller;
        }

        [Test]
        public void ResetRestoresTheInitialGrid() {
            ChangeController controller = WithChanges(1);
            controller.Forward();
            Assert.AreEqual(GridNodeType.Seen, controller.Current[0, 0]);

            controller.Reset();

            Assert.AreEqual(GridNodeType.Empty, controller.Current[0, 0]);
            Assert.AreEqual(-1, controller.CurrentChangeIndex);
        }

        // playback scrubs both ways, so walking the whole timeline out and back has to land exactly
        // where it started. anything left behind is a change that reversed into the wrong state.
        [Test]
        public void ForwardThenReverseRestoresTheInitialGrid() {
            ChangeController controller = WithChanges(12);
            controller.Forward(12);
            controller.Reverse(12);

            foreach (GridNodeType cell in controller.Current)
                Assert.AreEqual(GridNodeType.Empty, cell);
            Assert.AreEqual(-1, controller.CurrentChangeIndex);
        }
    }
}
