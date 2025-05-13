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
        }

        public IReadOnlyList<Vec2> Vertices => _vertices;
        public IReadOnlyList<Triangle> Triangles => _triangles;
    }
}
