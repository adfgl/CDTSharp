using CDTSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDTSharpTests
{
    public class CircleTests
    {
        [Fact]
        public void CircleBuildsCorrectly_FromThreeSparslyPositionedPoints()
        {
            Vec2 a = new Vec2(79.144, 143.238);
            Vec2 b = new Vec2(170.708, 197.264);
            Vec2 c = new Vec2(199.125, 103.244);

            Circle circle = new Circle(a, b, c);

            const double epsilon = 1e-3;
            Assert.Equal(144.018, circle.x, epsilon);
            Assert.Equal(137.893, circle.y, epsilon);
            Assert.Equal(65.094, Math.Sqrt(circle.radiusSquared), epsilon);
        }

        [Fact]
        public void CircleBuildsCorrectly_FromTwoUniquePoints()
        {
            Vec2 a = new Vec2(79.144, 143.238);
            Vec2 b = new Vec2(199.125, 103.244);

            Circle circle = new Circle(a, b);

            const double epsilon = 1e-3;
            Assert.Equal(139.135, circle.x, epsilon);
            Assert.Equal(123.241, circle.y, epsilon);
            Assert.Equal(63.236, Math.Sqrt(circle.radiusSquared), epsilon);
        }

        [Fact]
        public void CircleContains_PointStrictlyInside()
        {
            Vec2 a = new Vec2(-50, 0);
            Vec2 b = new Vec2(+50, 0);

            Circle circle = new Circle(a, b);

            Assert.True(circle.Contains(25, 25));
        }

        [Fact]
        public void CircleContains_PointStrictlyOutside()
        {
            Vec2 a = new Vec2(-50, 0);
            Vec2 b = new Vec2(+50, 0);

            Circle circle = new Circle(a, b);

            Assert.False(circle.Contains(125, 25));
        }

        [Fact]
        public void CircleContains_PointOnCircumference()
        {
            Vec2 a = new Vec2(-50, 0);
            Vec2 b = new Vec2(+50, 0);

            Circle circle = new Circle(a, b);

            Assert.False(circle.Contains(a.x, a.y));
        }
    }
}
