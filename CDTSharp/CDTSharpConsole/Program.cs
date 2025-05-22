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
                KeepSuper = false,
                MaxArea = 25,
                MinAngle = 33.3,
                Polygons = new List<CDTPolygon>()
                {
                    //new CDTPolygon([new (42.356, -18.238), new (0.33001188621764754, 22.71505548932538), new (-33.351, -20.41), new (16.079492752868575, 43.245628761923896), new (77.67121257066408, 55.90146160119696)])
                    //{
                    //    //Points = [new Vec2(-11, 10), new Vec2(20, 15)],

                    //    //Holes = new List<List<Vec2>>()
                    //    //{
                    //    //   StandardShapes.Star(0, 0, 25, 18, 6)
                    //    //},

                    //    //Constraints = [(new Vec2(-60, 0), new Vec2(60, 0))]
                    //},

                    new CDTPolygon( StandardShapes.Star(0, 0, 60, 30, 6))
                    {
                    }
                }
            };

            Stopwatch sw = new Stopwatch();
            sw.Start();
            var cdt = new CDT();

                cdt.Triangulate(input);
            try
            {
            }
            catch (Exception e)
            {
                cdt.FinalizeMesh();
                Console.WriteLine(cdt.ToSvg());
            }

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
            Console.WriteLine(cdt.ToSvg(fill: true));
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


    }
}
