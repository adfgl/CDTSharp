namespace CDTSharp
{
    using static CDTGeometry;

    public struct CDTTriangle
    {
        public int index;
        public int a, b, c;
        public int abAdj, bcAdj, caAdj;
        public bool abCon, bcCon, caCon;

        public CDTTriangle(int index, 
            int a, int b, int c, 
            int abAdj = NO_INDEX, int bcAdj = NO_INDEX, int caAdj = NO_INDEX)
        {
            this.index = index;

            this.a = a;
            this.b = b;
            this.c = c;

            this.abAdj = abAdj;
            this.bcAdj = bcAdj;
            this.caAdj = caAdj;
        }

        public int IndexOf(int v)
        {
            if (a == v) return 0;
            if (b == v) return 1;
            if (c == v) return 2;
            return NO_INDEX;
        }

        public int IndexOf(int from, int to)
        {
            if (a == from && b == to) return 0;
            if (b == from && c == to) return 1;
            if (c == from && a == to) return 2;
            return NO_INDEX;
        }
    }


}
