using System.ComponentModel.DataAnnotations;

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

        public CDTCircle(CDTVertex v1, CDTVertex v2, CDTVertex v3)
        {
            double x1 = v1.x, y1 = v1.y;
            double x2 = v2.x, y2 = v2.y;
            double x3 = v3.x, y3 = v3.y;

            // https://stackoverflow.com/questions/62488827/solving-equation-to-find-center-point-of-circle-from-3-points
            var x12 = x1 - x2;
            var x13 = x1 - x3;

            var y12 = y1 - y2;
            var y13 = y1 - y3;

            var y31 = y3 - y1;
            var y21 = y2 - y1;

            var x31 = x3 - x1;
            var x21 = x2 - x1;

            var sx13 = x1 * x1 - x3 * x3;
            var sy13 = y1 * y1 - y3 * y3;
            var sx21 = x2 * x2 - x1 * x1;
            var sy21 = y2 * y2 - y1 * y1;

            var f = (sx13 * x12 + sy13 * x12 + sx21 * x13 + sy21 * x13) / (2 * (y31 * x12 - y21 * x13));
            var g = (sx13 * y12 + sy13 * y12 + sx21 * y13 + sy21 * y13) / (2 * (x31 * y12 - x21 * y13));
            var c = -(x1 * x1) - y1 * y1 - 2 * g * x1 - 2 * f * y1;

            cx = -g;
            cy = -f;
            radiusSquared = cx * cx + cy * cy - c;
        }

        public bool Contains(double x, double y)
        {
            double dx = x - cx;
            double dy = y - cy;
            return dx * dx + dy * dy <= radiusSquared;
        }
    }
}
