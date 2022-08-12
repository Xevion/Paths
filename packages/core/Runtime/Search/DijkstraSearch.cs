#nullable enable
namespace Paths.Core.Search {
    /// <summary>
    /// Orders purely by cost so far. No estimate, so it spreads evenly in every direction and
    /// finds the shortest path whatever the step costs look like.
    /// </summary>
    public sealed class DijkstraSearch : BestFirstSearch {
        protected override bool UsesHeuristic => false;

        protected override float Priority(float cost, float estimate) => cost;
    }
}
