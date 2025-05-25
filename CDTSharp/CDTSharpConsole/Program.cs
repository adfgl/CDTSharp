using CDTSharp;
using System.Diagnostics;

namespace CDTSharpConsole
{
    // https://www.svgviewer.dev/

    internal class Program
    {
        public class Cone
        {
            public double X { get; }
            public double Y { get; }
            public double Height { get; }
            public double Radius { get; }
            public double Slope { get; }
            public CDTPolygon Geometry { get; set; }

            public Cone(double x, double y, double height, double slope = 1.5)
            {
                X = x;
                Y = y;
                Height = height;
                Slope = slope;
                Radius = slope * height;
                Geometry = StandardShapes.Pyramid(x, y, Radius);
            }

            public bool HeightAt(double height, double x, double y, out double depth)
            {
                double r = height * Slope;
                double dx = Math.Abs(x - X);
                double dy = Math.Abs(y - Y);
                if (dx >= r || dy >= r)
                {
                    depth = double.NaN;
                    return false;
                }

                /* 
                    +
                    |\ 
                    | \ 
                    |  \ 
                    |   +
                    (H) |\
                    |  (D)\
                    |   |  \
                    +---+(d)+
                        (r)

                    d / r = D / H
                */
                depth = Math.Min(r - dx, r - dy) / r * height;
                return true;
            }

            public bool HeightAt(double x, double y, out double depth)
            {
                return HeightAt(Height, x, y, out depth);
            }

        }

        static void Main(string[] args)
        {
            CDTInput input = new CDTInput()
            {
                Refine = true,
                KeepConvex = false,
                KeepSuper = false,
                MaxArea = 25,
                Polygons = new List<CDTPolygon>()
                {
                    new CDTPolygon([new (204.79475173652122, 54.536188290213886), new (-84.00071058315325, 44.2220646359398), new (-218.87771221596824, -124.77088446870488), new (-160.16654679933112, 150.5368776876881)]),
                }
            };


            var cdt = new CDT();
                cdt.Triangulate(input);
            Console.WriteLine(cdt.ToSvg(fill: false, drawConstraints: true, drawCircles: false));
            //try
            //{
            //    cdt.Summary();
            //    Console.WriteLine(cdt.ToSvg(fill: false, drawConstraints: true, drawCircles: false));
            //}
            //catch (Exception)
            //{
            //    //cdt.FinalizeMesh();
            //    Console.WriteLine(cdt.ToSvg());
            //}

        }


    }
}
