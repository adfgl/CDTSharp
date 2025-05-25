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

        readonly List<Vec2> _v = new List<Vec2>();
        readonly List<CDTTriangle> _t = new List<CDTTriangle>();
        readonly List<int> _affected = new List<int>();
        readonly Stack<Edge> _toLegalize = new Stack<Edge>();
        readonly List<Segment> _constrainedEdges = new List<Segment>();

        PointQuadtree _quad;

        public CDT Triangulate(CDTInput input)
        {
            Stopwatch sw = Stopwatch.StartNew();
            long last = 0;

            _v.Clear();
            _t.Clear();
            _affected.Clear();
            _toLegalize.Clear();
            _constrainedEdges.Clear();
            Console.WriteLine($"[Init] {sw.ElapsedMilliseconds - last} ms");
            last = sw.ElapsedMilliseconds;

            CDTPreprocessor processed = new CDTPreprocessor(input);
            Rect rect = processed.Rect;
            AddSuperTriangle(rect);
            Console.WriteLine($"[Preprocessing + SuperTriangle] {sw.ElapsedMilliseconds - last} ms");
            last = sw.ElapsedMilliseconds;

            double expansionMargin = Math.Max(5, Math.Max(rect.dx, rect.dy) * 0.01);
            _quad = new PointQuadtree(rect.Expand(expansionMargin));

            HashSet<Segment> seen = new HashSet<Segment>();
            foreach ((Vec2 a, Vec2 b) in processed.Constraints)
            {
                int ai = _quad.IndexOf(a.x, a.y);
                if (ai == NO_INDEX)
                {
                    ai = Insert(_quad, a);
                }
                else
                {
                    ai += 3;
                }

                int bi = _quad.IndexOf(b.x, b.y);
                if (bi == NO_INDEX)
                {
                    bi = Insert(_quad, b);
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

            Console.WriteLine($"[Add constraints] {sw.ElapsedMilliseconds - last} ms");
            last = sw.ElapsedMilliseconds;

            foreach (Vec2 item in processed.PointConstraints)
                Insert(_quad, item);
            Console.WriteLine($"[Insert point constraints] {sw.ElapsedMilliseconds - last} ms");
            last = sw.ElapsedMilliseconds;

            MarkHoles(processed.Polygons);
            Console.WriteLine($"[Mark holes] {sw.ElapsedMilliseconds - last} ms");
            last = sw.ElapsedMilliseconds;

            if (input.Refine)
            {
                int refined = Refine(processed, input.MaxArea, input.MinAngle);
                Console.WriteLine($"[Refine] {sw.ElapsedMilliseconds - last} ms (Refined: {refined})");
                last = sw.ElapsedMilliseconds;
            }

            FinalizeMesh(input.KeepConvex, input.KeepSuper);
            Console.WriteLine($"[Finalize mesh] {sw.ElapsedMilliseconds - last} ms");
            last = sw.ElapsedMilliseconds;

            Console.WriteLine($"[Total] Triangulation completed in {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"vts: {_v.Count} tris: {_t.Count}");
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

        bool Enchrouched(Segment seg)
        {
            var (a, b) = seg;
            Circle diam = new Circle(_v[a], _v[b]);
            double r = Math.Sqrt(diam.radiusSquared);
            Rect bounds = new Rect(diam.x - r, diam.y - r, diam.x + r, diam.y + r);

            List<PointQuadtree.Point> elems = _quad.Query(bounds);
            foreach (var item in elems)
            {
                int index = item.Index + 3;
                if (index == a || index == b) continue;

                Vec2 v = _v[index];
                if (diam.Contains(v.x, v.y))
                    return true;
            }
            return false;
        }

        public int Refine(CDTPreprocessor polys, double maxArea, double minAngle)
        {
            minAngle *= Math.PI / 180d;

            int refinedCount = 0;
            int falseCheck = 0;

            List<Segment> allSegments = new List<Segment>();
            Queue<Segment> segmentQueue = new Queue<Segment>();
            Queue<int> triangleQueue = new Queue<int>();
            foreach (var segment in _constrainedEdges)
            {
                allSegments.Add(segment);
                if (Enchrouched(segment))
                {
                    segmentQueue.Enqueue(segment);
                }
            }

            for (int i = 0; i < _t.Count; i++)
            {
                CDTTriangle tri = _t[i];
                if (IsBadTriangle(tri, minAngle, maxArea))
                {
                    triangleQueue.Enqueue(i);
                }
            }

            double minSqrLen = Math.Sqrt(4.0 * maxArea / Math.Sqrt(3)) * 0.25;
            minSqrLen *= minSqrLen;

            List<Vec2> segmentVertices = new List<Vec2>();
            List<Vec2> circumVertices = new List<Vec2>();

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
                    double sqrLen = Vec2.SquareLength(a - b);
                    if (sqrLen < minSqrLen)
                    {
                        continue;
                    }

                    Circle diam = new Circle(a, b);

                    Vec2 mid = new Vec2(diam.x, diam.y);
                    segmentVertices.Add(mid);

                    var (triIndex, edgeIndex) = FindContaining(mid, EPS);
                    if (edgeIndex == NO_INDEX)
                    {
                        Edge e = FindEdge(triIndex, ia, ib);
                        if (e.index == NO_INDEX)
                        {
                            throw new Exception($"Midpoint of segment ({ia},{ib}) not found on any edge.");
                        }
                        edgeIndex = e.index;
                    }

                    int insertedIndex = Insert(mid, triIndex, edgeIndex);

                    _quad.Insert(mid.x, mid.y);

                    refinedCount++;

                    Segment s1 = new Segment(ia, insertedIndex);
                    Segment s2 = new Segment(insertedIndex, ib);
                    allSegments.Remove(seg);
                    allSegments.Add(s1);
                    allSegments.Add(s2);

                    if (Enchrouched(s1) && IsVisibleFromInterior(allSegments, s1, mid)) segmentQueue.Enqueue(s1);
                    if (Enchrouched(s2) && IsVisibleFromInterior(allSegments, s2, mid)) segmentQueue.Enqueue(s2);

                    foreach (var item in _affected)
                    {
                        if (IsBadTriangle(_t[item], minAngle, maxArea))
                        {
                            triangleQueue.Enqueue(item);
                        }
                    }
                    continue;
                }

                if (triangleQueue.Count > 0)
                {
                    int triIndex = triangleQueue.Dequeue();
                    checkedTris++;
                    CDTTriangle tri = _t[triIndex];
                    if (!IsBadTriangle(tri, minAngle, maxArea))
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

                    circumVertices.Add(cc);
                 

                    var (tIndex, eIndex) = FindContaining(cc, EPS);
                    if (tIndex == NO_INDEX)
                    {
                        throw new Exception("Could not locate triangle for circumcenter.");
                    }

                    int vi = Insert(cc, tIndex, eIndex);
                    _quad.Insert(cc.x, cc.y);

                    refinedCount++;
                    foreach (var item in _affected)
                    {
                        if (IsBadTriangle(_t[item], minAngle, maxArea))
                        {
                            triangleQueue.Enqueue(item);
                        }
                    }
                }
            }

            Console.WriteLine("False check: " + falseCheck);
            Console.WriteLine("Checked tris: " + checkedTris);
            Console.WriteLine("Checked edges: " + checkedEdges);
            return refinedCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAngleTooSmall(Vec2 a, Vec2 b, Vec2 c, double cosMinAngle)
        {
            Vec2 ab = a - b;
            Vec2 cb = c - b;

            double abLen2 = Vec2.Dot(ab, ab);
            double cbLen2 = Vec2.Dot(cb, cb);
            if (abLen2 == 0 || cbLen2 == 0) return true; 

            double dot = Vec2.Dot(ab, cb);
            double cosTheta = dot / Math.Sqrt(abLen2 * cbLen2);

            return cosTheta > cosMinAngle;
        }

        public bool IsBadTriangle(CDTTriangle tri, double minAllowedRad, double maxAllowedArea)
        {
            if (tri.super || tri.parents.Count == 0) return false;

            if (tri.area > maxAllowedArea) return true;

            Vec2 a = _v[tri.indices[0]];
            Vec2 b = _v[tri.indices[1]];
            Vec2 c = _v[tri.indices[2]];

            double ab = Vec2.SquareLength(a - b);
            double bc = Vec2.SquareLength(b - c);
            double ac = Vec2.SquareLength(c - a);
            double minEdge = Math.Min(Math.Min(ab, bc), ac);

            if (tri.circle.radiusSquared / minEdge > 2)
            {
                return false;
            }

            double radABC = Angle(a, b, c);
            double radBCA = Angle(b, c, a);
            double radCAB = Math.PI - radABC - radBCA;
            double minRad = Math.Min(Math.Min(radABC, radBCA), radCAB);

            return minRad < minAllowedRad;
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
            double scale = 10;

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

        public int[] FlipEdge(int triangleIndex, int edgeIndex)
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

            IEnumerable<int> parents = tri0.parents.Concat(tri1.parents);

            double a0 = Area(v3, v1, v2);
            _t[t0] = new CDTTriangle(
                new Circle(v3, v1, v2), a0,
                i3, i1, i2,
                t1, tri0.adjacent[e12], tri1.adjacent[e23],
                false, tri0.constraint[e12], tri1.constraint[e23],
                parents);

            double a1 = tri0.area + tri1.area - a0;
            _t[t1] = new CDTTriangle(
               new Circle(v1, v3, v0), a1,
               i1, i3, i0,
               t0, tri1.adjacent[e30], tri0.adjacent[e01],
               false, tri1.constraint[e30], tri0.constraint[e01],
               parents);

            SetAdjacent(t0, 1);
            SetAdjacent(t0, 2);
            SetAdjacent(t1, 1);
            SetAdjacent(t1, 2);

            // push edge oppsote to v1
            _toLegalize.Push(new Edge(t0, 2));
            _toLegalize.Push(new Edge(t1, 1));

            return new[]
            {
                t0,
                t1,
                //tri0.adjacent[e12],
                //tri1.adjacent[e23],
                //tri1.adjacent[e30],
                //tri0.adjacent[e01]
            };
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
                        if (Vec2.Cross(_v[a], _v[b], vb) <= 0)
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

        public (int triangleIndex, int edgeIndex) FindContaining(Vec2 point, double tolerance = 1e-12, int seed = NO_INDEX)
        {
            int max = _t.Count * 3;
            int steps = 0;

            int current = seed == NO_INDEX ? _t.Count - 1 : seed;
            while (true)
            {
                if (steps++ > max)
                {
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

                    double cross = Vec2.Cross(a, b, point);
                    if (Math.Abs(cross) < tolerance && GeometryHelper.OnSegment(a, b, point, tolerance))
                    {
                        return (current, i);
                    }

                    if (cross > tolerance)
                    {
                        current = tri.adjacent[i];
                        inside = false;
                        break;
                    }
                }

                if (inside)
                {
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

                if (ShouldFlip(t0, edge))
                {
                    int[] affected = FlipEdge(t0, edge);
                    foreach (var item in affected)
                    {
                        if (item == NO_INDEX) continue;
                        _affected.Add(item);
                    }
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

            return new Edge(triangle, NO_INDEX);
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
            if (aIndex == bIndex)
            {
                return;
            }

            Edge edge = FindEdge(aIndex, bIndex);
            int triangle = edge.triangle;
            if (edge.index != NO_INDEX)
            {
                SetConstraint(triangle, edge.index);
                return;
            }

            Vec2 p1 = _v[aIndex];
            Vec2 p2 = _v[bIndex];

            int current = EntranceTriangle(triangle, aIndex, bIndex);
            while (true)
            {
                CDTTriangle currentTri = _t[current];
                for (int i = 0; i < 3; i++)
                {
                    if (currentTri.constraint[i]) continue;

                    int a = currentTri.indices[i];
                    int b = currentTri.indices[CDTTriangle.NEXT[i]];
                    if (GeometryHelper.Intersect(p1, p2, _v[a], _v[b], out _))
                    {
                        FlipEdge(current, i);
                        SetConstraint(current, _t[current].IndexOfInvariant(a, b));
                        Legalize();
                    }
                }

                if (currentTri.IndexOf(bIndex) != NO_INDEX)
                    break;

                bool advanced = false;
                for (int i = 0; i < 3; i++)
                {
                    Vec2 q1 = _v[currentTri.indices[i]];
                    Vec2 q2 = _v[currentTri.indices[CDTTriangle.NEXT[i]]];
                    if (Vec2.Cross(q1, q2, p2) > 0)
                    {
                        current = currentTri.adjacent[i];
                        advanced = true;
                        break;
                    }
                }

                if (!advanced)
                {
                    throw new Exception("Failed to advance triangle march — possibly bad mesh topology.");
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
            return -Vec2.Cross(a, b, c) * 0.5;
        }
    }
}
