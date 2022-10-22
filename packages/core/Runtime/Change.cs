namespace Paths.Core {
    /// <summary>
    /// One cell changing appearance, remembering what it replaced so playback can be wound backwards
    /// as readily as forwards.
    /// </summary>
    public readonly struct Change {
        public readonly int X;
        public readonly int Y;
        public readonly GridNodeType New;
        public readonly GridNodeType Old;

        public Change(int x, int y, GridNodeType newType, GridNodeType oldType) {
            this.X = x;
            this.Y = y;
            this.New = newType;
            this.Old = oldType;
        }

        public override string ToString() {
            return $"Change({X}, {Y}, {Old} -> {New})";
        }
    }
}
