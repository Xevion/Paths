#nullable enable
using System;

namespace Paths.Core.Search {
    /// <summary>
    /// A grid position. The searches work in flat integer indices internally; this is the shape
    /// positions take at the edges of the API, where x and y are what callers actually mean.
    /// </summary>
    public readonly struct Coord : IEquatable<Coord> {
        public readonly int X;
        public readonly int Y;

        public Coord(int x, int y) {
            X = x;
            Y = y;
        }

        public static Coord operator +(Coord a, Coord b) => new Coord(a.X + b.X, a.Y + b.Y);
        public static Coord operator -(Coord a, Coord b) => new Coord(a.X - b.X, a.Y - b.Y);
        public static bool operator ==(Coord a, Coord b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(Coord a, Coord b) => !(a == b);

        public bool Equals(Coord other) => this == other;
        public override bool Equals(object? obj) => obj is Coord other && this == other;

        public override int GetHashCode() => unchecked(X * 397 ^ Y);

        public override string ToString() => $"({X}, {Y})";
    }
}
