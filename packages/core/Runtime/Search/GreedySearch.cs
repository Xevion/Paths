#nullable enable
namespace Paths.Core.Search {
    /// <summary>
    /// Orders purely by the estimate, ignoring what the route has cost so far. Charges at the goal
    /// and gets there fast, but what it finds is not the shortest path.
    /// </summary>
    public sealed class GreedySearch : BestFirstSearch {
        protected override float Priority(float cost, float estimate) => estimate;

        // it never looks at what a cell costs to enter, so terrain does not steer it
        public override bool HonorsWeights => false;
    }
}
