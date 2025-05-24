namespace CDTSharp
{
    using System;
    using System.Runtime.CompilerServices;
    using static CDT;

    public struct CDTTriangle
    {
        public readonly static int[] NEXT = [1, 2, 0], PREV = [2, 0, 1];

        public readonly Circle circle;
        public readonly double area;
        public readonly int[] indices, adjacent;
        public readonly bool[] constraint;
        public readonly List<int> parents;
        public readonly bool super;

        public CDTTriangle(
            Circle circle, double area,
            int v0, int v1, int v2,
            int adj0 = NO_INDEX, int adj1 = NO_INDEX, int adj2 = NO_INDEX,
            bool con0 = false, bool con1 = false, bool con2 = false,
            IEnumerable<int>? parents = null)
        {
            this.circle = circle;
            this.area = area;
            this.super = v0 < 3 || v1 < 3 || v2 < 3;
            this.indices = [v0, v1, v2];
            this.adjacent = [adj0, adj1, adj2];
            this.constraint = [con0, con1, con2];
            this.parents = parents != null ? new List<int>(parents) : new List<int>();
        }

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

            return $"{(super ? "[super] " : "")} {String.Join(' ', indices.Select(i => i))} ({s})";
        }
    }
}
