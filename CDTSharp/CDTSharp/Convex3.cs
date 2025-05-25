using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace CDTSharp
{
    public class Convex3
    {
        public const int NO_INDEX = -1;

        double _minX, _minY, _minZ, _maxX, _maxY, _maxZ;
        readonly List<Face> _faces = new List<Face>();

        public Convex3(IEnumerable<Vec3> points)
        {
            List<Vec3> vts = points.ToList();

            _minX = _minY = _minZ = double.MaxValue;
            _maxX = _maxY = _maxZ = double.MinValue;
            int[] tetra = InitialHull(vts);
            Array.Sort(tetra);
            Array.Reverse(tetra);

            Vertex[] verts = new Vertex[4];
            for (int i = 0; i < 4; i++)
            {
                int index = tetra[i];
                Vec3 v = vts[index];
                Expand(v.x, v.y, v.z);
                vts.RemoveAt(index);
                verts[i] = new Vertex(v);
            }

            Face tetraBase = BuildFace(verts[0], verts[1], verts[2]);
            if (SignedDistance(tetraBase.Normal, tetraBase.DistanceToOrigin, verts[3].Position) > 0)
            {
                tetraBase = BuildFace(verts[2], verts[1], verts[0]);
            }

            _faces.Add(tetraBase);

            BuildNewFaces(verts[3], tetraBase.Backward().ToList());
            for (int i = 0; i < vts.Count; i++)
            {
                var (x, y, z) = vts[i];
                AddPoint(x, y, z);
            }
        }

        public IReadOnlyList<Face> Faces => _faces;

        public double MinX => _minX;
        public double MinY => _minY;
        public double MinZ => _minZ;
        public double MaxX => _maxX;
        public double MaxY => _maxY;
        public double MaxZ => _maxZ;

        public string AsObj()
        {
            StringBuilder sb = new StringBuilder();

            List<Vertex> vertices = new List<Vertex>();
            HashSet<Vertex> seen = new HashSet<Vertex>();
            foreach (var face in _faces)
            {
                foreach (var item in face.Forward())
                {
                    if (seen.Add(item.Origin))
                    {
                        vertices.Add(item.Origin);
                        Vec3 v = item.Origin.Position;
                        sb.AppendLine($"v {v.x} {v.y} {v.z}");
                    }
                }
            }
            foreach (var face in _faces)
            {
                int a = vertices.IndexOf(face.Edge.Origin) + 1;
                int b = vertices.IndexOf(face.Edge.Next.Origin) + 1;
                int c = vertices.IndexOf(face.Edge.Prev.Origin) + 1;
                sb.AppendLine($"f {a} {b} {c}");
            }
            string content = sb.ToString();
            File.WriteAllText(@"C:\Users\zhukopav\Documents\demoCVX.obj", content);
            return content;
        }

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
            if (RemoveVisibleFaces(point) == 0)
            {
                return false;
            }

            Expand(x, y, z);
            BuildNewFaces(new Vertex(point), BuildHorizon());
            return true;
        }

        void BuildNewFaces(Vertex vertex, List<HalfEdge> horizon)
        {
            Face firstFace = null!;
            Face lastFace = null!;
            for (int i = 0; i < horizon.Count; i++)
            {
                HalfEdge edge = horizon[i];
                Face face = BuildFace(edge.Next.Origin, edge.Origin, vertex);
                _faces.Add(face);

                SetTwin(face.Edge, edge);
                if (lastFace != null)
                {
                    SetTwin(face.Edge.Prev, lastFace.Edge.Next);
                }
                else
                {
                    firstFace = face;
                }
                lastFace = face;
            }
            SetTwin(firstFace.Edge.Prev, lastFace.Edge.Next);
        }

        Face BuildFace(Vertex a, Vertex b, Vertex c)
        {
            Vec3 normal = Vec3.Cross(b.Position - a.Position, c.Position - a.Position).Normalize();

            Face face = new Face()
            {
                Normal = normal,
                DistanceToOrigin = Vec3.Dot(normal, a.Position)
            };

            HalfEdge ab = new HalfEdge(a) { Face = face };
            HalfEdge bc = new HalfEdge(b) { Face = face };
            HalfEdge ca = new HalfEdge(c) { Face = face };

            face.Edge = ab;

            ab.Next = bc; ab.Prev = ca;
            bc.Next = ca; bc.Prev = ab;
            ca.Next = ab; ca.Prev = bc;

            return face;
        }

        static void SetTwin(HalfEdge edge, HalfEdge twin)
        {
            edge.Twin = twin;
            twin.Twin = edge;
        }

        List<HalfEdge> BuildHorizon()
        {
            HalfEdge? horizonStart = null;
            foreach (Face face in _faces)
            {
                if (horizonStart is not null)
                {
                    break;
                }

                foreach (HalfEdge e in face.Forward())
                {
                    if (e.Twin is null)
                    {
                        horizonStart = e;
                        break;
                    }
                }
            }

            if (horizonStart is null)
            {
                throw new Exception("LOGIC ERROR: Horizon start not found.");
            }

            List<HalfEdge> horizonEdges = new List<HalfEdge>();
            HalfEdge he = horizonStart;
            do
            {
                horizonEdges.Add(he);
                he = GetNextHorizonEdge(he);
            } while (he != horizonStart);
            return horizonEdges;
        }

        static HalfEdge GetNextHorizonEdge(HalfEdge he)
        {
            HalfEdge current = he.Prev;
            while (true)
            {
                if (current.Twin is null)
                {
                    return current;
                }
                current = current.Twin.Prev;
            }
        }

        int RemoveVisibleFaces(Vec3 p)
        {
            int removed = 0;
            for (int i = _faces.Count - 1; i >= 0; i--)
            {
                Face face = _faces[i];
                if (Orientation(face.Normal, face.DistanceToOrigin, p) == 1)
                {
                    foreach (HalfEdge he in face.Forward())
                    {
                        he.Origin = null;
                        he.Face = null;
                        if (he.Twin is not null)
                        {
                            he.Twin.Twin = null;
                            he.Twin = null;
                        }
                    }
                    _faces.RemoveAt(i);
                    removed++;
                }   
            }
            return removed;
        }

        public static double SignedDistance(Vec3 planeNormal, double distanceToOrigin, Vec3 point)
        {
            return Vec3.Dot(planeNormal, point) - distanceToOrigin;
        }

        public static Expansion SignedDistanceExact(Vec3 planeNormal, double distanceToOrigin, Vec3 point)
        {
            Expansion dx = Expansion.Multiply(planeNormal.x, point.x);
            Expansion dy = Expansion.Multiply(planeNormal.y, point.y);
            Expansion dz = Expansion.Multiply(planeNormal.z, point.z);

            Expansion dot = Expansion.Add(dx, Expansion.Add(dy, dz));
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

    public class Vertex
    {
        public Vertex(Vec3 position) 
        {
            Position = position;
        }

        public Vec3 Position { get; set; }
    }

    public class HalfEdge
    {
        public HalfEdge(Vertex origin)
        {
            Origin = origin;
        }

        public Vertex Origin { get; set; }
        public Face Face { get; set; } = null!;
        public HalfEdge? Twin { get; set; } = null;
        public HalfEdge Next { get; set; } = null!;
        public HalfEdge Prev { get; set; } = null!;
    }

    public class Face
    {
        public HalfEdge Edge { get; set; } = null!;
        public Vec3 Normal { get; set; }
        public double DistanceToOrigin { get; set; }

        public IEnumerable<HalfEdge> Forward()
        {
            HalfEdge he = Edge;
            HalfEdge current = he;
            do
            {
                yield return current;
                current = current.Next;
            } while (current != he);
        }

        public IEnumerable<HalfEdge> Backward()
        {
            HalfEdge he = Edge;
            HalfEdge current = he;
            do
            {
                yield return current;
                current = current.Prev;
            } while (current != he);
        }
    }
}
