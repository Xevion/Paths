#nullable enable
using System;
using System.Collections.Generic;
using Paths.Core.Search;

namespace Paths.Core.Generation {
    /// <summary>
    /// Carves rectangular rooms out of a solid grid and joins them with corridors. Expects the grid
    /// to arrive walled in: rooms are the open space, not the obstacles.
    /// </summary>
    public sealed class RoomCarve : ILevelGenerator {
        private readonly int _minRooms;
        private readonly int _maxRooms;
        private readonly int _minSize;
        private readonly int _maxSize;
        private readonly int _maxAttempts;

        public RoomCarve(int minRooms, int maxRooms, int minSize, int maxSize, int maxAttempts = 80) {
            _minRooms = minRooms;
            _maxRooms = maxRooms;
            _minSize = minSize;
            _maxSize = maxSize;
            _maxAttempts = maxAttempts;
        }

        public void Generate(Grid grid, Random random) {
            int wanted = random.Next(_minRooms, _maxRooms);
            var rooms = new List<Room>();
            int attempts = 0;

            while (rooms.Count < wanted && attempts <= _maxAttempts) {
                Coord at = grid.At(random.Next(grid.CellCount));
                Room room = new Room(at.X, at.Y,
                    Math.Min(random.Next(_minSize, _maxSize), grid.Width - at.X),
                    Math.Min(random.Next(_minSize, _maxSize), grid.Height - at.Y));

                // every rejection counts, including landing on the border. the old version only
                // counted overlaps, so a grid where most picks are edges could spin indefinitely
                if (IsEdge(grid, at) || Overlaps(rooms, room)) {
                    attempts++;
                    continue;
                }

                rooms.Add(room);
            }

            foreach (Room room in rooms)
                Carve(grid, room);

            // non-overlapping rooms are sealed islands on their own, so link each to the one before
            // it with an L-shaped corridor and every room ends up reachable from every other
            for (int i = 1; i < rooms.Count; i++)
                Corridor(grid, rooms[i - 1].Center, rooms[i].Center);
        }

        private static bool IsEdge(Grid grid, Coord at) =>
            at.X == 0 || at.Y == 0 || at.X == grid.Width - 1 || at.Y == grid.Height - 1;

        private static bool Overlaps(List<Room> rooms, Room room) {
            foreach (Room other in rooms)
                if (other.Overlaps(room))
                    return true;
            return false;
        }

        private static void Carve(Grid grid, Room room) {
            for (int x = room.X; x < room.MaxX; x++)
                for (int y = room.Y; y < room.MaxY; y++)
                    grid.SetWalkable(grid.Index(new Coord(x, y)), true);
        }

        // an axis-aligned dogleg: all the way across in x, then all the way in y
        private static void Corridor(Grid grid, Coord from, Coord to) {
            for (int x = Math.Min(from.X, to.X); x <= Math.Max(from.X, to.X); x++)
                grid.SetWalkable(grid.Index(new Coord(x, from.Y)), true);
            for (int y = Math.Min(from.Y, to.Y); y <= Math.Max(from.Y, to.Y); y++)
                grid.SetWalkable(grid.Index(new Coord(to.X, y)), true);
        }

        private readonly struct Room {
            public readonly int X;
            public readonly int Y;
            private readonly int _width;
            private readonly int _height;

            public Room(int x, int y, int width, int height) {
                X = x;
                Y = y;
                _width = width;
                _height = height;
            }

            public int MaxX => X + _width;
            public int MaxY => Y + _height;

            public Coord Center => new Coord(X + _width / 2, Y + _height / 2);

            public bool Overlaps(Room other) =>
                other.X < MaxX && other.MaxX > X && other.Y < MaxY && other.MaxY > Y;
        }
    }
}
