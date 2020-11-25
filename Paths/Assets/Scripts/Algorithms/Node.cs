using System;
using UnityEngine;

namespace Algorithms {
    public enum NodeState {
        None, Open, Closed
    }
    
    public class Node : IComparable<Node> {
        // Change this depending on what the desired size is for each element in the grid
        public Node Parent;
        public readonly Vector2Int Position;

        // A* Algorithm variables
        public float? DistanceToTarget;
        public float? Cost;
        public float Weight;

        public NodeState State = NodeState.None;

        public float F {
            get {
                if (DistanceToTarget.HasValue && Cost.HasValue)
                    return DistanceToTarget.Value + Cost.Value;
                return -1;
            }
        }

        public bool Walkable;

        public Node(Vector2Int pos, bool walkable, float weight = 1) {
            Parent = null;
            Position = pos;
            DistanceToTarget = null;
            Cost = 1;
            Weight = weight;
            Walkable = walkable;
        }

        public override bool Equals(object obj) {
            return obj is Node node && Position.Equals(node.Position);
        }

        public override int GetHashCode() {
            return Position.GetHashCode();
        }

        public int CompareTo(Node other) {
            int diff = (int) (this.F - other.F);
            return diff;
            // return diff != 0 ? diff : NodeGrid.SignedManhattan(Position, other.Position);
        }

        public override string ToString() {
            return string.Format(
                    "Node ({0:00}, {1:00}, {2}, {3})",
                    Position.x,
                    Position.y,
                    Walkable ? "Walkable" : "Not Walkable",
                    State == NodeState.None ? (Walkable ? "Openable" : "Wall") : State.ToString()
                );
        }
    }
}