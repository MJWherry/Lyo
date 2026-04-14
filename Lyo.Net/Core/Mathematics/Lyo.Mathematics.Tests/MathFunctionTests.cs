using Lyo.Mathematics.Functions;
using Lyo.Mathematics.Matrices;
using Lyo.Mathematics.Quantities;
using Lyo.Mathematics.Vectors;

namespace Lyo.Mathematics.Tests;

public class MathFunctionTests
{
    [Fact]
    public void PhysicsFunctions_Momentum_UsesTypedInput()
    {
        var result = PhysicsFunctions.Momentum(new(new(10d), new(5d)));
        Assert.Equal(50d, result.KilogramMetersPerSecond, 10);
    }

    [Fact]
    public void PhysicsFunctions_Force_UsesTypedInput()
    {
        var result = PhysicsFunctions.Force(new(new(4d), new(2.5d)));
        Assert.Equal(10d, result.Newtons, 10);
    }

    [Fact]
    public void PhysicsFunctions_KineticEnergy_ComputesExpectedResult()
    {
        var result = PhysicsFunctions.KineticEnergy(new(new(2d), new(3d)));
        Assert.Equal(9d, result.Joules, 10);
    }

    [Fact]
    public void PhysicsFunctions_BodyMassIndex_ComputesExpectedValue()
    {
        var result = PhysicsFunctions.BodyMassIndex(new(Mass.FromKilograms(81d), Length.FromMeters(1.8d)));
        Assert.Equal(25d, result, 10);
    }

    [Fact]
    public void GeometryFunctions_RectangleDiagonal_ComputesExpectedLength()
    {
        var result = GeometryFunctions.RectangleDiagonal(new(new(3d), new(4d)));
        Assert.Equal(5d, result.Meters, 10);
    }

    [Fact]
    public void GeometryFunctions_CircleArea_ComputesExpectedArea()
    {
        var result = GeometryFunctions.CircleArea(new(new(2d)));
        Assert.Equal(Math.PI * 4d, result.SquareMeters, 10);
    }

    [Fact]
    public void AlgebraFunctions_SolveQuadratic_ReturnsRealRoots()
    {
        var result = AlgebraFunctions.SolveQuadratic(new(1d, -3d, 2d));
        Assert.True(result.HasRealRoots);
        Assert.Equal(2d, result.Root1);
        Assert.Equal(1d, result.Root2);
    }

    [Fact]
    public void StatisticsFunctions_Describe_ComputesSummary()
    {
        var result = StatisticsFunctions.Describe([1d, 2d, 3d, 4d], false);
        Assert.Equal(2.5d, result.Mean, 10);
        Assert.Equal(2.5d, result.Median, 10);
        Assert.Equal(1.25d, result.Variance, 10);
        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void StatisticsFunctions_MovingAverage_ComputesWindowedValues()
    {
        var result = StatisticsFunctions.MovingAverage([1d, 2d, 3d, 4d, 5d], 3);
        Assert.Equal([2d, 3d, 4d], result);
    }

    [Fact]
    public void StatisticsFunctions_LinearRegression_ComputesSlopeAndIntercept()
    {
        var result = StatisticsFunctions.LinearRegression(new([1d, 2d, 3d], [2d, 4d, 6d]));
        Assert.Equal(2d, result.Slope, 10);
        Assert.Equal(0d, result.Intercept, 10);
        Assert.Equal(1d, result.CorrelationCoefficient, 10);
    }

    [Fact]
    public void StatisticsFunctions_Percentile_ComputesInterpolatedValue()
    {
        var result = StatisticsFunctions.Percentile([1d, 2d, 3d, 4d], 75d);
        Assert.Equal(3.25d, result, 10);
    }

    [Fact]
    public void StatisticsFunctions_RollingStandardDeviation_ComputesWindows()
    {
        var result = StatisticsFunctions.RollingStandardDeviation([1d, 2d, 3d, 4d], 2, false);
        Assert.Equal(3, result.Length);
        Assert.All(result, value => Assert.Equal(0.5d, value, 10));
    }

    [Fact]
    public void StatisticsFunctions_LatestZScore_ComputesExpectedValue()
    {
        var result = StatisticsFunctions.LatestZScore([10d, 10d, 10d, 25d], false);
        Assert.True(result > 1.7d);
    }

    [Fact]
    public void StatisticsFunctions_IsAnomalyByZScore_FlagsOutlier()
    {
        var result = StatisticsFunctions.IsAnomalyByZScore([10d, 10d, 10d, 25d], 1.5d, false);
        Assert.True(result);
    }

    [Fact]
    public void LinearAlgebraFunctions_Determinant2x2_ComputesExpectedValue()
    {
        var result = LinearAlgebraFunctions.Determinant(new Matrix2x2(1d, 2d, 3d, 4d));
        Assert.Equal(-2d, result, 10);
    }

    [Fact]
    public void LinearAlgebraFunctions_MultiplyMatrixVector_ComputesExpectedValue()
    {
        var result = LinearAlgebraFunctions.Multiply(new(1d, 2d, 3d, 4d), new Vector2D(5d, 6d));
        Assert.Equal(new(17d, 39d), result);
    }

    [Fact]
    public void LinearAlgebraFunctions_Solve2x2_ComputesExpectedSolution()
    {
        var result = LinearAlgebraFunctions.Solve2x2(new(new(2d, 1d, 1d, -1d), new(5d, 1d)));
        Assert.True(result.HasUniqueSolution);
        Assert.Equal(new(2d, 1d), result.Solution);
    }

    [Fact]
    public void ArithmeticFunctions_PercentageChange_ComputesExpectedValue()
    {
        var result = ArithmeticFunctions.PercentageChange(100d, 125d);
        Assert.Equal(25d, result, 10);
    }

    [Fact]
    public void ArithmeticFunctions_RatePerSecond_IsUsefulForMetricsStyleInputs()
    {
        var result = ArithmeticFunctions.RatePerSecond(240d, TimeInterval.FromMinutes(2d));
        Assert.Equal(2d, result, 10);
    }
}