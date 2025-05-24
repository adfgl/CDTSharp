namespace CDTSharp
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using static System.Net.Mime.MediaTypeNames;

    public class CDT
    {
        public const double EPS = 1e-12;
        public const int NO_INDEX = -1;

        readonly List<Vec2> _v = new List<Vec2>();
        readonly List<CDTTriangle> _t = new List<CDTTriangle>();
        readonly List<int> _affected = new List<int>();
        readonly Stack<Edge> _toLegalize = new Stack<Edge>();
        readonly List<(int, int)> _constrainedEdges = new List<(int, int)>();

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
            foreach ((Polygon poly, Polygon[] holes) in processed.Polygons)
            {
                foreach (Vec2 item in poly.verts)
                {
                    Insert(quad, item);
                }

                foreach (Polygon hole in holes)
                {
                    foreach (Vec2 item in hole.verts)
                    {
                        Insert(quad, item);
                    }
                }
            }

            foreach (Vec2 item in processed.PointConstraints)
            {
                Insert(quad, item);
            }

            HashSet<Segment> seen = new HashSet<Segment>();
            foreach ((Vec2 a, Vec2 b) in processed.Constraints)
            {
                int ai = quad.IndexOf(a.x, a.y);
                if (ai == NO_INDEX) ai = Insert(quad, a);

                int bi = quad.IndexOf(b.x, b.y);
                if (bi == NO_INDEX) bi = Insert(quad, a);

                if (seen.Add(new Segment(ai, bi)))
                {
                    AddConstraint(ai + 3, bi + 3);
                }
            }

            MarkHoles(processed.Polygons);

            if (input.Refine)
            {
                Refine(processed, input.MaxArea, input.MinAngle);
            }
            FinalizeMesh(input.KeepConvex, input.KeepSuper);
            return this;
        }

        public List<Vec2> Vertices => _v;
        public List<CDTTriangle> Triangles => _t;
        public List<(int, int)> Constraints => _constrainedEdges;

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
                        foreach (var hole in item.Item2)
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

        bool IsVisibleFromInterior(HashSet<Segment> segments, Segment seg, Vec2 point)
        {
            Vec2 mid = Vec2.MidPoint(_v[seg.a], _v[seg.b]);
            foreach (Segment s in segments)
            {
                if (s.Equals(seg))
                    continue;

                if (Intersect(mid, point, _v[s.a], _v[s.b], out _))
                {
                    return false;
                }
                  
            }
            return true;
        }

        bool Enchrouched(Segment seg)
        {
            var (a, b) = seg;
            Circle diam = new Circle(_v[a], _v[b]);
            for (int i = 0; i < _v.Count; i++)
            {
                if (a == i || b == i) continue;

                Vec2 v = _v[i];
                if (diam.Contains(v.x, v.y))
                {
                    return true;
                }
            }
            return false;
        }

        public void Refine(CDTPreprocessor polys, double maxArea, double minAngle)
        {
            minAngle *= Math.PI / 180d;

            HashSet<Segment> uniqueSegments = new HashSet<Segment>();

            Queue<Segment> segmentQueue = new Queue<Segment>();
            Queue<int> triangleQueue = new Queue<int>();
            for (int i = 0; i < _t.Count; i++)
            {
                CDTTriangle tri = _t[i];
                if (IsBadTriangle(tri, minAngle, maxArea))
                {
                    triangleQueue.Enqueue(i);
                }

                for (int j = 0; j < 3; j++)
                {
                    if (!tri.constraint[j]) continue;

                    int a = tri.indices[j];
                    int b = tri.indices[CDTTriangle.NEXT[j]];
                    Segment seg = new Segment(a, b);
                    if (uniqueSegments.Add(seg) && Enchrouched(seg))
                    {
                        segmentQueue.Enqueue(seg);
                    }
                }
            }

            double minSqrLen = Math.Sqrt(4.0 * maxArea / Math.Sqrt(3)) * 0.25;
            minSqrLen *= minSqrLen;
            while (segmentQueue.Count > 0 || triangleQueue.Count > 0)
            {
                if (segmentQueue.Count > 0)
                {
                    Segment seg = segmentQueue.Dequeue();
                    var (ia, ib) = seg;

                    Vec2 a = _v[ia];
                    Vec2 b = _v[ib];
                    double sqrLen = Vec2.SquareLength(a - b);
                    if (sqrLen < minSqrLen)
                        continue;

                    Circle diam = new Circle(a, b);

                    Vec2 mid = new Vec2(diam.x, diam.y);

                    var (triIndex, edgeIndex) = FindContaining(mid, EPS);
                    if (edgeIndex == NO_INDEX)
                    {
                        Edge edge = FindEdge(ia, ib);
                        if (edge.index == NO_INDEX)
                        {
                            throw new Exception($"Midpoint of segment ({ia},{ib}) not found on any edge.");
                        }
                        triIndex = edge.triangle;
                        edgeIndex = edge.index;
                    }

                    int insertedIndex = Insert(mid, triIndex, edgeIndex);

                    uniqueSegments.Remove(seg);
                    Segment s1 = new Segment(ia, insertedIndex);
                    Segment s2 = new Segment(insertedIndex, ib);

                    uniqueSegments.Add(s1);
                    uniqueSegments.Add(s2);

                    if (Enchrouched(s1)) segmentQueue.Enqueue(s1);
                    if (Enchrouched(s2)) segmentQueue.Enqueue(s2);

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
                    CDTTriangle tri = _t[triIndex];
                    if (!IsBadTriangle(tri, minAngle, maxArea)) continue;

                    Vec2 cc = new Vec2(tri.circle.x, tri.circle.y);

                    bool encroaches = false;
                    foreach (Segment seg in uniqueSegments)
                    {
                        Circle diam = new Circle(_v[seg.a], _v[seg.b]);
                        if (diam.Contains(cc.x, cc.y) && IsVisibleFromInterior(uniqueSegments, seg, cc))
                        {
                            segmentQueue.Enqueue(seg);
                            encroaches = true;
                        }
                    }

                    if (encroaches) continue;

                    var (tIndex, eIndex) = FindContaining(cc, EPS);
                    if (tIndex == NO_INDEX)
                    {
                        throw new Exception("Could not locate triangle for circumcenter.");
                    }

                    int vi = Insert(cc, tIndex, eIndex);
                    foreach (var item in _affected)
                    {
                        if (IsBadTriangle(_t[item], minAngle, maxArea))
                        {
                            triangleQueue.Enqueue(item);
                        }
                    }
                }
            }
        }

        public bool IsBadTriangle(CDTTriangle tri, double minAllowedRad, double maxAllowedArea)
        {
            if (tri.super || tri.parents.Count == 0) return false;

            if (tri.area > maxAllowedArea) return true;

            Vec2 a = _v[tri.indices[0]];
            Vec2 b = _v[tri.indices[1]];
            Vec2 c = _v[tri.indices[2]];

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
            Circle c0 = new Circle(v0, v1, v4);
            _t[t0] = new CDTTriangle(
                c0, a0,
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
            Circle c2 = new Circle(v2, v3, v4);
            _t.Add(new CDTTriangle(
                 c2, a2,
                 i2, i3, i4,
                 tri1.adjacent[e23], t3, t1,
                 tri1.constraint[e23], false, constrained,
                 tri1.parents));

            double a3 = tri1.area - a2;
            Circle c3 = new Circle(v3, v0, v4);
            _t.Add(new CDTTriangle(
                 c3, a3,
                 i3, i0, i4,
                 tri1.adjacent[e30], t0, t2,
                 tri1.constraint[e30], constrained, false,
                 tri1.parents));


            Debug.Assert(tri0.area + tri1.area == a0 + a1 + a2 + a3);

            int[] inds = [t0, t1, t2, t3];
            for (int i = 0; i < 4; i++)
            {
                int ti = inds[i];
                SetAdjacent(ti, 0);
                _toLegalize.Push(new Edge(ti, 0));
                _toLegalize.Push(new Edge(ti, 1));
                _toLegalize.Push(new Edge(ti, 2));
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
            Circle c0 = new Circle(v0, v1, v3);
            _t[t0] = new CDTTriangle(
                c0, a0,
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
            Circle c2 = new Circle(v2, v0, v3);
            _t.Add(new CDTTriangle(
               c2, a2,
               i2, i0, i3,
               tri.adjacent[2], t0, t1,
               tri.constraint[2], false, false,
               tri.parents));

            Debug.Assert(tri.area == a0 + a1 + a2);

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
            Circle c0 = new Circle(v3, v1, v2);
            _t[t0] = new CDTTriangle(
                c0, a0,
                i3, i1, i2,
                t1, tri0.adjacent[e12], tri1.adjacent[e23],
                false, tri0.constraint[e12], tri1.constraint[e23],
                parents);

            double a1 = tri0.area + tri1.area - a0;
            Circle c1 = new Circle(v1, v3, v0);
            _t[t1] = new CDTTriangle(
               c1, a1,
               i1, i3, i0,
               t0, tri1.adjacent[e30], tri0.adjacent[e01],
               false, tri1.constraint[e30], tri0.constraint[e01],
               parents);

            Debug.Assert(tri0.area + tri1.area == _t[t1].area + _t[t0].area);

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
                ConvexQuad(_v[i0], _v[i1], _v[i2], v3);
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

        public (int triangleIndex, int edgeIndex) FindContaining(Vec2 point, double tolerance = 1e-6)
        {
            List<Vec2> vertices = _v;
            List<CDTTriangle> triangles = _t; 

            int max = triangles.Count * 3;
            int steps = 0;

            int current = triangles.Count - 1;
            while (true)
            {
                if (steps++ > max)
                {
                    throw new Exception("Could not find containing triangle. Most likely mesh topology is invalid.");
                }

                if (current == NO_INDEX) return (NO_INDEX, NO_INDEX);

                bool inside = true;
                CDTTriangle tri = triangles[current];
                for (int i = 0; i < 3; i++)
                {
                    Vec2 a = vertices[tri.indices[i]];
                    Vec2 b = vertices[tri.indices[CDTTriangle.NEXT[i]]];

                    double cross = Vec2.Cross(a, b, point);
                    if (cross > tolerance)
                    {
                        current = tri.adjacent[i];
                        inside = false;
                        break;
                    }

                    if (Math.Abs(cross) < tolerance && OnSegment(a, b, point, tolerance))
                    {
                        return (current, i);
                    }
                }

                if (inside)
                {
                    return (current, NO_INDEX);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                    FlipEdge(t0, edge);

                    int t1 = _t[t0].adjacent[edge];
                    if (t1 != NO_INDEX)
                    {
                        _affected.Add(t1);
                    }
                }
            }
        }

        public Edge FindEdgeBrute(int aIndex, int bIndex)
        {
            for (int i = 0; i < _t.Count; i++)
            {
                int edge = _t[i].IndexOf(aIndex, bIndex);
                if (edge != NO_INDEX) return new Edge(i, edge);
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

            TriangleWalker walker = new TriangleWalker(_t, lastContained, aIndex);
            do
            {
                int current = walker.Current;
                int edge = _t[current].IndexOfInvariant(aIndex, bIndex);
                if (edge != NO_INDEX)
                    return new Edge(current, edge);
            }
            while (walker.MoveNextCW());

            return new Edge(lastContained, NO_INDEX);
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
                    if (Intersect(p1, p2, _v[a], _v[b], out _))
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
            return Math.Abs(Vec2.Cross(a, b, c)) * 0.5;
        }

        public double Area(CDTTriangle t)
        {
            Vec2 a = _v[t.indices[0]];
            Vec2 b = _v[t.indices[1]];
            Vec2 c = _v[t.indices[2]];
            return Area(a, b, c);
        }

        public bool Clockwise(CDTTriangle t)
        {
            Vec2 a = _v[t.indices[0]];
            Vec2 b = _v[t.indices[1]];
            Vec2 c = _v[t.indices[2]];
            return Clockwise(a, b, c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Intersect(Vec2 p1, Vec2 p2, Vec2 q1, Vec2 q2, out Vec2 intersection)
        {
            // P(u) = p1 + u * (p2 - p1)
            // Q(v) = q1 + v * (q2 - q1)

            // goal to vind such 'u' and 'v' so:
            // p1 + u * (p2 - p1) = q1 + v * (q2 - q1)
            // which is:
            // u * (p2x - p1x) - v * (q2x - q1x) = q1x - p1x
            // u * (p2y - p1y) - v * (q2y - q1y) = q1y - p1y

            // | p2x - p1x  -(q2x - q1x) | *  | u | =  | q1x - p1x |
            // | p2y - p1y  -(q2y - q1y) |    | v |    | q1y - p1y |

            // | a  b | * | u | = | e |
            // | c  d |   | v |   | f |

            intersection = Vec2.NaN;

            double a = p2.x - p1.x, b = q1.x - q2.x;
            double c = p2.y - p1.y, d = q1.y - q2.y;

            double det = a * d - b * c;
            if (Math.Abs(det) < 1e-12)
            {
                return false;
            }

            double e = q1.x - p1.x, f = q1.y - p1.y;

            double u = (e * d - b * f) / det;
            double v = (a * f - e * c) / det;
            if (u < 0 || u > 1 || v < 0 || v > 1)
            {
                return false;
            }
            intersection = new Vec2(p1.x + u * a, p1.y + u * c);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool OnSegment(Vec2 a, Vec2 b, Vec2 p, double epsilon)
        {
            double dxAB = b.x - a.x;
            double dyAB = b.y - a.y;
            double dxAP = p.x - a.x;
            double dyAP = p.y - a.y;

            double cross = dxAB * dyAP - dyAB * dxAP;
            if (Math.Abs(cross) > epsilon)
                return false;

            double dot = dxAP * dxAB + dyAP * dyAB;
            if (dot < 0) return false;

            double lenSq = dxAB * dxAB + dyAB * dyAB;
            if (dot > lenSq) return false;

            return true;

            double sqrA = (a - p).Length();
            double sqrB = (b - p).Length();
            double sqr = (a - b).Length();
            return Math.Abs(sqrA + sqrB - sqr) <= epsilon;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ConvexQuad(Vec2 a, Vec2 b, Vec2 c, Vec2 d)
        {
            return SameSide(a, b, c, d) && SameSide(d, c, b, a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool SameSide(Vec2 a, Vec2 b, Vec2 c, Vec2 d)
        {
            return Vec2.Cross(a, b, c) * Vec2.Cross(c, d, a) >= 0;
        }
    }
}
