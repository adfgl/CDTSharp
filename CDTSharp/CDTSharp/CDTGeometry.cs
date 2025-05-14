using System.Runtime.CompilerServices;

namespace CDTSharp
{
    public static class CDTGeometry
    {
        public const int NO_INDEX = -1;

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
        public static bool OnSegment(Vec2 pt, Vec2 start, Vec2 end, double tolerance = 0)
        {
            double dx1 = end.x - start.x;
            double dy1 = end.y - start.y;
            double dot = dx1 * (pt.x - start.x) + dy1 * (pt.y - start.y);
            return dot >= -tolerance && dot <= dx1 * dx1 + dy1 * dy1 + tolerance;
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

        public static List<Vec2> ExtractUnique(IEnumerable<Vec2> vertices, double eps = 1e-6)
        {
            List<Vec2> unique = new List<Vec2>();

            double epsSqr = eps * eps;
            foreach (Vec2 vtx in vertices)
            {
                double x = vtx.x;
                double y = vtx.y;

                bool duplicate = false;
                foreach (Vec2 existing in unique)
                {
                    double dx = existing.x - x;
                    double dy = existing.y - y;
                    if (dx * dx + dy * dy < epsSqr)
                    {
                        duplicate = true;
                        break;
                    }
                }

                if (!duplicate)
                {
                    unique.Add(new Vec2(x, y));
                }
            }
            return unique;
        }

        public static void SplitEdge(Stack<Edge> toLegalize, List<Vec2> vertices, List<Triangle> triangles, int triangleIndex, int edgeIndex, int vertexIndex)
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

            int t2Index = triangles.Count;
            int t3Index = t2Index + 1;

            bool e20Constrained = t0.constraints[e20];

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

                        con0 = t0.constraints[e01];
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

                        con0 = t0.constraints[e12];
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

                        con0 = t1.constraints[e23];
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

                        con0 = t1.constraints[e30];
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
                    toLegalize.Push(new Edge(ti, 0));
                }

                if (curr < 2)
                {
                    triangles[ti] = tri;
                }
                else
                {
                    triangles.Add(tri);
                }
            }
        }

        public static void FlipEdge(Stack<Edge> toLegalize, List<Vec2> vertices, List<Triangle> triangles, int triangleIndex, int edgeIndex)
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
                false, t0.constraints[e12], t1.constraints[e23]);

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

                toLegalize.Push(new Edge(t0Index, 2));
            }

            Triangle new1 = new Triangle(
                new Circle(vertices[i1], vertices[i3], vertices[i0]), 
                i1, i3, i0,
                triangleIndex, t1.adjacent[e30], t0.adjacent[e01],
                false, t1.constraints[e30], t0.constraints[e01]);


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

                toLegalize.Push(new Edge(t1Index, 1));
            }

            triangles[triangleIndex] = new0;
            triangles[t1Index] = new1;
        }

        public static void SplitTriangle(Stack<Edge> toLegalize, List<Vec2> vertices, List<Triangle> triangles, int triangleIndex, int vertexIndex)
        {
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
                bool constraint = t.constraints[curr];

                if (adjIndex != NO_INDEX)
                {
                    int triIndex = triIndices[curr];
                    Triangle adj = triangles[adjIndex];
                    adj.adjacent[adj.IndexOf(i2, i1)] = triIndex;
                    toLegalize.Push(new Edge(triIndex, 0));
                }

                Triangle newTri = new Triangle(
                    new Circle(v0, vertices[i1], vertices[i2]),
                    i1, i2, vertexIndex,
                    adjIndex, triIndices[next], triIndices[prev],
                    constraint, false, false,
                    t.hole
                );

                if (curr == 0)
                {
                    triangles[triangleIndex] = newTri;
                }
                else
                {
                    triangles.Add(newTri);
                }
            }
        }

        public static bool ShouldFlip(List<Vec2> vertices, List<Triangle> triangles, int triangleIndex, int edgeIndex)
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
            int t0Index = triangleIndex;
            Triangle t0 = triangles[t0Index];
            if (t0Index == NO_INDEX || t0.constraints[e20])
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

        public static int EntranceTriangle(List<Vec2> vertices, List<Triangle> triangles, int triangle, int aIndex, int bIndex)
        {
            Vec2 vb = vertices[bIndex];

            TriangleWalker walker = new TriangleWalker(triangles, triangle, aIndex);
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

        public static (int triangleIndex, int edgeIndex) FindContaining(List<Vec2> vertices, List<Triangle> triangles, Vec2 point, int startSearch = NO_INDEX, double tolerance = 1e-8)
        {
            int max = triangles.Count * 3;
            int count = 0;

            int contained = startSearch == NO_INDEX ? triangles.Count - 1 : startSearch;
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
            tri.constraints[edgeIndex] = true;

            int adjIndex = tri.adjacent[edgeIndex];
            if (adjIndex == NO_INDEX)
            {
                throw new InvalidOperationException($"Edge {edgeIndex} in triangle {triangleIndex} has no twin. Cannot propagate constraint across broken topology.");
            }

            Triangle adj = triangles[adjIndex];
            int a = tri.indices[edgeIndex];
            int b = tri.indices[Triangle.NEXT[edgeIndex]];
            adj.constraints[adj.IndexOf(b, a)] = true;
        }

        public static int Legalize(Stack<Edge> toLegalize, List<Vec2> vertices, List<Triangle> triangles)
        {
            int numFlips = 0;
            while (toLegalize.Count > 0)
            {
                var (triangle, edge) = toLegalize.Pop();
                if (ShouldFlip(vertices, triangles, triangle, edge))
                {
                    FlipEdge(toLegalize, vertices, triangles, triangle, edge);
                    numFlips++;
                }
            }
            return numFlips;
        }

        public static Edge FindEdge(List<Triangle> triangles, int aIndex, int bIndex)
        {
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
                            return new Edge(triIndex, edgeIndex);
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
                    return new Edge(current, edge);
            }
            while (walker.MoveNextCW());
        
            return new Edge(lastContained, NO_INDEX);
        }

        public static void AddConstraint(Stack<Edge> legalize, List<Vec2> vertices, List<Triangle> triangles, int aIndex, int bIndex)
        {
            if (aIndex == bIndex)
            {
                return;
            }

            Edge edge = FindEdge(triangles, aIndex, bIndex);
            int triangle = edge.triangle;
            if (edge.index != NO_INDEX)
            {
                MarkConstrained(triangles, triangle, edge.index);
                return;
            }


            Vec2 p1 = vertices[aIndex];
            Vec2 p2 = vertices[bIndex];

            int current = EntranceTriangle(vertices, triangles, triangle, aIndex, bIndex);
            while (true)
            {
                Triangle currentTri = triangles[current];
                for (int i = 0; i < 3; i++)
                {
                    if (currentTri.constraints[i]) continue;

                    Vec2 q1 = vertices[currentTri.indices[i]];
                    Vec2 q2 = vertices[currentTri.indices[Triangle.NEXT[i]]];

                    if (!Intersect(p1, p2, q1, q2).IsNaN())
                    {
                        FlipEdge(legalize, vertices, triangles, current, i);
                        MarkConstrained(triangles, current, i);
                        Legalize(legalize, vertices, triangles);
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
    }
}
