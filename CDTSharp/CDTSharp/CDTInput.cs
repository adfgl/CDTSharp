namespace CDTSharp
{
    public class CDTInput
    {
        public double MaxArea { get; set; } = double.MaxValue;
        public bool Refine { get; set; } = false;
        public bool KeepConvex { get; set; } = false;
        public bool KeepSuper { get; set; } = true;

        public List<CDTPolygon> Polygons { get; set; } = new List<CDTPolygon>();    
    }
}
