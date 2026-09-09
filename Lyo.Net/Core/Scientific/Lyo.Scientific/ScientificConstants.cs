namespace Lyo.Scientific;

/// <summary>Physical and mathematical constants exposed as compile-time <see cref="double" /> literals.</summary>
/// <remarks>Uses SI definitions where they apply. Also consumed by <c>Lyo.Mathematics.Functions.PhysicsFunctions</c> for gravitation and ideal-gas work.</remarks>
public static class ScientificConstants
{
    /// <summary>Newtonian gravitational constant G from CODATA 2018, in m³/(kg·s²).</summary>
    public const double GravitationalConstant = 6.67430e-11d;

    /// <summary>Standard gravity acceleration in m/s², per ISO 80000-3.</summary>
    public const double StandardGravity = 9.80665d;

    /// <summary>Vacuum speed of light in m/s; exact SI value.</summary>
    public const double SpeedOfLight = 299_792_458d;

    /// <summary>Molar gas constant R in J/(mol·K).</summary>
    public const double GasConstant = 8.314462618d;

    /// <summary>Vacuum permittivity ε₀ in F/m.</summary>
    public const double VacuumPermittivity = 8.8541878128e-12d;

    /// <summary>Planck constant h in J·s; exact SI value.</summary>
    public const double PlanckConstant = 6.62607015e-34d;

    /// <summary>Avogadro constant N_A in mol⁻¹; exact SI value.</summary>
    public const double AvogadroConstant = 6.02214076e23d;

    /// <summary>Boltzmann constant k in J/K; exact SI value.</summary>
    public const double BoltzmannConstant = 1.380649e-23d;

    /// <summary>Elementary charge e in C; exact SI value.</summary>
    public const double ElementaryCharge = 1.602176634e-19d;

    /// <summary>Circumference of a circle divided by its diameter.</summary>
    public const double Pi = Math.PI;

    /// <summary>Full-turn constant 2π.</summary>
    public const double Tau = 2d * Math.PI;
}