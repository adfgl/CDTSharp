using System;
using System.Collections.Generic;
using System.Linq;
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

        private double OrientAdaptive(Vec2 pa, Vec2 pb, Vec2 pc, double detsum)
        {
            double acx, acy, bcx, bcy;
            double acxtail, acytail, bcxtail, bcytail;
            double detleft, detright;
            double detlefttail, detrighttail;
            double det, errbound;
            // Edited to work around index out of range exceptions (changed array length from 4 to 5).
            // See unsafe indexing in FastExpansionSumZeroElim.
            double[] B = new double[5], u = new double[5];
            double[] C1 = new double[8], C2 = new double[12], D = new double[16];
            double B3;
            int C1length, C2length, Dlength;

            double u3;
            double s1, t1;
            double s0, t0;

            double bvirt;
            double avirt, bround, around;
            double c;
            double abig;
            double ahi, alo, bhi, blo;
            double err1, err2, err3;
            double _i, _j;
            double _0;

            acx = (double)(pa.x - pc.x);
            bcx = (double)(pb.x - pc.x);
            acy = (double)(pa.y - pc.y);
            bcy = (double)(pb.y - pc.y);

            detleft = (double)(acx * bcy); c = (double)(splitter * acx); abig = (double)(c - acx); ahi = c - abig; alo = acx - ahi; c = (double)(splitter * bcy); abig = (double)(c - bcy); bhi = c - abig; blo = bcy - bhi; err1 = detleft - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); detlefttail = (alo * blo) - err3;
            detright = (double)(acy * bcx); c = (double)(splitter * acy); abig = (double)(c - acy); ahi = c - abig; alo = acy - ahi; c = (double)(splitter * bcx); abig = (double)(c - bcx); bhi = c - abig; blo = bcx - bhi; err1 = detright - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); detrighttail = (alo * blo) - err3;

            _i = (double)(detlefttail - detrighttail); bvirt = (double)(detlefttail - _i); avirt = _i + bvirt; bround = bvirt - detrighttail; around = detlefttail - avirt; B[0] = around + bround; _j = (double)(detleft + _i); bvirt = (double)(_j - detleft); avirt = _j - bvirt; bround = _i - bvirt; around = detleft - avirt; _0 = around + bround; _i = (double)(_0 - detright); bvirt = (double)(_0 - _i); avirt = _i + bvirt; bround = bvirt - detright; around = _0 - avirt; B[1] = around + bround; B3 = (double)(_j + _i); bvirt = (double)(B3 - _j); avirt = B3 - bvirt; bround = _i - bvirt; around = _j - avirt; B[2] = around + bround;

            B[3] = B3;

            det = Estimate(4, B);
            errbound = ccwerrboundB * detsum;
            if ((det >= errbound) || (-det >= errbound))
            {
                return det;
            }

            bvirt = (double)(pa.x - acx); avirt = acx + bvirt; bround = bvirt - pc.x; around = pa.x - avirt; acxtail = around + bround;
            bvirt = (double)(pb.x - bcx); avirt = bcx + bvirt; bround = bvirt - pc.x; around = pb.x - avirt; bcxtail = around + bround;
            bvirt = (double)(pa.y - acy); avirt = acy + bvirt; bround = bvirt - pc.y; around = pa.y - avirt; acytail = around + bround;
            bvirt = (double)(pb.y - bcy); avirt = bcy + bvirt; bround = bvirt - pc.y; around = pb.y - avirt; bcytail = around + bround;

            if ((acxtail == 0.0) && (acytail == 0.0)
                && (bcxtail == 0.0) && (bcytail == 0.0))
            {
                return det;
            }

            errbound = ccwerrboundC * detsum + resulterrbound * ((det) >= 0.0 ? (det) : -(det));
            det += (acx * bcytail + bcy * acxtail)
                 - (acy * bcxtail + bcx * acytail);
            if ((det >= errbound) || (-det >= errbound))
            {
                return det;
            }

            s1 = (double)(acxtail * bcy); c = (double)(splitter * acxtail); abig = (double)(c - acxtail); ahi = c - abig; alo = acxtail - ahi; c = (double)(splitter * bcy); abig = (double)(c - bcy); bhi = c - abig; blo = bcy - bhi; err1 = s1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); s0 = (alo * blo) - err3;
            t1 = (double)(acytail * bcx); c = (double)(splitter * acytail); abig = (double)(c - acytail); ahi = c - abig; alo = acytail - ahi; c = (double)(splitter * bcx); abig = (double)(c - bcx); bhi = c - abig; blo = bcx - bhi; err1 = t1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); t0 = (alo * blo) - err3;
            _i = (double)(s0 - t0); bvirt = (double)(s0 - _i); avirt = _i + bvirt; bround = bvirt - t0; around = s0 - avirt; u[0] = around + bround; _j = (double)(s1 + _i); bvirt = (double)(_j - s1); avirt = _j - bvirt; bround = _i - bvirt; around = s1 - avirt; _0 = around + bround; _i = (double)(_0 - t1); bvirt = (double)(_0 - _i); avirt = _i + bvirt; bround = bvirt - t1; around = _0 - avirt; u[1] = around + bround; u3 = (double)(_j + _i); bvirt = (double)(u3 - _j); avirt = u3 - bvirt; bround = _i - bvirt; around = _j - avirt; u[2] = around + bround;
            u[3] = u3;
            C1length = FastExpansionSumZeroElim(4, B, 4, u, C1);

            s1 = (double)(acx * bcytail); c = (double)(splitter * acx); abig = (double)(c - acx); ahi = c - abig; alo = acx - ahi; c = (double)(splitter * bcytail); abig = (double)(c - bcytail); bhi = c - abig; blo = bcytail - bhi; err1 = s1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); s0 = (alo * blo) - err3;
            t1 = (double)(acy * bcxtail); c = (double)(splitter * acy); abig = (double)(c - acy); ahi = c - abig; alo = acy - ahi; c = (double)(splitter * bcxtail); abig = (double)(c - bcxtail); bhi = c - abig; blo = bcxtail - bhi; err1 = t1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); t0 = (alo * blo) - err3;
            _i = (double)(s0 - t0); bvirt = (double)(s0 - _i); avirt = _i + bvirt; bround = bvirt - t0; around = s0 - avirt; u[0] = around + bround; _j = (double)(s1 + _i); bvirt = (double)(_j - s1); avirt = _j - bvirt; bround = _i - bvirt; around = s1 - avirt; _0 = around + bround; _i = (double)(_0 - t1); bvirt = (double)(_0 - _i); avirt = _i + bvirt; bround = bvirt - t1; around = _0 - avirt; u[1] = around + bround; u3 = (double)(_j + _i); bvirt = (double)(u3 - _j); avirt = u3 - bvirt; bround = _i - bvirt; around = _j - avirt; u[2] = around + bround;
            u[3] = u3;
            C2length = FastExpansionSumZeroElim(C1length, C1, 4, u, C2);

            s1 = (double)(acxtail * bcytail); c = (double)(splitter * acxtail); abig = (double)(c - acxtail); ahi = c - abig; alo = acxtail - ahi; c = (double)(splitter * bcytail); abig = (double)(c - bcytail); bhi = c - abig; blo = bcytail - bhi; err1 = s1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); s0 = (alo * blo) - err3;
            t1 = (double)(acytail * bcxtail); c = (double)(splitter * acytail); abig = (double)(c - acytail); ahi = c - abig; alo = acytail - ahi; c = (double)(splitter * bcxtail); abig = (double)(c - bcxtail); bhi = c - abig; blo = bcxtail - bhi; err1 = t1 - (ahi * bhi); err2 = err1 - (alo * bhi); err3 = err2 - (ahi * blo); t0 = (alo * blo) - err3;
            _i = (double)(s0 - t0); bvirt = (double)(s0 - _i); avirt = _i + bvirt; bround = bvirt - t0; around = s0 - avirt; u[0] = around + bround; _j = (double)(s1 + _i); bvirt = (double)(_j - s1); avirt = _j - bvirt; bround = _i - bvirt; around = s1 - avirt; _0 = around + bround; _i = (double)(_0 - t1); bvirt = (double)(_0 - _i); avirt = _i + bvirt; bround = bvirt - t1; around = _0 - avirt; u[1] = around + bround; u3 = (double)(_j + _i); bvirt = (double)(u3 - _j); avirt = u3 - bvirt; bround = _i - bvirt; around = _j - avirt; u[2] = around + bround;
            u[3] = u3;
            Dlength = FastExpansionSumZeroElim(C2length, C2, 4, u, D);

            return (D[Dlength - 1]);
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
