namespace CDTSharp
{
    public class CDTPolygon
    {
        public CDTPolygon(List<CDTVector> contour)
        {
            Contour = contour;
        }

        public List<CDTVector> Contour { get; set; }
        public List<List<CDTVector>>? Holes { get; set; }
        public List<CDTVector>? Points { get; set; }
        public List<(CDTVector, CDTVector)>? Constraints { get; set; }
    }
}
