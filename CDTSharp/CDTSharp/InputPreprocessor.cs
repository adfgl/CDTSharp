using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDTSharp
{
    using static PolygonHelper;

    public class InputPreprocessor
    {
        readonly CDTInput _input;
        readonly List<CDTVector> _vertices;
        readonly List<(int a, int b)> _constraints;
        readonly List<(Polygon, Polygon[])> _polygons;
        readonly Rect _rect;

        public InputPreprocessor(CDTInput input, double eps = 1e-8)
        {
            _input = input;

            _vertices = new List<CDTVector>();
            _constraints = new List<(int, int)>();
            _polygons = new List<(Polygon, Polygon[])>();
            List<Constraint> constraints = new List<Constraint>();

            Rect rect = Rect.Empty;
            for (int i = 0; i < input.Polygons.Count; i++)
            {
                rect = rect.Union(ProcessPolygon(constraints, i, input.Polygons[i], eps));
            }
            _rect = rect;


            HashSet<Segment> seen = new HashSet<Segment>();
            foreach (Constraint item in constraints)
            {
                var (a, b) = item;
                int ai = AddPoint(_vertices, a, eps);
                int bi = AddPoint(_vertices, b, eps);

                if (ai == bi) continue;

                if (seen.Add(new Segment(ai, bi)))
                {
                    _constraints.Add((ai, bi));
                }
            }
        }

        public CDTInput Input => _input;
        public List<CDTVector> Vertices => _vertices;
        public List<(int a, int b)> Constraints => _constraints;
        public List<(Polygon, Polygon[])> Polygons => _polygons;
        public Rect Rect => _rect;

        public bool ContainsContour(CDTVector v)
        {
            foreach (var item in _polygons)
            {
                if (item.Item1.Contains(_vertices, v.x, v.y))
                {
                    return true;
                }
            }
            return false;
        }

        Rect ProcessPolygon(List<Constraint> constraints, int index, CDTPolygon cdtPolygon, double eps)
        {
            (List<CDTVector>, Rect) contour = ExtractContour(cdtPolygon.Contour, eps);
            List<(List<CDTVector>, Rect)> holeContours = new List<(List<CDTVector>, Rect)>();
            if (cdtPolygon.Holes != null) ExtractHoles(holeContours, contour, cdtPolygon.Holes, eps);
       
            ExtractConstraints(constraints, contour.Item1, EConstraint.Contour, eps);
            foreach ((List<CDTVector>, Rect) item in holeContours)
            {
                ExtractConstraints(constraints, item.Item1, EConstraint.Hole, eps);
            }

            if (cdtPolygon.Constraints != null)
            {
                foreach ((CDTVector a, CDTVector b) in cdtPolygon.Constraints)
                {
                    AddConstraint(constraints, a, b, EConstraint.User, eps);
                }
            }

            List<CDTVector> pointConstraints = new List<CDTVector>();
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
            _polygons.Add((contourPoly, holePolygons.ToArray()));

            foreach (var item in pointConstraints)
            {
                AddPoint(_vertices, item, eps);
            }
            return contour.Item2;
        }

        Polygon BuildPolygon(int index, List<CDTVector> points, double eps)
        {
            List<int> indices = new List<int>(points.Count);
            foreach (var item in points)
            {
                indices.Add(AddPoint(_vertices, item, eps));
            }
            return new Polygon(index, indices);
        }

        void CleanConstraints(List<Constraint> constraints, (List<CDTVector>, Rect) contour, List<(List<CDTVector>, Rect)> holeContours, double eps)
        {
            for (int i = constraints.Count - 1; i >= 0; i--)
            {
                Constraint current = constraints[i];
                var (a, b) = current;
                var (x, y) = CDTVector.MidPoint(a, b);

                bool remove;
                switch (current.type)
                {
                    case EConstraint.Hole:
                        remove = !Contains(contour, x, y, eps);
                        break;
                    case EConstraint.User:
                        remove = !Contains(contour, holeContours, x, y, eps);
                        break;
                    default:
                        remove = false;
                        break;
                }

                if (remove)
                {
                    constraints.RemoveAt(i);
                }
            }
        }


        void AddConstraint(List<Constraint> constraints, CDTVector p1, CDTVector p2, EConstraint type, double eps)
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

                    if (CDT.Intersect(a1, a2, b1, b2, out CDTVector inter))
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
                }

                if (!split)
                {
                    constraints.Add(current);
                }
            }
        }

        void ExtractConstraints(List<Constraint> constraints, List<CDTVector> contour, EConstraint type, double eps)
        {
            int count = contour.Count;
            for (int i = 0; i < count; i++)
            {
                CDTVector p1 = contour[i];
                CDTVector p2 = contour[(i + 1) % count];
                AddConstraint(constraints, p1, p2, type, eps);
            }
        }

        (List<CDTVector>, Rect) ExtractContour(List<CDTVector> vertices, double eps)
        {
            double minX, minY, maxX, maxY;
            minX = minY = double.MaxValue;
            maxX = maxY = double.MinValue;
            List<CDTVector> contour = new List<CDTVector>(vertices.Count);
            foreach (CDTVector item in vertices)
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

       void ExtractHoles(List<(List<CDTVector>, Rect)> contours, (List<CDTVector>, Rect) contour, List<List<CDTVector>> holes, double eps)
        {
            foreach (List<CDTVector> hole in holes)
            {
                (List<CDTVector>, Rect) holeContour = ExtractContour(hole, eps);
                if (!Contains(contour, holeContour, eps)
                    && 
                    !Intersects(contour, holeContour, eps))
                {
                    continue;
                }

                bool add = true;
                for (int i = contours.Count - 1; i >= 0; i--)
                {
                    (List<CDTVector>, Rect) existing = contours[i];
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

        public int AddPoint(List<CDTVector> all, CDTVector v, double eps = 0)
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

        public static int IndexOf(List<CDTVector> all, CDTVector v, double eps = 0)
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
