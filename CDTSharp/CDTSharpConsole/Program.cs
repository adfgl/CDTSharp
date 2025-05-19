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
                MaxArea = 55,
                MinAngle = 33.3,
                Polygons = new List<CDTPolygon>()
                {
                    new CDTPolygon(Circle(0, 0, 100, 25))
                    {
                        //Points = [new Vec2(-11, 10), new Vec2(20, 15)],

                        Holes = new List<List<Vec2>>()
                        {
                            Circle(0, 0, 65, 16),
                        },

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

            double minAng = double.MaxValue;
            double maxAng = double.MinValue;
            double avgAng = 0;
            foreach (var item in cdt.Triangles)
            {
                double area = cdt.Area(item);
                avgArea += area;

                if (!cdt.Clockwise(item))
                {
                    throw new Exception();
                }

                for (int i = 0; i < 3; i++)
                {
                    Vec2 a = cdt.Vertices[(i + 2) % 4];
                    Vec2 b = cdt.Vertices[i];
                    Vec2 c = cdt.Vertices[(i + 1) % 4];

                    double ang = CDT.Angle(a, b, c) * 180 / Math.PI;
                    if (minAng > ang) minAng = ang;
                    if (maxAng <  ang) maxAng = ang;
                    avgAng += ang;
                }

                if (minArea > area) minArea = area;
                if (maxArea < area) maxArea = area;
            }
            avgArea /= cdt.Triangles.Count;
            avgAng /= 3 * cdt.Triangles.Count;
            Console.WriteLine(cdt.ToSvg());
            Console.WriteLine();
            Console.WriteLine("count: " + cdt.Triangles.Count);
            Console.WriteLine("Area min: " + minArea);
            Console.WriteLine("Area max: " + maxArea);
            Console.WriteLine("Area avg: " + avgArea);
            Console.WriteLine();
            Console.WriteLine("Ang min: " + minAng);
            Console.WriteLine("Ang max: " + maxAng);
            Console.WriteLine("Ang avg: " + avgAng);



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
