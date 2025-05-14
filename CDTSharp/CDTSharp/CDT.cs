namespace CDTSharp
{
    using System.ComponentModel;
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

            RemoveTrianglesContainingSuperVertices();
        }

        public List<Vec2> Vertices => _vertices;
        public List<Triangle> Triangles => _triangles;

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

        void RemoveTrianglesContainingSuperVertices()
        {
            _vertices.RemoveRange(0, 3);

            int write = 0;
            for (int read = 0; read < _triangles.Count; read++)
            {
                Triangle tri = _triangles[read];

                bool discard = tri.ContainsSuper();
                if (discard)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        int twinIndex = tri.adjacent[i];
                        if (twinIndex == NO_INDEX)
                        {
                            continue;
                        }

                        Triangle twin = _triangles[twinIndex];
                        int a = tri.indices[i];
                        int b = tri.indices[Triangle.NEXT[i]];
                        int twinEdge = twin.IndexOf(b, a);
                        if (twinEdge != NO_INDEX)
                        {
                            twin.adjacent[twinEdge] = NO_INDEX;
                        }
                    }
                }
                else
                {
                    _triangles[write++] = tri;
                }
            }

            if (write < _triangles.Count)
            {
                _triangles.RemoveRange(write, _triangles.Count - write);
            }

            Dictionary<int, int> remap = new Dictionary<int, int>();
            for (int i = 0; i < _triangles.Count; i++)
            {
                Triangle tri = _triangles[i];

                for (int j = 0; j < 3; j++)
                {
                    tri.indices[j] -= 3;

                    int twin = tri.adjacent[j];
                    tri.adjacent[j] = remap.TryGetValue(twin, out int newTwin) ? newTwin : NO_INDEX;
                }

                _triangles[i] = tri;
            }
        }
    }
}
