using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDTSharp
{
    public enum EConstraint
    {
        Contour, Hole, User
    }

    public readonly struct Constraint
    {
        public readonly CDTVector a, b;
        public readonly EConstraint type;

        public Constraint(CDTVector a, CDTVector b, EConstraint type)
        {
            this.a = a;
            this.b = b;
            this.type = type;
        }

        public void Deconstruct(out CDTVector a, out CDTVector b)
        {
            a = this.a;
            b = this.b;
        }

        public (Constraint, Constraint) Split(CDTVector v)
        {
            return (new Constraint(a, v, type), new Constraint(v, b, type));
        }

        public bool OnNode(CDTVector v, double eps)
        {
            return a.AlmostEqual(v, eps) || b.AlmostEqual(v, eps);
        }

        public bool OnEdge(CDTVector v, double eps)
        {
            return CDT.OnSegment(a, b, v, eps);
        }
    }
}
