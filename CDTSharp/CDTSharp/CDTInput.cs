namespace CDTSharp
{
    public class CDTInput
    {
        public double MaxArea { get; set; } = double.MaxValue;
        public double MinAngle { get; set; } = double.MinValue;
        public bool Refine { get; set; } = false;
        public bool KeepConvex { get; set; } = false;

        public List<CDTPolygon> Polygons { get; set; } = new List<CDTPolygon>();    
    }
}
