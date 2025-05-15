namespace CDTSharp
{
    public readonly struct Polygon
    {
        public readonly int index;
        public readonly Vec2[] vertices;
        public readonly Rect rect;

        public Polygon(int index, Vec2[] vertices)
        {
            this.vertices = new Vec2[vertices.Length];

            double minX, minY, maxX, maxY;
            minX = minY = double.MaxValue;
            maxX = maxY = double.MinValue;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vec2 v = vertices[i];
                this.vertices[i] = v;

                var (x, y) = v;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
            this.rect = new Rect(minX, minY, maxX, maxY);
        }

        public bool Contains(double x, double y, double tolerance = 0)
        {
            if (!rect.Contains(x, y)) return false;

            Vec2[] verts = vertices;
            int count = verts.Length;
            bool inside = false;
            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                double xi = verts[i].x, yi = verts[i].y;
                double xj = verts[j].x, yj = verts[j].y;

                bool crosses = (yi > y + tolerance) != (yj > y + tolerance);
                if (!crosses) continue;

                double t = (y - yi) / (yj - yi);
                double xCross = xi + t * (xj - xi);
                if (x < xCross - tolerance)
                {
                    inside = !inside;
                }
            }
            return inside;
        }
    }
}
