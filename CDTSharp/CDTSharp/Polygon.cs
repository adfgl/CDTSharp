namespace CDTSharp
{
    public readonly struct Polygon
    {
        public readonly int index;
        public readonly Rect rect;
        public readonly List<Vec2> verts;

        public Polygon(int index, List<Vec2> verts)
        {
            this.index = index;
            this.verts = verts;
            this.rect = Rect.FromPoints(verts);
        }

        public Polygon(int index, List<Vec2> verts, Rect rect) : this(index, verts)
        {
            this.rect = rect;
        }

        public bool Contains(double x, double y, double tolerance = 0)
        {
            if (!rect.Contains(x, y)) return false;

            int count = verts.Count;
            bool inside = false;
            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                var (xi, yi) = verts[i];
                var (xj, yj) = verts[j];

                bool crosses = (yi > y + tolerance) != (yj > y + tolerance);
                if (!crosses) continue;

                double t = (y - yi) / (yj - yi);
                double xCross = xi + t * (xj - xi);
                if (x < xCross - tolerance)
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        public static bool Intersects(Polygon a, Polygon b, double eps)
        {
            if (!a.rect.Intersects(b.rect)) return false;

            List<Vec2> av = a.verts, ab = b.verts;
            int ac = av.Count, bc = ab.Count;
            for (int i = 0; i < ac; i++)
            {
                Vec2 p1 = av[i];
                Vec2 p2 = av[(i + 1) % ac];
                for (int j = 0; j < bc; j++)
                {
                    Vec2 q1 = ab[j];
                    Vec2 q2 = ab[(j + 1) % bc];
                    if (GeometryHelper.Intersect(p1, p2, q1, q2, out _))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool Contains(Polygon a, Polygon b, double eps)
        {
            if (!a.rect.Contains(b.rect)) return false;
            foreach (var v in b.verts)
            {
                if (!a.Contains(v.x, v.y, eps)) return false;
            }
            return !Intersects(a, b, eps);
        }

        public static bool Contains(Polygon contour, List<Polygon> holeContours, double x, double y, double eps)
        {
            if (!contour.Contains(x, y, eps)) return false;

            foreach (Polygon hole in holeContours)
            {
                if (hole.Contains(x, y, eps))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
