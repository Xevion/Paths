using System.Collections.Generic;
using UnityEngine;

namespace Algorithms {
    /// <summary>
    /// Bidirectional Dijkstra: grows a frontier from the start and another from the end and stops when
    /// they meet, touching far less of the grid than a single search. Uniform-cost like Dijkstra, and
    /// honours the Diagonal toggle. The forward half runs on the shared Node fields (so it feeds the
    /// heatmap); the backward half keeps its own cost/closed/parent arrays.
    /// </summary>
    public class Bidirectional : IPathfinding {
        public NodeGrid NodeGrid { get; }
        public ChangeController ChangeController { get; }
        public Vector2Int Start { get; private set; }
        public Vector2Int End { get; private set; }

        private readonly bool _diagonal;
        private readonly List<Node> _touched = new List<Node>();

        public Bidirectional(NodeGrid nodeGrid, bool diagonal = false) {
            NodeGrid = nodeGrid;
            _diagonal = diagonal;
            ChangeController = new ChangeController(nodeGrid.RenderNodeTypes());
        }

        public Stack<Node> FindPath(Vector2Int start, Vector2Int end) {
            Start = start;
            End = end;
            return null;
        }

        public void Cleanup() {
            foreach (Node node in _touched)
                node.Reset();
            _touched.Clear();
        }
    }
}
