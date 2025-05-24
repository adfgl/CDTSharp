namespace CDTSharp
{
    public readonly struct Polygon
    {
        public readonly int index;
        public readonly List<int> indices;

        public Polygon(int index, List<int> indices)
        {
            this.index = index;
            this.indices = indices;
        }

        public Rect Bounds(List<CDTVector> verts)
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

        public bool Contains<T>(List<T> vertices, Func<T, double> getX, Func<T, double> getY, double x, double y, double tolerance = 0)
        {
            int count = indices.Count;
            bool inside = false;
            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                T vi = vertices[i];
                T vj = vertices[j];

                double xi = getX(vi);
                double yi = getY(vi);

                double xj = getX(vj);
                double yj = getY(vj); 

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
