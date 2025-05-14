namespace CDTSharp
{
    public class CDT
    {
        readonly List<Vec2> _vertices = new List<Vec2>();
        readonly List<Triangle> _triangles = new List<Triangle>();

        public void Triangulate(IEnumerable<Vec2> vertices)
        {
            _vertices.Clear();
            _triangles.Clear();

            List<Vec2> unique = CDTGeometry.ExtractUnique(vertices);
            if (unique.Count < 3)
            {
                throw new ArgumentException("Set of points must contain at least 3 points.");
            }

            Rect bounds = Rect.FromPoints(unique);

            AddSuperTriangle(bounds);
        }

        public IReadOnlyList<Vec2> Vertices => _vertices;
        public IReadOnlyList<Triangle> Triangles => _triangles;

        void AddSuperTriangle(Rect rect)
        {
            double dmax = Math.Max(rect.maxX - rect.minX, rect.maxY - rect.minY);
            double midx = (rect.maxX + rect.minX) * 0.5;
            double midy = (rect.maxY + rect.minY) * 0.5;
            double scale = 5;

            Vec2 a = new Vec2(midx - scale * dmax, midy - dmax);
            Vec2 b = new Vec2(midx, midy + scale * dmax);
            Vec2 c = new Vec2(midx + scale * dmax, midy - dmax);

            _vertices.Add(a);
            _vertices.Add(b);
            _vertices.Add(c);

            _triangles.Add(new Triangle(new Circle(a, b, c), 0, 1, 2));
        }
    }
}
