using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDTSharp
{
    public static class CDTEx
    {
        public static string ToSvg(this CDT cdt, int size = 1000, float padding = 10f, bool fill = true, bool drawConstraints = false, bool drawCircles = false)
        {
            if (cdt.Vertices.Count == 0 || cdt.Triangles.Count == 0)
                return "<svg xmlns='http://www.w3.org/2000/svg'/>";

            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;

            foreach (var v in cdt.Vertices)
            {
                if (v.x < minX) minX = v.x;
                if (v.x > maxX) maxX = v.x;
                if (v.y < minY) minY = v.y;
                if (v.y > maxY) maxY = v.y;
            }

            double scale = (size - 2 * padding) / Math.Max(maxX - minX, maxY - minY);

            var sb = new StringBuilder();
            sb.Append("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 ");
            sb.Append(size); sb.Append(' '); sb.Append(size); sb.Append("'>");

            if (fill)
            {
                foreach (var tri in cdt.Triangles)
                {
                    var a = cdt.Vertices[tri.indices[0]];
                    var b = cdt.Vertices[tri.indices[1]];
                    var c = cdt.Vertices[tri.indices[2]];
                    var (x1, y1) = Project(a.x, a.y);
                    var (x2, y2) = Project(b.x, b.y);
                    var (x3, y3) = Project(c.x, c.y);

                    string color = BlendColorsFromIds(tri.parents);
                    sb.Append($"<polygon points='{x1:F1},{y1:F1} {x2:F1},{y2:F1} {x3:F1},{y3:F1}' fill='{color}' fill-opacity='0.5' stroke='#000' stroke-width='1'/>");
                }
            }
            else
            {
                // Wireframe without fill, deduplicated
                var drawn = new HashSet<(int, int)>();
                sb.Append("<path d='");
                foreach (var tri in cdt.Triangles)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        int a = tri.indices[i];
                        int b = tri.indices[(i + 1) % 3];
                        int lo = Math.Min(a, b), hi = Math.Max(a, b);
                        if (!drawn.Add((lo, hi))) continue;

                        var va = cdt.Vertices[a];
                        var vb = cdt.Vertices[b];
                        var (x1, y1) = Project(va.x, va.y);
                        var (x2, y2) = Project(vb.x, vb.y);
                        sb.Append($"M{x1:F1},{y1:F1}L{x2:F1},{y2:F1}");
                    }
                }
                sb.Append("' fill='none' stroke='#000' stroke-width='1'/>");
            }

            if (drawConstraints)
            {
                sb.Append("<path d='");
                foreach (var (a, b) in cdt.Constraints)
                {
                    var va = cdt.Vertices[a];
                    var vb = cdt.Vertices[b];
                    var (x1, y1) = Project(va.x, va.y);
                    var (x2, y2) = Project(vb.x, vb.y);
                    sb.Append($"M{x1:F1},{y1:F1}L{x2:F1},{y2:F1}");
                }
                sb.Append("' fill='none' stroke='red' stroke-width='2.5'/>");
            }

            if (drawCircles)
            {
                foreach (var tri in cdt.Triangles)
                {
                    var (cx, cy) = Project(tri.circle.x, tri.circle.y);
                    double r = Math.Sqrt(tri.circle.radiusSquared) * scale;
                    sb.Append($"<circle cx='{cx:F1}' cy='{cy:F1}' r='{r:F1}' fill='none' stroke='blue' stroke-opacity='0.6' stroke-width='1'/>");
                }
            }


            sb.Append("</svg>");
            return sb.ToString();

            (double x, double y) Project(double x, double y)
            {
                double sx = (x - minX) * scale + padding;
                double sy = (y - minY) * scale + padding;
                return (sx, size - sy); // Y-flip
            }
        }

        static string BlendColorsFromIds(List<int> ids)
        {
            if (ids.Count == 0)
                return "#CCCCCC"; // fallback gray

            float rSum = 0, gSum = 0, bSum = 0;
            foreach (int id in ids)
            {
                string hex = ColorFromId(id);
                int r = Convert.ToInt32(hex.Substring(1, 2), 16);
                int g = Convert.ToInt32(hex.Substring(3, 2), 16);
                int b = Convert.ToInt32(hex.Substring(5, 2), 16);

                rSum += r;
                gSum += g;
                bSum += b;
            }

            int R = (int)(rSum / ids.Count);
            int G = (int)(gSum / ids.Count);
            int B = (int)(bSum / ids.Count);

            return $"#{R:X2}{G:X2}{B:X2}";
        }

        static string ColorFromId(int id)
        {
            // Generate a visually distinct pastel color using HSV mapping
            float hue = (id * 137) % 360; // 137 = golden angle approximation
            float s = 0.5f, v = 0.95f;

            float c = v * s;
            float x = c * (1 - MathF.Abs((hue / 60f % 2) - 1));
            float m = v - c;

            float r, g, b;
            if (hue < 60) (r, g, b) = (c, x, 0);
            else if (hue < 120) (r, g, b) = (x, c, 0);
            else if (hue < 180) (r, g, b) = (0, c, x);
            else if (hue < 240) (r, g, b) = (0, x, c);
            else if (hue < 300) (r, g, b) = (x, 0, c);
            else (r, g, b) = (c, 0, x);

            int R = (int)((r + m) * 255);
            int G = (int)((g + m) * 255);
            int B = (int)((b + m) * 255);

            return $"#{R:X2}{G:X2}{B:X2}";
        }
    }
}
