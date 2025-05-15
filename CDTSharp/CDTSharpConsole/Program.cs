using CDTSharp;

namespace CDTSharpConsole
{
    // https://www.svgviewer.dev/

    internal class Program
    {
        static void Main(string[] args)
        {
            CDTInput input = new CDTInput()
            {
                Refine = false,
                KeepConvex = false,
                Polygons = new List<CDTPolygon>()
                {
                    new CDTPolygon(Square(0, 0, 10))
                }
            };

            var cdt = new CDT().Triangulate(input);

            Console.WriteLine(cdt.ToSvg());
        }

        static List<Vec2> Square(double cx, double cy, double r)
        {
            return new List<Vec2>
            {
                new Vec2(cx - r, cy - r),
                new Vec2(cx + r, cy - r),
                new Vec2(cx + r, cy + r),
                new Vec2(cx - r, cy + r)
            };
        }

        static List<Vec2> Circle(double cx, double cy, double r, int steps)
        {
            var result = new List<Vec2>();
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
    }
}
