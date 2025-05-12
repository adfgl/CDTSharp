namespace CDTSharp
{
    public readonly struct CDTVertex
    {
        public readonly int index;
        public readonly double x, y;

        public CDTVertex(double x, double y, int index)
        {
            this.x = x;
            this.y = y;
            this.index = index;
        }
    }
}
