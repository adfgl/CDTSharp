using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDTSharp
{
    public readonly struct Segment : IEquatable<Segment>
    {
        public readonly int a, b;
        public readonly Circle circle;

        public Segment(int a, int b, Circle circle)
        {
            this.circle = circle;   
            if (a < b)
            {
                this.a = a;
                this.b = b;
            }
            else
            {
                this.a = b;
                this.b = a;
            }
        }

        public void Deconstruct(out int a, out int b)
        {
            a = this.a;
            b = this.b;
        }

        public bool Equals(Segment other) => a == other.a && b == other.b;

        public override bool Equals(object? obj) => obj is Segment other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(a, b);

        public override string ToString()
        {
            return $"{a} {b}";
        }
    }
}
