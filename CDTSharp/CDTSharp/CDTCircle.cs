namespace CDTSharp
{
    public readonly struct CDTCircle
    {
        public readonly double cx, cy;
        public readonly double radiusSquared;

        public CDTCircle(double cx, double cy, double radius)
        {
            this.cx = cx;
            this.cy = cy;
            this.radiusSquared = radius * radius;
        }

        public CDTCircle(Vec2 v1, Vec2 v2, Vec2 v3)
        {
            // general:  x^2 + 2 * x * a + 2 * b * y + y^2 + c = 0
            //     
            // where: a -> negative Cx term
            //        b -> negative Cy term

            double x1 = v1.x, y1 = v1.y;
            double x2 = v2.x, y2 = v2.y;
            double x3 = v3.x, y3 = v3.y;

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
                throw new Exception();
            }

            Vec2 v = inv * new Vec2(
                -(x1 * x1 + y1 * y1),
                -(x2 * x2 + y2 * y2),
                -(x3 * x3 + y3 * y3));

            cx = -v.x;
            cy = -v.y;

            double dx = cx - x1;
            double dy = cy - y1;
            radiusSquared = dx * dx + dy * dy;
        }

        public bool Contains(double x, double y)
        {
            double dx = x - cx;
            double dy = y - cy;
            return dx * dx + dy * dy <= radiusSquared;
        }
    }
}
