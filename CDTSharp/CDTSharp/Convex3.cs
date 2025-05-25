using System.Runtime.CompilerServices;

namespace CDTSharp
{
   
    public class Convex3
    {
        public const int NO_INDEX = -1;

        double _minX, _minY, _minZ, _maxX, _maxY, _maxZ;
        readonly List<Face> _faces = new List<Face>();
        readonly List<Vec3> _vertices = new List<Vec3>();

        public Convex3(IEnumerable<Vec3> points)
        {
            List<Vec3> vts = points.ToList();

            _minX = _minY = _minZ = double.MaxValue;
            _maxX = _maxY = _maxZ = double.MinValue;

            int[] tetra = InitialHull(vts);
            Array.Sort(tetra);
            Array.Reverse(tetra);

            foreach (int i in tetra)
            {
                Vec3 v = vts[i];
                Expand(v.x, v.y, v.z);
                vts.RemoveAt(i);
                _vertices.Add(v);
            }

            List<HorizonEdge> horizon = new List<HorizonEdge>();
            for (int i = 2; i >= 0; i--)
            {
                horizon.Add(new HorizonEdge(0, i));
            }

            Face tetraBase = BuildFace(0, 1, 2);
            if (SignedDistance(tetraBase.normal, tetraBase.distanceToOrigin, _vertices[3]) > 0)
            {
                tetraBase = BuildFace(2, 1, 0);
            }
            _faces.Add(tetraBase);

            BuildNewFaces(3, horizon);

            for (int i = 0; i < vts.Count; i++)
            {
                var (x, y, z) = vts[i];
                AddPoint(x, y, z);
            }
        }

        public IReadOnlyList<Face> Faces => _faces;
        public List<Vec3> Vertices => _vertices;

        public double MinX => _minX;
        public double MinY => _minY;
        public double MinZ => _minZ;
        public double MaxX => _maxX;
        public double MaxY => _maxY;
        public double MaxZ => _maxZ;

        int[] InitialHull(IList<Vec3> points)
        {
            int minX = 0, maxX = 0, minY = 0, maxY = 0, minZ = 0, maxZ = 0;

            for (int i = 1; i < points.Count; i++)
            {
                var p = points[i];
                if (p.x < points[minX].x) minX = i;
                if (p.x > points[maxX].x) maxX = i;
                if (p.y < points[minY].y) minY = i;
                if (p.y > points[maxY].y) maxY = i;
                if (p.z < points[minZ].z) minZ = i;
                if (p.z > points[maxZ].z) maxZ = i;
            }

            // 1. Choose longest axis
            int i0 = minX, i1 = maxX;
            double maxDist = Vec3.SquareDistance(points[i0], points[i1]);

            double tryY = Vec3.SquareDistance(points[minY], points[maxY]);
            if (tryY > maxDist) { i0 = minY; i1 = maxY; maxDist = tryY; }

            double tryZ = Vec3.SquareDistance(points[minZ], points[maxZ]);
            if (tryZ > maxDist) { i0 = minZ; i1 = maxZ; maxDist = tryZ; }

            Vec3 a = points[i0];
            Vec3 b = points[i1];

            // 2. Find point with max area from line a-b (not colinear)
            int i2 = -1;
            double maxArea = -1;
            Vec3 ab = b - a;
            for (int i = 0; i < points.Count; i++)
            {
                if (i == i0 || i == i1) continue;
                Vec3 ac = points[i] - a;
                Vec3 n = Vec3.Cross(ab, ac);
                double area = Vec3.SquareLength(n);
                if (area > maxArea)
                {
                    maxArea = area;
                    i2 = i;
                }
            }

            if (i2 == -1)
                throw new Exception("Degenerate input: all points are colinear.");

            Vec3 c = points[i2];
            Vec3 normal = Vec3.Cross(b - a, c - a);

            // 3. Find point with max volume from plane abc (not coplanar)
            int i3 = -1;
            double maxVolume = -1;
            for (int i = 0; i < points.Count; i++)
            {
                if (i == i0 || i == i1 || i == i2) continue;
                Vec3 ad = points[i] - a;
                double vol = Math.Abs(Vec3.Dot(normal, ad));
                if (vol > maxVolume)
                {
                    maxVolume = vol;
                    i3 = i;
                }
            }

            if (i3 == -1)
                throw new Exception("Degenerate input: all points are coplanar.");

            return [i0, i1, i2, i3];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]  
        void Expand(double x, double y, double z)
        {
            if (x < _minX) _minX = x;
            if (y < _minY) _minY = y;
            if (z < _minZ) _minZ = z;
            if (x > _maxX) _maxX = x;
            if (y > _maxY) _maxY = y;
            if (z > _maxZ) _maxZ = z;
        }

        public bool AddPoint(double x, double y, double z)
        {
            Vec3 point = new Vec3(x, y, z);
            int remainingCount = RemoveVisibleFaces(point);
            if (remainingCount == _faces.Count)
            {
                return false;
            }

            _faces.RemoveRange(remainingCount, _faces.Count - remainingCount);

            _vertices.Add(point);
            Expand(x, y, z);

            List<HorizonEdge> horizon = BuildHorizon();
            BuildNewFaces(_vertices.Count - 1, horizon);
            return true;
        }

        void BuildNewFaces(int point, List<HorizonEdge> horizon)
        {
            int firstFace = NO_INDEX;
            int previousFace = NO_INDEX;
            for (int i = 0; i < horizon.Count; i++)
            {
                (int faceIndex, int edgeIndex) = horizon[i];

                Face face = _faces[faceIndex];

                int ai = face.indices[Face.NEXT[edgeIndex]];
                int bi = face.indices[edgeIndex];
                int ci = point;

                Face newFace = BuildFace(ai, bi, ci);
                int newFaceIndex = _faces.Count;
                _faces.Add(newFace);

                face.adjacent[edgeIndex] = newFaceIndex;
                newFace.adjacent[0] = faceIndex;

                if (previousFace != NO_INDEX)
                {
                    _faces[previousFace].adjacent[2] = newFaceIndex; 
                    newFace.adjacent[1] = previousFace;
                }
                else
                {
                    firstFace = newFaceIndex;
                }
                previousFace = newFaceIndex;
            }

            _faces[previousFace].adjacent[2] = firstFace;
            _faces[firstFace].adjacent[1] = previousFace;
        }

        Face BuildFace(int a, int b, int c)
        {
            Vec3 va = _vertices[a];
            Vec3 vb = _vertices[b];
            Vec3 vc = _vertices[c];

            Vec3 normal = Vec3.Cross(vb - va, vc - va).Normalize();
            double distanceToOrigin = Vec3.Dot(normal, va);
            return new Face(a, b, c, normal, distanceToOrigin);
        }

        List<HorizonEdge> BuildHorizon()
        {
            List<HorizonEdge> horizonEdges = new List<HorizonEdge>();
            HorizonEdge start = HorizonStart();

            HorizonEdge current = start;
            do
            {
                horizonEdges.Add(current);
                current = GetNextHorizonEdge(current);

            } while (!start.Equals(current));
            return horizonEdges;
        }

        HorizonEdge GetNextHorizonEdge(HorizonEdge current)
        {
            int prevEdge = Face.PREV[current.index];
            int adjFace = _faces[current.face].adjacent[prevEdge];
            while (true)
            {
                if (adjFace == NO_INDEX)
                {
                    Face adj = _faces[current.face];
                    return new HorizonEdge(current.face, prevEdge);
                }

                Face next = _faces[adjFace];

                int backEdge = next.IndexOf(current.face);
                if (backEdge == NO_INDEX)
                    throw new Exception("Non-manifold edge or invalid mesh linkage");

                prevEdge = Face.PREV[backEdge];
                adjFace = next.adjacent[prevEdge];

                current = new HorizonEdge(adjFace, prevEdge);
            }
        }

        readonly struct HorizonEdge : IEquatable<HorizonEdge>
        {
            public readonly int face, index;

            public HorizonEdge(int face, int edge)
            {
                this.face = face;
                this.index = edge;
            }

            public void Deconstruct(out int face, out int edge)
            {
                face = this.face;
                edge = this.index;
            }

            public bool Equals(HorizonEdge other)
            {
                return face == other.face && index == other.index;
            }
        }

        HorizonEdge HorizonStart()
        {
            for (int i = 0; i < _faces.Count; i++)
            {
                var face = _faces[i];
                for (int j = 0; j < 3; j++)
                {
                    if (face.adjacent[j] == NO_INDEX)
                    {
                        return new HorizonEdge(i, j);
                    }
                }
            }
            throw new Exception("Failed to locate start of horizon.");
        }

        int RemoveVisibleFaces(Vec3 p)
        {
            int write = 0;
            for (int read = 0; read < _faces.Count; read++)
            {
                Face face = _faces[read];
                if (Orientation(face.normal, face.distanceToOrigin, p) == 1)
                {
                    MarkRemoved(read);
                    continue;
                }

                if (write != read)
                {
                    _faces[write] = _faces[read];
                }
                write++;
            }
            return write;
        }

        void MarkRemoved(int index)
        {
            Face face = _faces[index];
            for (int i = 0; i < 3; i++)
            {
                int adjIndex = face.adjacent[i];
                if (adjIndex == NO_INDEX) continue;

                int start = face.indices[i];
                int end = face.indices[Face.NEXT[i]];

                Face adjFace = _faces[adjIndex];
                int edge = adjFace.IndexOf(end, start);
                adjFace.adjacent[edge] = NO_INDEX;

                face.adjacent[i] = NO_INDEX;
            }
        }

        public static double SignedDistance(Vec3 planeNormal, double distanceToOrigin, Vec3 point)
        {
            return Vec3.Dot(planeNormal, point) - distanceToOrigin;
        }

        public static Expansion SignedDistanceExact(Vec3 planeNormal, double distanceToOrigin, Vec3 point)
        {
            Expansion dot = new Expansion(planeNormal.x) * point.x
                        + new Expansion(planeNormal.y) * point.y
                        + new Expansion(planeNormal.z) * point.z;

            return dot - new Expansion(distanceToOrigin);
        }

        /// <summary>
        /// -1 behind plne, 0 on plane, +1 in front of plane
        /// </summary>
        /// <param name="normal"></param>
        /// <param name="distToOrigin"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public static int Orientation(Vec3 planeNormal, double distanceToOrigin, Vec3 point)
        {
            double maxAbs = Math.Max(Math.Abs(planeNormal.x * point.x),
                             Math.Max(Math.Abs(planeNormal.y * point.y),
                                      Math.Abs(planeNormal.z * point.z)));

            double bound = ExpansionConstants.Resulterrbound * maxAbs;
            double signedDistance = SignedDistance(planeNormal, distanceToOrigin, point);
            if (Math.Abs(signedDistance) > bound)
            {
                // Fast-path result is numerically safe
                return Math.Sign(signedDistance);
            }

            Expansion expansion = SignedDistanceExact(planeNormal, distanceToOrigin, point);
            return expansion.Sign();
        }

        public static Expansion SignedDistanceExp(Vec3 normal, Vec3 point, double distanceToOrigin)
        {
            Expansion dot = new Expansion(normal.x) * point.x
                          + new Expansion(normal.y) * point.y
                          + new Expansion(normal.z) * point.z;

            return dot - new Expansion(distanceToOrigin);
        }

      

    }

    public readonly struct Face 
    {
        public readonly static int[] NEXT = [1, 2, 0], PREV = [2, 0, 1];

        public readonly int[] indices, adjacent;
        public readonly Vec3 normal;
        public readonly double distanceToOrigin;

        public Face(int a, int b, int c, Vec3 normal, double distanceToOrigin)
        {
            this.normal = normal.Normalize();
            this.distanceToOrigin = distanceToOrigin;
            this.indices = [a, b, c];
            this.adjacent = [Convex3.NO_INDEX, Convex3.NO_INDEX, Convex3.NO_INDEX];
        }

        public int IndexOf(int v)
        {
            for (int i = 0; i < 3; i++)
            {
                if (indices[i] == v) return i;
            }
            return Convex3.NO_INDEX;
        }

        public int IndexOf(int from, int to)
        {
            for (int i = 0; i < 3; i++)
            {
                if (indices[i] == from && indices[NEXT[i]] == to)
                {
                    return i;
                }
            }
            return Convex3.NO_INDEX;
        }
    }
}
