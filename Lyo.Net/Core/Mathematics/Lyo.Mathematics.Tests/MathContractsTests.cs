using Lyo.Mathematics.Matrices;
using Lyo.Mathematics.Quantities;
using Lyo.Mathematics.Vectors;

namespace Lyo.Mathematics.Tests;

public class MathContractsTests
{
    [Fact]
    public void Mass_FromPounds_ConvertsToKilograms()
    {
        var mass = Mass.FromPounds(220.46226218487757);
        Assert.Equal(100d, mass.Kilograms, 10);
    }

    [Fact]
    public void Length_FromFeet_ConvertsToMeters()
    {
        var length = Length.FromFeet(3.280839895013123);
        Assert.Equal(1d, length.Meters, 10);
    }

    [Fact]
    public void TimeInterval_FromMinutes_ConvertsToSeconds()
    {
        var interval = TimeInterval.FromMinutes(2.5);
        Assert.Equal(150d, interval.Seconds, 10);
    }

    [Fact]
    public void Angle_FromDegrees_ConvertsToRadians()
    {
        var angle = Angle.FromDegrees(180d);
        Assert.Equal(Math.PI, angle.Radians, 10);
    }

    [Fact]
    public void Vector3D_Cross_ProducesExpectedVector()
    {
        var left = new Vector3D(1, 0, 0);
        var right = new Vector3D(0, 1, 0);
        var result = Vector3D.Cross(left, right);
        Assert.Equal(new(0, 0, 1), result);
    }

    [Fact]
    public void Velocity_FromKilometersPerHour_ConvertsToMetersPerSecond()
    {
        var velocity = Velocity.FromKilometersPerHour(36d);
        Assert.Equal(10d, velocity.MetersPerSecond, 10);
    }

    [Fact]
    public void Matrix2x2_Identity_HasExpectedValues()
    {
        var matrix = Matrix2x2.Identity;
        Assert.Equal(1d, matrix.M11);
        Assert.Equal(1d, matrix.M22);
        Assert.Equal(0d, matrix.M12);
        Assert.Equal(0d, matrix.M21);
    }

    [Fact]
    public void Temperature_FromCelsius_ConvertsToKelvin()
    {
        var temperature = Temperature.FromCelsius(100d);
        Assert.Equal(373.15d, temperature.Kelvin, 10);
    }

    [Fact]
    public void Pressure_FromAtmospheres_ConvertsToPascals()
    {
        var pressure = Pressure.FromAtmospheres(1d);
        Assert.Equal(101325d, pressure.Pascals, 10);
    }

    [Fact]
    public void Mass_WithNegativeValue_Throws() => Assert.Throws<ArgumentOutOfRangeException>(() => new Mass(-1d));
}