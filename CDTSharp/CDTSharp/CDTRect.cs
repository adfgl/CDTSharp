
namespace CDTSharp
{
    public readonly struct CDTRect
    {
        public readonly double minX, minY, maxX, maxY;

        public CDTRect(double minX, double minY, double maxX, double maxY)
        {
            this.minX = minX;
            this.minY = minY;
            this.maxX = maxX;
            this.maxY = maxY;
        }

        public bool Contains(float x, float y)
        {
            return 
                x >= minX && x <= maxX && 
                y >= minY && y <= maxY;
        }

        public bool Contains(CDTRect other)
        {
            return
                minX <= other.minX && minY <= other.minY &&
                maxX >= other.maxX && maxY >= other.maxY;
        }

        public bool Intersects(CDTRect other)
        {
            return 
                minX <= other.maxX && minY <= other.maxY &&
                maxX >= other.minX && maxY >= other.minY;
        }
    }
}
