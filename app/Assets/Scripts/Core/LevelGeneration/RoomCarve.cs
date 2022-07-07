using System;
using System.Collections.Generic;
using System.Linq;
using Algorithms;
using UnityEngine;

namespace LevelGeneration {
    public class RoomCarve : ILevelGenerator {
        private Vector2Int _roomRange;
        private Vector2Int _roomSizeRange;
        private int _maxAttempts;

        public RoomCarve(Vector2Int roomRange, Vector2Int roomSizeRange, int maxAttempts) {
            _roomRange = roomRange;
            _roomSizeRange = roomSizeRange;
            _maxAttempts = maxAttempts;
        }

        public NodeGrid Generate(NodeGrid nodeGrid) {
            int roomCount = nodeGrid.Next(_roomRange.x, _roomRange.y);
            int attempts = 0;

            // Generate rooms
            List<RectInt> rooms = new List<RectInt>();
            do {
                // Quit after too many attempts failed
                if (attempts > _maxAttempts)
                    break;
                
                // Get a position and check that it's not on the edge of the grid
                Vector2Int pos = nodeGrid.RandomPosition();
                if (nodeGrid.IsEdge(pos)) continue;
                
                
                var size = new Vector2Int(
                    Math.Min(nodeGrid.Next(_roomSizeRange.x, _roomSizeRange.y), nodeGrid.Width - pos.x),
                    Math.Min(nodeGrid.Next(_roomSizeRange.x, _roomSizeRange.y), nodeGrid.Height - pos.y)
                );
                var newRoom = new RectInt(pos, size);

                // Check that it doesn't overlap with any of the previous rooms
                bool skip = rooms.Any(room => room.Overlaps(newRoom));

                // If it didn't, 
                if (!skip)
                    rooms.Add(newRoom);
                else
                    attempts++;
            } while (rooms.Count < roomCount);

            // Set all nodes within rooms
            foreach (RectInt rect in rooms)
                nodeGrid.ClearRoom(rect);

            // rooms are carved not to overlap, so on their own they're sealed islands - link each one
            // to the previous with an L corridor so every room is reachable from every other
            for (int i = 1; i < rooms.Count; i++)
                Corridor(nodeGrid, Center(rooms[i - 1]), Center(rooms[i]));

            return nodeGrid;
        }

        private static Vector2Int Center(RectInt room) =>
            new Vector2Int(room.xMin + room.width / 2, room.yMin + room.height / 2);

        // an axis-aligned dogleg: all the way across in x, then all the way in y
        private static void Corridor(NodeGrid nodeGrid, Vector2Int from, Vector2Int to) {
            for (int x = Math.Min(from.x, to.x); x <= Math.Max(from.x, to.x); x++)
                nodeGrid.GetNode(new Vector2Int(x, from.y)).Walkable = true;
            for (int y = Math.Min(from.y, to.y); y <= Math.Max(from.y, to.y); y++)
                nodeGrid.GetNode(new Vector2Int(to.x, y)).Walkable = true;
        }
    }
}