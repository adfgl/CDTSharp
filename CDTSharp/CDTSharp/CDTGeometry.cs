namespace CDTSharp
{
    public static class CDTGeometry
    {
        public const int NO_INDEX = -1;

        public static CDTRect Bounds(IEnumerable<Vec2> vertices)
        {
            double minX, minY, maxX, maxY;
            minX = minY = double.MaxValue;
            maxX = maxY = double.MinValue;
            foreach (Vec2 vtx in vertices)
            {
                double x = vtx.x;
                double y = vtx.y;

                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
            return new CDTRect(minX, minY, maxX, maxY);
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

        public static bool CircleFromThreePoints(double x1, double y1, double x2, double y2, double x3, double y3, out double cx, out double cy, out double rSqr)
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
    }
}
