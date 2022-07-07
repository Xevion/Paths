using System.Collections.Generic;
using UnityEngine;

namespace Algorithms {
    /// <summary>
    /// Jump Point Search - A* on a uniform-cost 8-connected grid that skips over the long runs of
    /// symmetric, equally-good cells between "jump points", so it touches a fraction of what plain A*
    /// does. Always diagonal, and like the rest of the diagonal movement here it won't cut corners
    /// (a diagonal step needs both of its shared orthogonal cells open). The prune/jump rules are the
    /// no-corner-cutting variant - same model PathFinding.js calls "move diagonally if no obstacles".
    /// </summary>
    public class JumpPoint : IPathfinding {
        public NodeGrid NodeGrid { get; }
        public ChangeController ChangeController { get; }
        public Vector2Int Start { get; private set; }
        public Vector2Int End { get; private set; }

        private const float Sqrt2 = 1.41421356f;

        private readonly List<Node> _touched = new List<Node>();

        public JumpPoint(NodeGrid nodeGrid) {
            NodeGrid = nodeGrid;
            ChangeController = new ChangeController(nodeGrid.RenderNodeTypes());
        }

        public Stack<Node> FindPath(Vector2Int start, Vector2Int end) {
            Start = start;
            End = end;

            ChangeController.AddChange(new Change(start.x, start.y, GridNodeType.Start, GridNodeType.Empty));
            ChangeController.AddChange(new Change(end.x, end.y, GridNodeType.End, GridNodeType.Empty));

            Node startNode = NodeGrid.Grid[start.x, start.y];
            startNode.Cost = 0f;
            startNode.DistanceToTarget = Octile(start, end);
            startNode.State = NodeState.Open;
            Touch(startNode);

            var open = new List<Node> { startNode };
            bool reached = false;

            while (open.Count > 0) {
                Node current = TakeLowest(open);
                current.State = NodeState.Closed;
                ChangeController.AddChange(new Change(current.Position.x, current.Position.y,
                    GridNodeType.Expanded, GridNodeType.Seen));

                if (current.Position == end) {
                    reached = true;
                    break;
                }

                foreach (Vector2Int jump in Successors(current)) {
                    Node node = NodeGrid.Grid[jump.x, jump.y];
                    if (node.State == NodeState.Closed)
                        continue;

                    float cost = current.Cost.Value + Octile(current.Position, jump);
                    if (node.State == NodeState.None || cost < node.Cost.Value) {
                        node.Parent = current;
                        node.Cost = cost;
                        node.DistanceToTarget = Octile(jump, end);
                        if (node.State == NodeState.None) {
                            node.State = NodeState.Open;
                            ChangeController.AddChange(new Change(jump.x, jump.y, GridNodeType.Seen, GridNodeType.Empty));
                            Touch(node);
                            open.Add(node);
                        }
                    }
                }
            }

            if (!reached)
                return null;

            Stack<Node> path = BuildPath(NodeGrid.Grid[end.x, end.y]);
            ChangeController.RemovePositions(start, 1);
            ChangeController.RemovePositions(end, 3);
            return path;
        }

        /// <summary>Jump points reachable from node - one per pruned neighbour direction that lands somewhere.</summary>
        private IEnumerable<Vector2Int> Successors(Node node) {
            var result = new List<Vector2Int>();
            foreach (Vector2Int neighbour in Neighbours(node)) {
                Vector2Int? jump = Jump(neighbour.x, neighbour.y, node.Position.x, node.Position.y);
                if (jump.HasValue)
                    result.Add(jump.Value);
            }
            return result;
        }

        /// <summary>
        /// Walk in (dx,dy) until we hit the goal, a forced neighbour, or a wall. Diagonals recurse into a
        /// horizontal and vertical probe first; the straight runs fall through the same continuation step.
        /// </summary>
        private Vector2Int? Jump(int x, int y, int px, int py) {
            if (!Walkable(x, y))
                return null;
            if (x == End.x && y == End.y)
                return new Vector2Int(x, y);

            int dx = x - px, dy = y - py;

            if (dx != 0 && dy != 0) {
                // no explicit diagonal forced-neighbour test in the no-corner-cutting model - the
                // horizontal/vertical probes below catch everything a diagonal would have forced.
                if (Jump(x + dx, y, x, y).HasValue || Jump(x, y + dy, x, y).HasValue)
                    return new Vector2Int(x, y);
            }
            else if (dx != 0) {
                // forced when a cell beside us is open but the one diagonally behind it is a wall -
                // you couldn't have reached it any cheaper, so it has to be expanded from here.
                if ((Walkable(x, y - 1) && !Walkable(x - dx, y - 1)) ||
                    (Walkable(x, y + 1) && !Walkable(x - dx, y + 1)))
                    return new Vector2Int(x, y);
            }
            else {
                if ((Walkable(x - 1, y) && !Walkable(x - 1, y - dy)) ||
                    (Walkable(x + 1, y) && !Walkable(x + 1, y - dy)))
                    return new Vector2Int(x, y);
            }

            // continue: for a straight run this steps one along; for a diagonal it needs both orthogonals
            // open before it carries on (no corner cutting).
            if (Walkable(x + dx, y) && Walkable(x, y + dy))
                return Jump(x + dx, y + dy, x, y);
            return null;
        }

        /// <summary>Pruned neighbour cells - the directions worth jumping, given how we arrived at node.</summary>
        private List<Vector2Int> Neighbours(Node node) {
            var result = new List<Vector2Int>();
            int x = node.Position.x, y = node.Position.y;

            if (node.Parent == null) {
                // start: every direction we can actually move
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++) {
                        if (dx == 0 && dy == 0) continue;
                        if (Movable(x, y, dx, dy))
                            result.Add(new Vector2Int(x + dx, y + dy));
                    }
                return result;
            }

            int px = node.Parent.Position.x, py = node.Parent.Position.y;
            int hx = System.Math.Sign(x - px), hy = System.Math.Sign(y - py);

            if (hx != 0 && hy != 0) {
                bool wy = Walkable(x, y + hy), wx = Walkable(x + hx, y);
                if (wy) result.Add(new Vector2Int(x, y + hy));
                if (wx) result.Add(new Vector2Int(x + hx, y));
                if (wy && wx) result.Add(new Vector2Int(x + hx, y + hy));
            }
            else if (hx != 0) {
                bool next = Walkable(x + hx, y), up = Walkable(x, y + 1), down = Walkable(x, y - 1);
                if (next) {
                    result.Add(new Vector2Int(x + hx, y));
                    if (up) result.Add(new Vector2Int(x + hx, y + 1));
                    if (down) result.Add(new Vector2Int(x + hx, y - 1));
                }
                if (up) result.Add(new Vector2Int(x, y + 1));
                if (down) result.Add(new Vector2Int(x, y - 1));
            }
            else {
                bool next = Walkable(x, y + hy), right = Walkable(x + 1, y), left = Walkable(x - 1, y);
                if (next) {
                    result.Add(new Vector2Int(x, y + hy));
                    if (right) result.Add(new Vector2Int(x + 1, y + hy));
                    if (left) result.Add(new Vector2Int(x - 1, y + hy));
                }
                if (right) result.Add(new Vector2Int(x + 1, y));
                if (left) result.Add(new Vector2Int(x - 1, y));
            }
            return result;
        }

        /// <summary>Fill the jump-point chain back in: each segment is straight or diagonal, so step it cell by cell.</summary>
        private Stack<Node> BuildPath(Node endNode) {
            var cells = new List<Vector2Int>();
            for (Node n = endNode; n != null; n = n.Parent)
                cells.Add(n.Position);
            cells.Reverse(); // start .. end

            var full = new List<Vector2Int> { cells[0] };
            for (int i = 1; i < cells.Count; i++) {
                Vector2Int from = cells[i - 1], to = cells[i];
                int sx = System.Math.Sign(to.x - from.x), sy = System.Math.Sign(to.y - from.y);
                Vector2Int cur = from;
                while (cur != to) {
                    cur = new Vector2Int(cur.x + sx, cur.y + sy);
                    full.Add(cur);
                }
            }

            // push goal-first toward start (excluding start), recording each cell as Path
            var path = new Stack<Node>();
            for (int i = full.Count - 1; i >= 1; i--) {
                Node cell = NodeGrid.Grid[full[i].x, full[i].y];
                path.Push(cell);
                GridNodeType old = cell.State == NodeState.Closed ? GridNodeType.Expanded
                    : cell.State == NodeState.Open ? GridNodeType.Seen
                    : GridNodeType.Empty; // an interpolated cell the search jumped clean over
                ChangeController.AddChange(new Change(full[i].x, full[i].y, GridNodeType.Path, old));
            }
            return path;
        }

        private static float Octile(Vector2Int a, Vector2Int b) {
            int dx = Mathf.Abs(a.x - b.x), dy = Mathf.Abs(a.y - b.y);
            return dx + dy + (Sqrt2 - 2f) * Mathf.Min(dx, dy);
        }

        private bool Walkable(int x, int y) {
            return x >= 0 && y >= 0 && x < NodeGrid.Width && y < NodeGrid.Height && NodeGrid.Grid[x, y].Walkable;
        }

        // can we step (dx,dy) off (x,y) without cutting a corner
        private bool Movable(int x, int y, int dx, int dy) {
            if (!Walkable(x + dx, y + dy))
                return false;
            if (dx != 0 && dy != 0)
                return Walkable(x + dx, y) && Walkable(x, y + dy);
            return true;
        }

        private static Node TakeLowest(List<Node> open) {
            int best = 0;
            for (int i = 1; i < open.Count; i++)
                if (open[i].F < open[best].F)
                    best = i;
            Node node = open[best];
            open.RemoveAt(best);
            return node;
        }

        private void Touch(Node node) => _touched.Add(node);

        public void Cleanup() {
            foreach (Node node in _touched)
                node.Reset();
            _touched.Clear();
        }
    }
}
