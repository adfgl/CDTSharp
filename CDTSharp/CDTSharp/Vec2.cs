using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace CDTSharp
{
    public readonly struct Vec2 : IEquatable<Vec2>
    {
        public readonly double x, y, w;
        public readonly bool normalized;

        public static Vec2 Zero => new Vec2(0, 0, 1, true);
        public static Vec2 NaN => new Vec2(double.NaN, double.NaN, 1, true);
        public static Vec2 UnitX => new Vec2(1, 0, 1, true);
        public static Vec2 UnitY => new Vec2(0, 1, 1, true);

        public Vec2(double x, double y, double w = 1, bool normalized = false)
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
        public static double SquareLength(Vec2 v) => v.x * v.x + v.y * v.y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Length() => Math.Sqrt(x * x + y * y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec2 Normalize()
        {
            if (normalized) return this;
            double length = Length();
            if (length == 0) return Vec2.NaN;
            return new Vec2(x / length, y / length, 1, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Dot(Vec2 a, Vec2 b) => a.x * b.x + a.y * b.y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Cross(Vec2 a, Vec2 b) => a.x * b.y - a.y * b.x;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Cross(Vec2 a, Vec2 b, Vec2 c)
        {
            double abx = b.x - a.x, aby = b.y - a.y;
            double acx = c.x - a.x, acy = c.y - a.y;
            return abx * acy - aby * acx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.x + b.x, a.y + b.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.x - b.x, a.y - b.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2 operator -(Vec2 v) => new Vec2(-v.x, -v.y, v.w, v.normalized);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2 operator *(Vec2 v, double scalar) => new Vec2(v.x * scalar, v.y * scalar);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2 operator *(double scalar, Vec2 v) => new Vec2(v.x * scalar, v.y * scalar);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2 operator /(Vec2 v, double scalar) => new Vec2(v.x / scalar, v.y / scalar);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vec2 a, Vec2 b) => a.x == b.x && a.y == b.y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vec2 a, Vec2 b) => a.x != b.x || a.y != b.y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AlmostEqual(Vec2 other, double eps = 1e-8)
        {
            return Math.Abs(x - other.x) < eps && Math.Abs(y - other.y) < eps;
        }

        public bool Equals(Vec2 other)
        {
            return x == other.x && y == other.y;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is Vec2 && Equals((Vec2)obj);
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
        public static Vec2 MidPoint(Vec2 a, Vec2 b)
        {
            return new Vec2((a.x + b.x) * 0.5, (a.y + b.y) * 0.5);
        }
    }
}
