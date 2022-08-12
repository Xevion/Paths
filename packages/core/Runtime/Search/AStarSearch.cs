#nullable enable
namespace Paths.Core.Search {
    /// <summary>Orders by cost so far plus estimate to the goal. The default.</summary>
    public sealed class AStarSearch : BestFirstSearch {
        protected override float Priority(float cost, float estimate) => cost + estimate;
    }
}
