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

        public CDTCircle(CDTVertex a, CDTVertex b, CDTVertex c)
        {
            double x1 = a.x, y1 = a.y;
            double x2 = b.x, y2 = b.y;
            double x3 = c.x, y3 = c.y;

            double dx12 = x1 - x2;
            double dy12 = y1 - y2;
            double dx13 = x1 - x3;
            double dy13 = y1 - y3;

            double midpointProjection12 = (x1 * x1 - x2 * x2 + y1 * y1 - y2 * y2) * 0.5;
            double midpointProjection13 = (x1 * x1 - x3 * x3 + y1 * y1 - y3 * y3) * 0.5;

            double determinant = dx12 * dy13 - dy12 * dx13;
            cx = (dy13 * midpointProjection12 - dy12 * midpointProjection13) / determinant;
            cy = (dx12 * midpointProjection13 - dx13 * midpointProjection12) / determinant;

            double dx = x1 - cx;
            double dy = y1 - cy;
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
