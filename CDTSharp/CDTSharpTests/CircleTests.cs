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

            CDTCircle actual = new CDTCircle(a, b, c);

            const double epsilon = 1e-3;
            Assert.Equal(144.018, actual.cx, epsilon);
            Assert.Equal(137.893, actual.cy, epsilon);
            Assert.Equal(65.094, Math.Sqrt(actual.radiusSquared), epsilon);
        }
    }
}
