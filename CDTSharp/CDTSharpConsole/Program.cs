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
                MaxArea = 250,
                MinAngle = 33.3,
                Polygons = new List<CDTPolygon>()
                {
                    new CDTPolygon(Circle(0, 0, 100, 4))
                    {
                        //Points = [new Vec2(-11, 10), new Vec2(20, 15)],

                        //Holes = new List<List<Vec2>>()
                        //{
                        //    Circle(0, 0, 15, 16),
                        //},

                        //Constraints = [(new Vec2(-60, 0), new Vec2(60, 0))]
                    }
                }
            };

            Stopwatch sw = new Stopwatch();
            sw.Start();
            var cdt = new CDT().Triangulate(input);
            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds + " ms");


            double minArea = double.MaxValue;
            double maxArea = double.MinValue;
            double avgArea = 0;
            foreach (var item in cdt.Triangles)
            {
                double area = cdt.Area(item);
                avgArea += area;

                if (!cdt.Clockwise(item))
                {
                    throw new Exception();
                }

                if (minArea > area) minArea = area;
                if (maxArea < area) maxArea = area;
            }
            avgArea /= cdt.Triangles.Count;

            Console.WriteLine(cdt.ToSvg());
            Console.WriteLine();
            Console.WriteLine("count: " + cdt.Triangles.Count);
            Console.WriteLine("min: " + minArea);
            Console.WriteLine("max: " + maxArea);
            Console.WriteLine("avg: " + avgArea);



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
