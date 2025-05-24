namespace CDTSharp
{
    public static class StandardShapes
    {
        public static CDTPolygon Pyramid(double cx, double cy, double radius, int details = 4)
        {
            List<Vec2> square = details == 4 ? Square(cx, cy, radius) : Circle(cx, cy, radius, details);

            Vec2 center = Vec2.Zero;
            foreach (var c in square)
            {
                center += c;
            }
            center /= square.Count;

            List<(Vec2, Vec2)> constraints = new List<(Vec2, Vec2)>();
            foreach (Vec2 c in square)
            {
                constraints.Add((center, c));
            }

            return new CDTPolygon(square)
            {
                Constraints = constraints,
            };
        }

        public static List<Vec2> Square(double cx, double cy, double r)
        {
            return new List<Vec2>
            {
                new Vec2(cx - r, cy - r),
                new Vec2(cx + r, cy - r),
                new Vec2(cx + r, cy + r),
                new Vec2(cx - r, cy + r)
            };
        }

        public static List<Vec2> Circle(double cx, double cy, double r, int steps)
        {
            List<Vec2> result = new List<Vec2>(steps);
            for (int i = 0; i < steps; i++)
            {
                double angle = 2 * MathF.PI * i / steps;
                result.Add(new Vec2(
                    cx + r * Math.Cos(angle),
                    cy + r * Math.Sin(angle)
                ));
            }
            return result;
        }

        public static List<Vec2> Star(double cx, double cy, double outerRadius, double innerRadius, int points)
        {
            int totalSteps = points * 2;
            List<Vec2> result = new List<Vec2>(totalSteps);
            for (int i = 0; i < totalSteps; i++)
            {
                double angle = 2 * Math.PI * i / totalSteps;
                double r = (i % 2 == 0) ? outerRadius : innerRadius;

                result.Add(new Vec2(
                    cx + r * Math.Cos(angle),
                    cy + r * Math.Sin(angle)
                ));
            }
            return result;
        }
    }
}
