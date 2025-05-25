namespace CDTSharp
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Security.Cryptography;

    public class CDT
    {
        public const double EPS = 1e-12;
        public const int NO_INDEX = -1;

        readonly RobustPredicates predicates = new RobustPredicates();

        readonly List<Vec2> _v = new List<Vec2>();
        readonly List<CDTTriangle> _t = new List<CDTTriangle>();
        readonly List<int> _affected = new List<int>();
        readonly Stack<Edge> _toLegalize = new Stack<Edge>();
        readonly List<Segment> _constrainedEdges = new List<Segment>();

        public void Summary()
        {
            double minArea = double.MaxValue;
            double maxArea = double.MinValue;
            double avgArea = 0;

            double minAng = double.MaxValue;
            double maxAng = double.MinValue;
            double avgAng = 0;
            foreach (var item in Triangles)
            {
                double area = item.area;
                avgArea += area;

                for (int i = 0; i < 3; i++)
                {
                    Vec2 a = Vertices[(i + 2) % 4];
                    Vec2 b = Vertices[i];
                    Vec2 c = Vertices[(i + 1) % 4];

                    double ang = CDT.Angle(a, b, c) * 180 / Math.PI;
                    if (minAng > ang) minAng = ang;
                    if (maxAng < ang) maxAng = ang;
                    avgAng += ang;
                }

                if (minArea > area) minArea = area;
                if (maxArea < area) maxArea = area;
            }
            avgArea /= Triangles.Count;
            avgAng /= 3 * Triangles.Count;
            Console.WriteLine();
            Console.WriteLine("count tri: " + Triangles.Count);
            Console.WriteLine("count vtx: " + Vertices.Count);
            Console.WriteLine("Area min: " + minArea);
            Console.WriteLine("Area max: " + maxArea);
            Console.WriteLine("Area avg: " + avgArea);
            Console.WriteLine();
            Console.WriteLine("Ang min: " + minAng);
            Console.WriteLine("Ang max: " + maxAng);
            Console.WriteLine("Ang avg: " + avgAng);
        }

        public CDT Triangulate(CDTInput input)
        {
            _v.Clear();
            _t.Clear();
            _affected.Clear();
            _toLegalize.Clear();
            _constrainedEdges.Clear();

            CDTPreprocessor processed = new CDTPreprocessor(input);
            Rect rect = processed.Rect;
            AddSuperTriangle(rect);

            double expansionMargin = Math.Max(5, Math.Max(rect.dx, rect.dy) * 0.01);
            PointQuadtree quad = new PointQuadtree(rect.Expand(expansionMargin));

            HashSet<Segment> seen = new HashSet<Segment>();
            foreach ((Vec2 a, Vec2 b) in processed.Constraints)
            {
                int ai = quad.IndexOf(a.x, a.y);
                if (ai == NO_INDEX)
                {
                    ai = Insert(quad, a);
                }
                else
                {
                    ai += 3;
                }

                int bi = quad.IndexOf(b.x, b.y);
                if (bi == NO_INDEX)
                {
                    bi = Insert(quad, b);
                }
                else
                {
                    bi += 3;
                }

                var s = new Segment(ai, bi);
                if (seen.Add(s))
                {
                    _constrainedEdges.Add(s);
                    AddConstraint(ai, bi);
                }
            }

            foreach (Vec2 item in processed.PointConstraints)
            {
                Insert(quad, item);
            }

            MarkHoles(processed.Polygons);

            if (input.Refine)
            {
                int refined = Refine(input.MaxArea);
            }

            FinalizeMesh(input.KeepConvex, input.KeepSuper);
            return this;
        }


        public List<Vec2> Vertices => _v;
        public List<CDTTriangle> Triangles => _t;
        public List<Segment> Constraints => _constrainedEdges;

        void MarkHoles(List<(Polygon, Polygon[])> polys)
        {
            for (int i = 0; i < _t.Count; i++)
            {
                CDTTriangle t = _t[i];
                t.parents.Clear();
                if (t.super)
                    continue;

                var (x, y) = Center(t);

                foreach (var item in polys)
                {
                    Polygon contour = item.Item1;
                    if (item.Item1.Contains(x, y))
                    {
                        bool insideHole = false;
                        foreach (Polygon hole in item.Item2)
                        {
                            if (hole.Contains(x, y))
                            {
                                insideHole = true;
                                break;
                            }
                        }

                        if (!insideHole)
                        {
                            t.parents.Add(contour.index);
                        }
                    }
                }

                _t[i] = t;
            }
        }


        Vec2 Center(CDTTriangle t)
        {
            return (_v[t.indices[0]] + _v[t.indices[1]] + _v[t.indices[2]]) / 3;
        }

        bool IsVisibleFromInterior(List<Segment> segments, Segment seg, Vec2 point)
        {
            Vec2 mid = Vec2.MidPoint(_v[seg.a], _v[seg.b]);
            foreach (Segment s in segments)
            {
                if (s.Equals(seg))
                    continue;

                if (GeometryHelper.Intersect(mid, point, _v[s.a], _v[s.b], out _))
                {
                    return false;
                }
                  
            }
            return true;
        }

        bool Enchrouched(Segment seg, List<Vec2> verts)
        {
            var (a, b) = seg;
            Circle diam = new Circle(_v[a], _v[b]);

            foreach (var item in verts)
            {
                if (diam.Contains(item.x, item.y))
                    return true;
            }
            return false;
        }

        public int Refine(double maxArea)
        {
            int refinedCount = 0;
            int falseCheck = 0;

            List<Segment> allSegments = new List<Segment>();
            Queue<Segment> segmentQueue = new Queue<Segment>();
            Queue<int> triangleQueue = new Queue<int>();
            foreach (var segment in _constrainedEdges)
            {
                allSegments.Add(segment);
                if (Enchrouched(segment, _v))
                {
                    segmentQueue.Enqueue(segment);
                }
            }

            for (int i = 0; i < _t.Count; i++)
            {
                CDTTriangle tri = _t[i];
                if (IsBadTriangle(tri, maxArea))
                {
                    triangleQueue.Enqueue(i);
                }
            }

            double minSqrLen = Math.Sqrt(4.0 * maxArea / Math.Sqrt(3));
            minSqrLen *= minSqrLen;

            int checkedTris = 0;
            int checkedEdges = 0;
            while (segmentQueue.Count > 0 || triangleQueue.Count > 0)
            {
                if (segmentQueue.Count > 0)
                {
                    Segment seg = segmentQueue.Dequeue();
                    checkedEdges++;

                    var (ia, ib) = seg;

                    Vec2 a = _v[ia];
                    Vec2 b = _v[ib];
                    Circle diam = new Circle(a, b);
                    if (diam.radiusSquared * 4 < minSqrLen)
                    {
                        continue;
                    }

                    Vec2 mid = new Vec2(diam.x, diam.y);
                    Edge e = FindEdge(ia, ib);
                    if (e.index == NO_INDEX)
                    {
                        throw new Exception($"Midpoint of segment ({ia},{ib}) not found on any edge.");
                    }

                    int insertedIndex = Insert(mid, e.triangle, e.index);
                    refinedCount++;

                    Segment s1 = new Segment(ia, insertedIndex);
                    Segment s2 = new Segment(insertedIndex, ib);
                    allSegments.Remove(seg);
                    allSegments.Add(s1);
                    allSegments.Add(s2);

                    if (IsVisibleFromInterior(allSegments, s1, mid) && Enchrouched(s1, _v)) segmentQueue.Enqueue(s1);
                    if (IsVisibleFromInterior(allSegments, s2, mid) && Enchrouched(s2, _v)) segmentQueue.Enqueue(s2);

                    foreach (var item in _affected)
                    {
                        if (IsBadTriangle(_t[item], maxArea))
                        {
                            triangleQueue.Enqueue(item);
                        }
                    }

                    //for (int i = 0; i < _t.Count; i++)
                    //{
                    //    if (IsBadTriangle(_t[i], maxArea))
                    //    {
                    //        triangleQueue.Enqueue(i);
                    //    }
                    //}
                    continue;
                }


                if (triangleQueue.Count > 0)
                {
                    int triIndex = triangleQueue.Dequeue();
                    checkedTris++;
                    CDTTriangle tri = _t[triIndex];

                    if (!IsBadTriangle(tri, maxArea))
                    {
                        falseCheck++;
                        continue;
                    }

                    Vec2 cc = new Vec2(tri.circle.x, tri.circle.y);
                    bool encroaches = false;
                    foreach (Segment seg in allSegments)
                    {
                        Circle diam = new Circle(_v[seg.a], _v[seg.b]);
                        if (diam.Contains(cc.x, cc.y) && IsVisibleFromInterior(allSegments, seg, cc))
                        {
                            segmentQueue.Enqueue(seg);
                            encroaches = true;
                        }
                    }

                    if (encroaches)
                    {
                        continue;
                    }

                    var (tIndex, eIndex) = FindContaining(cc, EPS);
                    if (tIndex == NO_INDEX || _t[tIndex].parents.Count == 0) continue;

                    int vi = Insert(cc, tIndex, eIndex);
                    refinedCount++;

                    //for (int i = 0; i < _t.Count; i++)
                    //{
                    //    if (IsBadTriangle(_t[i], maxArea))
                    //    {
                    //        triangleQueue.Enqueue(i);
                    //    }
                    //}

                    foreach (var item in _affected)
                    {
                        if (IsBadTriangle(_t[item], maxArea))
                        {
                            triangleQueue.Enqueue(item);
                        }
                    }
                }
            }
            return refinedCount;
        }

        public bool IsBadTriangle(CDTTriangle tri, double maxAllowedArea)
        {
            if (tri.super || tri.parents.Count == 0) return false;

            if (tri.area > maxAllowedArea) return true;

            Vec2 a = _v[tri.indices[0]];
            Vec2 b = _v[tri.indices[1]];
            Vec2 c = _v[tri.indices[2]];

            double ab = Vec2.SquareLength(a - b);
            double bc = Vec2.SquareLength(b - c);
            double ca = Vec2.SquareLength(c - a);
            double minEdgeSq = Math.Min(ab, Math.Min(bc, ca));
            return tri.circle.radiusSquared / minEdgeSq > 2;
        }

        int Insert(PointQuadtree quad, Vec2 v)
        {
            if (quad.Contains(v.x, v.y, EPS))
            {
                return NO_INDEX;
            }

            quad.Insert(v.x, v.y);
            (int triangleIndex, int edgeIndex) = FindContaining(v, EPS);
            return Insert(v, triangleIndex, edgeIndex);
        }

        int Insert(Vec2 vertex, int triangle, int edge)
        {
            int vertexindex = _v.Count;
            _v.Add(vertex);

            if (edge == NO_INDEX)
            {
                SplitTriangle(triangle, vertexindex);
            }
            else
            {
                SplitEdge(triangle, edge, vertexindex);
            }
            Legalize();
            return vertexindex;
        }

        void AddSuperTriangle(Rect rect)
        {
            double dmax = Math.Max(rect.maxX - rect.minX, rect.maxY - rect.minY);
            double midx = (rect.maxX + rect.minX) * 0.5;
            double midy = (rect.maxY + rect.minY) * 0.5;
            double scale = 2;

            Vec2 a = new Vec2(midx - scale * dmax, midy - scale * dmax);
            Vec2 b = new Vec2(midx, midy + scale * dmax);
            Vec2 c = new Vec2(midx + scale * dmax, midy - scale * dmax);

            _v.Add(a);
            _v.Add(b);
            _v.Add(c);

            Circle circle = new Circle(a, b, c);
            double area = Area(a, b, c);
            _t.Add(new CDTTriangle(circle, area, 0, 1, 2));
        }

        public void FinalizeMesh(bool keepConvex = false, bool keepSuper = false)
        {
            if (!keepSuper)
            {
                _v.RemoveRange(0, 3);
            }

            Dictionary<int, int> remap = new Dictionary<int, int>();
            int write = 0;
            for (int read = 0; read < _t.Count; read++)
            {
                CDTTriangle tri = _t[read];

                bool discard = false;
                if (!keepSuper)
                {
                    discard = tri.super || (!keepConvex && tri.parents.Count == 0);
                }

                if (!discard)
                {
                    _t[write] = tri;
                    remap[read] = write;
                    write++;
                }
                else
                {
                    for (int i = 0; i < 3; i++)
                    {
                        int twinIndex = tri.adjacent[i];
                        if (twinIndex == NO_INDEX) continue;

                        CDTTriangle twin = _t[twinIndex];
                        int a = tri.indices[i];
                        int b = tri.indices[CDTTriangle.NEXT[i]];
                        int edgeInTwin = twin.IndexOf(b, a);
                        if (edgeInTwin != NO_INDEX)
                        {
                            twin.adjacent[edgeInTwin] = NO_INDEX;
                            _t[twinIndex] = twin;
                        }
                    }
                }
            }

            if (write < _t.Count)
                _t.RemoveRange(write, _t.Count - write);

            for (int i = 0; i < _t.Count; i++)
            {
                CDTTriangle tri = _t[i];
                for (int j = 0; j < 3; j++)
                {
                    if (!keepSuper) tri.indices[j] -= 3;
                    int oldAdj = tri.adjacent[j];
                    tri.adjacent[j] = remap.TryGetValue(oldAdj, out int newAdj) ? newAdj : NO_INDEX;
                }
                _t[i] = tri;
            }
        }
        
        public void SplitEdge(int triangleIndex, int edgeIndex, int vertexIndex)
        {
            /*
                        v1                          v1            
                        /\                          /|\             
                       /  \                        / | \           
                      /    \                      /  |  \          
                 e01 /      \ e12            e01 /   |   \ e12     
                    /   t0   \                  /    |    \        
                   /          \                / t0  |  t1 \       
                  /    e20     \              /      |      \      
              v0 +--------------+ v2      v0 +-------v4------+ v2  
                  \     e02    /              \      |      /      
                   \          /                \ t3  |  t2 /       
                    \   t1   /                  \    |    /        
                 e30 \      / e23            e30 \   |   / e23     
                      \    /                      \  |  /          
                       \  /                        \ | /           
                        \/                          \|/            
                        v3                          v3            
            */


            int t0 = triangleIndex;
            CDTTriangle tri0 = _t[t0];

            int t1 = tri0.adjacent[edgeIndex];
            CDTTriangle tri1 = _t[t1];

            int t2 = _t.Count;
            int t3 = t2 + 1;

            int e20 = edgeIndex;
            int e01 = CDTTriangle.NEXT[e20];
            int e12 = CDTTriangle.PREV[e20];

            int i0 = tri0.indices[e01];
            int i1 = tri0.indices[e12];
            int i2 = tri0.indices[e20];

            int e02 = tri1.IndexOf(i0, i2);
            int e23 = CDTTriangle.NEXT[e02];
            int e30 = CDTTriangle.PREV[e02];

            int i3 = tri1.indices[e30];
            int i4 = vertexIndex;

            Vec2 v0 = _v[i0];
            Vec2 v1 = _v[i1];
            Vec2 v2 = _v[i2];
            Vec2 v3 = _v[i3];
            Vec2 v4 = _v[i4];

            bool constrained = tri0.constraint[e20];

            double a0 = Area(v0, v1, v4);
            _t[t0] = new CDTTriangle(
                new Circle(v0, v1, v4), a0,
                i0, i1, i4,
                tri0.adjacent[e01], t1, t3,
                tri0.constraint[e01], false, constrained,
                tri0.parents);

            double a1 = tri0.area - a0;
            Circle c1 = new Circle(v1, v2, v4);
            _t[t1] = new CDTTriangle(
                c1, a1,
                i1, i2, i4,
                tri0.adjacent[e12], t2, t0,
                tri0.constraint[e12], constrained, false,
                tri0.parents);

            double a2 = Area(v2, v3, v4);
            _t.Add(new CDTTriangle(
                 new Circle(v2, v3, v4), a2,
                 i2, i3, i4,
                 tri1.adjacent[e23], t3, t1,
                 tri1.constraint[e23], false, constrained,
                 tri1.parents));

            double a3 = tri1.area - a2;
            _t.Add(new CDTTriangle(
                 new Circle(v3, v0, v4), a3,
                 i3, i0, i4,
                 tri1.adjacent[e30], t0, t2,
                 tri1.constraint[e30], constrained, false,
                 tri1.parents));

            int[] inds = [t0, t1, t2, t3];
            for (int i = 0; i < 4; i++)
            {
                int ti = inds[i];
                SetAdjacent(ti, 0);
                _toLegalize.Push(new Edge(ti, 0));
            }
        }

        public void SplitTriangle(int triangleIndex, int vertexIndex)
        {

            /*
                    /|\ 
                   / | \
                  /  |  \
                 /  / \  \ 
                / _/   \_ \
               +/----+----\+

             */

            CDTTriangle tri = _t[triangleIndex];

            int t0 = triangleIndex;
            int t1 = _t.Count;
            int t2 = t1 + 1;

            int i0 = tri.indices[0];
            int i1 = tri.indices[1];
            int i2 = tri.indices[2];
            int i3 = vertexIndex;

            Vec2 v0 = _v[i0];
            Vec2 v1 = _v[i1];
            Vec2 v2 = _v[i2];
            Vec2 v3 = _v[i3];

            double a0 = Area(v0, v1, v3);
            _t[t0] = new CDTTriangle(
                new Circle(v0, v1, v3), a0,
               i0, i1, i3,
               tri.adjacent[0], t1, t2,
               tri.constraint[0], false, false,
               tri.parents);

            double a1 = Area(v1, v2, v3);
            Circle c1 = new Circle(v1, v2, v3);
            _t.Add(new CDTTriangle(
               c1, a1,
               i1, i2, i3,
               tri.adjacent[1], t2, t0,
               tri.constraint[1], false, false,
               tri.parents));

            double a2 = tri.area - a0 - a1;
            _t.Add(new CDTTriangle(
                new Circle(v2, v0, v3), a2,
               i2, i0, i3,
               tri.adjacent[2], t0, t1,
               tri.constraint[2], false, false,
               tri.parents));

            int[] inds = [t0, t1, t2];
            for (int i = 0; i < 3; i++)
            {
                int ti = inds[i];
                SetAdjacent(ti, 0);
                _toLegalize.Push(new Edge(ti, 0));
            }
        }

        public void FlipEdge(int triangleIndex, int edgeIndex)
        {
            /*
             
             v1 - is inserted point, we want to propagate flip away from it, otherwise we 
             are risking ending up in flipping degeneracy
                          v1                        v1
                          /\                        /|\
                         /  \                      / | \
                        /    \                    /  |  \
                   e01 /      \ e12          e01 /   |   \ e12
                      /   t0   \                /    |    \
                     /          \              /     | e20 \ 
                    /    e20     \            /      |      \
                v0 +--------------+ v2    v0 +   t1  |  t0   + v2
                    \     e02    /            \      |      /
                     \          /              \ e02 |     /
                      \   t1   /                \    |    /
                   e30 \      / e23          e30 \   |   / e23
                        \    /                    \  |  /
                         \  /                      \ | /
                          \/                        \|/
                          v3                        v3
            */

            int t0 = triangleIndex;
            CDTTriangle tri0 = _t[t0];

            int t1 = tri0.adjacent[edgeIndex];
            CDTTriangle tri1 = _t[t1];

            int e20 = edgeIndex;
            int e01 = CDTTriangle.NEXT[e20];
            int e12 = CDTTriangle.PREV[e20];

            int i0 = tri0.indices[e01];
            int i1 = tri0.indices[e12];
            int i2 = tri0.indices[e20];

            int e02 = tri1.IndexOf(i0, i2);
            int e23 = CDTTriangle.NEXT[e02];
            int e30 = CDTTriangle.PREV[e02];

            int i3 = tri1.indices[e30];

            Vec2 v0 = _v[i0];
            Vec2 v1 = _v[i1];
            Vec2 v2 = _v[i2];
            Vec2 v3 = _v[i3];

            List<int> parents = new List<int>(tri0.parents);
            foreach (int p in tri1.parents)
            {
                if (!parents.Contains(p)) parents.Add(p);
            }

            bool constraint = tri0.constraint[e20];

            double a0 = Area(v3, v1, v2);
            _t[t0] = new CDTTriangle(
                new Circle(v3, v1, v2), a0,
                i3, i1, i2,
                t1, tri0.adjacent[e12], tri1.adjacent[e23],
                constraint, tri0.constraint[e12], tri1.constraint[e23],
                parents);

            double a1 = tri0.area + tri1.area - a0;
            _t[t1] = new CDTTriangle(
               new Circle(v1, v3, v0), a1,
               i1, i3, i0,
               t0, tri1.adjacent[e30], tri0.adjacent[e01],
               constraint, tri1.constraint[e30], tri0.constraint[e01],
               parents);

            SetAdjacent(t0, 1);
            SetAdjacent(t0, 2);
            SetAdjacent(t1, 1);
            SetAdjacent(t1, 2);

            // push edge oppsote to v1
            _toLegalize.Push(new Edge(t0, 2));
            _toLegalize.Push(new Edge(t1, 1));
        }

        public bool ShouldFlip(int triangleIndex, int edgeIndex)
        {
            /*
                          v1            
                          /\            
                         /  \           
                        /    \          
                   e01 /      \ e12     
                      /   t0   \        
                     /          \       
                    /    e20     \      
                v0 +--------------+ v2  
                    \     e02    /      
                     \          /       
                      \   t1   /        
                   e30 \      / e23     
                        \    /          
                         \  /           
                          \/            
                          v3            
            */

            int e20 = edgeIndex;
            CDTTriangle t0 = _t[triangleIndex];
            int t1Index = t0.adjacent[e20];

            if (t1Index == NO_INDEX || t0.constraint[e20])
            {
                return false;
            }

            int i0 = t0.indices[CDTTriangle.NEXT[e20]];
            int i1 = t0.indices[CDTTriangle.PREV[e20]];
            int i2 = t0.indices[e20];

            CDTTriangle t1 = _t[t1Index];

            int e02 = t1.IndexOf(i0, i2);

            int i3 = t1.indices[CDTTriangle.PREV[e02]];
            Vec2 v3 = _v[i3];
            return
                t0.circle.Contains(v3.x, v3.y) &&
                GeometryHelper.ConvexQuad(_v[i0], _v[i1], _v[i2], v3);
        }

        public int EntranceTriangle(int triangleIndexContainingA, int aIndex, int bIndex)
        {
            Vec2 vb = _v[bIndex];
            TriangleWalker walker = new TriangleWalker(_t, triangleIndexContainingA, aIndex);
            do
            {
                CDTTriangle tri = _t[walker.Current];
                int toRightCount = 0;
                for (int i = 0; i < 3; i++)
                {
                    int a = tri.indices[i];
                    int b = tri.indices[CDTTriangle.NEXT[i]];
                    if (a == aIndex || b == aIndex)
                    {
                        double orientation = predicates.Orient(_v[a], _v[b], vb);
                        if (orientation <= 0)
                        {
                            toRightCount++;
                        }
                    }
                }

                if (toRightCount == 2)
                {
                    return walker.Current;
                }
            }
            while (walker.MoveNextCW());

            throw new Exception("Could not find entrance triangle.");
        }

        void Panic()
        {
            FinalizeMesh();
            Console.WriteLine(this.ToSvg(fill:false)); ;
        }

        public (int triangleIndex, int edgeIndex) FindContaining(Vec2 point, double tolerance = 1e-12, int seed = NO_INDEX)
        {
            int max = _t.Count * 3;
            int steps = 0;

            int current = seed == NO_INDEX ? _t.Count - 1 : seed;
            while (true)
            {
                if (steps++ > max)
                {
                    Panic();
                    throw new Exception("Could not find containing triangle. Most likely mesh topology is invalid.");
                }

                if (current == NO_INDEX)
                {
                    return (NO_INDEX, NO_INDEX);
                }

                bool inside = true;
                CDTTriangle tri = _t[current];
                for (int i = 0; i < 3; i++)
                {
                    Vec2 a = _v[tri.indices[i]];
                    Vec2 b = _v[tri.indices[CDTTriangle.NEXT[i]]];

                    double orientation = predicates.Orient(a, b, point);
                    if (orientation > 0)
                    {
                        current = tri.adjacent[i];
                        inside = false;
                        break;
                    }
                }

                if (inside)
                {
                    CDTTriangle t = _t[current];
                    for (int i = 0; i < 3; i++)
                    {
                        Vec2 a = _v[t.indices[i]];
                        Vec2 b = _v[t.indices[CDTTriangle.NEXT[i]]];
                        if (GeometryHelper.OnSegment(a, b, point, EPS))
                        {
                            return (current, i);
                        }
                    }
                    return (current, NO_INDEX);
                }
            }
        }

        void SetAdjacent(int triangle, int edge)
        {
            CDTTriangle t = _t[triangle];
            int adjIndex = t.adjacent[edge];
            if (adjIndex != NO_INDEX)
            {
                CDTTriangle adj = _t[adjIndex];
                int a = t.indices[edge];
                int b = t.indices[CDTTriangle.NEXT[edge]];

                int adjEdge = adj.IndexOf(b, a);
                if (adjEdge == NO_INDEX)
                {
                    throw new Exception();
                }
                adj.adjacent[adjEdge] = triangle;
            }
        }

        void SetConstraint(int triangle, int edge)
        {
            CDTTriangle t = _t[triangle];
            t.constraint[edge] = true;
            
            int adjIndex = t.adjacent[edge];
            if (adjIndex == NO_INDEX)
            {
                throw new InvalidOperationException($"Edge {triangle} in triangle {edge} has no twin. Cannot propagate constraint across broken topology.");
            }

            CDTTriangle adj = _t[adjIndex];
            int a = t.indices[edge];
            int b = t.indices[CDTTriangle.NEXT[edge]];
            adj.constraint[adj.IndexOf(b, a)] = true;
        }

        public void Legalize()
        {
            _affected.Clear();

            while (_toLegalize.Count > 0)
            {
                var (t0, edge) = _toLegalize.Pop();
                _affected.Add(t0);
   
                if (ShouldFlip(t0, edge))
                {
                    int t1 = _t[t0].adjacent[edge];
                    _affected.Add(t1);
                    FlipEdge(t0, edge);
                }
            }
        }

        public Edge FindEdge(int triangle, int start, int end)
        {
            TriangleWalker walker = new TriangleWalker(_t, triangle, end);
            do
            {
                int current = walker.Current;
                int edge = _t[current].IndexOfInvariant(start, end);
                if (edge != NO_INDEX)
                    return new Edge(current, edge);
            }
            while (walker.MoveNextCW());

            walker = new TriangleWalker(_t, triangle, end);
            return new Edge(triangle, NO_INDEX);
        }

        public Edge FindEdgeBrute(int aIndex, int bIndex)
        {
            for (int i = 0; i < _t.Count; i++)
            {
                int edge = _t[i].IndexOf(aIndex, bIndex);
                if (edge != NO_INDEX)
                {
                    return new Edge(i, edge);
                }
            }
            return new Edge(NO_INDEX, NO_INDEX);
        }

        public Edge FindEdge(int aIndex, int bIndex)
        {
            int lastContained = NO_INDEX;
            for (int triIndex = 0; triIndex < _t.Count; triIndex++)
            {
                CDTTriangle tri = _t[triIndex];
                for (int edgeIndex = 0; edgeIndex < 3; edgeIndex++)
                {
                    if (tri.indices[edgeIndex] == aIndex)
                    {
                        if (tri.indices[CDTTriangle.NEXT[edgeIndex]] == bIndex)
                        {
                            return new Edge(triIndex, edgeIndex);
                        }
                        lastContained = triIndex;
                        break;
                    }

                }
            }
            return FindEdge(lastContained, aIndex, bIndex);
        }

        public void AddConstraint(int aIndex, int bIndex)
        {
            if (aIndex == bIndex) return;

            Edge edge = FindEdge(aIndex, bIndex);
            if (edge.index != NO_INDEX)
            {
                SetConstraint(edge.triangle, edge.index);
                return;
            }

            Vec2 p1 = _v[aIndex], p2 = _v[bIndex];
            HashSet<int> visited = new();
            Queue<int> queue = new();

            int start = EntranceTriangle(edge.triangle, aIndex, bIndex);
            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                CDTTriangle tri = _t[current];

                for (int i = 0; i < 3; i++)
                {
                    if (tri.constraint[i]) continue;

                    int ia = tri.indices[i];
                    int ib = tri.indices[CDTTriangle.NEXT[i]];
                    Vec2 q1 = _v[ia], q2 = _v[ib];

                    if (GeometryHelper.Intersect(p1, p2, q1, q2, out _))
                    {
                        SetConstraint(current, i);
                        FlipEdge(current, i);
                        Legalize();

                        queue.Clear();
                        visited.Clear();
                        queue.Enqueue(current);
                        visited.Add(current);
                        break;
                    }
                }

                if (tri.IndexOf(bIndex) != NO_INDEX)
                    break;

                for (int i = 0; i < 3; i++)
                {
                    int next = tri.adjacent[i];
                    if (next != NO_INDEX && !visited.Contains(next))
                    {
                        int ia = tri.indices[i];
                        int ib = tri.indices[CDTTriangle.NEXT[i]];
                        Vec2 q1 = _v[ia], q2 = _v[ib];
                        double orient = predicates.Orient(q1, q2, p2);
                        if (orient >= 0)
                        {
                            queue.Enqueue(next);
                            visited.Add(next);
                        }
                    }
                }
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Angle(Vec2 a, Vec2 b, Vec2 c)
        {
            Vec2 ab = (a - b).Normalize();
            Vec2 cb = (c - b).Normalize();
            double dot = Vec2.Dot(ab, cb);
            return Math.Acos(Math.Clamp(dot, -1.0, 1.0)); 
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Clockwise(Vec2 a, Vec2 b, Vec2 c)
        {
            return Vec2.Cross(a, b, c) < 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Area(Vec2 a, Vec2 b, Vec2 c)
        {
            return Math.Abs(Vec2.Cross(a, b, c)) * 0.5;
        }
    }


}
