using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CDTSharp
{
    public class RobustPredicates
    {
        private static readonly double epsilon;
        private static readonly double splitter;
        private static readonly double resulterrbound;
        private static readonly double ccwerrboundA, ccwerrboundB, ccwerrboundC;
        private static readonly double iccerrboundA, iccerrboundB, iccerrboundC;

        static RobustPredicates()
        {
            double half = 0.5;
            double check, lastcheck;
            double eps = 1.0;
            double split = 1.0;
            bool everyOther = true;

            do
            {
                lastcheck = 1.0 + eps;
                eps *= half;
                if (everyOther)
                    split *= 2.0;
                everyOther = !everyOther;
                check = 1.0 + eps;
            } while (check != 1.0 && check != lastcheck);

            epsilon = eps;
            splitter = split + 1.0;

            resulterrbound = (3.0 + 8.0 * epsilon) * epsilon;
            ccwerrboundA = (3.0 + 16.0 * epsilon) * epsilon;
            ccwerrboundB = (2.0 + 12.0 * epsilon) * epsilon;
            ccwerrboundC = (9.0 + 64.0 * epsilon) * epsilon * epsilon;
            iccerrboundA = (10.0 + 96.0 * epsilon) * epsilon;
            iccerrboundB = (4.0 + 48.0 * epsilon) * epsilon;
            iccerrboundC = (44.0 + 576.0 * epsilon) * epsilon * epsilon;
        }

        public double InCircle(Vec2 a, Vec2 b, Vec2 point)
        {
            // The circle with diameter (a, b) has its center at the midpoint of a and b.
            // A point p is inside this circle if the angle a-p-b is obtuse (i.e., dot product < 0).
            // Using the determinant form for robust predicate:
            //
            // | a.x  a.y  a.x²+a.y²  1 |
            // | b.x  b.y  b.x²+b.y²  1 |
            // | p.x  p.y  p.x²+p.y²  1 |
            //
            // Can be simplified into an adaptive determinant for 2 points.

            double ax = a.x - point.x;
            double ay = a.y - point.y;
            double bx = b.x - point.x;
            double by = b.y - point.y;

            double aLenSq = ax * ax + ay * ay;
            double bLenSq = bx * bx + by * by;

            double det = (ax * by - ay * bx) * (aLenSq - bLenSq);

            if (Math.Abs(det) > iccerrboundA * Math.Abs(ax * by - ay * bx))
                return det;

            return InCircleAdaptiveTwoPoints(a, b, point);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double InCircleAdaptiveTwoPoints(Vec2 a, Vec2 b, Vec2 point)
        {
            double ax = a.x - point.x;
            double ay = a.y - point.y;
            double bx = b.x - point.x;
            double by = b.y - point.y;

            double[] B = new double[5];

            double aLenSq = TwoProduct(ax, ax, out double axTail);
            double aySq = TwoProduct(ay, ay, out double ayTail);
            double bLenSq = TwoProduct(bx, bx, out double bxTail);
            double bySq = TwoProduct(by, by, out double byTail);

            double aNormSq = aLenSq + aySq;
            double bNormSq = bLenSq + bySq;
            double normDiff = aNormSq - bNormSq;

            double cross = ax * by - ay * bx;

            double result = cross * normDiff;

            if (Math.Abs(result) > iccerrboundB * Math.Abs(cross))
                return result;

            // Final fallback if needed: compute robustly
            double[] u = new double[4], v = new double[4];
            TailProductExpansion(ax, ax, ay, ay, u); // a^2
            TailProductExpansion(bx, bx, by, by, v); // b^2

            double[] diff = new double[8];
            int len = FastExpansionSumZeroElim(4, u, 4, NegateExpansion(v), diff);

            double[] crossExpansion = new double[8];
            len = ScaleExpansionZeroElim(len, diff, ax * by - ay * bx, crossExpansion);

            return Estimate(len, crossExpansion);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double[] NegateExpansion(double[] e)
        {
            var r = new double[e.Length];
            for (int i = 0; i < e.Length; i++)
                r[i] = -e[i];
            return r;
        }

        /// <summary>
        /// Adaptive InCircle test. Returns a positive value if point d lies inside the circle
        /// passing through a, b, and c (in that order), negative if outside, and 0 if on it.
        /// </summary>
        public double InCircle(Vec2 a, Vec2 b, Vec2 c, Vec2 d)
        {
            double adx = a.x - d.x;
            double ady = a.y - d.y;
            double bdx = b.x - d.x;
            double bdy = b.y - d.y;
            double cdx = c.x - d.x;
            double cdy = c.y - d.y;

            double abdet = adx * bdy - bdx * ady;
            double bcdet = bdx * cdy - cdx * bdy;
            double cadet = cdx * ady - adx * cdy;

            double alift = adx * adx + ady * ady;
            double blift = bdx * bdx + bdy * bdy;
            double clift = cdx * cdx + cdy * cdy;

            double det = alift * bcdet + blift * cadet + clift * abdet;

            double permanent = (Math.Abs(bcdet) * alift + Math.Abs(cadet) * blift + Math.Abs(abdet) * clift);
            double errbound = iccerrboundA * permanent;

            if (Math.Abs(det) > errbound) return det;

            return InCircleAdaptive(a, b, c, d, permanent);
        }

        private double InCircleAdaptive(Vec2 a, Vec2 b, Vec2 c, Vec2 d, double permanent)
        {
            double adx = a.x - d.x;
            double bdx = b.x - d.x;
            double cdx = c.x - d.x;
            double ady = a.y - d.y;
            double bdy = b.y - d.y;
            double cdy = c.y - d.y;

            double[] axby = new double[4], bxay = new double[4];
            double[] bxcy = new double[4], cxby = new double[4];
            double[] cxay = new double[4], axcy = new double[4];
            double[] temp8a = new double[8], temp8b = new double[8];
            double[] temp16 = new double[16];
            double[] ab = new double[16], bc = new double[16], ca = new double[16];

            // TwoProduct computes product and tail term
            double adet = TwoProduct(adx, bdy, out double at1);
            double bdet = TwoProduct(bdx, ady, out double bt1);
            int ablen = FastExpansionSumZeroElim(2, new double[] { at1, adet }, 2, new double[] { -bt1, -bdet }, ab);

            double cdet = TwoProduct(bdx, cdy, out double ct1);
            double ddet = TwoProduct(cdx, bdy, out double dt1);
            int bclen = FastExpansionSumZeroElim(2, new double[] { ct1, cdet }, 2, new double[] { -dt1, -ddet }, bc);

            double edet = TwoProduct(cdx, ady, out double et1);
            double fdet = TwoProduct(adx, cdy, out double ft1);
            int calen = FastExpansionSumZeroElim(2, new double[] { et1, edet }, 2, new double[] { -ft1, -fdet }, ca);

            double alift = TwoProduct(adx, adx, out double alifttail);
            alift += ady * ady;

            double blift = TwoProduct(bdx, bdx, out double blifttail);
            blift += bdy * bdy;

            double clift = TwoProduct(cdx, cdx, out double clifttail);
            clift += cdy * cdy;

            int temp16len = ScaleExpansionZeroElim(ablen, ab, clift, temp16);
            int temp8alen = ScaleExpansionZeroElim(bclen, bc, alift, temp8a);
            int temp8blen = ScaleExpansionZeroElim(calen, ca, blift, temp8b);

            int temp16alen = FastExpansionSumZeroElim(temp8alen, temp8a, temp8blen, temp8b, ab);
            int finlen = FastExpansionSumZeroElim(temp16len, temp16, temp16alen, ab, temp16);

            return temp16[finlen - 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ScaleExpansionZeroElim(int elen, double[] e, double b, double[] h)
        {
            double Q, sum, product1, product0;
            double hh;
            double bvirt;
            double avirt, bround, around;
            double c;
            int eindex, hindex = 0;
            double enow;

            enow = e[0];
            Q = TwoProduct(enow, b, out product0);
            if (product0 != 0.0) h[hindex++] = product0;

            for (eindex = 1; eindex < elen; eindex++)
            {
                enow = e[eindex];
                double product = TwoProduct(enow, b, out double producttail);
                sum = Q + producttail;
                bvirt = sum - Q;
                avirt = sum - bvirt;
                bround = producttail - bvirt;
                around = Q - avirt;
                hh = around + bround;
                if (hh != 0.0) h[hindex++] = hh;
                Q = product + sum;
            }

            if (Q != 0.0 || hindex == 0)
                h[hindex++] = Q;

            return hindex;
        }

        public double Orient(Vec2 a, Vec2 b, Vec2 c, bool noExact = false)
        {
            double detleft = (a.x - c.x) * (b.y - c.y);
            double detright = (a.y - c.y) * (b.x - c.x);
            double det = detleft - detright;

            if (noExact)
                return det;

            double detsum, errbound;
            if (detleft > 0.0)
            {
                if (detright <= 0.0)
                {
                    return det;
                }
                else
                {
                    detsum = detleft + detright;
                }
            }
            else if (detleft < 0.0)
            {
                if (detright >= 0.0)
                {
                    return det;
                }
                else
                {
                    detsum = -detleft - detright;
                }
            }
            else
            {
                return det;
            }

            errbound = ccwerrboundA * detsum;
            if ((det >= errbound) || (-det >= errbound))
            {
                return det;
            }

            return OrientAdaptive(a, b, c, detsum);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double OrientAdaptive(Vec2 pa, Vec2 pb, Vec2 pc, double detsum)
        {
            double acx = pa.x - pc.x, bcx = pb.x - pc.x;
            double acy = pa.y - pc.y, bcy = pb.y - pc.y;

            // Tail terms (rounding errors from high precision multiplication)
            double[] B = new double[5], u = new double[5];
            double[] C1 = new double[8], C2 = new double[12], D = new double[16];

            // Compute high-precision determinant difference and its tail
            double detleft = TwoProduct(acx, bcy, out double detlefttail);
            double detright = TwoProduct(acy, bcx, out double detrighttail);

            double det = FastExpansionSum3(
                detlefttail - detrighttail,
                detleft,
                -detright,
                B);

            if (Math.Abs(det) >= ccwerrboundB * detsum)
                return det;

            // Compute tail terms for coordinates
            double acxtail = Tail(pa.x, pc.x, acx);
            double bcxtail = Tail(pb.x, pc.x, bcx);
            double acytail = Tail(pa.y, pc.y, acy);
            double bcytail = Tail(pb.y, pc.y, bcy);

            if ((acxtail == 0.0 && acytail == 0.0) && (bcxtail == 0.0 && bcytail == 0.0))
                return det;

            // Higher precision error bound check
            double errbound = ccwerrboundC * detsum + resulterrbound * Math.Abs(det);
            det += (acx * bcytail + bcy * acxtail) - (acy * bcxtail + bcx * acytail);

            if (Math.Abs(det) >= errbound)
                return det;

            // Final round of tail term products and summations
            int len;
            len = TailProductExpansion(acxtail, bcy, acytail, bcx, u);
            int c1len = FastExpansionSumZeroElim(4, B, len, u, C1);

            len = TailProductExpansion(acx, bcytail, acy, bcxtail, u);
            int c2len = FastExpansionSumZeroElim(c1len, C1, len, u, C2);

            len = TailProductExpansion(acxtail, bcytail, acytail, bcxtail, u);
            int dlen = FastExpansionSumZeroElim(c2len, C2, len, u, D);
            return D[dlen - 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double TwoProduct(double a, double b, out double tail)
        {
            double product = a * b;
            Split(a, out double ahi, out double alo);
            Split(b, out double bhi, out double blo);
            double err1 = product - (ahi * bhi);
            double err2 = err1 - (alo * bhi);
            double err3 = err2 - (ahi * blo);
            tail = (alo * blo) - err3;
            return product;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Split(double a, out double hi, out double lo)
        {
            double c = splitter * a;
            double abig = c - a;
            hi = c - abig;
            lo = a - hi;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double Tail(double a, double b, double ab)
        {
            double bvirt = a - ab;
            double avirt = ab + bvirt;
            double bround = bvirt - b;
            double around = a - avirt;
            return around + bround;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double FastExpansionSum3(double e0, double e1, double e2, double[] result)
        {
            result[0] = e0;
            result[1] = e1;
            result[2] = e2;
            double sum = e0 + e1 + e2;
            result[3] = sum;
            return Estimate(4, result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int TailProductExpansion(double a1, double b1, double a2, double b2, double[] result)
        {
            double p1 = TwoProduct(a1, b1, out double t1);
            double p2 = TwoProduct(a2, b2, out double t2);
            double i = t1 - t2;
            double j = p1 + i;
            double k = j - p2;
            result[0] = i - (j - p1);
            result[1] = k - (j - p2);
            double sum = j + k;
            result[2] = sum - j;
            result[3] = sum;
            return 4;
        }


        private int FastExpansionSumZeroElim(int elen, double[] e, int flen, double[] f, double[] h)
        {
            double Q;
            double Qnew;
            double hh;
            double bvirt;
            double avirt, bround, around;
            int eindex, findex, hindex;
            double enow, fnow;

            enow = e[0];
            fnow = f[0];
            eindex = findex = 0;
            if ((fnow > enow) == (fnow > -enow))
            {
                Q = enow;
                enow = e[++eindex];
            }
            else
            {
                Q = fnow;
                fnow = f[++findex];
            }
            hindex = 0;
            if ((eindex < elen) && (findex < flen))
            {
                if ((fnow > enow) == (fnow > -enow))
                {
                    Qnew = (double)(enow + Q); bvirt = Qnew - enow; hh = Q - bvirt;
                    enow = e[++eindex];
                }
                else
                {
                    Qnew = (double)(fnow + Q); bvirt = Qnew - fnow; hh = Q - bvirt;
                    fnow = f[++findex];
                }
                Q = Qnew;
                if (hh != 0.0)
                {
                    h[hindex++] = hh;
                }
                while ((eindex < elen) && (findex < flen))
                {
                    if ((fnow > enow) == (fnow > -enow))
                    {
                        Qnew = (double)(Q + enow);
                        bvirt = (double)(Qnew - Q);
                        avirt = Qnew - bvirt;
                        bround = enow - bvirt;
                        around = Q - avirt;
                        hh = around + bround;

                        enow = e[++eindex];
                    }
                    else
                    {
                        Qnew = (double)(Q + fnow);
                        bvirt = (double)(Qnew - Q);
                        avirt = Qnew - bvirt;
                        bround = fnow - bvirt;
                        around = Q - avirt;
                        hh = around + bround;

                        fnow = f[++findex];
                    }
                    Q = Qnew;
                    if (hh != 0.0)
                    {
                        h[hindex++] = hh;
                    }
                }
            }
            while (eindex < elen)
            {
                Qnew = (double)(Q + enow);
                bvirt = (double)(Qnew - Q);
                avirt = Qnew - bvirt;
                bround = enow - bvirt;
                around = Q - avirt;
                hh = around + bround;

                enow = e[++eindex];
                Q = Qnew;
                if (hh != 0.0)
                {
                    h[hindex++] = hh;
                }
            }
            while (findex < flen)
            {
                Qnew = (double)(Q + fnow);
                bvirt = (double)(Qnew - Q);
                avirt = Qnew - bvirt;
                bround = fnow - bvirt;
                around = Q - avirt;
                hh = around + bround;

                fnow = f[++findex];
                Q = Qnew;
                if (hh != 0.0)
                {
                    h[hindex++] = hh;
                }
            }
            if ((Q != 0.0) || (hindex == 0))
            {
                h[hindex++] = Q;
            }
            return hindex;
        }

        /// <summary>
        /// Produce a one-word estimate of an expansion's value. 
        /// </summary>
        /// <param name="elen"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private double Estimate(int elen, double[] e)
        {
            double Q;
            int eindex;

            Q = e[0];
            for (eindex = 1; eindex < elen; eindex++)
            {
                Q += e[eindex];
            }
            return Q;
        }
    }
}
