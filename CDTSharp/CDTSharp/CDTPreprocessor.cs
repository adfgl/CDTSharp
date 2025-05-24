using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CDTSharp
{
    public class CDTPreprocessor
    {
        readonly CDTInput _input;
        readonly List<(Vec2 a, Vec2 b)> _constraints = new List<(Vec2, Vec2)>();
        readonly List<(Polygon, Polygon[])> _polygons = new List<(Polygon, Polygon[])>();
        readonly List<Vec2> _constraintPoint = new List<Vec2>();
        readonly Rect _rect;

        public CDTPreprocessor(CDTInput input, double eps = 1e-8)
        {
            _input = input;
            _rect = Rect.Empty;

            List<Constraint> constraints = new List<Constraint>();
            for (int i = 0; i < input.Polygons.Count; i++)
            {
                _rect = _rect.Union(ProcessPolygon(constraints, i, input.Polygons[i], eps));
            }

            for (int i = constraints.Count - 1; i >= 0; i--)
            {
                Constraint item = constraints[i];
                var (a, b) = item;
                var (x, y) = Vec2.MidPoint(a, b);

                bool remove = false;
                if (item.type == EConstraint.Hole)
                {
                    remove = true;
                    foreach ((Polygon contour, _) in _polygons)
                    {
                        if (contour.Contains(x, y))
                        {
                            remove = false;
                            break;
                        }
                    }
                }
                else if (item.type == EConstraint.User)
                {
                    // User constraint must be inside at least one contour and outside all holes of that contour
                    bool inAtLeastOneContour = false;
                    bool inAnyHole = false;
                    foreach ((Polygon contour, Polygon[] holes) in _polygons)
                    {
                        if (contour.Contains(x, y))
                        {
                            inAtLeastOneContour = true;

                            foreach (var hole in holes)
                            {
                                if (hole.Contains(x, y))
                                {
                                    inAnyHole = true;
                                    break;
                                }
                            }

                            if (inAnyHole)
                                break;
                        }
                    }

                    remove = !inAtLeastOneContour || inAnyHole;
                }

                if (remove)
                {
                    constraints.RemoveAt(i);
                    continue;
                }
                _constraints.Add((a, b));
            }
        }

        public CDTInput Input => _input;
        public List<(Vec2 a, Vec2 b)> Constraints => _constraints;
        public List<Vec2> PointConstraints => _constraintPoint;
        public List<(Polygon, Polygon[])> Polygons => _polygons;
        public Rect Rect => _rect;

        Rect ProcessPolygon(List<Constraint> constraints, int index, CDTPolygon cdtPolygon, double eps)
        {
            Polygon contour = new Polygon(index, cdtPolygon.Contour);
            List<Polygon> holeContours = new List<Polygon>();
            if (cdtPolygon.Holes != null) ExtractHoles(holeContours, contour, cdtPolygon.Holes, eps);
       
            ExtractConstraints(constraints, contour.verts, EConstraint.Contour, eps);
            foreach (Polygon item in holeContours)
            {
                ExtractConstraints(constraints, item.verts, EConstraint.Hole, eps);
            }

            if (cdtPolygon.Constraints != null)
            {
                foreach ((Vec2 a, Vec2 b) in cdtPolygon.Constraints)
                {
                    AddConstraint(constraints, a, b, EConstraint.User, eps);
                }
            }

            if (cdtPolygon.Points != null)
            {
                foreach (var v in cdtPolygon.Points)
                {
                    if (!Polygon.Contains(contour, holeContours, v.x, v.y, eps))
                    {
                        continue;
                    }

                    _constraintPoint.Add(v);

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

            _polygons.Add((contour, holeContours.ToArray()));
            return contour.rect;
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

                    if (GeometryHelper.Intersect(a1, a2, b1, b2, out Vec2 inter))
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

       void ExtractHoles(List<Polygon> contours, Polygon contour, List<List<Vec2>> holes, double eps)
        {
            foreach (List<Vec2> hole in holes)
            {
                Polygon holeContour = new Polygon(-1, hole);
                if (!Polygon.Contains(contour, holeContour, eps)
                    && 
                    !Polygon.Intersects(contour, holeContour, eps))
                {
                    continue;
                }

                bool add = true;
                for (int i = contours.Count - 1; i >= 0; i--)
                {
                    Polygon existing = contours[i];
                    if (Polygon.Contains(existing, holeContour, eps))
                    {
                        add = false;
                        break;
                    }

                    if (Polygon.Contains(holeContour, existing, eps))
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
    }
}
