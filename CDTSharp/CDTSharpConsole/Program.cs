using CDTSharp;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace CDTSharpConsole
{
    // https://www.svgviewer.dev/

   
    internal class Program
    {

        static void Main(string[] args)
        {
            List<Vec3> cubePoints = GenerateCubePoints(-1, 1);
            Convex3 cubeHull = new Convex3(cubePoints);

            Console.WriteLine(cubeHull.AsObj());

            //CDTInput input = new CDTInput()
            //{
            //    Refine = true,
            //    KeepConvex = true,
            //    KeepSuper = false,
            //    MaxArea = 25,
            //    Polygons = new List<CDTPolygon>()
            //    {
            //        //new CDTPolygon(StandardShapes.Circle(0, 0, 45, 16))
            //        new CDTPolygon([new Vec2(-152.171, -51.51),
            //    new Vec2(-134.948, 159.616),
            //    new Vec2(91.088, 166.264),
            //    new Vec2(146.488, -43.152)]),
            //    }
            //};


            //var cdt = new CDT();
            //    cdt.Triangulate(input);
            //Console.WriteLine(cdt.ToSvg(fill: false, drawConstraints: true, drawCircles: false));
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

        public static List<Vec3> GenerateCubePoints(double min = -1, double max = 1)
        {
            return new List<Vec3>
    {
        new Vec3(min, min, min),
        new Vec3(max, min, min),
        new Vec3(max, max, min),
        new Vec3(min, max, min),
        new Vec3(min, min, max),
        new Vec3(max, min, max),
        new Vec3(max, max, max),
        new Vec3(min, max, max)
    };
        }

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

    }
}
