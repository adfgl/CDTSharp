
namespace CDTSharp
{
    public enum EConstraint
    {
        Contour, Hole, User
    }

    public readonly struct Constraint
    {
        public readonly Vec2 a, b;
        public readonly EConstraint type;

        public Constraint(Vec2 a, Vec2 b, EConstraint type)
        {
            this.a = a;
            this.b = b;
            this.type = type;
        }

        public void Deconstruct(out Vec2 a, out Vec2 b)
        {
            a = this.a;
            b = this.b;
        }

        public (Constraint, Constraint) Split(Vec2 v)
        {
            return (new Constraint(a, v, type), new Constraint(v, b, type));
        }

        public bool OnNode(Vec2 v, double eps)
        {
            return a.AlmostEqual(v, eps) || b.AlmostEqual(v, eps);
        }

        public bool OnEdge(Vec2 v, double eps)
        {
            return GeometryHelper.OnSegment(a, b, v, eps);
        }
    }
}
