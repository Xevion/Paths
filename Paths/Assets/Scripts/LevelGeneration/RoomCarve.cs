using System;
using System.Collections.Generic;
using System.Linq;
using Algorithms;
using UnityEngine;
using Random = UnityEngine.Random;

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
            int roomCount = Random.Range(_roomRange.x, _roomRange.y);
            int attempts = 0;

            // TODO: Update to latest Unity 2020 update to gain access to RectInt.Overlaps (?)

            // Generate rooms
            List<Rect> rooms = new List<Rect>();
            do {
                // Quit after too many attempts failed
                if (attempts > _maxAttempts)
                    break;
                
                // Get a position and check that it's not on the edge of the grid
                Vector2Int pos = nodeGrid.RandomPosition();
                if (nodeGrid.IsEdge(pos)) continue;
                
                
                var size = new Vector2Int(
                    Math.Min(Random.Range(_roomSizeRange.x, _roomSizeRange.y), nodeGrid.Width - pos.x),
                    Math.Min(Random.Range(_roomSizeRange.x, _roomSizeRange.y), nodeGrid.Height - pos.y)
                );
                var newRoom = new Rect(pos, size);

                // Check that it doesn't overlap with any of the previous rooms
                bool skip = rooms.Any(room => room.Overlaps(newRoom));

                // If it didn't, 
                if (!skip)
                    rooms.Add(newRoom);
                else
                    attempts++;
            } while (rooms.Count < roomCount);

            // Set all nodes within rooms
            foreach (Rect rect in rooms)
                nodeGrid.ClearRoom(rect);
            
            return nodeGrid;
        }
    }
}