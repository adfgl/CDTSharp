using CDTSharp;
using System.Diagnostics;

namespace CDTSharpConsole
{
    // https://www.svgviewer.dev/

    internal class Program
    {
        static void Main(string[] args)
        {
            CDTInput input = new CDTInput()
            {
                Refine = true,
                KeepConvex = false,
                MaxArea = 22,
                MinAngle = 33,
                Polygons = new List<CDTPolygon>()
                {
                    new CDTPolygon(Square(0, 0, 100))
                    {
                        Holes = new List<List<Vec2>>()
                        {
                            //Square(23, 20, 11),
                        }
                    }
                }
            };

            Stopwatch sw = new Stopwatch();
            sw.Start();
            var cdt = new CDT().Triangulate(input);
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds + " ms");
            Console.WriteLine(cdt.Triangles.Count);

            foreach (var item in cdt.Triangles)
            {
                Console.WriteLine(item);
            }
            
            Console.WriteLine();



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
