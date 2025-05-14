namespace CDTSharp
{
    public readonly struct Edge
    {
        public readonly int triangle, edge;

        public Edge(int triangle, int edge)
        {
            this.triangle = triangle;
            this.edge = edge;
        }

        public void Deconstruct(out int triangle, out int edge)
        {
            triangle = this.triangle;
            edge = this.edge;
        }

        public override string ToString()
        {
            return $"t{triangle} e{edge}";
        }
    }
}
