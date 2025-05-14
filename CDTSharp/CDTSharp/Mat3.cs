using System.Runtime.CompilerServices;

namespace CDTSharp
{
    public readonly struct Mat3
    {
        public readonly double m11, m12, m13;
        public readonly double m21, m22, m23;
        public readonly double m31, m32, m33;

        public Mat3(double m11, double m12, double m13, double m21, double m22, double m23, double m31, double m32, double m33)
        {
            this.m11 = m11; this.m12 = m12; this.m13 = m13;
            this.m21 = m21; this.m22 = m22; this.m23 = m23;
            this.m31 = m31; this.m32 = m32; this.m33 = m33;
        }

        public static Mat3 Identity => new Mat3(1, 0, 0, 0, 1, 0, 0, 0, 1);

        public bool IsIdentity() => m11 == 1 && m12 == 0 && m13 == 0 &&
                                    m21 == 0 && m22 == 1 && m23 == 0 &&
                                    m31 == 0 && m32 == 0 && m33 == 1;

        public double Get(int row, int col)
        {
            return this[row, col];
        }

        public double this[int row, int col]
        {
            get
            {
                return row switch
                {
                    0 => col switch
                    {
                        0 => m11,
                        1 => m12,
                        2 => m13,
                        _ => throw new IndexOutOfRangeException($"Invalid column index {col}. Valid column indices are 0, 1, and 2."),
                    },
                    1 => col switch
                    {
                        0 => m21,
                        1 => m22,
                        2 => m23,
                        _ => throw new IndexOutOfRangeException($"Invalid column index {col}. Valid column indices are 0, 1, and 2."),
                    },
                    2 => col switch
                    {
                        0 => m31,
                        1 => m32,
                        2 => m33,
                        _ => throw new IndexOutOfRangeException($"Invalid column index {col}. Valid column indices are 0, 1, and 2."),
                    },
                    _ => throw new IndexOutOfRangeException($"Invalid row index {row}. Valid row indices are 0, 1, and 2."),
                };
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mat3 RotateAt(double x, double y, double rad)
        {
            return Translation(x, y) * Rotation(rad) * Translation(-x, -y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mat3 Translation(double x, double y)
        {
            return new(
                1, 0, x,
                0, 1, y,
                0, 0, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mat3 Rotation(double rad)
        {
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);
            return new(
                cos, -sin, 0,
                sin, cos, 0,
                0, 0, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mat3 Scale(double x, double y)
        {
            if (x < 0 || y < 0)
                throw new ArgumentException("Scaling factors must be non-negative.");

            return new(
                x, 0, 0,
                0, y, 0,
                0, 0, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Determinant()
        {
            return
                m11 * (m22 * m33 - m23 * m32) -
                m12 * (m21 * m33 - m23 * m31) +
                m13 * (m21 * m32 - m22 * m31);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Inverse(out Mat3 inverse)
        {
            double det = Determinant();
            if (det == 0)
            {
                inverse = default;
                return false;
            }

            det = 1.0 / det;
            inverse = new Mat3(
                det * (m22 * m33 - m23 * m32),
                det * (m13 * m32 - m12 * m33),
                det * (m12 * m23 - m13 * m22),

                det * (m23 * m31 - m21 * m33),
                det * (m11 * m33 - m13 * m31),
                det * (m13 * m21 - m11 * m23),

                det * (m21 * m32 - m22 * m31),
                det * (m12 * m31 - m11 * m32),
                det * (m11 * m22 - m12 * m21));
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Mat3 Transpose() => new Mat3(
            m11, m21, m31,
            m12, m22, m32,
            m13, m23, m33);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mat3 Multiply(Mat3 a, Mat3 b) => new Mat3(
            a.m11 * b.m11 + a.m12 * b.m21 + a.m13 * b.m31,
            a.m11 * b.m12 + a.m12 * b.m22 + a.m13 * b.m32,
            a.m11 * b.m13 + a.m12 * b.m23 + a.m13 * b.m33,

            a.m21 * b.m11 + a.m22 * b.m21 + a.m23 * b.m31,
            a.m21 * b.m12 + a.m22 * b.m22 + a.m23 * b.m32,
            a.m21 * b.m13 + a.m22 * b.m23 + a.m23 * b.m33,

            a.m31 * b.m11 + a.m32 * b.m21 + a.m33 * b.m31,
            a.m31 * b.m12 + a.m32 * b.m22 + a.m33 * b.m32,
            a.m31 * b.m13 + a.m32 * b.m23 + a.m33 * b.m33);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2 Multiply(Mat3 m, Vec2 v) => new Vec2(
            x: v.x * m.m11 + v.y * m.m12 + v.w * m.m13,
            y: v.x * m.m21 + v.y * m.m22 + v.w * m.m23,
            w: v.x * m.m31 + v.y * m.m32 + v.w * m.m33);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mat3 Multiply(Mat3 m, double scalar) => new Mat3(
            m.m11 * scalar, m.m12 * scalar, m.m13 * scalar,
            m.m21 * scalar, m.m22 * scalar, m.m23 * scalar,
            m.m31 * scalar, m.m32 * scalar, m.m33 * scalar);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mat3 Divide(Mat3 m, double scalar) => new Mat3(
            m.m11 / scalar, m.m12 / scalar, m.m13 / scalar,
            m.m21 / scalar, m.m22 / scalar, m.m23 / scalar,
            m.m31 / scalar, m.m32 / scalar, m.m33 / scalar);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mat3 operator *(Mat3 a, Mat3 b) => Multiply(a, b);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2 operator *(Mat3 m, Vec2 v) => Multiply(m, v);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec2 operator *(Vec2 v, Mat3 m) => Multiply(m, v);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mat3 operator *(Mat3 m, double scalar) => Multiply(m, scalar);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mat3 operator *(double scalar, Mat3 m) => Multiply(m, scalar);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mat3 operator /(Mat3 m, double scalar) => Divide(m, scalar);

        public string ToString(int precision)
        {
            string format = $"F{precision}";

            int colWidth = Math.Max(
                Math.Max(m11.ToString(format).Length, m12.ToString(format).Length),
                Math.Max(m13.ToString(format).Length, m21.ToString(format).Length)
            );

            string paddedFormat = $"{{0,{colWidth}:{format}}}";

            return string.Format(
                "{0} {1} {2}\n{3} {4} {5}\n{6} {7} {8}",
                string.Format(paddedFormat, m11), string.Format(paddedFormat, m12), string.Format(paddedFormat, m13),
                string.Format(paddedFormat, m21), string.Format(paddedFormat, m22), string.Format(paddedFormat, m23),
                string.Format(paddedFormat, m31), string.Format(paddedFormat, m32), string.Format(paddedFormat, m33)
            );
        }

        public override string ToString()
        {
            return ToString(2);
        }
    }
}
