namespace CDTSharp
{
    using System;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    public class CDT
    {
        public const double EPS = 1e-12;
        public const int NO_INDEX = -1;

        readonly List<Vec2> _v = new List<Vec2>();
        readonly List<Triangle> _t = new List<Triangle>();
        readonly Stack<LegalizeEdge> _toLegalize = new Stack<LegalizeEdge>();

        public CDT Triangulate(CDTInput input)
        {
            _v.Clear();
            _t.Clear();

            InputPreprocessor processed = new InputPreprocessor(input);

            AddSuperTriangle(processed.Rect);

            foreach (Vec2 v in processed.Vertices)
            {
                (int triangleIndex, int edgeIndex) = FindContaining(v, EPS);
                Insert(v, triangleIndex, edgeIndex);
            }

            foreach ((int a, int b) in processed.Constraints)
            {
                AddConstraint(a + 3, b + 3);
            }

            MarkHoles(processed.Polygons, processed.Vertices);

            if (input.Refine)
            {
                Refine(processed, input.MaxArea, input.MinAngle);
            }

            FinalizeMesh(input.KeepConvex);
            return this;
        }


        public List<Vec2> Vertices => _v;
        public List<Triangle> Triangles => _t;

        void MarkHoles(List<(Polygon, Polygon[])> polys, List<Vec2> vertices)
        {
            for (int i = 0; i < _t.Count; i++)
            {
                Triangle t = _t[i];
                if (t.ContainsSuper()) continue;

                var (x, y) = Center(t);

                foreach (var item in polys)
                {
                    var p = item.Item1;
                    if (p.Contains(vertices, x, y))
                    {
                        t.parent = p.index;
                        foreach (var hole in item.Item2)
                        {
                            if (hole.Contains(vertices, x, y))
                            {
                                t.hole = true;
                                break;
                            }
                        }
                        _t[i] = t;
                        break;
                    }
                }
            }
        }

        Vec2 Center(Triangle t)
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

                if (!Intersect(mid, point, _v[s.a], _v[s.b]).IsNaN())
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

                var v = _v[i];
                if (diam.Contains(v.x, v.y))
                {
                    return true;
                }
            }
            return false;
        }

        public void Refine(InputPreprocessor polys, double maxArea, double minAngle)
        {
            HashSet<Segment> uniqueSegments = new HashSet<Segment>();

            Queue<Segment> segmentQueue = new Queue<Segment>();
            Queue<int> triangleQueue = new Queue<int>();
            for (int i = 0; i < _t.Count; i++)
            {
                Triangle tri = _t[i];
                if (IsBadTriangle(tri, minAngle, maxArea))
                    triangleQueue.Enqueue(i);

                for (int j = 0; j < 3; j++)
                {
                    if (!tri.constraint[j]) continue;

                    int a = tri.indices[j];
                    int b = tri.indices[Triangle.NEXT[j]];
                    Segment seg = new Segment(a, b);
                    if (uniqueSegments.Add(seg) && Enchrouched(seg))
                    {
                        segmentQueue.Enqueue(seg);
                    }
                }
            }

            while (segmentQueue.Count > 0 || triangleQueue.Count > 0 || _affected.Count > 0)
            {
                _affected.Clear();

                if (segmentQueue.Count > 0)
                {
                    Segment seg = segmentQueue.Dequeue();
                    var (ia, ib) = seg;

                    Vec2 a = _v[ia];
                    Vec2 b = _v[ib];

                    Circle diam = new Circle(a, b);

                    Vec2 mid = new Vec2(diam.x, diam.y);
                    var (triIndex, edgeIndex) = FindContaining(mid, EPS);
                    if (edgeIndex == NO_INDEX)
                    {
                        throw new Exception("Midpoint not on any edge.");
                    }

                    int insertedIndex = Insert(mid, triIndex, edgeIndex);

                    uniqueSegments.Remove(seg);
                    Segment s1 = new Segment(ia, insertedIndex);
                    Segment s2 = new Segment(insertedIndex, ib);

                    uniqueSegments.Add(s1);
                    uniqueSegments.Add(s2);

                    if (Enchrouched(s1)) segmentQueue.Enqueue(s1);
                    if (Enchrouched(s2)) segmentQueue.Enqueue(s2);

                    if (segmentQueue.Count == 0)
                    {
                        triangleQueue.Clear();
                        for (int i = 0; i < _t.Count; i++)
                        {
                            if (IsBadTriangle(_t[i], minAngle, maxArea))
                            {
                                triangleQueue.Enqueue(i);
                            }
                        }
                    }
                    continue;
                }

                if (triangleQueue.Count > 0)
                {
                    int triIndex = triangleQueue.Dequeue();
                    Triangle tri = _t[triIndex];

                    Vec2 cc = new Vec2(tri.circle.x, tri.circle.y);

                    if (!polys.ContainsContour(cc)) continue;

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
                        FinalizeMesh();
                        Console.WriteLine(this.ToSvg());
                        throw new Exception("Could not locate triangle for circumcenter.");
                    }

                    int vi = Insert(cc, tIndex, eIndex);
                    if (eIndex != NO_INDEX)
                    {
                        Triangle t = _t[tIndex];
                        int a = t.indices[eIndex];
                        int b = t.indices[Triangle.NEXT[eIndex]];

                        Segment split = new Segment(a, b);
                        if (uniqueSegments.Remove(split))
                        {
                            Segment s1 = new Segment(a, vi);
                            Segment s2 = new Segment(vi, b);

                            uniqueSegments.Add(s1);
                            uniqueSegments.Add(s2);

                            if (Enchrouched(s1)) segmentQueue.Enqueue(s1);
                            if (Enchrouched(s2)) segmentQueue.Enqueue(s2);
                        }
                    }

                    triangleQueue.Clear();
                    for (int i = 0; i < _t.Count; i++)
                    {
                        if (IsBadTriangle(_t[i], minAngle, maxArea))
                        {
                            triangleQueue.Enqueue(i);
                        }
                    }
                }
            }
        }

        public bool IsBadTriangle(Triangle tri, double minAllowedDeg, double maxAllowedArea)
        {
            if (tri.hole || tri.ContainsSuper()) return false;

            double minRad = double.MaxValue;
            for (int i = 0; i < 3; i++)
            {
                int prev = tri.indices[Triangle.PREV[i]];
                int curr = tri.indices[i];
                int next = tri.indices[Triangle.NEXT[i]];

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

        readonly HashSet<int> _affected = new HashSet<int>();

        bool IsTooCloseToNeighbors(Vec2 toInsert, int triangleIndex, double proximity)
        {
            Triangle tri = _t[triangleIndex];

            for (int i = 0; i < 3; i++)
            {
                Vec2 v = _v[tri.indices[i]];
                if (v.AlmostEqual(toInsert, proximity))
                    return true;

                int adjIndex = tri.adjacent[i];
                if (adjIndex != NO_INDEX)
                {
                    Triangle adj = _t[adjIndex];
                    for (int j = 0; j < 3; j++)
                    {
                        v = _v[adj.indices[j]];
                        if (v.AlmostEqual(toInsert, proximity))
                            return true;
                    }
                }
            }

            return false;
        }

        int Insert(Vec2 vertex, int triangle, int edge)
        {
            int vertexindex = _v.Count;
            _v.Add(vertex);

            int[] affected;
            if (edge == NO_INDEX)
            {
                affected = SplitTriangle(triangle, vertexindex);
            }
            else
            {
                affected = SplitEdge(triangle, edge, vertexindex);
            }

            Legalize();

            foreach (int i in affected)
            {
                _affected.Add(i);
            }
            return vertexindex;
        }

        void AddSuperTriangle(Rect rect)
        {
            double dmax = Math.Max(rect.maxX - rect.minX, rect.maxY - rect.minY);
            double midx = (rect.maxX + rect.minX) * 0.5;
            double midy = (rect.maxY + rect.minY) * 0.5;
            double scale = 5;

            Vec2 a = new Vec2(midx - scale * dmax, midy - scale * dmax);
            Vec2 b = new Vec2(midx, midy + scale * dmax);
            Vec2 c = new Vec2(midx + scale * dmax, midy - scale * dmax);

            _v.Add(a);
            _v.Add(b);
            _v.Add(c);

            _t.Add(new Triangle(new Circle(a, b, c), 0, 1, 2));
        }

        public void FinalizeMesh(bool keepConvex = false)
        {
            // Step 1: Remove supertriangle vertices
            _v.RemoveRange(0, 3);

            // Step 2: Build remapping of triangle indices while compacting
            Dictionary<int, int> remap = new();
            int write = 0;

            for (int read = 0; read < _t.Count; read++)
            {
                Triangle tri = _t[read];
                bool discard = tri.ContainsSuper() || (!keepConvex && tri.hole);

                if (!discard)
                {
                    _t[write] = tri;
                    remap[read] = write;
                    write++;
                }
                else
                {
                    // Unlink adjacent triangles
                    for (int i = 0; i < 3; i++)
                    {
                        int twinIndex = tri.adjacent[i];
                        if (twinIndex == NO_INDEX) continue;

                        Triangle twin = _t[twinIndex];
                        int a = tri.indices[i];
                        int b = tri.indices[Triangle.NEXT[i]];
                        int edgeInTwin = twin.IndexOf(b, a);
                        if (edgeInTwin != NO_INDEX)
                        {
                            twin.adjacent[edgeInTwin] = NO_INDEX;
                            _t[twinIndex] = twin;
                        }
                    }
                }
            }

            // Remove leftover triangles at the end
            if (write < _t.Count)
                _t.RemoveRange(write, _t.Count - write);

            // Step 3: Remap indices and adjacency
            for (int i = 0; i < _t.Count; i++)
            {
                Triangle tri = _t[i];

                for (int j = 0; j < 3; j++)
                {
                    // Adjust vertex indices (remove offset from supertriangle removal)
                    tri.indices[j] -= 3;

                    // Remap adjacency if the twin still exists
                    int oldAdj = tri.adjacent[j];
                    tri.adjacent[j] = remap.TryGetValue(oldAdj, out int newAdj) ? newAdj : NO_INDEX;
                }

                _t[i] = tri;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2 Intersect(Vec2 p1, Vec2 p2, Vec2 q1, Vec2 q2)
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

            double a = p2.x - p1.x, b = q1.x - q2.x;
            double c = p2.y - p1.y, d = q1.y - q2.y;

            double det = a * d - b * c;
            if (Math.Abs(det) < 1e-12)
            {
                return Vec2.NaN;
            }

            double e = q1.x - p1.x, f = q1.y - p1.y;

            double u = (e * d - b * f) / det;
            double v = (a * f - e * c) / det;
            if (u < 0 || u > 1 || v < 0 || v > 1)
            {
                return Vec2.NaN;
            }
            return new Vec2(p1.x + u * a, p1.y + u * c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool OnSegment(Vec2 a, Vec2 b, Vec2 p, double epsilon)
        {
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

        public int[] GetQuad(int triangleIndex, int edgeIndex, out Triangle t0, out Triangle t1)
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

            t0 = _t[triangleIndex];
            t1 = _t[t0.adjacent[edgeIndex]];

            int i0 = t0.indices[Triangle.NEXT[edgeIndex]];
            int i1 = t0.indices[Triangle.PREV[edgeIndex]];
            int i2 = t0.indices[edgeIndex];
            int i3 = t1.indices[Triangle.PREV[t1.IndexOf(i0, i2)]];

            return [i0, i1, i2, i3];
        }

        readonly static int[] NEXT4 = [1, 2, 3, 0], PREV4 = [3, 0, 1, 2];

        public int[] SplitEdge(int triangleIndex, int edgeIndex, int vertexIndex)
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

            Triangle t0, t1;
            int[] inds = GetQuad(triangleIndex, edgeIndex, out t0, out t1);
            int[] tris = [triangleIndex, t0.adjacent[edgeIndex], _t.Count, _t.Count + 1];

            Vec2 v = _v[vertexIndex];
            for (int i = 0; i < 4; i++)
            {
                int ia = inds[i];
                int ib = inds[NEXT4[i]];

                Triangle donor = i < 2 ? t0 : t1;
                int edge = donor.IndexOf(ia, ib);

                Triangle newTri = new Triangle(new Circle(_v[ia], _v[ib], v),
                    ia, ib, vertexIndex,
                    donor.adjacent[edge], tris[NEXT4[i]], tris[PREV4[i]],
                    donor.constraint[edge], false, false,
                    donor.hole, donor.parent);

                int triIndex = tris[i];
                int adjIndex = newTri.adjacent[0];

                //_toLegalize.Push(new LegalizeEdge(triIndex, 1));
                //_toLegalize.Push(new LegalizeEdge(triIndex, 2));

                if (adjIndex != NO_INDEX)
                {
                    Triangle adj = _t[adjIndex];
                    adj.adjacent[adj.IndexOf(ib, ia)] = triIndex;
                    _toLegalize.Push(new LegalizeEdge(triIndex, 0));
                }

                if (triIndex < _t.Count)
                {
                    _t[triIndex] = newTri;
                }
                else
                {
                    _t.Add(newTri);
                }
            }
            return tris;
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

            Triangle tri0, tri1;
            int[] inds = GetQuad(triangleIndex, edgeIndex, out tri0, out tri1);

            int t0 = triangleIndex;
            int t1 = tri0.adjacent[edgeIndex];

            int a = inds[3], b = inds[1], c = inds[2];
            _t[t0] = new Triangle(new Circle(_v[a], _v[b], _v[c]), a, b, c, t1, parent: tri0.parent, hole: tri0.hole);

            a = inds[1]; b = inds[3]; c = inds[0];
            _t[t1] = new Triangle(new Circle(_v[a], _v[b], _v[c]), a, b, c, t0, parent: tri1.parent, hole: tri1.hole);

            for (int i = 0; i < 4; i++)
            {
                Triangle donor = i < 2 ? tri0 : tri1;
                int tri = (i == 1 || i == 2) ? t0 : t1;
                Triangle target = _t[tri];

                int aa = inds[i];
                int bb = inds[NEXT4[i]];
                int donorEdge = donor.IndexOf(aa, bb);
                int targetEdge = target.IndexOf(aa, bb);
                target.constraint[targetEdge] = donor.constraint[donorEdge];

                int adjIndex = donor.adjacent[donorEdge];
                if (adjIndex == NO_INDEX) continue;

                Triangle adj = _t[adjIndex];
                int adjEdge = adj.IndexOf(bb, aa);
                adj.adjacent[adjEdge] = tri;

                target.adjacent[targetEdge] = adjIndex;
            }

            _toLegalize.Push(new LegalizeEdge(t0, 2));
            _toLegalize.Push(new LegalizeEdge(t1, 1));
        }

        public int[] SplitTriangle(int triangleIndex, int vertexIndex)
        {
            Triangle t = _t[triangleIndex];
            int lastTriangle = _t.Count;
            int[] triIndices = [triangleIndex, lastTriangle, lastTriangle + 1];
            Vec2 v0 = _v[vertexIndex];

            for (int curr = 0; curr < 3; curr++)
            {
                int next = Triangle.NEXT[curr];
                int prev = Triangle.PREV[curr];

                int triIndex = triIndices[curr];
                int a = t.indices[curr];
                int b = t.indices[next];
                int adjIndex = t.adjacent[curr];

                if (adjIndex != NO_INDEX)
                {
                    Triangle adj = _t[adjIndex];
                    adj.adjacent[adj.IndexOf(b, a)] = triIndex;

                    _toLegalize.Push(new LegalizeEdge(triIndex, 0));
                }

                Triangle newTri = new Triangle(
                    new Circle(v0, _v[a], _v[b]),
                    a, b, vertexIndex,
                    adjIndex, triIndices[next], triIndices[prev],
                    t.constraint[curr], false, false,
                    t.hole, t.parent
                );

                if (triIndex < _t.Count)
                {
                    _t[triangleIndex] = newTri;
                }
                else
                {
                    _t.Add(newTri);
                }

                _toLegalize.Push(new LegalizeEdge(triIndex, 1));
                _toLegalize.Push(new LegalizeEdge(triIndex, 2));
            }
            return triIndices;
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
            Triangle t0 = _t[triangleIndex];
            int t1Index = t0.adjacent[e20];
            if (t0.hole || t1Index == NO_INDEX || t0.constraint[e20])
            {
                return false;
            }

            int i0 = t0.indices[Triangle.NEXT[e20]];
            int i1 = t0.indices[Triangle.PREV[e20]];
            int i2 = t0.indices[e20];

            Triangle t1 = _t[t1Index];

            int i3 = t1.indices[Triangle.PREV[t1.IndexOf(i0, i2)]];
            Vec2 v3 = _v[i3];
            return
                t0.circle.Contains(v3.x, v3.y) &&
                ConvexQuad(_v[i0], _v[i1], _v[i2], v3);
        }

        public int EntranceTriangle(int triangleIndexContainingA, int aIndex, int bIndex)
        {
            List<Vec2> vertices = _v;
            List<Triangle> triangles = _t;

            Vec2 vb = vertices[bIndex];

            TriangleWalker walker = new TriangleWalker(triangles, triangleIndexContainingA, aIndex);
            Triangle tri = triangles[walker.Current];
            do
            {
                int toRightCount = 0;
                for (int i = 0; i < 3; i++)
                {
                    int a = tri.indices[i];
                    int b = tri.indices[Triangle.NEXT[i]];
                    if (a == aIndex || b == aIndex)
                    {
                        if (Vec2.Cross(vertices[a], vertices[b], vb) <= 0)
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
            List<Triangle> triangles = _t; 

            int max = triangles.Count * 3;
            int steps = 0;

            int contained = triangles.Count - 1;
            while (true)
            {
                if (steps++ > max)
                {
                    throw new Exception("Could not find containing triangle. Most likely mesh topology is invalid.");
                }

                if (contained == NO_INDEX) return (NO_INDEX, NO_INDEX);

                bool inside = true;
                Triangle tri = triangles[contained];
                for (int i = 0; i < 3; i++)
                {
                    Vec2 start = vertices[tri.indices[i]];
                    Vec2 end = vertices[tri.indices[Triangle.NEXT[i]]];

                    double cross = Vec2.Cross(start, end, point);
                    if (Math.Abs(cross) < tolerance && OnSegment(start, end, point, tolerance))
                    {
                        return (contained, i);
                    }

                    if (cross > 0)
                    {
                        contained = tri.adjacent[i];
                        inside = false;
                        break;
                    }
                }

                if (inside)
                {
                    return (contained, NO_INDEX);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MarkConstrained(List<Triangle> triangles, int triangleIndex, int edgeIndex)
        {
            Triangle tri = triangles[triangleIndex];
            tri.constraint[edgeIndex] = true;

            int adjIndex = tri.adjacent[edgeIndex];
            if (adjIndex == NO_INDEX)
            {
                throw new InvalidOperationException($"Edge {edgeIndex} in triangle {triangleIndex} has no twin. Cannot propagate constraint across broken topology.");
            }

            Triangle adj = triangles[adjIndex];
            int a = tri.indices[edgeIndex];
            int b = tri.indices[Triangle.NEXT[edgeIndex]];
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

                    _affected.Add(t0);
                    int t1 = _t[t0].adjacent[edge];
                    if (t1 != NO_INDEX) _affected.Add(t1);
                }
            }
        }

        public LegalizeEdge FindEdgeBrute(int aIndex, int bIndex)
        {
            for (int i = 0; i < _t.Count; i++)
            {
                int edge = _t[i].IndexOf(aIndex, bIndex);
                if (edge != NO_INDEX) return new LegalizeEdge(i, edge);
            }
            return new LegalizeEdge(NO_INDEX, NO_INDEX);
        }

        public LegalizeEdge FindEdge(int aIndex, int bIndex)
        {
            int lastContained = NO_INDEX;
            for (int triIndex = 0; triIndex < _t.Count; triIndex++)
            {
                Triangle tri = _t[triIndex];
                for (int edgeIndex = 0; edgeIndex < 3; edgeIndex++)
                {
                    if (tri.indices[edgeIndex] == aIndex)
                    {
                        if (tri.indices[Triangle.NEXT[edgeIndex]] == bIndex)
                        {
                            return new LegalizeEdge(triIndex, edgeIndex);
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
                    return new LegalizeEdge(current, edge);
            }
            while (walker.MoveNextCW());

            return new LegalizeEdge(lastContained, NO_INDEX);
        }

        public void AddConstraint(int aIndex, int bIndex)
        {
            if (aIndex == bIndex)
            {
                return;
            }

            List<Triangle> triangles = _t;
            List<Vec2> vertices = _v;
            Stack<LegalizeEdge> legalize = _toLegalize;

            LegalizeEdge edge = FindEdge(aIndex, bIndex);
            int triangle = edge.triangle;
            if (edge.index != NO_INDEX)
            {
                MarkConstrained(triangles, triangle, edge.index);
                return;
            }


            Vec2 p1 = vertices[aIndex];
            Vec2 p2 = vertices[bIndex];

            int current = EntranceTriangle(triangle, aIndex, bIndex);
            while (true)
            {
                Triangle currentTri = triangles[current];
                for (int i = 0; i < 3; i++)
                {
                    if (currentTri.constraint[i]) continue;

                    Vec2 q1 = vertices[currentTri.indices[i]];
                    Vec2 q2 = vertices[currentTri.indices[Triangle.NEXT[i]]];

                    if (!Intersect(p1, p2, q1, q2).IsNaN())
                    {
                        FlipEdge(current, i);
                        MarkConstrained(triangles, current, i);
                        Legalize();
                    }
                }

                if (currentTri.IndexOf(bIndex) != NO_INDEX)
                    break;

                bool advanced = false;
                for (int i = 0; i < 3; i++)
                {
                    Vec2 q1 = vertices[currentTri.indices[i]];
                    Vec2 q2 = vertices[currentTri.indices[Triangle.NEXT[i]]];

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

        public double Area(Triangle t)
        {
            Vec2 a = _v[t.indices[0]];
            Vec2 b = _v[t.indices[1]];
            Vec2 c = _v[t.indices[2]];
            return Area(a, b, c);
        }

        public bool Clockwise(Triangle t)
        {
            Vec2 a = _v[t.indices[0]];
            Vec2 b = _v[t.indices[1]];
            Vec2 c = _v[t.indices[2]];
            return Clockwise(a, b, c);
        }
    }
}
