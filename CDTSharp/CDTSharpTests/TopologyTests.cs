using CDTSharp;
using Xunit.Sdk;

namespace CDTSharpTests
{
    using static CDT;

    public class TopologyTests
    {
        static CDT TestCase()
        {
            /*
               5-------6------7
               |\  4  /\   5 /|
               | \   /  \   / |
               |  \ /  0 \ /  |
               | 6 3------4  7|
               |  / \  1 / \  |
               | /   \  /   \ |
               |/  2  \/   3 \|
               0-------1------2
             */

            List<CDTVector> v = [
                new CDTVector(-50, -50), new CDTVector(0, -50), new CDTVector(50, -50),
                new CDTVector(-25, 0), new CDTVector(25, 0),
                new CDTVector(-50, 50), new CDTVector(0, 50), new CDTVector(50, 50)
                ];

            List<CDTTriangle> t = [
                new CDTTriangle(new Circle(), 3, 6, 4, 4, 5, 1),   // 0
                new CDTTriangle(new Circle(), 1, 3, 4, 2, 0, 3),   // 1
                new CDTTriangle(new Circle(), 1, 0, 3, NO_INDEX, 6, 1), // 2
                new CDTTriangle(new Circle(), 4, 2, 1, 7, NO_INDEX, 1), // 3
                new CDTTriangle(new Circle(), 5, 6, 3, NO_INDEX, 0, 6), // 4
                new CDTTriangle(new Circle(), 7, 4, 6, 7, 0, NO_INDEX), // 5
                new CDTTriangle(new Circle(), 5, 3, 0, 4, 2, NO_INDEX), // 6
                new CDTTriangle(new Circle(), 7, 2, 4, NO_INDEX, 3, 5), // 7
                ];

            var cdt = new CDT();
            cdt.Vertices.AddRange(v);
            cdt.Triangles.AddRange(t);
            return cdt;
        }

        [Fact]
        public void FindContaining_CorrectlyFindsTriangleWhenPointIsInside()
        {
            CDT cdt = TestCase();

            for (int i = 0; i < cdt.Triangles.Count; i++)
            {
                CDTVector center = CDTVector.Zero;
                foreach (var item in cdt.Triangles[i].indices)
                {
                    center += cdt.Vertices[item];
                }
                center /= 3;

                (int triangleIndex, int edgeIndex) = cdt.FindContaining(center);

                Assert.Equal(i, triangleIndex);
                Assert.Equal(NO_INDEX, edgeIndex);
            }
        }

        [Fact]
        public void FindContaining_CorrecltlyFindsTriangleWhenPointOnEdge()
        {
            CDT cdt = TestCase();
            for (int i = 0; i < cdt.Triangles.Count; i++)
            {
                CDTTriangle tri = cdt.Triangles[i];
                for (int j = 0; j < 3; j++)
                {
                    int a = tri.indices[j];
                    int b = tri.indices[(j + 1) % 3];
                    Edge expectedA = cdt.FindEdge(a, b);
                    Edge expectedB = cdt.FindEdge(b, a);

                    CDTVector center = (cdt.Vertices[a] + cdt.Vertices[b]) / 2;

                    (int ti, int ei) = cdt.FindContaining(center);

                    Assert.True(ti == expectedA.triangle ||  ti == expectedB.triangle);
                    Assert.True(ei == expectedA.index || ei == expectedB.index);
                }
            }
        }

        [Fact]
        public void TriangleWalker_CorrectlyObtainsSurroundingTriangles_CW()
        {
            CDT cdt = TestCase();

            TriangleWalker walker = new TriangleWalker(cdt.Triangles, 0, 3);

            List<int> tris = [walker.Current];
            while (walker.MoveNextCCW())
            {
                tris.Add(walker.Current);
            }

            int[] expected = [0, 4, 6, 2, 1];
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], tris[i]);
            }
        }

        [Fact]
        public void TriangleWalker_CorrectlyObtainsSurroundingTriangles_CCW()
        {
            CDT cdt = TestCase();

            TriangleWalker walker = new TriangleWalker(cdt.Triangles, 0, 3);

            List<int> tris = [walker.Current];
            while (walker.MoveNextCW())
            {
                tris.Add(walker.Current);
            }

            int[] expected = [0, 1, 2, 6, 4];
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], tris[i]);
            }
        }

        [Fact]
        public void TriangleEdgeSplitCorrectly()
        {
            CDT cdt = TestCase();

            /*
                5-------6------7  >  5-------6-------7
                |\  4  /\   5 /|  >  |\  4  /|\   5 /|
                | \   /  \   / |  >  | \   / | \   / |
                |  \ /  0 \ /  |  >  |  \ / 0|1 \ /  |
                | 6 3------4  7|  >  | 6 3---+---4  7|
                |  / \  1 / \  |  >  |  / \ 9|8 / \  |
                | /   \  /   \ |  >  | /   \ | /   \ |
                |/  2  \/   3 \|  >  |/  2  \|/   3 \|
                0-------1------2  >  0-------1-------2
              */

            List<CDTTriangle> triangles = cdt.Triangles;

            int vi = cdt.Vertices.Count;
            cdt.Vertices.Add(new CDTVector(0, 0));
            cdt.SplitEdge(0, triangles[0].IndexOf(4, 3), vi);

            AssertHelper.Equal(new CDTTriangle(new Circle(), 3, 6, vi, 4, 1, 9), triangles, 0);
            AssertHelper.Equal(new CDTTriangle(new Circle(), 6, 4, vi, 5, 8, 0), triangles, 1);
            AssertHelper.Equal(new CDTTriangle(new Circle(), 4, 1, vi, 3, 9, 1), triangles, 8);
            AssertHelper.Equal(new CDTTriangle(new Circle(), 1, 3, vi, 2, 0, 8), triangles, 9);
        }

        [Fact]
        public void TriangleSplitCenterSplitWorksCorrectlyWithNoNeighbours()
        {
            CDT cdt = new CDT();
            cdt.Vertices.AddRange([new CDTVector(-100, -100), new CDTVector(0, 100), new CDTVector(100, -100)]);
            cdt.Triangles.AddRange([new CDTTriangle(new Circle(), 0, 1, 2)]);

            cdt.Vertices.Add(new CDTVector(0, 0));
            cdt.SplitTriangle(0, cdt.Vertices.Count - 1);

            AssertHelper.Equal(new CDTTriangle(new Circle(), 0, 1, 3, NO_INDEX, 1, 2), cdt.Triangles, 0);
            AssertHelper.Equal(new CDTTriangle(new Circle(), 1, 2, 3, NO_INDEX, 2, 0), cdt.Triangles, 1);
            AssertHelper.Equal(new CDTTriangle(new Circle(), 2, 0, 3, NO_INDEX, 0, 1), cdt.Triangles, 2);
        }

        [Fact]
        public void DiagonalSwapWorksCorrectly()
        {
            CDT cdt = TestCase();

            /*
                  5-------6------7    >    5-------6------7
                  |\  4  /\   5 /|    >    |\  4  /|\   5 /|
                  | \   /  \   / |    >    | \   / | \   / |
                  |  \ /  0 \ /  |    >    |  \ /  |  \ /  |
                  | 6 3------4  7|    >    | 6 3 1 | 0 4  7|
                  |  / \  1 / \  |    >    |  / \  |  / \  |
                  | /   \  /   \ |    >    | /   \ | /   \ |
                  |/  2  \/   3 \|    >    |/  2  \|/   3 \|
                  0-------1------2    >    0-------1------2
             */

            List<CDTTriangle> triangles = cdt.Triangles;

            cdt.FlipEdge(0, triangles[0].IndexOf(4, 3));

            AssertHelper.Equal(new CDTTriangle(new Circle(), 1, 6, 4, 1, 5, 3), triangles, 0);
            AssertHelper.Equal(new CDTTriangle(new Circle(), 6, 1, 3, 0, 2, 4), triangles, 1);
                
            CDTTriangle t5 = triangles[5];
            Assert.Equal(0, t5.adjacent[t5.IndexOf(4, 6)]);

            CDTTriangle t3 = triangles[3];
            Assert.Equal(0, t3.adjacent[t3.IndexOf(1, 4)]);

            CDTTriangle t4 = triangles[4];
            Assert.Equal(1, t4.adjacent[t4.IndexOf(6, 3)]);

            CDTTriangle t2 = triangles[2];
            Assert.Equal(1, t2.adjacent[t2.IndexOf(3, 1)]);
        }

        [Fact]
        public void QuadIsConvex_ReturnsTrueWhenTrulyConvex_CW()
        {
            CDTVector v0 = new CDTVector(-1, 0);
            CDTVector v1 = new CDTVector(0, +1);
            CDTVector v2 = new CDTVector(+1, 0);
            CDTVector v3 = new CDTVector(0, -1);

            Assert.True(ConvexQuad(v0, v1, v2, v3));
        }

        [Fact]
        public void QuadIsConvex_ReturnsTrueWhenTrulyConvex_CCW()
        {
            CDTVector v0 = new CDTVector(-1, 0);
            CDTVector v1 = new CDTVector(0, +1);
            CDTVector v2 = new CDTVector(+1, 0);
            CDTVector v3 = new CDTVector(0, -1);

            Assert.True(ConvexQuad(v3, v2, v1, v0));
        }

        [Fact]
        public void QuadIsConvex_ReturnsFalseWhenToConvex()
        {
            CDTVector v0 = new CDTVector(-1, 0);
            CDTVector v1 = new CDTVector(0, +1);
            CDTVector v2 = new CDTVector(-0.75, 0);
            CDTVector v3 = new CDTVector(0, -1);

            Assert.False(ConvexQuad(v0, v1, v2, v3));
        }

        [Fact]
        public void Intersect_FindsIntersectionWhenActuallyIntersect()
        {
            CDTVector p1 = new CDTVector(-50, -50), p2 = new CDTVector(+50, +50);
            CDTVector q1 = new CDTVector(-50, +50), q2 = new CDTVector(+50, -50);

            Assert.True(Intersect(p1, p2, q1, q2, out CDTVector inter));
            Assert.Equal(0, inter.x);
            Assert.Equal(0, inter.y);
        }

        [Fact]
        public void Intersect_NoIntersectionIfParallel()
        {
            CDTVector p1 = new CDTVector(0, -50), p2 = new CDTVector(0, +50);
            CDTVector q1 = new CDTVector(20, +50), q2 = new CDTVector(20, -50);

            Assert.False(Intersect(p1, p2, q1, q2, out CDTVector inter));
            Assert.Equal(Double.NaN, inter.x);
            Assert.Equal(Double.NaN, inter.y);
        }

        [Fact]
        public void Intersect_NoIntersectionWhenOverlap()
        {
            CDTVector p1 = new CDTVector(0, -50), p2 = new CDTVector(0, +50);
            CDTVector q1 = new CDTVector(0, -25), q2 = new CDTVector(0, +25);

            Assert.False(Intersect(p1, p2, q1, q2, out CDTVector inter));
            Assert.Equal(Double.NaN, inter.x);
            Assert.Equal(Double.NaN, inter.y);
        }

        [Fact]
        public void Intersect_NoIntersectionWhenHitsNode()
        {
            CDTVector p1 = new CDTVector(0, -50), p2 = new CDTVector(0, +50);
            CDTVector q1 = p1, q2 = new CDTVector(0, -70);

            Assert.False(Intersect(p1, p2, q1, q2, out CDTVector inter));
            Assert.Equal(Double.NaN, inter.x);
            Assert.Equal(Double.NaN, inter.y);
        }

        [Fact]
        public void Intersect_HasIntersectionWhenNodeLiesOnSegment()
        {
            CDTVector p1 = new CDTVector(0, -50), p2 = new CDTVector(0, +50);
            CDTVector q1 = new CDTVector(0, 0), q2 = new CDTVector(50, 0);

            Assert.True(Intersect(p1, p2, q1, q2, out CDTVector inter));
            Assert.Equal(q1.x, inter.x);
            Assert.Equal(q1.y, inter.y);
        }

        [Fact]
        public void OnSegment_InTheCenter()
        {
            CDTVector start = new CDTVector(0, 0), end = new CDTVector(0, 50);
            CDTVector center = (start + end) / 2;
            Assert.True(OnSegment(start, end, center, 0));
        }

        [Fact]
        public void OnSegment_OnStart()
        {
            CDTVector start = new CDTVector(0, 0), end = new CDTVector(0, 50);
            Assert.True(OnSegment(start, end, start, 0));
        }

        [Fact]
        public void OnSegment_OnEnd()
        {
            CDTVector start = new CDTVector(0, 0), end = new CDTVector(0, 50);
            Assert.True(OnSegment(start, end, end, 0));
        }
    }
}
