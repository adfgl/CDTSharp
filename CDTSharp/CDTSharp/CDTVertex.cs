namespace CDTSharp
{
    public readonly struct CDTVertex
    {
        public readonly int index;
        public readonly double x, y;

        public CDTVertex(int index, double x, double y)
        {
            this.index = index;
            this.x = x;
            this.y = y;
        }

        public override string ToString()
        {
            return $"[{index}] {x} {y}";
        }
    }
}
