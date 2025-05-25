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
                Refine = false,
                KeepConvex = true,
                KeepSuper = false,
                MaxArea = 25,
                Polygons = new List<CDTPolygon>()
                {
                    new CDTPolygon([new (95.6401, -150.9838), new (-21.7466, -123.1639), new (157.3868, 19.3286), new (91.5689, 44.4344), new (-181.881, 66.8261), new (124.1386, 177.4274), new (272.7379, 121.7875)]),
                }
            };


            var cdt = new CDT();
                cdt.Triangulate(input);
            Console.WriteLine(cdt.ToSvg(fill: true, drawConstraints: true, drawCircles: false));
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
