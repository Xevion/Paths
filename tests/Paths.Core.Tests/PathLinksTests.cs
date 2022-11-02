using System.Collections.Generic;
using NUnit.Framework;
using Paths.Core;
using Paths.Core.Search;
using Paths.Tests.Search;
using static Paths.Tests.Search.SearchCorpus;

namespace Paths.Tests {
    /// <summary>
    /// The shape of the drawn route, which is a different question from where it goes.
    ///
    /// Two invariants below cannot fail on a route built from its own steps: it has no diagonal
    /// bit to set without a diagonal step, and no corner cut without a step the grid refuses. They
    /// guard against a search handing one out, and their positive control is <see cref="Adjacency"/>,
    /// which violates both.
    /// </summary>
    public class PathLinksTests {
        private const int Size = 12;
        private static readonly int[] Seeds = { 3, 7, 11 };

        private static (Grid grid, IReadOnlyList<int> path) Run(ISearch search, int seed, bool diagonal) {
            Grid grid = Seeded(seed, Size, Size, 0.28);
            int start = grid.Index(new Coord(0, 0));
            int goal = grid.Index(new Coord(Size - 1, Size - 1));
            SearchResult result = search.Search(grid, start, goal, new SearchOptions(diagonal, Heuristic.Octile));
            return (grid, result.Path);
        }

        private static IEnumerable<(Grid grid, IReadOnlyList<int> path, string where)> Corpus(
            string name, ISearch search) {
            foreach (bool diagonal in new[] { false, true })
                foreach (int seed in Seeds) {
                    var (grid, path) = Run(search, seed, diagonal);
                    if (path.Count > 1)
                        yield return (grid, path, $"{name} seed {seed} diagonal {diagonal}");
                }
        }

        /// <summary>
        /// Eight neighbours, eight distinct bits, all inside a byte. The renderer decodes this
        /// order from its own copy, so a change here is a change there.
        /// </summary>
        [Test]
        public void EveryNeighbourGetsItsOwnBit() {
            var seen = new Dictionary<int, string>();
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++) {
                    if (dx == 0 && dy == 0) continue;
                    int bit = PathLinks.Bit(dx, dy);
                    Assert.AreNotEqual(0, bit, $"({dx}, {dy}) has no bit");
                    Assert.LessOrEqual(bit, 128, $"({dx}, {dy}) does not fit in a byte");
                    Assert.IsFalse(seen.ContainsKey(bit),
                        $"({dx}, {dy}) shares bit {bit} with {(seen.TryGetValue(bit, out string other) ? other : "")}");
                    seen[bit] = $"({dx}, {dy})";
                    Assert.IsTrue(PathLinks.Linked((byte) bit, dx, dy), $"({dx}, {dy}) does not decode back");
                }
            Assert.AreEqual(8, seen.Count);
        }

        /// <summary>
        /// The centre is the one that matters: fall through to the arithmetic and it collides with
        /// a real direction, so a route standing still would draw a segment sideways.
        /// </summary>
        [TestCase(0, 0)]
        [TestCase(2, 0)]
        [TestCase(0, -2)]
        [TestCase(-2, 2)]
        [TestCase(1, 5)]
        public void AStepThatIsNotToANeighbourJoinsNothing(int dx, int dy) {
            Assert.AreEqual(0, PathLinks.Bit(dx, dy));
            Assert.IsFalse(PathLinks.Linked(0xFF, dx, dy));
        }

        /// <summary>
        /// Each segment is drawn by every cell it crosses, so a one-sided join draws half a
        /// segment, stopping dead at the cell border.
        /// </summary>
        [TestCaseSource(typeof(Searches), nameof(Searches.All))]
        public void BothCellsOfAStepAgreeItHappened(string name, ISearch search) {
            foreach (var (grid, path, where) in Corpus(name, search)) {
                byte[,] links = PathLinks.Of(grid, path);
                Each(grid, links, (x, y, dx, dy) => {
                    var to = new Coord(x + dx, y + dy);
                    Assert.IsTrue(grid.InBounds(to), $"({x}, {y}) joins off the grid on {where}");
                    Assert.IsTrue(PathLinks.Linked(links[to.X, to.Y], -dx, -dy),
                        $"({x}, {y}) joins ({to.X}, {to.Y}) but not the other way round on {where}");
                });
            }
        }

        /// <summary>
        /// Every step is a join and nothing else is. The second half is the one adjacency fails.
        /// </summary>
        [TestCaseSource(typeof(Searches), nameof(Searches.All))]
        public void TheJoinsAreExactlyTheStepsTheRouteTook(string name, ISearch search) {
            foreach (var (grid, path, where) in Corpus(name, search)) {
                byte[,] links = PathLinks.Of(grid, path);

                var steps = new HashSet<(int, int, int, int)>();
                for (int i = 1; i < path.Count; i++) {
                    Coord a = grid.At(path[i - 1]), b = grid.At(path[i]);
                    steps.Add((a.X, a.Y, b.X, b.Y));
                    steps.Add((b.X, b.Y, a.X, a.Y));
                    Assert.IsTrue(PathLinks.Linked(links[a.X, a.Y], b.X - a.X, b.Y - a.Y),
                        $"step {i} of the route is not a join on {where}");
                }

                Each(grid, links, (x, y, dx, dy) =>
                    Assert.IsTrue(steps.Contains((x, y, x + dx, y + dy)),
                        $"({x}, {y}) joins ({x + dx}, {y + dy}), which the route never stepped between, on {where}"));
            }
        }

        /// <summary>
        /// A route visits each cell once, so the shape is one open line: two joins along it, one at
        /// each end. A third is the reported bug, drawn as a spur off the side of the ribbon.
        /// </summary>
        [TestCaseSource(typeof(Searches), nameof(Searches.All))]
        public void TheShapeIsOneLineWithTwoEnds(string name, ISearch search) {
            foreach (var (grid, path, where) in Corpus(name, search)) {
                byte[,] links = PathLinks.Of(grid, path);
                Coord first = grid.At(path[0]), last = grid.At(path[path.Count - 1]);

                for (int x = 0; x < grid.Width; x++)
                    for (int y = 0; y < grid.Height; y++) {
                        int degree = Degree(links[x, y]);
                        var at = new Coord(x, y);
                        int expected = at == first || at == last ? 1 : OnRoute(grid, path, at) ? 2 : 0;
                        Assert.AreEqual(expected, degree, $"({x}, {y}) has {degree} joins on {where}");
                    }
            }
        }

        /// <summary>
        /// Both cells open, and a diagonal's two corners open too, which is how the grid steps.
        /// </summary>
        [TestCaseSource(typeof(Searches), nameof(Searches.All))]
        public void NoJoinCutsThroughAWall(string name, ISearch search) {
            foreach (var (grid, path, where) in Corpus(name, search)) {
                byte[,] links = PathLinks.Of(grid, path);
                Each(grid, links, (x, y, dx, dy) => {
                    Assert.IsTrue(grid.IsWalkable(grid.Index(new Coord(x, y))), $"({x}, {y}) joins out of a wall on {where}");
                    Assert.IsTrue(grid.IsWalkable(grid.Index(new Coord(x + dx, y + dy))),
                        $"({x}, {y}) joins into a wall on {where}");
                    if (dx == 0 || dy == 0)
                        return;
                    Assert.IsTrue(grid.IsWalkable(grid.Index(new Coord(x + dx, y))),
                        $"the join from ({x}, {y}) cuts the corner at ({x + dx}, {y}) on {where}{Render(grid, path)}");
                    Assert.IsTrue(grid.IsWalkable(grid.Index(new Coord(x, y + dy))),
                        $"the join from ({x}, {y}) cuts the corner at ({x}, {y + dy}) on {where}{Render(grid, path)}");
                });
            }
        }

        /// <summary>
        /// Read off the route rather than the Diagonal setting on purpose: Jump Point Search moves
        /// diagonally whatever that setting says, so the setting would erase real segments.
        /// </summary>
        [TestCaseSource(typeof(Searches), nameof(Searches.All))]
        public void ARouteWithNoDiagonalStepIsNeverDrawnWithOne(string name, ISearch search) {
            foreach (var (grid, path, where) in Corpus(name, search)) {
                bool stepped = false;
                for (int i = 1; i < path.Count; i++) {
                    Coord a = grid.At(path[i - 1]), b = grid.At(path[i]);
                    if (a.X != b.X && a.Y != b.Y)
                        stepped = true;
                }
                if (stepped)
                    continue;

                byte[,] links = PathLinks.Of(grid, path);
                Each(grid, links, (x, y, dx, dy) =>
                    Assert.IsTrue(dx == 0 || dy == 0,
                        $"({x}, {y}) is drawn joining diagonally on a route with no diagonal step, on {where}"));
            }
        }

        /// <summary>Nothing found means nothing drawn, so the last route does not linger.</summary>
        [Test]
        public void AFailedSearchLeavesNoJoins() {
            var grid = new Grid(5, 5);
            for (int y = 0; y < 5; y++)
                grid.SetWalkable(grid.Index(new Coord(2, y)), false);
            int start = grid.Index(new Coord(0, 0));
            int goal = grid.Index(new Coord(4, 4));

            SearchResult result = new AStarSearch().Search(grid, start, goal, new SearchOptions(true, Heuristic.Octile));
            Assume.That(!result.Found, "the wall should have cut the grid in two");

            byte[,] links = PathLinks.Of(grid, result.Path);
            for (int x = 0; x < 5; x++)
                for (int y = 0; y < 5; y++)
                    Assert.AreEqual(0, links[x, y], $"({x}, {y}) is joined to something with no route found");
        }

        /// <summary>
        /// The reported case. A and B sit either side of a wall corner, seven steps apart along the
        /// route; adjacency joins them anyway, diagonally, on a route with no diagonal step.
        /// </summary>
        [Test]
        public void ARouteFoldingPastItselfIsNotJoinedAcrossTheGap() {
            Grid grid = Corridor();
            int start = grid.Index(new Coord(1, 3));
            int goal = grid.Index(new Coord(3, 1));
            SearchResult result = new AStarSearch().Search(grid, start, goal, new SearchOptions(false, Heuristic.Manhattan));
            Assume.That(result.Found, "the corridor should be walkable end to end");

            var a = new Coord(1, 2);
            var b = new Coord(2, 1);

            byte[,] guessed = Adjacency.Of(grid, result.Path);
            Assert.IsTrue(PathLinks.Linked(guessed[a.X, a.Y], 1, -1),
                "the witness has stopped witnessing: adjacency no longer joins across the corner");
            Assert.AreEqual(3, Degree(guessed[a.X, a.Y]), "the spurious join should give the cell a third one");

            byte[,] links = PathLinks.Of(grid, result.Path);
            Assert.IsFalse(PathLinks.Linked(links[a.X, a.Y], 1, -1),
                "the route is joined across a wall corner it never stepped over");
            Assert.IsFalse(PathLinks.Linked(links[b.X, b.Y], -1, 1), "and not from the other side either");
            Assert.AreEqual(2, Degree(links[a.X, a.Y]));
            Assert.AreEqual(2, Degree(links[b.X, b.Y]));
        }

        /// <summary>
        /// The same mistake without a diagonal. A hairpin lays two stretches of one route side by
        /// side, and adjacency joins them rung by rung into a filled block.
        /// </summary>
        [Test]
        public void TwoStretchesOfOneRouteSideBySideAreNotJoinedToEachOther() {
            var grid = new Grid(2, 4);
            int start = grid.Index(new Coord(0, 0));
            int goal = grid.Index(new Coord(1, 0));
            var path = new List<int>();
            foreach (Coord at in new[] {
                new Coord(0, 0), new Coord(0, 1), new Coord(0, 2), new Coord(0, 3),
                new Coord(1, 3), new Coord(1, 2), new Coord(1, 1), new Coord(1, 0)
            })
                path.Add(grid.Index(at));
            Assert.AreEqual(start, path[0]);
            Assert.AreEqual(goal, path[path.Count - 1]);

            byte[,] guessed = Adjacency.Of(grid, path);
            Assert.IsTrue(PathLinks.Linked(guessed[0, 1], 1, 0),
                "the witness has stopped witnessing: adjacency no longer joins the two stretches");

            byte[,] links = PathLinks.Of(grid, path);
            for (int y = 0; y < 3; y++)
                Assert.IsFalse(PathLinks.Linked(links[0, y], 1, 0),
                    $"the two stretches are joined across at y={y}, which the route never stepped over");
            Assert.AreEqual(2, Degree(links[0, 1]));
            Assert.AreEqual(2, Degree(links[0, 2]));
        }

        /// <summary>
        /// Ordinary grids reach this, so the fix is not aimed at a case only a test can build:
        /// across 20 seeds and both diagonal modes, adjacency was wrong on 6 of 126 routes.
        /// All six came from Greedy or depth-first, which only wander because a route has to pass
        /// close to itself for this to bite.
        /// </summary>
        [TestCaseSource(nameof(Wanderers))]
        public void AdjacencyGoesWrongOnOrdinaryGridsToo(string name, ISearch search, int seed, bool diagonal) {
            Grid grid = Seeded(seed, 16, 16, 0.3);
            int start = grid.Index(new Coord(0, 0));
            int goal = grid.Index(new Coord(15, 15));
            IReadOnlyList<int> path = search
                .Search(grid, start, goal, new SearchOptions(diagonal, Heuristic.Octile)).Path;
            Assume.That(path.Count > 1, "this seed has no route to draw");

            var steps = new HashSet<(int, int, int, int)>();
            for (int i = 1; i < path.Count; i++) {
                Coord a = grid.At(path[i - 1]), b = grid.At(path[i]);
                steps.Add((a.X, a.Y, b.X, b.Y));
                steps.Add((b.X, b.Y, a.X, a.Y));
            }
            string where = $"{name} seed {seed} diagonal {diagonal}";

            int guessedWrong = 0;
            Each(grid, Adjacency.Of(grid, path), (x, y, dx, dy) => {
                if (!steps.Contains((x, y, x + dx, y + dy))) guessedWrong++;
            });
            Assert.Greater(guessedWrong, 0, $"adjacency no longer goes wrong on {where}, so this case witnesses nothing");

            Each(grid, PathLinks.Of(grid, path), (x, y, dx, dy) =>
                Assert.IsTrue(steps.Contains((x, y, x + dx, y + dy)),
                    $"({x}, {y}) joins ({x + dx}, {y + dy}), which the route never stepped between, on {where}"));
        }

        /// <summary>The searches and seeds whose routes fold back close enough to be misread.</summary>
        private static IEnumerable<TestCaseData> Wanderers() {
            yield return Wanderer("Greedy", new GreedySearch(), 5, false);
            yield return Wanderer("Greedy", new GreedySearch(), 5, true);
            yield return Wanderer("DFS", new DepthFirstSearch(), 5, false);
            yield return Wanderer("DFS", new DepthFirstSearch(), 5, true);
            yield return Wanderer("DFS", new DepthFirstSearch(), 19, false);
            yield return Wanderer("DFS", new DepthFirstSearch(), 4, true);
        }

        private static TestCaseData Wanderer(string name, ISearch search, int seed, bool diagonal) =>
            new TestCaseData(name, search, seed, diagonal).SetName($"{{m}}({name} {seed} diagonal {diagonal})");

        /// <summary>
        /// The invariants run against the reading that is wrong, and have to fail on it. A matrix
        /// that passes either way measures nothing.
        /// </summary>
        [Test]
        public void TheInvariantsRejectJoinsReadFromAdjacency() {
            Grid grid = Corridor();
            int start = grid.Index(new Coord(1, 3));
            int goal = grid.Index(new Coord(3, 1));
            IReadOnlyList<int> path = new AStarSearch()
                .Search(grid, start, goal, new SearchOptions(false, Heuristic.Manhattan)).Path;
            Assume.That(path.Count > 1);

            byte[,] guessed = Adjacency.Of(grid, path);
            var steps = new HashSet<(int, int, int, int)>();
            for (int i = 1; i < path.Count; i++) {
                Coord a = grid.At(path[i - 1]), b = grid.At(path[i]);
                steps.Add((a.X, a.Y, b.X, b.Y));
            }

            int spurious = 0, diagonal = 0, cornerCuts = 0, wrongDegree = 0;
            Each(grid, guessed, (x, y, dx, dy) => {
                if (!steps.Contains((x, y, x + dx, y + dy))) spurious++;
                if (dx != 0 && dy != 0) {
                    diagonal++;
                    if (!grid.IsWalkable(grid.Index(new Coord(x + dx, y))) ||
                        !grid.IsWalkable(grid.Index(new Coord(x, y + dy))))
                        cornerCuts++;
                }
            });
            Coord first = grid.At(path[0]), end = grid.At(path[path.Count - 1]);
            for (int x = 0; x < grid.Width; x++)
                for (int y = 0; y < grid.Height; y++) {
                    var at = new Coord(x, y);
                    int expected = at == first || at == end ? 1 : OnRoute(grid, path, at) ? 2 : 0;
                    if (Degree(guessed[x, y]) != expected) wrongDegree++;
                }

            Assert.Greater(spurious, 0, "adjacency drew no join the route never took");
            Assert.Greater(diagonal, 0, "adjacency drew no diagonal on an orthogonal route");
            Assert.Greater(cornerCuts, 0, "adjacency cut no corner");
            Assert.Greater(wrongDegree, 0, "adjacency left every cell with the right number of joins");
        }

        /// <summary>
        /// One route, dead end to dead end, the long way round. A and B flank the wall corner.
        /// <code>
        ///   y=4  . . . . .
        ///   y=3  . S # . .
        ///   y=2  o A # . .
        ///   y=1  o # B G .
        ///   y=0  o o o # .
        /// </code>
        /// </summary>
        private static Grid Corridor() {
            var grid = new Grid(5, 5);
            var open = new HashSet<Coord> {
                new Coord(1, 3), new Coord(1, 2), new Coord(0, 2), new Coord(0, 1),
                new Coord(0, 0), new Coord(1, 0), new Coord(2, 0), new Coord(2, 1), new Coord(3, 1)
            };
            for (int x = 0; x < 5; x++)
                for (int y = 0; y < 5; y++)
                    grid.SetWalkable(grid.Index(new Coord(x, y)), open.Contains(new Coord(x, y)));
            return grid;
        }

        private static void Each(Grid grid, byte[,] links, System.Action<int, int, int, int> check) {
            for (int x = 0; x < grid.Width; x++)
                for (int y = 0; y < grid.Height; y++)
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                            if (PathLinks.Linked(links[x, y], dx, dy))
                                check(x, y, dx, dy);
        }

        private static int Degree(byte mask) {
            int count = 0;
            for (int bit = 0; bit < 8; bit++)
                if ((mask & (1 << bit)) != 0)
                    count++;
            return count;
        }

        private static bool OnRoute(Grid grid, IReadOnlyList<int> path, Coord at) {
            int index = grid.Index(at);
            for (int i = 0; i < path.Count; i++)
                if (path[i] == index)
                    return true;
            return false;
        }

        private static string Render(Grid grid, IReadOnlyList<int> path) {
            var route = new HashSet<int>(path);
            var sb = new System.Text.StringBuilder().AppendLine();
            for (int y = grid.Height - 1; y >= 0; y--) {
                sb.Append("    ");
                for (int x = 0; x < grid.Width; x++) {
                    int index = grid.Index(new Coord(x, y));
                    sb.Append(!grid.IsWalkable(index) ? '#' : route.Contains(index) ? 'o' : '.');
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Joins read off which cells sit next to each other, which is all a renderer working from a
    /// picture of the grid has. The flank test is the best it can do: drop a diagonal when the
    /// route could have turned through one of the cells beside it. Here to be violated, not used.
    /// </summary>
    internal static class Adjacency {
        public static byte[,] Of(Grid grid, IReadOnlyList<int> path) {
            var linkEnd = new bool[grid.Width, grid.Height];
            var isPath = new bool[grid.Width, grid.Height];
            for (int i = 0; i < path.Count; i++) {
                Coord at = grid.At(path[i]);
                linkEnd[at.X, at.Y] = true;
                isPath[at.X, at.Y] = i > 0 && i < path.Count - 1;
            }

            var links = new byte[grid.Width, grid.Height];
            for (int x = 0; x < grid.Width; x++)
                for (int y = 0; y < grid.Height; y++)
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++) {
                            if (dx == 0 && dy == 0)
                                continue;
                            bool joined = dx == 0 || dy == 0
                                ? Link(grid, linkEnd, isPath, x, y, x + dx, y + dy)
                                : Diagonal(grid, linkEnd, isPath, x, y, dx, dy);
                            if (joined)
                                links[x, y] |= (byte) PathLinks.Bit(dx, dy);
                        }
            return links;
        }

        // one end has to be actual path, so a start touching an end draws nothing between them
        private static bool Link(Grid grid, bool[,] linkEnd, bool[,] isPath, int ax, int ay, int bx, int by) {
            if (!In(grid, ax, ay) || !In(grid, bx, by))
                return false;
            return linkEnd[ax, ay] && linkEnd[bx, by] && (isPath[ax, ay] || isPath[bx, by]);
        }

        private static bool Diagonal(Grid grid, bool[,] linkEnd, bool[,] isPath, int x, int y, int dx, int dy) {
            if (!Link(grid, linkEnd, isPath, x, y, x + dx, y + dy))
                return false;
            return !End(grid, linkEnd, x, y + dy) && !End(grid, linkEnd, x + dx, y);
        }

        private static bool End(Grid grid, bool[,] linkEnd, int x, int y) => In(grid, x, y) && linkEnd[x, y];

        private static bool In(Grid grid, int x, int y) => grid.InBounds(new Coord(x, y));
    }
}
