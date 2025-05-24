using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace CDTSharp
{
    public readonly struct CDTVector : IEquatable<CDTVector>
    {
        public readonly double x, y, w;
        public readonly bool normalized;

        public static CDTVector Zero => new CDTVector(0, 0, 1, true);
        public static CDTVector NaN => new CDTVector(double.NaN, double.NaN, 1, true);
        public static CDTVector UnitX => new CDTVector(1, 0, 1, true);
        public static CDTVector UnitY => new CDTVector(0, 1, 1, true);

        public CDTVector(double x, double y, double w = 1, bool normalized = false)
        {
            this.x = x;
            this.y = y;
            this.w = w;
            this.normalized = normalized;
        }

        public void Deconstruct(out double x, out double y)
        {
            x = this.x;
            y = this.y;
        }

        public bool IsNaN() => double.IsNaN(x) || double.IsNaN(y);
        public bool IsZero() => x == 0 && y == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SquareLength(CDTVector v) => v.x * v.x + v.y * v.y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Length() => Math.Sqrt(x * x + y * y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CDTVector Normalize()
        {
            if (normalized) return this;
            double length = Length();
            if (length == 0) return CDTVector.NaN;
            return new CDTVector(x / length, y / length, 1, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Dot(CDTVector a, CDTVector b) => a.x * b.x + a.y * b.y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Cross(CDTVector a, CDTVector b) => a.x * b.y - a.y * b.x;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Cross(CDTVector a, CDTVector b, CDTVector c)
        {
            double abx = b.x - a.x, aby = b.y - a.y;
            double acx = c.x - a.x, acy = c.y - a.y;
            return abx * acy - aby * acx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CDTVector operator +(CDTVector a, CDTVector b) => new CDTVector(a.x + b.x, a.y + b.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CDTVector operator -(CDTVector a, CDTVector b) => new CDTVector(a.x - b.x, a.y - b.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CDTVector operator -(CDTVector v) => new CDTVector(-v.x, -v.y, v.w, v.normalized);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CDTVector operator *(CDTVector v, double scalar) => new CDTVector(v.x * scalar, v.y * scalar);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CDTVector operator *(double scalar, CDTVector v) => new CDTVector(v.x * scalar, v.y * scalar);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CDTVector operator /(CDTVector v, double scalar) => new CDTVector(v.x / scalar, v.y / scalar);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(CDTVector a, CDTVector b) => a.x == b.x && a.y == b.y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(CDTVector a, CDTVector b) => a.x != b.x || a.y != b.y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AlmostEqual(CDTVector other, double eps = 1e-8)
        {
            return Math.Abs(x - other.x) < eps && Math.Abs(y - other.y) < eps;
        }

        public bool Equals(CDTVector other)
        {
            return x == other.x && y == other.y;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is CDTVector && Equals((CDTVector)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(x, y);
        }

        public override string ToString()
        {
            CultureInfo culture = CultureInfo.InvariantCulture;
            return $"[{x.ToString(culture)} {y.ToString(culture)}]";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CDTVector MidPoint(CDTVector a, CDTVector b)
        {
            return new CDTVector((a.x + b.x) * 0.5, (a.y + b.y) * 0.5);
        }
    }
}
