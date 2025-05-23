namespace CDTSharp
{
    public static class StandardShapes
    {
        public static List<CDTVector> Square(double cx, double cy, double r)
        {
            return new List<CDTVector>
            {
                new CDTVector(cx - r, cy - r),
                new CDTVector(cx + r, cy - r),
                new CDTVector(cx + r, cy + r),
                new CDTVector(cx - r, cy + r)
            };
        }

        public static List<CDTVector> Circle(double cx, double cy, double r, int steps)
        {
            List<CDTVector> result = new List<CDTVector>(steps);
            for (int i = 0; i < steps; i++)
            {
                double angle = 2 * MathF.PI * i / steps;
                result.Add(new CDTVector(
                    cx + r * Math.Cos(angle),
                    cy + r * Math.Sin(angle)
                ));
            }
            return result;
        }

        public static List<CDTVector> Star(double cx, double cy, double outerRadius, double innerRadius, int points)
        {
            int totalSteps = points * 2;
            List<CDTVector> result = new List<CDTVector>(totalSteps);
            for (int i = 0; i < totalSteps; i++)
            {
                double angle = 2 * Math.PI * i / totalSteps;
                double r = (i % 2 == 0) ? outerRadius : innerRadius;

                result.Add(new CDTVector(
                    cx + r * Math.Cos(angle),
                    cy + r * Math.Sin(angle)
                ));
            }
            return result;
        }
    }
}
