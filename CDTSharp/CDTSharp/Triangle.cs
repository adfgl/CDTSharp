namespace CDTSharp
{
    using System.Runtime.CompilerServices;
    using static CDTGeometry;

    public struct Triangle
    {
        public int parent;
        public readonly int v0, v1, v2;
        public int adj0, adj1, adj2;
        public bool con0, con1, con2;
        public bool hole;

        public Triangle(
            int v0, int v1, int v2,
            int adj0 = NO_INDEX, int adj1 = NO_INDEX, int adj2 = NO_INDEX,
            bool con0 = false, bool con1 = false, bool con2 = false,
            bool hole = false)
        {
            this.v0 = v0; this.v1 = v1; this.v2 = v2;
            this.adj0 = adj0; this.adj1 = adj1; this.adj2 = adj2;
            this.con0 = con0; this.con1 = con1; this.con2 = con2;
            this.hole = hole;
        }

        public int GetVertex(int i) => i switch { 0 => v0, 1 => v1, 2 => v2, _ => throw new ArgumentOutOfRangeException() };
        public int GetAdjacent(int i) => i switch { 0 => adj0, 1 => adj1, 2 => adj2, _ => throw new ArgumentOutOfRangeException() };
        public bool GetConstraint(int i) => i switch { 0 => con0, 1 => con1, 2 => con2, _ => throw new ArgumentOutOfRangeException() };

        public (int v, int e, bool c) GetEdge(int i)
        {
            switch (i)
            {
                case 0: return (v0, adj0, con0);
                case 1: return (v1, adj1, con1);
                case 2: return (v2, adj2, con2);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void SetAdjacent(int i, int value)
        {
            switch (i)
            {
                case 0: adj0 = value; break;
                case 1: adj1 = value; break;
                case 2: adj2 = value; break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public int IndexOf(int from, int to)
        {
            if (v0 == from && v1 == to) return 0;
            if (v1 == from && v2 == to) return 1;
            if (v2 == from && v0 == to) return 2;
            return NO_INDEX;
        }
    }
}
