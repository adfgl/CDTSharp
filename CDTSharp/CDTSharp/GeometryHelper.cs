using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CDTSharp
{
    public static class GeometryHelper
    {
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
        }

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
