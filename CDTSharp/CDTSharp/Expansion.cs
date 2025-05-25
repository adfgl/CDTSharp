using System.Runtime.CompilerServices;

namespace CDTSharp
{
    public readonly struct Expansion : IComparable<Expansion>, IEquatable<Expansion>
    {
        private readonly double[] _components;

        public Expansion(params double[] components)
        {
            _components = Compress(components);
        }

        public static implicit operator Expansion(double value) => new Expansion(value);

        public static Expansion operator +(Expansion a, Expansion b) => Add(a, b);
        public static Expansion operator +(Expansion a, double b) => Add(a, new Expansion(b));
        public static Expansion operator -(Expansion a) => Multiply(a, -1);
        public static Expansion operator -(Expansion a, Expansion b) => Add(a, -b);
        public static Expansion operator *(Expansion a, double b) => Multiply(a, b);
        public static Expansion operator /(Expansion a, double b) => Divide(a, b);

        public static bool operator <(Expansion a, Expansion b) => a.CompareTo(b) < 0;
        public static bool operator >(Expansion a, Expansion b) => a.CompareTo(b) > 0;
        public static bool operator <=(Expansion a, Expansion b) => a.CompareTo(b) <= 0;
        public static bool operator >=(Expansion a, Expansion b) => a.CompareTo(b) >= 0;
        public static bool operator ==(Expansion a, Expansion b) => a.Equals(b);
        public static bool operator !=(Expansion a, Expansion b) => !a.Equals(b);

        public double Approximate => _components.Sum();
        public override string ToString() => Approximate.ToString("R");

        public int CompareTo(Expansion other) => Approximate.CompareTo(other.Approximate);
        public bool Equals(Expansion other) => CompareTo(other) == 0;
        public override bool Equals(object obj) => obj is Expansion other && Equals(other);
        public override int GetHashCode() => Approximate.GetHashCode();

        // ----------------- Core Arithmetic Logic ------------------

        public int Sign()
        {
            // Walk components from most to least significant
            for (int i = _components.Length - 1; i >= 0; i--)
            {
                if (_components[i] > 0) return 1;
                if (_components[i] < 0) return -1;
            }
            return 0;
        }

        /// <summary>
        /// Adds two expansions and returns a new high-precision result.
        /// </summary>
        public static Expansion Add(Expansion left, Expansion right)
        {
            double[] result = new double[left._components.Length + right._components.Length];
            int length = SumWithElimination(left._components, right._components, result);
            return new Expansion(result.Take(length).ToArray());
        }


        /// <summary>
        /// Multiplies an expansion by a scalar.
        /// </summary>
        public static Expansion Multiply(Expansion expansion, double scalar)
        {
            List<double> result = new();
            foreach (double component in expansion._components)
            {
                TwoProduct(component, scalar, out double product, out double productError);
                result = FastExpansionSum(result.ToArray(), new[] { productError, product }).ToList();
            }
            return new Expansion(result.ToArray());
        }

        /// <summary>
        /// Divides an expansion by a scalar using a 2-term high-precision division.
        /// </summary>
        public static Expansion Divide(Expansion expansion, double divisor)
        {
            List<double> result = new();
            foreach (double component in expansion._components)
            {
                double mainQuotient = component / divisor;
                TwoProduct(mainQuotient, divisor, out double product, out double productError);
                double residual = component - product;
                double correction = (residual - productError) / divisor;
                result = FastExpansionSum(result.ToArray(), new[] { correction, mainQuotient }).ToList();
            }
            return new Expansion(result.ToArray());
        }

        // ----------------- Low-Level Building Blocks ------------------

        /// <summary>
        /// Computes the sum and rounding error of two doubles.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TwoSum(double a, double b, out double sum, out double error)
        {
            sum = a + b;
            double bVirtual = sum - a;
            double aVirtual = sum - bVirtual;
            double bRound = b - bVirtual;
            double aRound = a - aVirtual;
            error = aRound + bRound;
        }

        /// <summary>
        /// Computes the product and error of two doubles using Dekker's algorithm.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TwoProduct(double a, double b, out double product, out double error)
        {
            product = a * b;
            Split(a, out double aHigh, out double aLow);
            Split(b, out double bHigh, out double bLow);

            double err1 = product - (aHigh * bHigh);
            double err2 = err1 - (aLow * bHigh);
            double err3 = err2 - (aHigh * bLow);
            error = (aLow * bLow) - err3;
        }

        /// <summary>
        /// Splits a double into high and low components.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Split(double value, out double high, out double low)
        {
            double c = ExpansionConstants.Splitter * value;
            double bigPart = c - value;
            high = c - bigPart;
            low = value - high;
        }

        /// <summary>
        /// Compresses an expansion by eliminating near-zero terms.
        /// </summary>
        public static double[] Compress(double[] input)
        {
            List<double> result = new();
            double accumulator = input[0];
            for (int i = 1; i < input.Length; i++)
            {
                TwoSum(accumulator, input[i], out double sum, out double error);
                if (error != 0.0) result.Add(error);
                accumulator = sum;
            }
            result.Add(accumulator);
            return result.ToArray();
        }

        /// <summary>
        /// Performs a fast expansion sum without eliminating zero.
        /// </summary>
        public static double[] FastExpansionSum(double[] a, double[] b)
        {
            List<double> result = new();
            int i = 0, j = 0;
            double current, carry;

            if (a.Length == 0) return b;
            if (b.Length == 0) return a;

            if (Math.Abs(a[0]) < Math.Abs(b[0]))
            {
                current = a[0]; i++;
            }
            else
            {
                current = b[0]; j++;
            }

            while (i < a.Length && j < b.Length)
            {
                double next = (Math.Abs(a[i]) < Math.Abs(b[j])) ? a[i++] : b[j++];
                TwoSum(current, next, out current, out carry);
                if (carry != 0.0) result.Add(carry);
            }

            while (i < a.Length)
            {
                TwoSum(current, a[i++], out current, out carry);
                if (carry != 0.0) result.Add(carry);
            }

            while (j < b.Length)
            {
                TwoSum(current, b[j++], out current, out carry);
                if (carry != 0.0) result.Add(carry);
            }

            result.Add(current);
            return result.ToArray();
        }

        /// <summary>
        /// Fast expansion sum with zero elimination.
        /// Returns the number of valid entries stored in h.
        /// </summary>
        public static int SumWithElimination(double[] a, double[] b, double[] output)
        {
            int aIndex = 0, bIndex = 0, outputIndex = 0;
            bool nonzero = false;

            if (a.Length == 0 && b.Length == 0)
            {
                output[0] = 0.0;
                return 1;
            }

            double current, newSum, error;
            double aNow = a[aIndex], bNow = b[bIndex];

            if ((bNow > aNow) == (bNow > -aNow))
            {
                current = aNow;
                aIndex++;
                if (aIndex < a.Length) aNow = a[aIndex];
            }
            else
            {
                current = bNow;
                bIndex++;
                if (bIndex < b.Length) bNow = b[bIndex];
            }

            while (aIndex < a.Length && bIndex < b.Length)
            {
                if ((bNow > aNow) == (bNow > -aNow))
                {
                    TwoSum(current, aNow, out newSum, out error);
                    aNow = a[++aIndex];
                }
                else
                {
                    TwoSum(current, bNow, out newSum, out error);
                    bNow = b[++bIndex];
                }

                if (error != 0.0)
                {
                    output[outputIndex++] = error;
                    nonzero = true;
                }

                current = newSum;
            }

            while (aIndex < a.Length)
            {
                TwoSum(current, a[aIndex++], out newSum, out error);
                if (error != 0.0)
                {
                    output[outputIndex++] = error;
                    nonzero = true;
                }
                current = newSum;
            }

            while (bIndex < b.Length)
            {
                TwoSum(current, b[bIndex++], out newSum, out error);
                if (error != 0.0)
                {
                    output[outputIndex++] = error;
                    nonzero = true;
                }
                current = newSum;
            }

            if (current != 0.0 || !nonzero)
                output[outputIndex++] = current;

            return outputIndex;
        }

    }
}