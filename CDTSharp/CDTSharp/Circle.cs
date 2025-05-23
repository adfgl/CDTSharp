namespace CDTSharp
{
    public readonly struct Circle
    {
        public readonly double x, y, radiusSquared;

        public Circle(double x, double y, double radiusSquared)
        {
            this.x = x;
            this.y = y;
            this.radiusSquared = radiusSquared;
        }

        public Circle(CDTVector v1, CDTVector v2, CDTVector v3)
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
            double x2 = v2.x, y2 = v2.y;
            double x3 = v3.x, y3 = v3.y;

            if (new Mat3(
                2 * x1, 2 * y1, 1,
                2 * x2, 2 * y2, 1,
                2 * x3, 2 * y3, 1).Inverse(out Mat3 inv) == false)
            {
                x = y = radiusSquared = Double.NaN;
                return;
            }

            CDTVector v = inv * new CDTVector(
                -(x1 * x1 + y1 * y1),
                -(x2 * x2 + y2 * y2),
                -(x3 * x3 + y3 * y3));

            x = -v.x;
            y = -v.y;
            double dx = x - x1;
            double dy = y - y1;
            radiusSquared = dx * dx + dy * dy;
        }

        public Circle(CDTVector v1, CDTVector v2)
        {
            x = (v1.x + v2.x) * 0.5;
            y = (v1.y + v2.y) * 0.5;

            double dx = v1.x - v2.x;
            double dy = v1.y - v2.y;
            radiusSquared = 0.25 * (dx * dx + dy * dy);
        }

        public bool Contains(double x, double y)
        {
            double dx = this.x - x;
            double dy = this.y - y;
            return dx * dx + dy * dy < radiusSquared;
        }
    }
}
