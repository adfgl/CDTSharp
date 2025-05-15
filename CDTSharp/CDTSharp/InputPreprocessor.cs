using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            for (int i = 0; i < input.Polygons.Count; i++)
            {
                ProcessPolygon(i, input.Polygons[i], eps);
            }
        }

        public CDTInput Input => _input;
        public IReadOnlyList<Vec2> Vertices => _vertices;
        public IReadOnlyList<(int a, int b)> Constraints => _constraints;
        public IReadOnlyList<(Polygon, Polygon[])> Polygons => _polygons;

        void ProcessPolygon(int index, CDTPolygon cdtPolygon, double eps)
        {
            (List<Vec2>, Rect) contour = ExtractContour(cdtPolygon.Contour, eps);
            List<(List<Vec2>, Rect)> holeContours = new List<(List<Vec2>, Rect)>();
            if (cdtPolygon.Holes != null) ExtractHoles(holeContours, contour, cdtPolygon.Holes, eps);
       
            List<Constraint> constraints = new List<Constraint>();

            ExtractConstraints(constraints, contour.Item1, EConstraint.Contour, eps);
            foreach ((List<Vec2>, Rect) item in holeContours)
            {
                ExtractConstraints(constraints, item.Item1, EConstraint.Hole, eps);
            }

            if (cdtPolygon.Constraints != null)
            {
                foreach ((Vec2 a, Vec2 b) in cdtPolygon.Constraints)
                {
                    AddConstraint(constraints, a, b, EConstraint.User, eps);
                }
            }

            List<Vec2> pointConstraints = new List<Vec2>();
            if (cdtPolygon.Points != null)
            {
                foreach (var v in cdtPolygon.Points)
                {
                    if (!Contains(contour, holeContours, v.x, v.y, eps))
                    {
                        continue;
                    }

                    pointConstraints.Add(v);

                    for (int i = constraints.Count - 1; i >= 0; i--)
                    {
                        Constraint c = constraints[i];
                        if (c.OnNode(v, eps))
                        {
                            break; 
                        }

                        if (c.OnEdge(v, eps))
                        {
                            constraints.RemoveAt(i);
                            var (first, second) = c.Split(v);
                            constraints.Add(first);
                            constraints.Add(second);
                            break;
                        }
                    }
                }
            }

            CleanConstraints(constraints, contour, holeContours, eps);

            Polygon contourPoly = BuildPolygon(index, contour.Item1, eps);
            List<Polygon> holePolygons = new List<Polygon>(holeContours.Count);
            foreach (var item in holeContours)
            {
                holePolygons.Add(BuildPolygon(-1, item.Item1, eps));
            }

            foreach (var item in pointConstraints)
            {
                AddPoint(_vertices, item, eps);
            }

            foreach (Constraint item in constraints)
            {
                var (a, b) = item;
                int ai = AddPoint(_vertices, a, eps);
                int bi = AddPoint(_vertices, b, eps);
                _constraints.Add((ai, bi)); 
            }
        }

        Polygon BuildPolygon(int index, List<Vec2> points, double eps)
        {
            List<int> indices = new List<int>(points.Count);
            foreach (var item in points)
            {
                indices.Add(AddPoint(_vertices, item, eps));
            }
            return new Polygon(index, indices);
        }

        void CleanConstraints(List<Constraint> constraints, (List<Vec2>, Rect) contour, List<(List<Vec2>, Rect)> holeContours, double eps)
        {
            for (int i = constraints.Count - 1; i >= 0; i--)
            {
                Constraint current = constraints[i];
                var (a, b) = current;
                var (x, y) = Vec2.MidPoint(a, b);
                if (!Contains(contour, holeContours, x, y, eps))
                {
                    constraints.RemoveAt(i);
                }
            }
        }

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

        void AddConstraint(List<Constraint> constraints, Vec2 p1, Vec2 p2, EConstraint type, double eps)
        {
            Stack<Constraint> toProcess = new Stack<Constraint>();
            toProcess.Push(new Constraint(p1, p2, type));
            while (toProcess.Count > 0)
            {
                Constraint current = toProcess.Pop();
                var (a1, a2) = current;
                bool split = false;

                for (int i = constraints.Count - 1; i >= 0; i--)
                {
                    Constraint existing = constraints[i];
                    var (b1, b2) = existing;

                    if (existing.OnNode(a1, eps) || existing.OnNode(a2, eps))
                        continue;

                    Vec2 inter = CDT.Intersect(a1, a2, b1, b2);
                    if (!inter.IsNaN())
                    {
                        constraints.RemoveAt(i);
                        var (c1a, c1b) = current.Split(inter);
                        var (c2a, c2b) = existing.Split(inter);
                        toProcess.Push(c1a);
                        toProcess.Push(c1b);
                        toProcess.Push(c2a);
                        toProcess.Push(c2b);
                        split = true;
                        break;
                    }

                    if (existing.OnEdge(a1, eps))
                    {
                        constraints.RemoveAt(i);
                        var (e1, e2) = existing.Split(a1);
                        toProcess.Push(e1);
                        toProcess.Push(e2);
                        toProcess.Push(current);
                        split = true;
                        break;
                    }

                    if (existing.OnEdge(a2, eps))
                    {
                        constraints.RemoveAt(i);
                        var (e1, e2) = existing.Split(a2);
                        toProcess.Push(e1);
                        toProcess.Push(e2);
                        toProcess.Push(current);
                        split = true;
                        break;
                    }

                    if (current.OnEdge(b1, eps))
                    {
                        constraints.RemoveAt(i);
                        var (c1, c2) = current.Split(b1);
                        toProcess.Push(c1);
                        toProcess.Push(c2);
                        toProcess.Push(existing);
                        split = true;
                        break;
                    }

                    if (current.OnEdge(b2, eps))
                    {
                        constraints.RemoveAt(i);
                        var (c1, c2) = current.Split(b2);
                        toProcess.Push(c1);
                        toProcess.Push(c2);
                        toProcess.Push(existing);
                        split = true;
                        break;
                    }
                }

                if (!split)
                {
                    constraints.Add(current);
                }
            }
        }

        void ExtractConstraints(List<Constraint> constraints, List<Vec2> contour, EConstraint type, double eps)
        {
            int count = contour.Count;
            for (int i = 0; i < count; i++)
            {
                Vec2 p1 = contour[i];
                Vec2 p2 = contour[(i + 1) % count];
                AddConstraint(constraints, p1, p2, type, eps);
            }
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
                    if (!CDT.Intersect(p1, p2, q1, q2).IsNaN())
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


        public int AddPoint(List<Vec2> all, Vec2 v, double eps = 0)
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
