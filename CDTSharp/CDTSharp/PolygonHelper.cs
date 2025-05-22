using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDTSharp
{
    public static class PolygonHelper
    {
        public static bool Contains((List<Vec2>, Rect) contour, List<(List<Vec2>, Rect)> holeContours, double x, double y, double eps)
        {
            if (!Contains(contour, x, y, eps)) return false;

            foreach (var item in holeContours)
            {
                if (Contains(item, x, y, eps))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool Contains((List<Vec2>, Rect) a, (List<Vec2>, Rect) b, double eps)
        {
            if (!a.Item2.Contains(b.Item2)) return false;
            foreach (var v in b.Item1)
            {
                if (!Contains(a, v.x, v.y, eps)) return false;
            }
            return !Intersects(a, b, eps);
        }

        public static bool Intersects((List<Vec2>, Rect) a, (List<Vec2>, Rect) b, double eps)
        {
            if (!a.Item2.Intersects(b.Item2)) return false;

            List<Vec2> av = a.Item1, ab = b.Item1;
            int ac = av.Count, bc = ab.Count;
            for (int i = 0; i < ac; i++)
            {
                Vec2 p1 = av[i];
                Vec2 p2 = av[(i + 1) % ac];
                for (int j = 0; j < bc; j++)
                {
                    Vec2 q1 = ab[j];
                    Vec2 q2 = ab[(j + 1) % bc];
                    if (CDT.Intersect(p1, p2, q1, q2, out _))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool Contains((List<Vec2>, Rect) poly, double x, double y, double tolerance = 0)
        {
            if (!poly.Item2.Contains(x, y)) return false;

            var vertices = poly.Item1;
            int count = vertices.Count;
            bool inside = false;
            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                var (xi, yi) = vertices[i];
                var (xj, yj) = vertices[j];

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
    }
}
