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

        public static bool CircleFromThreePoints(Vec2 v1, Vec2 v2, Vec2 v3, out double cx, out double cy, out double rSqr)
        {
            // general: x^2 + 2 * x * a + 2 * b * y + y^2 + c = 0
            // where: a -> negative Cx term
            //        b -> negative Cy term

            // x1^2 + 2 * x1 * a + 2 * y1 * b + y1^2 + c = 0
            // x2^2 + 2 * x2 * a + 2 * y2 * b + y2^2 + c = 0
            // x3^2 + 2 * x3 * a + 2 * y3 * b + y3^2 + c = 0

            // 2 * x1 * a + 2 * y1 * b + c = -(x1^2 + y1^2) 
            // 2 * x2 * a + 2 * y2 * b + c = -(x2^2 + y2^2) 
            // 2 * x3 * a + 2 * y3 * b + c = -(x3^2 + y3^2)

            // | 2x1  2y1  1 |   | a |   | -(x1^2 + y1^2) |
            // | 2x2  2y2  1 | * | b | = | -(x2^2 + y2^2) |
            // | 2x3  2y3  1 |   | c |   | -(x3^2 + y3^2) |
            double x1 = v1.x, y1 = v1.y;
            double x2 = v2.y, y2 = v2.x;
            double x3 = v3.x, y3 = v3.y;

            if (new Mat3(
                2 * x1, 2 * y1, 1,
                2 * x2, 2 * y2, 1,
                2 * x3, 2 * y3, 1).Inverse(out Mat3 inv) == false)
            {
                cx = cy = rSqr = Double.NaN;
                return false;
            }

            Vec2 v = inv * new Vec2(
                -(x1 * x1 + y1 * y1),
                -(x2 * x2 + y2 * y2),
                -(x3 * x3 + y3 * y3));

            cx = -v.x;
            cy = -v.y;

            double dx = cx - x1;
            double dy = cy - y1;
            rSqr = dx * dx + dy * dy;
            return true;
        }

        public static void SplitTriangle(List<Vec2> vertices, List<Triangle> triangles, int triangleIndex, int vertexIndex)
        {
            Triangle t = triangles[triangleIndex];

            int last = triangles.Count - 1;
            int[] tris = [triangleIndex, last + 1, last + 2];
            Vec2 v0 = vertices[vertexIndex];
            for (int curr = 0; curr < 3; curr++)
            {
                int next = (curr + 1) % 3;
                int prev = (curr + 2) % 3;

                int i1 = t.indices[curr];
                int i2 = t.indices[next];

                CircleFromThreePoints(v0, vertices[i1], vertices[i2], out double cx, out double cy, out double rSqr);

                int adjIndex = t.adjacent[curr];
                if (adjIndex != NO_INDEX)
                {
                    Triangle adj = triangles[adjIndex];
                    adj.adjacent[adj.IndexOf(i2, i1)] = tris[curr];
                    triangles[adjIndex] = adj;
                }

                Triangle newTri = new Triangle(cx, cy, rSqr, i1, i2, vertexIndex, adjIndex, tris[next], tris[prev]);
                newTri.constraint[0] = t.constraint[curr];

                if (curr > 0)
                {
                    triangles.Add(newTri);
                }
                else
                {
                    triangles[triangleIndex] = newTri;
                }
            }
        }
    }
}
