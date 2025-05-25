namespace CDTSharp
{
    public struct TriangleWalker<T> where T : ITriangle
    {
        readonly List<T> _triangles;
        readonly int _start;
        readonly int _vertex;
        int _current;

        public int Current => _current;

        public TriangleWalker(List<T> triangles, int triangleIndex, int globalVertexIndex)
        {
            _triangles = triangles;
            _vertex = globalVertexIndex;
            _current = _start = triangleIndex;
        }

        public bool MoveNextCCW()
        {
            ITriangle tri = _triangles[_current];
            int next = tri.Adjacent[tri.IndexOf(_vertex)];
            if (next == _start) return false;
            _current = next;
            return true;
        }

        public bool MoveNextCW()
        {
            ITriangle tri = _triangles[_current];
            int indexOfVertex = tri.IndexOf(_vertex);
            if (indexOfVertex == -1)
            {
                return false;
            }

            int next = tri.Adjacent[CDTTriangle.PREV[indexOfVertex]];
            if (next == _start) return false;
            _current = next;
            return true;
        }
    }
}
