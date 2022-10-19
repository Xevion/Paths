using System.Collections.Generic;
using NUnit.Framework;
using Paths.Core.Search;

namespace Paths.Tests.Search {
    /// <summary>
    /// The searches the app can actually pick, as test cases. Anything that holds for a search holds
    /// for all of them, so a new one joining the picker joins every matrix that reads this.
    /// </summary>
    internal static class Searches {
        public static IEnumerable<TestCaseData> All() {
            yield return Case("A*", new AStarSearch());
            yield return Case("Dijkstra", new DijkstraSearch());
            yield return Case("Greedy", new GreedySearch());
            yield return Case("BFS", new BreadthFirstSearch());
            yield return Case("DFS", new DepthFirstSearch());
            yield return Case("JPS", new JumpPointSearch());
            yield return Case("Bidirectional", new BidirectionalSearch());
            yield return Case("Incremental", new PlanOnce());
        }

        private static TestCaseData Case(string name, ISearch search) =>
            new TestCaseData(name, search).SetName($"{{m}}({name})");

        /// <summary>
        /// D* Lite behind the one-shot interface, so the incremental search sits in the same matrices
        /// as the rest. Only its first plan; the repairs are exercised where edits are the subject.
        /// </summary>
        private sealed class PlanOnce : ISearch {
            public bool HonorsWeights => true;

            public SearchResult Search(Grid grid, int start, int goal, SearchOptions options) =>
                new DStarLite().Start(grid, start, goal, options);
        }
    }
}
