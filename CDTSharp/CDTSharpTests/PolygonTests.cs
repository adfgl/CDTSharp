using CDTSharp;

namespace CDTSharpTests
{
    public class PolygonTests
    {
        static Polygon Rombus()
        {
            return new Polygon(0,[new Vec2(-50, 0), new Vec2(0, 50), new Vec2(50, 0), new Vec2(0, -50)]);
        }

        [Fact]
        public void Contains_ReturnsTrueWhenPointIsActuallyInside()
        {
            Polygon poly = Rombus();
            var (x, y) = poly.rect.Center();
            Assert.True(Rombus().Contains(x, y));
        }

        [Fact]
        public void Contains_ReturnsFalseWhenPointIsOutside()
        {
            Polygon poly = Rombus();
            Assert.False(poly.Contains(poly.rect.minX - 100, 0));
        }

        [Fact]
        public void Contains_ReturnsTrueWhenPointIsNode()
        {
            Polygon poly = Rombus();
            var (x, y) = poly.vertices[0];
            Assert.True(poly.Contains(x, y));
        }

        [Fact]
        public void Contains_ReturnsTrueWhenPointIsOnEdge()
        {
            Polygon poly = Rombus();
            var (x, y) = Vec2.MidPoint(poly.vertices[0], poly.vertices[1]);
            Assert.True(poly.Contains(x, y));
        }
    }
}
