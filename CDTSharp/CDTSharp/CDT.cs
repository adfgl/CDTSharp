namespace CDTSharp
{
    using System;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    public class CDT
    {
        public const int NO_INDEX = -1;

        readonly List<Vec2> _vertices = new List<Vec2>();
        readonly List<Triangle> _triangles = new List<Triangle>();
        readonly Stack<LegalizeEdge> _toLegalize = new Stack<LegalizeEdge>();

        public CDT Triangulate(CDTInput input)
        {
            _vertices.Clear();
            _triangles.Clear();

            InputPreprocessor processed = new InputPreprocessor(input);

            AddSuperTriangle(processed.Rect);

            foreach (Vec2 v in processed.Vertices)
            {
                (int triangleIndex, int edgeIndex) = FindContaining(v);
                Insert(v, triangleIndex, edgeIndex);
            }

            foreach ((int a, int b) in processed.Constraints)
            {
                AddConstraint(a + 3, b + 3);
            }

            MarkHoles(processed.Polygons, processed.Vertices);

            if (input.Refine)
            {
                Refine(input.MaxArea, input.MinAngle / 180d * Math.PI);
            }
            FinalizeMesh(input.KeepConvex);
            return this;
        }

        public List<Vec2> Vertices => _vertices;
        public List<Triangle> Triangles => _triangles;

        void MarkHoles(List<(Polygon, Polygon[])> polys, List<Vec2> vertices)
        {
            for (int i = 0; i < _triangles.Count; i++)
            {
                Triangle t = _triangles[i];
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
                        _triangles[i] = t;
                        break;
                    }
                }
            }
        }

        Vec2 Center(Triangle t)
        {
            return (_vertices[t.indices[0]] + _vertices[t.indices[1]] + _vertices[t.indices[2]]) / 3;
        }

        public bool Encroached(Segment seg)
        {
            int a = seg.a;
            int b = seg.b;
            Circle circle = new Circle(_vertices[a], _vertices[b]);
            for (int i = 0; i < _vertices.Count; i++)
            {
                if (i == a || i == b) continue;

                Vec2 v = _vertices[i];
                if (circle.Contains(v.x, v.y)) //  && IsVisible(v, a, b)
                {
                    return true;
                }
            }
            return false;
        }

        public void Refine(double maxArea, double minAngleRad)
        {
            double minCos = Math.Cos(minAngleRad);

            Queue<Segment> segmentQueue = new();
            Queue<int> triangleQueue = new();
            HashSet<Segment> seenSegments = new();

            // Step 1: Collect constrained segments and initially bad triangles
            for (int i = 0; i < _triangles.Count; i++)
            {
                Triangle tri = _triangles[i];
                for (int j = 0; j < 3; j++)
                {
                    if (tri.constraint[j])
                    {
                        int a = tri.indices[j];
                        int b = tri.indices[Triangle.NEXT[j]];
                        Segment seg = new Segment(a, b);
                        if (seenSegments.Add(seg))
                        {
                            segmentQueue.Enqueue(seg);
                        }
                    }
                }

                if (IsBadTriangle(tri, minCos, maxArea))
                    triangleQueue.Enqueue(i);
            }

            while (segmentQueue.Count > 0 || triangleQueue.Count > 0)
            {
                // STEP 1: Handle encroached segments (highest priority)
                if (segmentQueue.Count > 0)
                {
                    Segment seg = segmentQueue.Dequeue();
                    Vec2 a = _vertices[seg.a];
                    Vec2 b = _vertices[seg.b];

                    // Check for encroachment: any vertex inside diametral circle and visible
                    if (!Encroached(seg))
                    {
                        continue;
                    }

                    // Insert midpoint
                    Vec2 mid = Vec2.MidPoint(a, b);
                    var (triIndex, edgeIndex) = FindContaining(mid);
                    if (edgeIndex == NO_INDEX)
                        throw new Exception("Midpoint not on any edge.");

                    int vi = Insert(mid, triIndex, edgeIndex);

                    // Enqueue new subsegments
                    Segment s1 = new Segment(seg.a, vi);
                    Segment s2 = new Segment(vi, seg.b);
                    if (seenSegments.Add(s1)) segmentQueue.Enqueue(s1);
                    if (seenSegments.Add(s2)) segmentQueue.Enqueue(s2);
                    continue;
                }

                // STEP 2: Handle skinny triangle
                if (triangleQueue.Count > 0)
                {
                    int triIndex = triangleQueue.Dequeue();
                    Triangle tri = _triangles[triIndex];
                    Vec2 cc = new Vec2(tri.circle.x, tri.circle.y);

                    // Check for segment encroachment
                    bool encroaches = false;
                    foreach (Segment seg in seenSegments)
                    {
                        Circle circle = new Circle(_vertices[seg.a], _vertices[seg.b]);
                        if (circle.Contains(cc.x, cc.y)) 
                        {
                            segmentQueue.Enqueue(seg); 
                            encroaches = true;
                            break;
                        }
                    }

                    if (encroaches)
                    {
                        continue;
                    }

                    var (tIndex, eIndex) = FindContaining(cc);
                    if (tIndex == NO_INDEX)
                        throw new Exception("Could not locate triangle for circumcenter.");

                    Insert(cc, tIndex, eIndex);

                    foreach (int i in _affected)
                    {
                        if (IsBadTriangle(_triangles[i], minCos, maxArea))
                            triangleQueue.Enqueue(i);
                    }

                    //for (int i = 0; i < _triangles.Count; i++)
                    //{
                       
                    //}
                }
            }
        }


        public bool IsBadTriangle(Triangle tri, double minAllowedCos, double maxAllowedArea)
        {
            if (tri.hole || tri.ContainsSuper()) return false;

            Vec2 a = _vertices[tri.indices[0]];
            Vec2 b = _vertices[tri.indices[1]];
            Vec2 c = _vertices[tri.indices[2]];

            double minAngle = double.MaxValue;
            for (int curr = 0; curr < 3; curr++)
            {
                double angle = AngleCos(
                    _vertices[Triangle.PREV[curr]],
                    _vertices[curr],
                    _vertices[Triangle.NEXT[curr]]);
                if (minAngle > angle) minAngle = angle;
            }

            return minAngle < minAllowedCos || Area(a, b, c) > maxAllowedArea;
        }

        readonly HashSet<int> _affected = new HashSet<int>();

        int Insert(Vec2 vertex, int triangle, int edge)
        {
            int vertexindex = _vertices.Count;
            _vertices.Add(vertex);

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
            double scale = 5;

            Vec2 a = new Vec2(midx - scale * dmax, midy - dmax);
            Vec2 b = new Vec2(midx, midy + scale * dmax);
            Vec2 c = new Vec2(midx + scale * dmax, midy - dmax);

            _vertices.Add(a);
            _vertices.Add(b);
            _vertices.Add(c);

            _triangles.Add(new Triangle(new Circle(a, b, c), 0, 1, 2));
        }

        public void FinalizeMesh(bool keepConvex = false)
        {
            // Step 1: Remove supertriangle vertices
            _vertices.RemoveRange(0, 3);

            // Step 2: Build remapping of triangle indices while compacting
            Dictionary<int, int> remap = new();
            int write = 0;

            for (int read = 0; read < _triangles.Count; read++)
            {
                Triangle tri = _triangles[read];
                bool discard = tri.ContainsSuper() || (!keepConvex && tri.hole);

                if (!discard)
                {
                    _triangles[write] = tri;
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

                        Triangle twin = _triangles[twinIndex];
                        int a = tri.indices[i];
                        int b = tri.indices[Triangle.NEXT[i]];
                        int edgeInTwin = twin.IndexOf(b, a);
                        if (edgeInTwin != NO_INDEX)
                        {
                            twin.adjacent[edgeInTwin] = NO_INDEX;
                            _triangles[twinIndex] = twin;
                        }
                    }
                }
            }

            // Remove leftover triangles at the end
            if (write < _triangles.Count)
                _triangles.RemoveRange(write, _triangles.Count - write);

            // Step 3: Remap indices and adjacency
            for (int i = 0; i < _triangles.Count; i++)
            {
                Triangle tri = _triangles[i];

                for (int j = 0; j < 3; j++)
                {
                    // Adjust vertex indices (remove offset from supertriangle removal)
                    tri.indices[j] -= 3;

                    // Remap adjacency if the twin still exists
                    int oldAdj = tri.adjacent[j];
                    tri.adjacent[j] = remap.TryGetValue(oldAdj, out int newAdj) ? newAdj : NO_INDEX;
                }

                _triangles[i] = tri;
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
        public static bool OnSegment(Vec2 a, Vec2 b, Vec2 p, double tol)
        {
            double minX = Math.Min(a.x, b.x) - tol;
            double maxX = Math.Max(a.x, b.x) + tol;
            double minY = Math.Min(a.y, b.y) - tol;
            double maxY = Math.Max(a.y, b.y) + tol;

            return p.x >= minX && p.x <= maxX &&
                   p.y >= minY && p.y <= maxY;
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

        public void SplitBoundaryEdge(int triangleIndex, int edgeIndex, int vertexIndex)
        {
            List<Vec2> vertices = _vertices;
            List<Triangle> triangles = _triangles;
            Stack<LegalizeEdge> toLegalize = _toLegalize;

            /*
                        v1                          v1            
                        /\                          /|\             
                       /  \                        / | \           
                      /    \                      /  |  \          
                 e01 /      \ e12            e01 /   |   \ e12     
                    /   t0   \                  /    |    \        
                   /          \                / t0  |  t1 \       
                  /            \              /      |      \      
              v0 +--------------+ v2      v0 +-------+-------+ v2  
            */

            Triangle t0 = triangles[triangleIndex];

            int i0 = t0.indices[Triangle.NEXT[edgeIndex]];
            int i1 = t0.indices[Triangle.PREV[edgeIndex]];
            int i2 = t0.indices[edgeIndex];

            int e01 = Triangle.NEXT[edgeIndex];
            int e12 = Triangle.PREV[edgeIndex];

            int newIndex = triangles.Count;

            bool constrained = t0.constraint[edgeIndex];
            Vec2 v0 = vertices[vertexIndex];

            Triangle new0 = new Triangle(
                new Circle(v0, vertices[i0], vertices[i1]),
                i0, i1, vertexIndex,
                t0.adjacent[e01], newIndex, NO_INDEX,
                t0.constraint[e01], false, constrained
            );

            Triangle new1 = new Triangle(
                new Circle(v0, vertices[i1], vertices[i2]),
                i1, i2, vertexIndex,
                t0.adjacent[e12], NO_INDEX, triangleIndex,
                t0.constraint[e12], constrained, false
            );

            int adjndex;

            adjndex = new0.adjacent[0];
            if (adjndex != NO_INDEX)
            {
                Triangle adj = triangles[adjndex];
                adj.adjacent[adj.IndexOf(i1, i0)] = triangleIndex;
            }

            adjndex = new1.adjacent[0];
            if (adjndex != NO_INDEX)
            {
                Triangle adj = triangles[adjndex];
                adj.adjacent[adj.IndexOf(i2, i1)] = newIndex;
            }

            triangles[triangleIndex] = new0;
            triangles.Add(new1);

            toLegalize.Push(new LegalizeEdge(triangleIndex, 0));
            toLegalize.Push(new LegalizeEdge(newIndex, 0));
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

            Stack<LegalizeEdge> toLegalize = _toLegalize;
            List<Vec2> vertices = _vertices;
            List<Triangle> triangles = _triangles;

            int e20 = edgeIndex;
            int t0Index = triangleIndex;
            Triangle t0 = triangles[t0Index];

            int i0 = t0.indices[Triangle.NEXT[e20]];
            int i1 = t0.indices[Triangle.PREV[e20]];
            int i2 = t0.indices[e20];

            int t1Index = t0.adjacent[e20];
            if (t1Index == NO_INDEX)
            {
                SplitBoundaryEdge(triangleIndex, edgeIndex, vertexIndex);
                return;
            }

            Triangle t1 = triangles[t1Index];

            int e02 = t1.IndexOf(i0, i2);
            int i3 = t1.indices[Triangle.PREV[e02]];

            int t2Index = triangles.Count;
            int t3Index = t2Index + 1;

            bool e20Constrained = t0.constraint[e20];

            Vec2 v0 = vertices[vertexIndex];
            for (int curr = 0; curr < 4; curr++)
            {
                int ai, bi, ti;
                int adj0, adj1, adj2;
                bool con0, con1, con2;
                switch (curr)
                {
                    case 0:
                        int e01 = Triangle.NEXT[e20];
                        ai = i0;
                        bi = i1;

                        ti = t0Index;

                        adj0 = t0.adjacent[e01];
                        adj1 = t1Index;
                        adj2 = t3Index;

                        con0 = t0.constraint[e01];
                        con1 = false;
                        con2 = e20Constrained;
                        break;
                    case 1:
                        int e12 = Triangle.PREV[e20];
                        ai = i1;
                        bi = i2;

                        ti = t1Index;

                        adj0 = t0.adjacent[e12];
                        adj1 = t2Index;
                        adj2 = t0Index;

                        con0 = t0.constraint[e12];
                        con1 = e20Constrained;
                        con2 = false;
                        break;
                    case 2:
                        int e23 = Triangle.NEXT[e02];
                        ai = i2;
                        bi = i3;

                        ti = t2Index;

                        adj0 = t1.adjacent[e23];
                        adj1 = t3Index;
                        adj2 = t1Index;

                        con0 = t1.constraint[e23];
                        con1 = false;
                        con2 = e20Constrained;
                        break;
                    case 3:
                        int e30 = Triangle.PREV[e02];
                        ai = i3;
                        bi = i0;

                        ti = t3Index;

                        adj0 = t1.adjacent[e30];
                        adj1 = t0Index;
                        adj2 = t2Index;

                        con0 = t1.constraint[e30];
                        con1 = e20Constrained;
                        con2 = false;
                        break;
                    default:
                        throw new InvalidOperationException();
                }

                Triangle tri = new Triangle(
                    new Circle(v0, vertices[ai], vertices[bi]),
                    ai, bi, vertexIndex,
                    adj0, adj1, adj2,
                    con0, con1, con2);

                if (adj0 != NO_INDEX)
                {
                    Triangle adjTri = triangles[adj0];
                    adjTri.adjacent[adjTri.IndexOf(bi, ai)] = ti;
                    toLegalize.Push(new LegalizeEdge(ti, 0));
                    triangles[adj0] = adjTri;
                }

                if (ti < triangles.Count)
                {
                    triangles[ti] = tri;
                }
                else
                {
                    triangles.Add(tri);
                }

                toLegalize.Push(new LegalizeEdge(ti, 1));
                toLegalize.Push(new LegalizeEdge(ti, 2));
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

            Stack<LegalizeEdge> toLegalize = _toLegalize;
            List<Vec2> vertices = _vertices;
            List<Triangle> triangles = _triangles;

            int e20 = edgeIndex;
            int t0Index = triangleIndex;
            Triangle t0 = triangles[t0Index];

            int i0 = t0.indices[Triangle.NEXT[e20]];
            int i1 = t0.indices[Triangle.PREV[e20]];
            int i2 = t0.indices[e20];

            int t1Index = t0.adjacent[e20];
            Triangle t1 = triangles[t1Index];

            int e02 = t1.IndexOf(i0, i2);
            int i3 = t1.indices[Triangle.PREV[e02]];

            int e01 = Triangle.NEXT[e20];
            int e12 = Triangle.PREV[e20];
            int e23 = Triangle.NEXT[e02];
            int e30 = Triangle.PREV[e02];

            int adjIndex;

            Triangle new0 = new Triangle(
                new Circle(vertices[i3], vertices[i1], vertices[i2]),
                i3, i1, i2,
                t1Index, t0.adjacent[e12], t1.adjacent[e23],
                false, t0.constraint[e12], t1.constraint[e23]
                , t0.hole, t0.parent);

            adjIndex = t0.adjacent[e12];
            if (adjIndex != NO_INDEX)
            {
                Triangle adj = triangles[adjIndex];
                adj.adjacent[adj.IndexOf(i2, i1)] = t0Index;
            }

            adjIndex = t1.adjacent[e23];
            if (adjIndex != NO_INDEX)
            {
                Triangle adj = triangles[adjIndex];
                adj.adjacent[adj.IndexOf(i3, i2)] = t0Index;

                toLegalize.Push(new LegalizeEdge(t0Index, 2));
            }

            Triangle new1 = new Triangle(
                new Circle(vertices[i1], vertices[i3], vertices[i0]),
                i1, i3, i0,
                triangleIndex, t1.adjacent[e30], t0.adjacent[e01],
                false, t1.constraint[e30], t0.constraint[e01],
                t1.hole, t1.parent);

            adjIndex = t0.adjacent[e01];
            if (adjIndex != NO_INDEX)
            {
                Triangle adj = triangles[adjIndex];
                adj.adjacent[adj.IndexOf(i1, i0)] = t1Index;
            }

            adjIndex = t1.adjacent[e30];
            if (adjIndex != NO_INDEX)
            {
                Triangle adj = triangles[adjIndex];
                adj.adjacent[adj.IndexOf(i0, i3)] = t1Index;

                toLegalize.Push(new LegalizeEdge(t1Index, 1));
            }

            triangles[triangleIndex] = new0;
            triangles[t1Index] = new1;
        }

        public void SplitTriangle(int triangleIndex, int vertexIndex)
        {
            Stack<LegalizeEdge> toLegalize = _toLegalize;
            List<Vec2> vertices = _vertices;
            List<Triangle> triangles = _triangles;

            Triangle t = triangles[triangleIndex];
            int lastTriangle = triangles.Count;
            int[] triIndices = [triangleIndex, lastTriangle, lastTriangle + 1];
            Vec2 v0 = vertices[vertexIndex];

            for (int curr = 0; curr < 3; curr++)
            {
                int next = Triangle.NEXT[curr];
                int prev = Triangle.PREV[curr];

                int i1 = t.indices[curr];
                int i2 = t.indices[next];
                int adjIndex = t.adjacent[curr];
                bool constraint = t.constraint[curr];

                int triIndex = triIndices[curr];
                if (adjIndex != NO_INDEX)
                {
                    Triangle adj = triangles[adjIndex];
                    adj.adjacent[adj.IndexOf(i2, i1)] = triIndex;
                    toLegalize.Push(new LegalizeEdge(triIndex, 0));
                    triangles[adjIndex] = adj;
                }

                Triangle newTri = new Triangle(
                    new Circle(v0, vertices[i1], vertices[i2]),
                    i1, i2, vertexIndex,
                    adjIndex, triIndices[next], triIndices[prev],
                    constraint, false, false,
                    t.hole, t.parent
                );

                if (curr == 0)
                {
                    triangles[triangleIndex] = newTri;
                }
                else
                {
                    triangles.Add(newTri);
                }

                toLegalize.Push(new LegalizeEdge(triIndex, 1));
                toLegalize.Push(new LegalizeEdge(triIndex, 2));
            }
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

            List<Vec2> vertices = _vertices;
            List<Triangle> triangles = _triangles;

            int e20 = edgeIndex;
            int t0Index = triangleIndex;
            Triangle t0 = triangles[t0Index];
            if (t0.hole || t0Index == NO_INDEX || t0.constraint[e20])
            {
                return false;
            }

            int i0 = t0.indices[Triangle.NEXT[e20]];
            int i1 = t0.indices[Triangle.PREV[e20]];
            int i2 = t0.indices[e20];

            int t1Index = t0.adjacent[e20];
            Triangle t1 = triangles[t1Index];

            int i3 = t1.indices[Triangle.PREV[t1.IndexOf(i0, i2)]];

            Vec2 v3 = vertices[i3];
            return
                t0.circle.Contains(v3.x, v3.y) &&
                ConvexQuad(vertices[i0], vertices[i1], vertices[i2], v3);
        }

        public int EntranceTriangle(int triangleIndexContainingA, int aIndex, int bIndex)
        {
            List<Vec2> vertices = _vertices;
            List<Triangle> triangles = _triangles;

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

            Console.WriteLine(this.ToSvg()); ;

            throw new Exception("Could not find entrance triangle.");
        }

        public (int triangleIndex, int edgeIndex) FindContaining(Vec2 point, double tolerance = 1e-6)
        {
            List<Vec2> vertices = _vertices;
            List<Triangle> triangles = _triangles; 

            int max = triangles.Count * 3;
            int count = 0;

            int contained = triangles.Count - 1;

            while (true)
            {
                if (count++ > max)
                {
                    throw new Exception("Could not find containing triangle. Most likely mesh topology is invalid.");
                }

                bool inside = true;
                Triangle tri = triangles[contained];
                for (int i = 0; i < 3; i++)
                {
                    Vec2 start = vertices[tri.indices[i]];
                    Vec2 end = vertices[tri.indices[Triangle.NEXT[i]]];

                    double cross = Vec2.Cross(start, end, point);
                    if (Math.Abs(cross) < tolerance  && OnSegment(start, end, point, tolerance))
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
            Stack<LegalizeEdge> toLegalize = _toLegalize;
            List<Vec2> vertices = _vertices;
            List<Triangle> triangles = _triangles;

            _affected.Clear();
            while (toLegalize.Count > 0)
            {
                var (triangle, edge) = toLegalize.Pop();
                if (ShouldFlip(triangle, edge))
                {
                    FlipEdge(triangle, edge);

                    Triangle t0 = triangles[triangle];
                    int t1Index = triangles[triangle].adjacent[edge];
                    _affected.Add(t1Index);
                    _affected.Add(triangle);
                }
            }
        }

        public LegalizeEdge FindEdge(int aIndex, int bIndex)
        {
            List<Triangle> triangles = _triangles;

            int lastContained = NO_INDEX;
            for (int triIndex = 0; triIndex < triangles.Count; triIndex++)
            {
                Triangle tri = triangles[triIndex];
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

            TriangleWalker walker = new TriangleWalker(triangles, lastContained, aIndex);
            do
            {
                int current = walker.Current;
                int edge = triangles[current].IndexOfInvariant(aIndex, bIndex);
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

            List<Triangle> triangles = _triangles;
            List<Vec2> vertices = _vertices;
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
        public static double AngleCos(Vec2 a, Vec2 b, Vec2 c)
        {
            return Vec2.Dot((a - b).Normalize(), (c - b).Normalize());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsClockwise(Vec2 a, Vec2 b, Vec2 c)
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
