using UnityEngine;

namespace Algorithms {
    public class Node {
        // Change this depending on what the desired size is for each element in the grid
        public Node Parent;
        public Vector2Int Position;

        // A* Algorithm variables
        public float DistanceToTarget;
        public float Cost;
        public float Weight;

        public float F {
            get {
                if (DistanceToTarget != -1 && Cost != -1)
                    return DistanceToTarget + Cost;
                return -1;
            }
        }

        public bool Walkable;

        public Node(Vector2Int pos, bool walkable, float weight = 1) {
            Parent = null;
            Position = pos;
            DistanceToTarget = -1;
            Cost = 1;
            Weight = weight;
            Walkable = walkable;
        }

        public override string ToString() {
            return $"Node({Position.x}, {Position.y}, {Walkable})";
        }
    }
}