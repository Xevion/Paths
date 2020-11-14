using Algorithms;

namespace LevelGeneration {
    public class DrunkardWalk : ILevelGenerator {
        private readonly int _walks;
        private readonly int _walkLength;
        private readonly bool _add;
        private readonly double _centerBias;
        private readonly double _continueBias;

        /// <summary>
        /// A simple level generator implementing the Drunkard Walk algorithm.
        /// This implementation simply starts 'walks' a certain number of times.
        /// Each walk chooses a node, and then repeatedly chooses an adjacent node randomly.
        /// Each move clears (or adds) walls.
        /// </summary>
        /// <param name="walks">The number of independent walks that will be started.</param>
        /// <param name="walkLength">The maximum number of nodes to be added/cleared</param>
        /// <param name="add">if true, adds nodes to the grid - if false, removes nodes</param>
        /// <param name="centerBias">the bias to walk towards the center of the grid</param>
        /// <param name="continueBias">a bias to walk in the same direction as before</param>
        public DrunkardWalk(int walks, int walkLength, bool add, double centerBias = 0.1, double continueBias = 0.2) {
            _walks = walks;
            _walkLength = walkLength;
            _add = add;
            _centerBias = centerBias;
            _continueBias = continueBias;
        }

        public NodeGrid Generate(NodeGrid nodeGrid) {
            for (int unused = 0; unused < _walks; unused++) {
                Node node = nodeGrid.GetRandomNode();
                Node[] nodes = nodeGrid.GetAdjacentNodesArray(node);
            }

            return nodeGrid;
        }
    }
}