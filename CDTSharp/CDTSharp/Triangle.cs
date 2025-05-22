namespace CDTSharp
{
    using System;
    using System.Runtime.CompilerServices;
    using static CDT;

    public struct Triangle
    {
        public readonly static int[] NEXT = [1, 2, 0], PREV = [2, 0, 1];

        public readonly Circle circle;
        public readonly int[] indices, adjacent;
        public readonly bool[] constraint;
        public int parent;

        public Triangle(
            Circle circle,
            int v0, int v1, int v2,
            int adj0 = NO_INDEX, int adj1 = NO_INDEX, int adj2 = NO_INDEX,
            bool con0 = false, bool con1 = false, bool con2 = false,
            int parent = NO_INDEX)
        {
            this.circle = circle;
            this.indices = [v0, v1, v2];
            this.adjacent = [adj0, adj1, adj2];
            this.constraint = [con0, con1, con2];
            this.parent = parent;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool ContainsSuper() => indices[0] < 3 || indices[1] < 3 || indices[2] < 3;

        public int IndexOf(int v)
        {
            for (int i = 0; i < 3; i++)
            {
                if (indices[i] == v) return i;
            }
            return NO_INDEX;
        }

        public int IndexOf(int from, int to)
        {
            for (int i = 0; i < 3; i++)
            {
                if (indices[i] == from && indices[NEXT[i]] == to)
                {
                    return i;
                }
            }
            return NO_INDEX;
        }

        public int IndexOfInvariant(int from, int to)
        {
            for (int i = 0; i < 3; i++)
            {
                if (indices[i] == from && (indices[NEXT[i]] == to || indices[PREV[i]] == to))
                {
                    return i;
                }
            }
            return NO_INDEX;
        }

        public override string ToString()
        {
            string s = "";
            for (int i = 0; i < 3; i++)
            {
                s += adjacent[i] + " " + constraint[i] + (i != 2 ? ", " : "");
            }

            return $"{(ContainsSuper() ? "[super] " : "")} {String.Join(' ', indices.Select(i => i))} ({s})";
        }
    }
}
