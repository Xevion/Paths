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

        private const float Sqrt2 = 1.41421356f;

        private readonly bool _diagonal;
        private readonly List<Node> _touched = new List<Node>();

        // backward search state, kept off the shared Node (the forward half owns those fields)
        private float[,] _distB;
        private bool[,] _closedB;
        private Vector2Int[,] _parentB;
        // the current rendered type per cell, so a Change records the right old value for reverse/scrub
        private GridNodeType[,] _view;

        public Bidirectional(NodeGrid nodeGrid, bool diagonal = false) {
            NodeGrid = nodeGrid;
            _diagonal = diagonal;
            ChangeController = new ChangeController(nodeGrid.RenderNodeTypes());
        }

        public Stack<Node> FindPath(Vector2Int start, Vector2Int end) {
            Start = start;
            End = end;

            ChangeController.AddChange(new Change(start.x, start.y, GridNodeType.Start, GridNodeType.Empty));
            ChangeController.AddChange(new Change(end.x, end.y, GridNodeType.End, GridNodeType.Empty));

            if (start == end) {
                Node only = NodeGrid.Grid[start.x, start.y];
                only.Cost = 0f;
                Touch(only);
                var trivial = new Stack<Node>();
                trivial.Push(only);
                return trivial;
            }

            int w = NodeGrid.Width, h = NodeGrid.Height;
            _view = NodeGrid.RenderNodeTypes();
            _distB = new float[w, h];
            _closedB = new bool[w, h];
            _parentB = new Vector2Int[w, h];
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++) {
                    _distB[x, y] = float.PositiveInfinity;
                    _parentB[x, y] = new Vector2Int(-1, -1);
                }

            Node startNode = NodeGrid.Grid[start.x, start.y];
            startNode.Cost = 0f;
            startNode.State = NodeState.Open;
            Touch(startNode);
            var openF = new List<Node> { startNode };

            _distB[end.x, end.y] = 0f;
            var openB = new List<Vector2Int> { end };

            float mu = float.PositiveInfinity; // best meeting cost found so far
            Node meetF = null;                 // forward-tree side of the meeting edge
            Vector2Int meetB = new Vector2Int(-1, -1); // backward-tree side

            while (openF.Count > 0 && openB.Count > 0) {
                float minF = MinCostF(openF), minB = MinCostB(openB);
                // once neither frontier can improve on the best meeting, it's optimal
                if (minF + minB >= mu)
                    break;

                // expand the shallower frontier so the two waves stay balanced
                if (minF <= minB)
                    ExpandForward(PopMinF(openF), openF, openB, ref mu, ref meetF, ref meetB);
                else
                    ExpandBackward(PopMinB(openB), openB, ref mu, ref meetF, ref meetB);
            }

            if (meetF == null)
                return null;

            return BuildPath(meetF, meetB, mu, end);
        }

        private void ExpandForward(Node u, List<Node> openF, List<Vector2Int> openB,
            ref float mu, ref Node meetF, ref Vector2Int meetB) {
            u.State = NodeState.Closed;
            Mark(u.Position, GridNodeType.Expanded);

            foreach (Node v in NodeGrid.Neighbors(u, _diagonal)) {
                if (!v.Walkable || v.State == NodeState.Closed)
                    continue;
                float step = Step(u.Position, v.Position);
                float nd = u.Cost.Value + step;
                if (v.State == NodeState.None) {
                    v.State = NodeState.Open;
                    v.Cost = nd;
                    v.Parent = u;
                    Touch(v);
                    openF.Add(v);
                    Mark(v.Position, GridNodeType.Seen);
                }
                else if (nd < v.Cost.Value) {
                    v.Cost = nd;
                    v.Parent = u;
                }

                // v also reached from the end -> a path through the edge u-v
                float db = _distB[v.Position.x, v.Position.y];
                if (!float.IsPositiveInfinity(db) && u.Cost.Value + step + db < mu) {
                    mu = u.Cost.Value + step + db;
                    meetF = u;
                    meetB = v.Position;
                }
            }
        }

        private void ExpandBackward(Vector2Int u, List<Vector2Int> openB,
            ref float mu, ref Node meetF, ref Vector2Int meetB) {
            _closedB[u.x, u.y] = true;
            Mark(u, GridNodeType.Expanded);

            foreach (Node vn in NodeGrid.Neighbors(NodeGrid.Grid[u.x, u.y], _diagonal)) {
                Vector2Int v = vn.Position;
                if (!vn.Walkable || _closedB[v.x, v.y])
                    continue;
                float step = Step(u, v);
                float nd = _distB[u.x, u.y] + step;
                if (nd < _distB[v.x, v.y]) {
                    bool discovered = float.IsPositiveInfinity(_distB[v.x, v.y]);
                    _distB[v.x, v.y] = nd;
                    _parentB[v.x, v.y] = u;
                    if (discovered) {
                        openB.Add(v);
                        Mark(v, GridNodeType.Seen);
                    }
                }

                // v also reached from the start -> a path through the edge u-v
                if (vn.State != NodeState.None && _distB[u.x, u.y] + step + vn.Cost.Value < mu) {
                    mu = _distB[u.x, u.y] + step + vn.Cost.Value;
                    meetF = vn;
                    meetB = u;
                }
            }
        }

        // stitch the forward half (start..meetF) to the backward half (meetB..end) across their edge
        private Stack<Node> BuildPath(Node meetF, Vector2Int meetB, float mu, Vector2Int end) {
            var cells = new List<Vector2Int>();
            for (Node n = meetF; n != null; n = n.Parent)
                cells.Add(n.Position);
            cells.Reverse(); // start .. meetF
            for (Vector2Int c = meetB; c.x >= 0; c = _parentB[c.x, c.y])
                cells.Add(c); // meetB .. end

            var path = new Stack<Node>();
            for (int i = cells.Count - 1; i >= 0; i--) {
                Vector2Int c = cells[i];
                path.Push(NodeGrid.Grid[c.x, c.y]);
                Mark(c, GridNodeType.Path);
            }

            Node endNode = NodeGrid.Grid[end.x, end.y];
            if (endNode.State == NodeState.None)
                Touch(endNode);
            endNode.Cost = mu; // so callers can read the path cost off the goal, like the other searches
            return path;
        }

        // record a state change for a cell, skipping start/end (they keep their markers) and no-op repaints
        private void Mark(Vector2Int p, GridNodeType type) {
            if (p == Start || p == End || _view[p.x, p.y] == type)
                return;
            ChangeController.AddChange(new Change(p.x, p.y, type, _view[p.x, p.y]));
            _view[p.x, p.y] = type;
        }

        private static float Step(Vector2Int a, Vector2Int b) =>
            a.x != b.x && a.y != b.y ? Sqrt2 : 1f;

        private static float MinCostF(List<Node> open) {
            float m = float.PositiveInfinity;
            foreach (Node n in open)
                if (n.Cost.Value < m) m = n.Cost.Value;
            return m;
        }

        private float MinCostB(List<Vector2Int> open) {
            float m = float.PositiveInfinity;
            foreach (Vector2Int c in open)
                if (_distB[c.x, c.y] < m) m = _distB[c.x, c.y];
            return m;
        }

        private static Node PopMinF(List<Node> open) {
            int best = 0;
            for (int i = 1; i < open.Count; i++)
                if (open[i].Cost.Value < open[best].Cost.Value) best = i;
            Node n = open[best];
            open.RemoveAt(best);
            return n;
        }

        private Vector2Int PopMinB(List<Vector2Int> open) {
            int best = 0;
            for (int i = 1; i < open.Count; i++)
                if (_distB[open[i].x, open[i].y] < _distB[open[best].x, open[best].y]) best = i;
            Vector2Int c = open[best];
            open.RemoveAt(best);
            return c;
        }

        private void Touch(Node node) => _touched.Add(node);

        public void Cleanup() {
            foreach (Node node in _touched)
                node.Reset();
            _touched.Clear();
        }
    }
}
