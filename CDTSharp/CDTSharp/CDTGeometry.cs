using System;
using System.Numerics;
using static System.Net.Mime.MediaTypeNames;

namespace CDTSharp
{
    public static class CDTGeometry
    {
        public const int NO_INDEX = -1;

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

            int lastTriangle = triangles.Count;
            Triangle t0 = triangles[triangleIndex];
            int t1Index = t0.GetAdjacent(edgeIndex);
            Triangle t1 = triangles[t1Index];

            (int v, int e, bool c) e20, e01, e12, e23, e30;
            e20 = t0.GetEdge(edgeIndex);
            e01 = t0.GetEdge((edgeIndex + 1) % 3);
            e12 = t0.GetEdge((edgeIndex + 2) % 3);

            int adjEdgeIndex = t1.IndexOf(e01.v, e20.v);
            e23 = t1.GetEdge((adjEdgeIndex + 1) % 3);
            e30 = t1.GetEdge((edgeIndex + 2) % 3);


            int[] triIndices = [triangleIndex, t1Index, lastTriangle, lastTriangle + 1];
            (int v, int e, bool c)[] edges = [e01, e12, e23, e30];
            for (int curr = 0; curr < 4; curr++)
            {
                int next = (curr + 1) % 4;

                Triangle tri = new Triangle(edges[curr].v, edges[next].v, vertexIndex);


                if (curr < 2)
                {
                    triangles[triIndices[curr]] = tri;
                }
                else
                {
                    triangles.Add(tri);
                }
            }

        }

        public static void SplitTriangle(List<Vec2> vertices, List<Triangle> triangles, int triangleIndex, int vertexIndex)
        {
            Triangle t = triangles[triangleIndex];
            int lastTriangle = triangles.Count;
            int[] triIndices = [triangleIndex, lastTriangle, lastTriangle + 1];
            Vec2 v0 = vertices[vertexIndex];
            for (int curr = 0; curr < 3; curr++)
            {
                int next = (curr + 1) % 3;
                int prev = (curr + 2) % 3;

                int i1, i2, adjIndex;
                bool constraint;
                switch (curr)
                {
                    case 0:
                        i1 = t.v0;
                        i2 = t.v1;
                        adjIndex = t.adj0;
                        constraint = t.con0;
                        break;

                    case 1:
                        i1 = t.v1;
                        i2 = t.v2;
                        adjIndex = t.adj1;
                        constraint = t.con1;
                        break;

                    default:
                        i1 = t.v2;
                        i2 = t.v0;
                        adjIndex = t.adj2;
                        constraint = t.con2;
                        break;
                }

                if (adjIndex != NO_INDEX)
                {
                    Triangle adj = triangles[adjIndex];
                    adj.SetAdjacent(adj.IndexOf(i2, i1), triIndices[curr]);
                    triangles[adjIndex] = adj;
                }

                Triangle newTri = new Triangle(
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
