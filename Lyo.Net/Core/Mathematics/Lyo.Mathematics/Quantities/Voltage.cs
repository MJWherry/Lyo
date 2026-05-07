using System.Diagnostics;

namespace Lyo.Mathematics.Quantities;

[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct Voltage
{

    public Voltage(double volts)

    {

        Volts = MathValueGuards.Finite(volts, nameof(volts));

    }


    public double Volts { get;  }
    public static Voltage FromVolts(double volts) => new(volts);

    public override string ToString() => $"{Volts:0.###} V";
}