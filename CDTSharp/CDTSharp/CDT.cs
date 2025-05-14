namespace CDTSharp
{
    using static CDTGeometry;

    public class CDT
    {
        readonly List<Vec2> _vertices = new List<Vec2>();
        readonly List<Triangle> _triangles = new List<Triangle>();

        public void Triangulate(IEnumerable<Vec2> vertices)
        {
            _vertices.Clear();
            _triangles.Clear();

            List<Vec2> unique = ExtractUnique(vertices);
            if (unique.Count < 3)
            {
                throw new ArgumentException("Set of points must contain at least 3 points.");
            }

            Rect bounds = Rect.FromPoints(unique);

            AddSuperTriangle(bounds);

            Stack<Edge> legalize = new Stack<Edge>();
            foreach (Vec2 v in vertices)
            {
                (int triangleIndex, int edgeIndex) = FindContaining(_vertices, _triangles, v);
                Insert(legalize, v, triangleIndex, edgeIndex);
            }


        }

        public IReadOnlyList<Vec2> Vertices => _vertices;
        public IReadOnlyList<Triangle> Triangles => _triangles;

        void Insert(Stack<Edge> legalize, Vec2 vertex, int triangle, int edge)
        {
            int vertexindex = _vertices.Count;
            _vertices.Add(vertex);

            Vec2 vtx = _vertices[vertexindex];
            if (edge != NO_INDEX)
            {
                SplitTriangle(legalize, _vertices, _triangles, triangle, vertexindex);
            }
            else
            {
                SplitEdge(legalize, _vertices, _triangles, triangle, edge, vertexindex);
            }
            Legalize(legalize, _vertices, _triangles);
        }

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
