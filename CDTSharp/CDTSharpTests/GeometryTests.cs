using CDTSharp;
using Xunit.Sdk;

namespace CDTSharpTests
{
    using static CDTGeometry;

    public class GeometryTests
    {
        [Fact]
        public void CircleBuildsCorrectlyFromThreeSparslyPositionedPoints()
        {
            Vec2 a = new Vec2(79.144, 143.238);
            Vec2 b = new Vec2(170.708, 197.264);
            Vec2 c = new Vec2(199.125, 103.244);

            Assert.True(CircleFromThreePoints(a, b, c, out double cx, out double cy, out double rSqr));

            const double epsilon = 1e-3;
            Assert.Equal(144.018, cx, epsilon);
            Assert.Equal(137.893, cy, epsilon);
            Assert.Equal(65.094, Math.Sqrt(rSqr), epsilon);
        }

        [Fact]
        public void TriangleSplitCenterSplitWorksCorrectlyWithNoNeighbours()
        {
            List<Vec2> points = [new Vec2(-100, -100), new Vec2(0, 100), new Vec2(100, -100)];

            CircleFromThreePoints(points[0], points[1], points[2], out double cx, out double cy, out double rSqr);
            List<Triangle> triangles = [new Triangle(cx, cy, rSqr, 0, 1, 2)];

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

    }
}
