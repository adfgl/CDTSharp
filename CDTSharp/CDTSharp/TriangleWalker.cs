namespace CDTSharp
{
    public struct TriangleWalker
    {
        readonly List<Triangle> _triangles;
        readonly int _start;
        readonly int _vertex;
        int _current;

        public int Current => _current;

        public TriangleWalker(List<Triangle> triangles, int triangleIndex, int globalVertexIndex)
        {
            _triangles = triangles;
            _vertex = globalVertexIndex;
            _current = _start = triangleIndex;
        }

        public bool MoveNextCCW()
        {
            Triangle tri = _triangles[_current];
            int next = tri.adjacent[tri.IndexOf(_vertex)];
            if (next == _start) return false;
            _current = next;
            return true;
        }

        public bool MoveNextCW()
        {
            Triangle tri = _triangles[_current];
            int next = tri.adjacent[Triangle.PREV[tri.IndexOf(_vertex)]];
            if (next == _start) return false;
            _current = next;
            return true;
        }
    }
}
