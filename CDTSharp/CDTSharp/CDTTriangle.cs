namespace CDTSharp
{
    using static CDTGeometry;

    public struct CDTTriangle
    {
        public int vtx1, vtx2, vtx3;
        public int adj12, adj23, adj31;
        public bool con12, con23, con31;
        public double ccX, ccY, rSqr;

        public CDTTriangle(double ccX, double ccY, double rSqr, int vtx1, int vtx2, int vtx3, int adj12 = NO_INDEX, int adj23 = NO_INDEX, int adj31 = NO_INDEX)
        {
            this.ccX = ccX;
            this.ccY = ccY;
            this.rSqr = rSqr;

            this.vtx1 = vtx1;
            this.vtx2 = vtx2;
            this.vtx3 = vtx3;

            this.adj12 = adj12;
            this.adj23 = adj23;
            this.adj31 = adj31;
        }

        public int IndexOf(int v)
        {
            if (vtx1 == v) return 0;
            if (vtx2 == v) return 1;
            if (vtx3 == v) return 2;
            return NO_INDEX;
        }

        public int IndexOf(int from, int to)
        {
            if (vtx1 == from && vtx2 == to) return 0;
            if (vtx2 == from && vtx3 == to) return 1;
            if (vtx3 == from && vtx1 == to) return 2;
            return NO_INDEX;
        }
    }
}
