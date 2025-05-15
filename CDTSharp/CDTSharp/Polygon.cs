namespace CDTSharp
{
    public readonly struct Polygon
    {
        public readonly int index;
        public readonly List<int> indices;

        public Polygon(int index, List<int> vertices)
        {
            this.index = index;
            this.indices = vertices;
        }

        public Rect Bounds(List<Vec2> verts)
        {
            double minX, minY, maxX, maxY;
            minX = minY = double.MaxValue;
            maxX = maxY = double.MinValue;
            foreach (int index in indices)
            {
                var (x, y) = verts[index];
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
            return new Rect(minX, minY, maxX, maxY);
        }

        public bool Contains(List<Vec2> vertices, double x, double y, double tolerance = 0)
        {
            List<Vec2>? verts = vertices;
            int count = verts.Count;
            bool inside = false;
            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                var (xi, yi) = verts[indices[i]];
                var (xj, yj) = verts[indices[j]];

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
