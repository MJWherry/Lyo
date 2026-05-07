using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Force
{

    public Force(double newtons)

    {

        Newtons = MathValueGuards.Finite(newtons, nameof(newtons));

    }


    public double Newtons { get;  }
    public static Force FromNewtons(double newtons) => new(newtons);

    public override string ToString() => $"{Newtons:0.###} N";
}