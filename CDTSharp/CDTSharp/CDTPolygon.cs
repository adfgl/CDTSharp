namespace CDTSharp
{
    public class CDTPolygon
    {
        public CDTPolygon(List<Vec2> contour)
        {
            Contour = contour;
        }

        public List<Vec2> Contour { get; set; }
        public List<List<Vec2>>? Holes { get; set; }
        public List<Vec2>? Points { get; set; }
        public List<(Vec2, Vec2)>? Constraints { get; set; }
    }
}
