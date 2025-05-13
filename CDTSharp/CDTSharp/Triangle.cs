namespace CDTSharp
{
    using System.Runtime.CompilerServices;
    using static CDTGeometry;

    public readonly struct Triangle
    {
        public readonly int[] indices, adjacent;
        public readonly bool[] constraint;
        public readonly double ccX, ccY, rSqr;

        public Triangle(double ccX, double ccY, double rSqr, int vtx1, int vtx2, int vtx3, int adj12 = NO_INDEX, int adj23 = NO_INDEX, int adj31 = NO_INDEX)
        {
            this.ccX = ccX;
            this.ccY = ccY;
            this.rSqr = rSqr;

            this.indices = [vtx1, vtx2, vtx3];
            this.adjacent = [adj12, adj23, adj31];
            this.constraint = [false, false, false];
        }

        public int IndexOf(int v)
        {
            for (int i = 0; i < 3; i++)
            {
                if (this.indices[i] == v) return i;
            }
            return NO_INDEX;
        }

        public int IndexOf(int from, int to)
        {
            for (int i = 0; i < 3; i++)
            {
                if (from == indices[i] && to == indices[(i + 1) % 3]) return i;
            }
            return NO_INDEX;
        }

        public bool CircumCircleContains(double x, double y)
        {
            double dx = x - ccX;
            double dy = y - ccY;
            return dx * dx + dy * dy <= rSqr;
        }
    }
}
