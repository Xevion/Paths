using System;

// Core still leans on a handful of UnityEngine value types. Unity compiles Runtime/ against the
// real ones; this stands in for them so the same sources build under dotnet. It goes away once
// Coord replaces Vector2Int. Only the members Core actually calls are here.
namespace UnityEngine {
    public struct Vector2Int : IEquatable<Vector2Int> {
        public int x;
        public int y;

        public Vector2Int(int x, int y) {
            this.x = x;
            this.y = y;
        }

        public static Vector2Int zero => new Vector2Int(0, 0);
        public static Vector2Int up => new Vector2Int(0, 1);
        public static Vector2Int down => new Vector2Int(0, -1);
        public static Vector2Int left => new Vector2Int(-1, 0);
        public static Vector2Int right => new Vector2Int(1, 0);

        public static Vector2Int operator +(Vector2Int a, Vector2Int b) => new Vector2Int(a.x + b.x, a.y + b.y);
        public static Vector2Int operator -(Vector2Int a, Vector2Int b) => new Vector2Int(a.x - b.x, a.y - b.y);
        public static bool operator ==(Vector2Int a, Vector2Int b) => a.x == b.x && a.y == b.y;
        public static bool operator !=(Vector2Int a, Vector2Int b) => !(a == b);

        public bool Equals(Vector2Int other) => this == other;
        public override bool Equals(object obj) => obj is Vector2Int other && this == other;

        // matches Unity's own so hashed collections order identically under both compilers
        public override int GetHashCode() => x.GetHashCode() ^ (y.GetHashCode() << 2);

        public override string ToString() => $"({x}, {y})";
    }

    public struct RectInt {
        public int xMin;
        public int yMin;
        public int width;
        public int height;

        public RectInt(int xMin, int yMin, int width, int height) {
            this.xMin = xMin;
            this.yMin = yMin;
            this.width = width;
            this.height = height;
        }

        public RectInt(Vector2Int position, Vector2Int size) : this(position.x, position.y, size.x, size.y) { }

        public int xMax => xMin + width;
        public int yMax => yMin + height;

        public bool Overlaps(RectInt other) =>
            other.xMin < xMax && other.xMax > xMin && other.yMin < yMax && other.yMax > yMin;
    }

    public static class Mathf {
        public static int Abs(int value) => Math.Abs(value);
        public static float Abs(float value) => Math.Abs(value);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static float Sqrt(float value) => (float) Math.Sqrt(value);
        public static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;

        // Unity rounds halves to even, and Math.Round already does that by default.
        public static int RoundToInt(float value) => (int) Math.Round(value);
    }

    // Change stamps itself with this. Outside the player there's no engine clock, so measure from
    // process start instead - nothing in Core reads it back for anything but relative timing.
    public static class Time {
        private static readonly DateTime Started = DateTime.UtcNow;

        public static float realtimeSinceStartup => (float) (DateTime.UtcNow - Started).TotalSeconds;
    }
}
