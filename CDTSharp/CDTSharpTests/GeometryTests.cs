using CDTSharp;

namespace CDTSharpTests
{
    public class CircleTests
    {
        [Fact]
        public void CircleBuildsCorrectlyFromThreeSparslyPositionedPoints()
        {
            Vec2 a = new Vec2(79.144, 143.238);
            Vec2 b = new Vec2(170.708, 197.264);
            Vec2 c = new Vec2(199.125, 103.244);

            Assert.True(CDTGeometry.CircleFromThreePoints(a.x, a.y, b.x, b.y, c.x, c.y, out double cx, out double cy, out double rSqr));

            const double epsilon = 1e-3;
            Assert.Equal(144.018, cx, epsilon);
            Assert.Equal(137.893, cy, epsilon);
            Assert.Equal(65.094, Math.Sqrt(rSqr), epsilon);
        }
    }
}
