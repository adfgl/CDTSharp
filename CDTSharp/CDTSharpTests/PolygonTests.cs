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
    }
}
