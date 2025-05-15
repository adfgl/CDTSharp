using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDTSharp
{
    public class InputPreprocessor
    {
        readonly CDTInput _input;
        readonly List<Vec2> _vertices;
        readonly List<(int a, int b)> _constraints;
        readonly List<(Polygon, Polygon[])> _polygons;

        public InputPreprocessor(CDTInput input, double eps = 1e-8)
        {
            _input = input;

            _vertices = new List<Vec2>();
            _constraints = new List<(int, int)>();
            _polygons = new List<(Polygon, Polygon[])>();


        }

        public CDTInput Input => _input;

        void ProcessPolygon(int index, CDTPolygon cdtPolygon, double eps)
        {
            (List<Vec2>, Rect) contour = ExtractContour(cdtPolygon.Contour, eps);
            List<(List<Vec2>, Rect)> holeContours = new List<(List<Vec2>, Rect)>();
            if (cdtPolygon.Holes != null) ExtractHoles(holeContours, contour, cdtPolygon.Holes, eps);
        }

        (List<Vec2>, Rect) ExtractContour(List<Vec2> vertices, double eps)
        {
            double minX, minY, maxX, maxY;
            minX = minY = double.MaxValue;
            maxX = maxY = double.MinValue;
            List<Vec2> contour = new List<Vec2>(vertices.Count);
            foreach (Vec2 item in vertices)
            {
                if (IndexOf(contour, item, eps) == CDT.NO_INDEX)
                {
                    contour.Add(item);

                    var (x, y) = item;
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }
            return (contour, new Rect(minX, minY, maxX, maxY));
        }

       void ExtractHoles(List<(List<Vec2>, Rect)> contours, (List<Vec2>, Rect) contour, List<List<Vec2>> holes, double eps)
        {
            foreach (List<Vec2> hole in holes)
            {
                (List<Vec2>, Rect) holeContour = ExtractContour(hole, eps);
                if (!Contains(contour, holeContour, eps)
                    && 
                    !Intersects(contour, holeContour, eps))
                {
                    continue;
                }

                bool add = true;
                for (int i = holes.Count - 1; i >= 0; i--)
                {
                    (List<Vec2>, Rect) existing = contours[i];
                    if (Contains(existing, holeContour, eps))
                    {
                        add = false;
                        break;
                    }

                    if (Contains(holeContour, existing, eps))
                    {
                        holes.RemoveAt(i);
                    }
                }

                if (add)
                {
                    contours.Add(holeContour);
                }
            }
        }


        public static bool Contains((List<Vec2>, Rect) a, (List<Vec2>, Rect) b, double eps)
        {
            if (!a.Item2.Contains(b.Item2)) return false;
            foreach (var v in b.Item1)
            {
                if (!Contains(a.Item1, v.x, v.y, eps)) return false;
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
                    if (!CDT.Intersect(p1, p2, q1, q2).IsNaN())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool Contains(List<Vec2> vertices, double x, double y, double tolerance = 0)
        {
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


        public int Add(List<Vec2> all, Vec2 v, double eps = 0)
        {
            int index = IndexOf(all, v, eps);
            if (index < 0)
            {
                index = all.Count;
                all.Add(v);
                return index;
            }
            return index;
        }

        public static int IndexOf(List<Vec2> all, Vec2 v, double eps = 0)
        {
            var (x, y) = v;
            double epsSqr = eps * eps;
            for (int i = 0; i < all.Count; i++)
            {
                var (x0, y0) = all[i];
                double dx = x0 - x;
                double dy = y0 - y;
                if (dx * dx + dy * dy <= epsSqr)
                {
                    return i;
                }
            }
            return CDT.NO_INDEX;
        }
    }
}
