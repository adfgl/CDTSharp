namespace CDTSharp
{
    public static class ExpansionConstants
    {
        public static readonly double Epsilon;
        public static readonly double Splitter;

        public static readonly double Resulterrbound;
        public static readonly double CcwErrboundA;
        public static readonly double CcwErrboundB;
        public static readonly double CcwErrboundC;
        public static readonly double IncircleErrboundA;
        public static readonly double IncircleErrboundB;
        public static readonly double IncircleErrboundC;

        static ExpansionConstants()
        {
            double half = 0.5;
            double check, lastCheck;
            double eps = 1.0;
            double split = 1.0;
            bool everyOther = true;

            // Determine machine epsilon and splitter
            do
            {
                lastCheck = 1.0 + eps;
                eps *= half;
                if (everyOther)
                    split *= 2.0;
                everyOther = !everyOther;
                check = 1.0 + eps;
            } while (check != 1.0 && check != lastCheck);

            Epsilon = eps;
            Splitter = split + 1.0;

            Resulterrbound = (3.0 + 8.0 * Epsilon) * Epsilon;
            CcwErrboundA = (3.0 + 16.0 * Epsilon) * Epsilon;
            CcwErrboundB = (2.0 + 12.0 * Epsilon) * Epsilon;
            CcwErrboundC = (9.0 + 64.0 * Epsilon) * Epsilon * Epsilon;
            IncircleErrboundA = (10.0 + 96.0 * Epsilon) * Epsilon;
            IncircleErrboundB = (4.0 + 48.0 * Epsilon) * Epsilon;
            IncircleErrboundC = (44.0 + 576.0 * Epsilon) * Epsilon * Epsilon;
        }
    }

}
