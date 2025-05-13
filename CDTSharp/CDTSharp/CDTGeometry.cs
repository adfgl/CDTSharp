namespace CDTSharp
{
    public static class CDTGeometry
    {
        public const int NO_INDEX = -1;

        public static CDTRect Bounds(IEnumerable<CDTVertex> vertices)
        {
            double minX, minY, maxX, maxY;
            minX = minY = double.MaxValue;
            maxX = maxY = double.MinValue;
            foreach (CDTVertex vtx in vertices)
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

        public static List<CDTVertex> ExtractUnique(IEnumerable<CDTVertex> vertices, double eps = 1e-6)
        {
            List<CDTVertex> unique = new List<CDTVertex>();

            double epsSqr = eps * eps;
            foreach (CDTVertex vtx in vertices)
            {
                double x = vtx.x;
                double y = vtx.y;

                bool duplicate = false;
                foreach (CDTVertex existing in unique)
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
                    unique.Add(new CDTVertex(x, y, unique.Count));
                }
            }
            return unique;
        }
    }
}
