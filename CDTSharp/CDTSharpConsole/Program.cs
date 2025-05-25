using CDTSharp;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace CDTSharpConsole
{
    // https://www.svgviewer.dev/

    public static class PreciseMath
    {
        public static double[] TwoDivide(double a, double b)
        {
            double q0 = a / b;

            double product = TwoProduct(q0, b, out double productError);

            double delta = a - product;
            double q1 = (delta - productError) / b;

            return new[] { q0, q1 };
        }

        public static double TwoSum(double a, double b, out double err)
        {
            double sum = a + b;
            double bVirtual = sum - a;
            double aVirtual = sum - bVirtual;
            double bRoundoff = b - bVirtual;
            double aRoundoff = a - aVirtual;
            err = aRoundoff + bRoundoff;
            return sum;
        }

        public static double TwoProduct(double a, double b, out double err)
        {
            double product = a * b;

            Split(a, out double aHigh, out double aLow);
            Split(b, out double bHigh, out double bLow);

            double err1 = product - (aHigh * bHigh);
            double err2 = err1 - (aLow * bHigh);
            double err3 = err2 - (aHigh * bLow);
            err = (aLow * bLow) - err3;
            return product;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]  
        public static void Split(double a, out double high, out double low)
        {
            const double splitter = (1 << 27) + 1; // = 2^27 + 1 = 134217729

            double c = splitter * a;
            double abig = c - a;
            high = c - abig;
            low = a - high;
        }
    }

    internal class Program
    {

        static void Main(string[] args)
        {
            double a = 1e16;
            double b = 1.0;

            double c = a + b;
            Console.WriteLine(c);

            double ab = PreciseMath.TwoSum(a, b, out double err);
            double abc = PreciseMath.TwoSum(ab, 32, out double err1);
            Console.WriteLine(abc);
            Console.WriteLine(err + err1);


            Console.WriteLine();
            Console.WriteLine(err);

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
