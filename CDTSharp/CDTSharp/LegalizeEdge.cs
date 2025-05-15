namespace CDTSharp
{
    public readonly struct LegalizeEdge
    {
        public readonly int triangle, index;

        public LegalizeEdge(int triangle, int edge)
        {
            this.triangle = triangle;
            this.index = edge;
        }

        public void Deconstruct(out int triangle, out int edge)
        {
            triangle = this.triangle;
            edge = this.index;
        }

        public override string ToString()
        {
            return $"t{triangle} e{index}";
        }
    }
}
