namespace CDTSharp
{
    using System;
    using System.Runtime.CompilerServices;

    public class CDT
    {
        public const double EPS = 1e-12;
        public const int NO_INDEX = -1;

        readonly List<CDTVector> _v = new List<CDTVector>();
        readonly List<CDTTriangle> _t = new List<CDTTriangle>();
        readonly HashSet<int> _affected = new HashSet<int>();
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
            _constrainedEdges.AddRange(processed.Constraints);

            List<CDTVector> vertices = processed.Quad.Items.Select(o => o.Value).ToList();

            AddSuperTriangle(processed.Rect);
            foreach (CDTVector v in vertices)
            {
                (int triangleIndex, int edgeIndex) = FindContaining(v, EPS);
                Insert(v, triangleIndex, edgeIndex);
            }

            foreach ((int a, int b) in processed.Constraints)
            {
                AddConstraint(a + 3, b + 3);
            }

            MarkHoles(processed.Polygons, vertices);

            if (input.Refine)
            {
                Refine(processed, input.MaxArea, input.MinAngle);
            }
            FinalizeMesh(input.KeepConvex, input.KeepSuper);
            return this;
        }

        public List<CDTVector> Vertices => _v;
        public List<CDTTriangle> Triangles => _t;
        public List<(int, int)> Constraints => _constrainedEdges;

        void MarkHoles(List<(Polygon, Polygon[])> polys, List<CDTVector> vertices)
        {
            for (int i = 0; i < _t.Count; i++)
            {
                CDTTriangle t = _t[i];
                if (t.ContainsSuper())
                    continue;

                var (x, y) = Center(t);
                t.parents.Clear();

                foreach (var item in polys)
                {
                    Polygon contour = item.Item1;
                    Polygon[] holes = item.Item2;

                    if (contour.Contains(vertices, (o => o.x), (o => o.y), x, y))
                    {
                        bool insideHole = false;
                        foreach (var hole in holes)
                        {
                            if (hole.Contains(vertices, (o => o.x), (o => o.y), x, y))
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


        CDTVector Center(CDTTriangle t)
        {
            return (_v[t.indices[0]] + _v[t.indices[1]] + _v[t.indices[2]]) / 3;
        }

        bool IsVisibleFromInterior(HashSet<Segment> segments, Segment seg, CDTVector point)
        {
            CDTVector mid = CDTVector.MidPoint(_v[seg.a], _v[seg.b]);
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

                CDTVector v = _v[i];
                if (diam.Contains(v.x, v.y))
                {
                    return true;
                }
            }
            return false;
        }

        public void Refine(CDTPreprocessor polys, double maxArea, double minAngle)
        {
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

                    CDTVector a = _v[ia];
                    CDTVector b = _v[ib];
                    double sqrLen = CDTVector.SquareLength(a - b);
                    if (sqrLen < minSqrLen)
                        continue;

                    Circle diam = new Circle(a, b);

                    CDTVector mid = new CDTVector(diam.x, diam.y);

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

                    CDTVector cc = new CDTVector(tri.circle.x, tri.circle.y);

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

        public bool IsBadTriangle(CDTTriangle tri, double minAllowedDeg, double maxAllowedArea)
        {
            if (tri.parents.Count == 0 || tri.ContainsSuper()) return false;

            double minRad = double.MaxValue;
            for (int i = 0; i < 3; i++)
            {
                int prev = tri.indices[CDTTriangle.PREV[i]];
                int curr = tri.indices[i];
                int next = tri.indices[CDTTriangle.NEXT[i]];

                double deg = Angle(_v[prev], _v[curr], _v[next]) * 180d / Math.PI;

                if (deg < minAllowedDeg)
                {
                    return true;
                }

                if (deg < minRad) minRad = deg;
            }

            double area = Area(_v[tri.indices[0]], _v[tri.indices[1]], _v[tri.indices[2]]);
            return area > maxAllowedArea;
        }

        int Insert(CDTVector vertex, int triangle, int edge)
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

            CDTVector a = new CDTVector(midx - scale * dmax, midy - scale * dmax);
            CDTVector b = new CDTVector(midx, midy + scale * dmax);
            CDTVector c = new CDTVector(midx + scale * dmax, midy - scale * dmax);

            _v.Add(a);
            _v.Add(b);
            _v.Add(c);

            _t.Add(new CDTTriangle(new Circle(a, b, c), 0, 1, 2));
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
                    discard = tri.ContainsSuper() || (!keepConvex && tri.parents.Count == 0);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Intersect(CDTVector p1, CDTVector p2, CDTVector q1, CDTVector q2, out CDTVector intersection)
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

            intersection = CDTVector.NaN;

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
            intersection = new CDTVector(p1.x + u * a, p1.y + u * c);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool OnSegment(CDTVector a, CDTVector b, CDTVector p, double epsilon)
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
        public static bool ConvexQuad(CDTVector a, CDTVector b, CDTVector c, CDTVector d)
        {
            return SameSide(a, b, c, d) && SameSide(d, c, b, a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool SameSide(CDTVector a, CDTVector b, CDTVector c, CDTVector d)
        {
            return CDTVector.Cross(a, b, c) * CDTVector.Cross(c, d, a) >= 0;
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
              v0 +--------------+ v2      v0 +-------+-------+ v2  
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

            CDTVector v0 = _v[i0];
            CDTVector v1 = _v[i1];
            CDTVector v2 = _v[i2];
            CDTVector v3 = _v[i3];
            CDTVector v = _v[vertexIndex];

            bool constrained = tri0.constraint[e20];

            _t[t0] = new CDTTriangle(
                new Circle(v0, v1, v), 
                i0, i1, vertexIndex,
                tri0.adjacent[e01], t1, t3,
                tri0.constraint[e01], false, constrained,
                tri0.parents);

            _t[t1] = new CDTTriangle(
                new Circle(v1, v2, v),
                i1, i2, vertexIndex,
                tri0.adjacent[e12], t2, t0,
                tri0.constraint[e12], constrained, false,
                tri0.parents);

            _t.Add(new CDTTriangle(
                 new Circle(v2, v3, v),
                 i2, i3, vertexIndex,
                 tri1.adjacent[e23], t3, t1,
                 tri1.constraint[e23], false, constrained,
                 tri1.parents));

            _t.Add(new CDTTriangle(
                 new Circle(v3, v0, v),
                 i3, i0, vertexIndex,
                 tri1.adjacent[e30], t0, t2,
                 tri1.constraint[e30], constrained, false,
                 tri1.parents));

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
            CDTTriangle t = _t[triangleIndex];

            int t0 = triangleIndex;
            int t1 = _t.Count;
            int t2 = t1 + 1;

            int i0 = t.indices[0];
            int i1 = t.indices[1];
            int i2 = t.indices[2];
            int i3 = vertexIndex;

            CDTVector v = _v[i3];
            _t[t0] = new CDTTriangle(
                new Circle(_v[i0], _v[i1], v),
               i0, i1, i3,
               t.adjacent[0], t1, t2,
               t.constraint[0], false, false,
               t.parents);

            _t.Add(new CDTTriangle(
                new Circle(_v[i1], _v[i2], v),
               i1, i2, i3,
               t.adjacent[1], t2, t0,
               t.constraint[1], false, false,
               t.parents));

            _t.Add(new CDTTriangle(
                new Circle(_v[i2], _v[i0], v),
               i2, i0, i3,
               t.adjacent[2], t0, t1,
               t.constraint[2], false, false,
               t.parents));

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

            IEnumerable<int> parents = tri0.parents.Concat(tri1.parents);

            _t[t0] = new CDTTriangle(
                new Circle(_v[i3], _v[i1], _v[i2]),
                i3, i1, i2,
                t1, tri0.adjacent[e12], tri1.adjacent[e23],
                false, tri0.constraint[e12], tri1.constraint[e23],
                parents);

            _t[t1] = new CDTTriangle(
               new Circle(_v[i1], _v[i3], _v[i0]),
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
            CDTVector v3 = _v[i3];
            return
                t0.circle.Contains(v3.x, v3.y) &&
                ConvexQuad(_v[i0], _v[i1], _v[i2], v3);
        }

        public int EntranceTriangle(int triangleIndexContainingA, int aIndex, int bIndex)
        {
            CDTVector vb = _v[bIndex];
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
                        if (CDTVector.Cross(_v[a], _v[b], vb) <= 0)
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

        public (int triangleIndex, int edgeIndex) FindContaining(CDTVector point, double tolerance = 1e-6)
        {
            List<CDTVector> vertices = _v;
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
                    CDTVector a = vertices[tri.indices[i]];
                    CDTVector b = vertices[tri.indices[CDTTriangle.NEXT[i]]];

                    double cross = CDTVector.Cross(a, b, point);
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

                int t1 = _t[t0].adjacent[edge];
                if (t1 != NO_INDEX)
                {
                    _affected.Add(t1);
                }

                if (ShouldFlip(t0, edge))
                {
                    FlipEdge(t0, edge);
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


            CDTVector p1 = _v[aIndex];
            CDTVector p2 = _v[bIndex];

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
                    CDTVector q1 = _v[currentTri.indices[i]];
                    CDTVector q2 = _v[currentTri.indices[CDTTriangle.NEXT[i]]];
                    if (CDTVector.Cross(q1, q2, p2) > 0)
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
        public static double Angle(CDTVector a, CDTVector b, CDTVector c)
        {
            CDTVector ab = (a - b).Normalize();
            CDTVector cb = (c - b).Normalize();
            double dot = CDTVector.Dot(ab, cb);
            return Math.Acos(Math.Clamp(dot, -1.0, 1.0)); 
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Clockwise(CDTVector a, CDTVector b, CDTVector c)
        {
            return CDTVector.Cross(a, b, c) < 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Area(CDTVector a, CDTVector b, CDTVector c)
        {
            return Math.Abs(CDTVector.Cross(a, b, c)) * 0.5;
        }

        public double Area(CDTTriangle t)
        {
            CDTVector a = _v[t.indices[0]];
            CDTVector b = _v[t.indices[1]];
            CDTVector c = _v[t.indices[2]];
            return Area(a, b, c);
        }

        public bool Clockwise(CDTTriangle t)
        {
            CDTVector a = _v[t.indices[0]];
            CDTVector b = _v[t.indices[1]];
            CDTVector c = _v[t.indices[2]];
            return Clockwise(a, b, c);
        }
    }
}
