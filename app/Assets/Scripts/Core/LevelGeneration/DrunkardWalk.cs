using System;
using Algorithms;
using UnityEngine;

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

        private static readonly Vector2Int[] Directions =
            { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        public NodeGrid Generate(NodeGrid nodeGrid) {
            var center = new Vector2Int(nodeGrid.Width / 2, nodeGrid.Height / 2);

            for (int walk = 0; walk < _walks; walk++) {
                Vector2Int position = nodeGrid.RandomPosition();
                Vector2Int direction = RandomDirection(nodeGrid);

                for (int step = 0; step < _walkLength; step++) {
                    nodeGrid.GetNode(position).Walkable = !_add;

                    // mostly turn, but hold the line often enough to carve corridors rather than blobs
                    if (!Roll(nodeGrid, _continueBias))
                        direction = Roll(nodeGrid, _centerBias)
                            ? Toward(position, center, nodeGrid)
                            : RandomDirection(nodeGrid);

                    Vector2Int next = position + direction;
                    // walked into the edge - turn rather than stall out against it
                    if (!nodeGrid.IsValid(next)) {
                        direction = RandomDirection(nodeGrid);
                        continue;
                    }

                    position = next;
                }
            }

            return nodeGrid;
        }

        private static bool Roll(NodeGrid nodeGrid, double bias) => nodeGrid.Next(0, 10000) < bias * 10000;

        private static Vector2Int RandomDirection(NodeGrid nodeGrid) =>
            Directions[nodeGrid.Next(0, Directions.Length)];

        // one step along whichever axis is furthest from the target
        private static Vector2Int Toward(Vector2Int from, Vector2Int to, NodeGrid nodeGrid) {
            Vector2Int delta = to - from;
            if (delta == Vector2Int.zero)
                return RandomDirection(nodeGrid);
            if (Math.Abs(delta.x) >= Math.Abs(delta.y))
                return delta.x > 0 ? Vector2Int.right : Vector2Int.left;
            return delta.y > 0 ? Vector2Int.up : Vector2Int.down;
        }
    }
}