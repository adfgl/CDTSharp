using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDTSharp
{
    public readonly struct Vec3 : IEquatable<Vec3>
    {
        public readonly double x, y, z, w;
        public readonly bool normalized;

        public static Vec3 Zero => new Vec3(0, 0, 0, 1, true);
        public static Vec3 NaN => new Vec3(double.NaN, double.NaN, double.NaN, 1, true);
        public static Vec3 UnitX => new Vec3(1, 0, 0, 1, true);
        public static Vec3 UnitY => new Vec3(0, 1, 0, 1, true);
        public static Vec3 UnitZ => new Vec3(0, 0, 1, 1, true);

        public Vec3(double x, double y, double z, double w = 1, bool normalized = false)
        {
            this.x = x;
            this.y = y;
            this.w = w;
            this.z = z;
            this.normalized = normalized;
        }

        public void Deconstruct(out double x, out double y, out double z)
        {
            x = this.x;
            y = this.y;
            z = this.z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNaN() => double.IsNaN(x) || double.IsNaN(y) || double.IsNaN(z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsZero() => x == 0 && y == 0 && z == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SquareLength(Vec3 v) => v.x * v.x + v.y * v.y + v.z * v.z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Length() => Math.Sqrt(x * x + y * y + z * z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec3 Normalize()
        {
            if (normalized) return this;
            double length = Length();
            if (length == 0) return Vec3.NaN;
            return new Vec3(x / length, y / length, z / length, 1, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Dot(Vec3 a, Vec3 b)
        {
            return a.x * b.x + a.y * b.y + a.z * b.z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec3 Cross(Vec3 a, Vec3 b)
        {
            return new Vec3(
                a.y * b.z - a.z * b.y,
                a.z * b.x - a.x * b.z,
                a.x * b.y - a.y * b.x
            );
        }

        public static Vec3 Round(Vec3 v, int precision)
        {
            return new Vec3(Math.Round(v.x, precision), Math.Round(v.y, precision), Math.Round(v.z, precision));
        }

        public double this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return x;
                    case 1: return y;
                    case 2: return z;
                    case 3: return w;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(index));
                }
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.x + b.x, a.y + b.y, a.z + b.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.x - b.x, a.y - b.y, a.z - b.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec3 operator -(Vec3 v) => new Vec3(-v.x, -v.y, -v.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec3 operator *(Vec3 v, double scalar) => new Vec3(v.x * scalar, v.y * scalar, v.z * scalar);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec3 operator *(double scalar, Vec3 v) => new Vec3(v.x * scalar, v.y * scalar, v.z * scalar);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec3 operator *(Vec3 a, Vec3 b) => new Vec3(a.x * b.x, a.y * b.y, a.z * b.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec3 operator /(Vec3 v, double scalar) => new Vec3(v.x / scalar, v.y / scalar, v.z / scalar);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec3 operator /(Vec3 a, Vec3 b) => new Vec3(a.x / b.x, a.y / b.y, a.z / b.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vec3 a, Vec3 b) => a.x == b.x && a.y == b.y && a.z == b.z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vec3 a, Vec3 b) => a.x != b.x || a.y != b.y || a.z != b.z;

        public bool Equals(Vec3 other)
        {
            return x == other.x && y == other.y && z == other.z;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is Vec3 && Equals((Vec3)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(x, y, z);
        }

        public override string ToString()
        {
            CultureInfo culture = CultureInfo.InvariantCulture;
            return $"[{Math.Round(x, 3).ToString(culture)}, {Math.Round(y, 3).ToString(culture)}, {Math.Round(z, 3).ToString(culture)}]";
        }

        public static Vec3 Between(Vec3 a, Vec3 b)
        {
            return new Vec3((a.x + b.x) / 2, (a.y + b.y) / 2, (a.z + b.z) / 2);
        }

        public static double Distance(Vec3 a, Vec3 b)
        {
            double dx = b.x - a.x;
            double dy = b.y - a.y;
            double dz = b.z - a.z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public static double SquareDistance(Vec3 a, Vec3 b)
        {
            double dx = b.x - a.x;
            double dy = b.y - a.y;
            double dz = b.z - a.z;
            return dx * dx + dy * dy + dz * dz;
        }

        public static bool AlmostEqual(Vec3 a, Vec3 b, double tolerance = 0.001)
        {
            return Math.Abs(a.x - b.x) < tolerance &&
                   Math.Abs(a.y - b.y) < tolerance &&
                   Math.Abs(a.z - b.z) < tolerance;
        }
    }
}
