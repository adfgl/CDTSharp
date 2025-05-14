using CDTSharp;
using Xunit.Sdk;

namespace CDTSharpTests
{
    using static CDTGeometry;

    public class GeometryTests
    {
        [Fact]
        public void TriangleSplitCenterSplitWorksCorrectlyWithNoNeighbours()
        {
            List<Vec2> points = [new Vec2(-100, -100), new Vec2(0, 100), new Vec2(100, -100)];
            List<Triangle> triangles = [new Triangle(new Circle(), 0, 1, 2)];

            points.Add(new Vec2(0, 0));
            SplitTriangle(points, triangles, 0, points.Count - 1);

            Assert.Equal(3, triangles.Count);

            Triangle t0 = triangles[0];
            Assert.Equal(0, t0.indices[0]);
            Assert.Equal(1, t0.indices[1]);
            Assert.Equal(3, t0.indices[2]);

            Assert.Equal(NO_INDEX, t0.adjacent[0]);
            Assert.Equal(1, t0.adjacent[1]);
            Assert.Equal(2, t0.adjacent[2]);

            Triangle t1 = triangles[1];
            Assert.Equal(1, t1.indices[0]);
            Assert.Equal(2, t1.indices[1]);
            Assert.Equal(3, t1.indices[2]);

            Assert.Equal(NO_INDEX, t1.adjacent[0]);
            Assert.Equal(2, t1.adjacent[1]);
            Assert.Equal(0, t1.adjacent[2]);

            Triangle t2 = triangles[2];
            Assert.Equal(2, t2.indices[0]);
            Assert.Equal(0, t2.indices[1]);
            Assert.Equal(3, t2.indices[2]);

            Assert.Equal(NO_INDEX, t2.adjacent[0]);
            Assert.Equal(0, t2.adjacent[1]);
            Assert.Equal(1, t2.adjacent[2]);
        }

        static void DiagonalSwapTestCase(out List<Vec2> vertices, out List<Triangle> triangles)
        {
            /*
               5-------6------7
               |\  4  /\   5 /|
               | \   /  \   / |
               |  \ /  0 \ /  |
               | 7 3------4  8|
               |  / \  1 / \  |
               | /   \  /   \ |
               |/  2  \/   3 \|
               0-------1------2
             */

            List<Vec2> v = [
                new Vec2(-50, -50), new Vec2(0, -50), new Vec2(50, -50),
                new Vec2(-25, 0), new Vec2(25, 0),
                new Vec2(-50, 50), new Vec2(0, 50), new Vec2(50, 50)
                ];

            List<Triangle> t = [
                new Triangle(new Circle(), 3, 6, 4, 4, 5, 1),   // 0
                new Triangle(new Circle(), 1, 3, 4, 2, 0, 3),   // 1
                new Triangle(new Circle(), 1, 0, 3, NO_INDEX, 7, 1), // 2
                new Triangle(new Circle(), 4, 2, 1, 8, NO_INDEX, 1), // 3
                new Triangle(new Circle(), 5, 6, 3, NO_INDEX, 0, 7), // 4
                new Triangle(new Circle(), 7, 4, 6, 8, 0, NO_INDEX), // 5
                ];

            vertices = v;
            triangles = t;
        }

        [Fact]
        public void DiagonalSwapWorksCorrectly()
        {
            DiagonalSwapTestCase(out List<Vec2> vertices, out List<Triangle> triangles);

            /*
                  5-------6------7    >    5-------6------7
                  |\  4  /\   5 /|    >    |\  4  /|\   5 /|
                  | \   /  \   / |    >    | \   / | \   / |
                  |  \ /  0 \ /  |    >    |  \ /  |  \ /  |
                  | 7 3------4  8|    >    | 7 3 1 | 0 4  8|
                  |  / \  1 / \  |    >    |  / \  |  / \  |
                  | /   \  /   \ |    >    | /   \ | /   \ |
                  |/  2  \/   3 \|    >    |/  2  \|/   3 \|
                  0-------1------2    >    0-------1------2
             */

            FlipEdge(vertices, triangles, 0, triangles[0].IndexOf(4, 3));

            Triangle t0 = triangles[0];
            Assert.Equal(1, t0.indices[0]);
            Assert.Equal(6, t0.indices[1]);
            Assert.Equal(4, t0.indices[2]);

            Assert.Equal(1, t0.adjacent[0]);
            Assert.Equal(5, t0.adjacent[1]);
            Assert.Equal(3, t0.adjacent[2]);

            Triangle t1 = triangles[1];
            Assert.Equal(6, t1.indices[0]);
            Assert.Equal(1, t1.indices[1]);
            Assert.Equal(3, t1.indices[2]);

            Assert.Equal(0, t1.adjacent[0]);
            Assert.Equal(2, t1.adjacent[1]);
            Assert.Equal(4, t1.adjacent[2]);

            Triangle t5 = triangles[5];
            Assert.Equal(0, t5.adjacent[t5.IndexOf(4, 6)]);

            Triangle t3 = triangles[3];
            Assert.Equal(0, t3.adjacent[t3.IndexOf(1, 4)]);

            Triangle t4 = triangles[4];
            Assert.Equal(1, t4.adjacent[t4.IndexOf(6, 3)]);

            Triangle t2 = triangles[2];
            Assert.Equal(1, t2.adjacent[t2.IndexOf(3, 1)]);
        }

        [Fact]
        public void QuadIsConvex_ReturnsTrueWhenTrulyConvex()
        {
            Vec2 v0 = new Vec2(-1, 0);
            Vec2 v1 = new Vec2(0, +1);
            Vec2 v2 = new Vec2(+1, 0);
            Vec2 v3 = new Vec2(0, -1);

            Assert.True(ConvexQuad(v0, v1, v2, v3));
        }

        [Fact]
        public void QuadIsConvex_ReturnsFalseWhenToConvex()
        {
            Vec2 v0 = new Vec2(-1, 0);
            Vec2 v1 = new Vec2(0, +1);
            Vec2 v2 = new Vec2(-0.75, 0);
            Vec2 v3 = new Vec2(0, -1);

            Assert.False(ConvexQuad(v0, v1, v2, v3));
        }
    }
}
