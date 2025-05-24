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
                MaxArea = 215,
                MinAngle = 33.3,
                Polygons = new List<CDTPolygon>()
                {
                    new CDTPolygon(StandardShapes.Circle(0, 0, 100, 36))
                    //StandardShapes.Pyramid(0, 0, 46, 4),
                     //StandardShapes.Pyramid(33, 33, 46, 4),
                     //   StandardShapes.Pyramid(-33, 33, 46, 4),
                     //                  StandardShapes.Pyramid(-33, -33, 46, 4),
                     //                              StandardShapes.Pyramid(33, -33, 46, 4),
                }
            };


            Stopwatch sw = new Stopwatch();
            sw.Start();
            var cdt = new CDT();

            try
            {
                cdt.Triangulate(input);
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
                    CDTVector a = cdt.Vertices[(i + 2) % 4];
                    CDTVector b = cdt.Vertices[i];
                    CDTVector c = cdt.Vertices[(i + 1) % 4];

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
            Console.WriteLine(cdt.ToSvg(fill: true, drawConstraints: false, drawCircles: false));
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
