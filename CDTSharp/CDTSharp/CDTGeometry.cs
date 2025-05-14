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
            if (det == 0)
            {
                return Vec2.NaN;
            }

            double e = q1.x - p1.x, f = q1.y - p1.y;

            double u = (e * d - b * f) / det;
            double v = (a * f - e * c) / det;

            if (u < 0 || u > 1 || v < 0 || v > 1)
                return Vec2.NaN; 

            return new Vec2(p1.x + u * a, p1.y + u * c);
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

        public static void SplitEdge(List<Vec2> vertices, List<Triangle> triangles, int triangleIndex, int edgeIndex, int vertexIndex)
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

            //int lastTriangle = triangles.Count;
            //Triangle t0 = triangles[triangleIndex];
            //int t1Index = t0.GetAdjacent(edgeIndex);
            //Triangle t1 = triangles[t1Index];

            //(int v, int e, bool c) e20, e01, e12, e23, e30;
            //e20 = t0.GetEdge(edgeIndex);
            //e01 = t0.GetEdge((edgeIndex + 1) % 3);
            //e12 = t0.GetEdge((edgeIndex + 2) % 3);

            //int adjEdgeIndex = t1.IndexOf(e01.v, e20.v);
            //e23 = t1.GetEdge((adjEdgeIndex + 1) % 3);
            //e30 = t1.GetEdge((edgeIndex + 2) % 3);


            //int[] triIndices = [triangleIndex, t1Index, lastTriangle, lastTriangle + 1];
            //(int v, int e, bool c)[] edges = [e01, e12, e23, e30];
            //for (int curr = 0; curr < 4; curr++)
            //{
            //    int next = (curr + 1) % 4;

            //    Triangle tri = new Triangle(edges[curr].v, edges[next].v, vertexIndex);


            //    if (curr < 2)
            //    {
            //        triangles[triIndices[curr]] = tri;
            //    }
            //    else
            //    {
            //        triangles.Add(tri);
            //    }
            //}

        }

        public static void FlipEdge(List<Vec2> vertices, List<Triangle> triangles, int triangleIndex, int edgeIndex)
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

            int i0 = t0.indices[Triangle.NEXT3[e20]];
            int i1 = t0.indices[Triangle.PREV3[e20]];
            int i2 = t0.indices[e20];

            int t1Index = t0.adjacent[e20];
            Triangle t1 = triangles[t1Index];

            int e02 = t1.IndexOf(i0, i2);
            int i3 = t1.indices[Triangle.PREV3[e02]];

            int e01 = Triangle.NEXT3[e20];
            int e12 = Triangle.PREV3[e20];
            int e23 = Triangle.NEXT3[e02];
            int e30 = Triangle.PREV3[e02];

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
            }

            triangles[triangleIndex] = new0;
            triangles[t1Index] = new1;
        }

        public static void SplitTriangle(List<Vec2> vertices, List<Triangle> triangles, int triangleIndex, int vertexIndex)
        {
            Triangle t = triangles[triangleIndex];
            int lastTriangle = triangles.Count;
            int[] triIndices = [triangleIndex, lastTriangle, lastTriangle + 1];
            Vec2 v0 = vertices[vertexIndex];
            for (int curr = 0; curr < 3; curr++)
            {
                int next = Triangle.NEXT3[curr];
                int prev = Triangle.PREV3[curr];

                int i1 = t.indices[curr];
                int i2 = t.indices[next];
                int adjIndex = t.adjacent[curr];
                bool constraint = t.constraints[curr];

                if (adjIndex != NO_INDEX)
                {
                    Triangle adj = triangles[adjIndex];
                    adj.adjacent[adj.IndexOf(i2, i1)] = triIndices[curr];
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
    }
}
