namespace Lyo.Scientific;

/// <summary>Well-known physical and mathematical constants as compile-time <see cref="double" /> literals.</summary>
/// <remarks>SI definitions where applicable; also referenced from <c>Lyo.Mathematics.Functions.PhysicsFunctions</c> for gravitation and ideal-gas calculations.</remarks>
public static class ScientificConstants
{
    /// <summary>Newtonian gravitational constant G (CODATA 2018), m³/(kg·s²).</summary>
    public const double GravitationalConstant = 6.67430e-11d;

    /// <summary>Standard acceleration due to gravity (m/s²), ISO 80000-3.</summary>
    public const double StandardGravity = 9.80665d;

    /// <summary>Speed of light in vacuum (m/s), exact SI definition.</summary>
    public const double SpeedOfLight = 299_792_458d;

    /// <summary>Molar gas constant R, J/(mol·K).</summary>
    public const double GasConstant = 8.314462618d;

    /// <summary>Electric constant epsilon0, F/m.</summary>
    public const double VacuumPermittivity = 8.8541878128e-12d;

    /// <summary>Planck constant h, J·s, exact SI definition.</summary>
    public const double PlanckConstant = 6.62607015e-34d;

    /// <summary>Avogadro constant N_A, mol⁻¹, exact SI definition.</summary>
    public const double AvogadroConstant = 6.02214076e23d;

    /// <summary>Boltzmann constant k, J/K, exact SI definition.</summary>
    public const double BoltzmannConstant = 1.380649e-23d;

    /// <summary>Elementary charge e, C, exact SI definition.</summary>
    public const double ElementaryCharge = 1.602176634e-19d;

    /// <summary>Ratio of a circle’s circumference to its diameter.</summary>
    public const double Pi = Math.PI;

    /// <summary>Full circle constant 2π.</summary>
    public const double Tau = 2d * Math.PI;
}