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
        public void CircleBuildsCorrectlyFromThreeSparslyPositionedPoints()
        {
            Vec2 a = new Vec2(79.144, 143.238);
            Vec2 b = new Vec2(170.708, 197.264);
            Vec2 c = new Vec2(199.125, 103.244);


            var circle = new Circle(a, b, c);
            Assert.False(Double.IsNaN(circle.x));
            Assert.False(Double.IsNaN(circle.y));
            Assert.False(Double.IsNaN(circle.radiusSquared));

            const double epsilon = 1e-3;
            Assert.Equal(144.018, circle.x, epsilon);
            Assert.Equal(137.893, circle.y, epsilon);
            Assert.Equal(65.094, Math.Sqrt(circle.radiusSquared), epsilon);
        }
    }
}
