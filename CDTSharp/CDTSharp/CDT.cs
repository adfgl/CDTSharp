namespace CDTSharp
{
    public class CDT
    {
        readonly List<CDTVertex> _vertices = new List<CDTVertex>();
        readonly List<CDTTriangle> _triangles = new List<CDTTriangle>();

        public void Triangulate(IEnumerable<CDTVertex> vertices)
        {
            _vertices.Clear();
            _triangles.Clear();

            List<CDTVertex> unique = CDTGeometry.ExtractUnique(vertices);
            if (unique.Count < 3)
            {
                throw new ArgumentException("Set of points must contain at least 3 points.");
            }
        }




    }
}
