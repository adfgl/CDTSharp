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
            double acxtail, acytail, bcxtail, bcytail;

            // Error expansions
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
            acxtail = Tail(pa.x, pc.x, acx);
            bcxtail = Tail(pb.x, pc.x, bcx);
            acytail = Tail(pa.y, pc.y, acy);
            bcytail = Tail(pb.y, pc.y, bcy);

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
