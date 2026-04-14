using Lyo.Mathematics.Functions;
using Lyo.Mathematics.Matrices;
using Lyo.Mathematics.Quantities;
using Lyo.Mathematics.Registry;
using Lyo.Mathematics.Vectors;

namespace Lyo.Mathematics.Tests;

public class AdvancedMathFunctionTests
{
    [Fact]
    public void AlgebraFunctions_EvaluatePolynomial_UsesHornersMethod()
    {
        var result = AlgebraFunctions.EvaluatePolynomial(new([2d, -6d, 2d, -1d], 3d));
        Assert.Equal(5d, result, 10);
    }

    [Fact]
    public void AlgebraFunctions_EvaluatePolynomialDerivative_ComputesExpectedValue()
    {
        var result = AlgebraFunctions.EvaluatePolynomialDerivative([3d, 0d, -4d], 2d);
        Assert.Equal(12d, result, 10);
    }

    [Fact]
    public void LinearAlgebraFunctions_Inverse2x2_ComputesExpectedMatrix()
    {
        var result = LinearAlgebraFunctions.Inverse(new Matrix2x2(4d, 7d, 2d, 6d));
        Assert.Equal(0.6d, result.M11, 10);
        Assert.Equal(-0.7d, result.M12, 10);
        Assert.Equal(-0.2d, result.M21, 10);
        Assert.Equal(0.4d, result.M22, 10);
    }

    [Fact]
    public void LinearAlgebraFunctions_Solve3x3_ComputesExpectedVector()
    {
        var result = LinearAlgebraFunctions.Solve3x3(new(3d, 2d, -1d, 2d, -2d, 4d, -1d, 0.5d, -1d), new(1d, -2d, 0d));
        Assert.Equal(1d, result.X, 10);
        Assert.Equal(-2d, result.Y, 10);
        Assert.Equal(-2d, result.Z, 10);
    }

    [Fact]
    public void StatisticsFunctions_Quartiles_ComputesExpectedValues()
    {
        var result = StatisticsFunctions.Quartiles([1d, 2d, 3d, 4d, 5d]);
        Assert.Equal(2d, result.Q1, 10);
        Assert.Equal(3d, result.Q2, 10);
        Assert.Equal(4d, result.Q3, 10);
    }

    [Fact]
    public void StatisticsFunctions_WeightedStatistics_ComputesExpectedValues()
    {
        var result = StatisticsFunctions.WeightedStatistics(new([1d, 2d, 10d], [1d, 1d, 3d]));
        Assert.Equal(6.6d, result.WeightedMean, 10);
        Assert.True(result.WeightedVariance > 10d);
    }

    [Fact]
    public void StatisticsFunctions_PearsonCorrelation_ComputesExpectedValue()
    {
        var result = StatisticsFunctions.PearsonCorrelation([1d, 2d, 3d, 4d], [2d, 4d, 6d, 8d]);
        Assert.Equal(1d, result, 10);
    }

    [Fact]
    public void StatisticsFunctions_MeanConfidenceInterval_ProducesBounds()
    {
        var result = StatisticsFunctions.MeanConfidenceInterval([10d, 12d, 14d, 16d, 18d], 0.95d, true);
        Assert.Equal(14d, result.Mean, 10);
        Assert.True(result.LowerBound < result.Mean);
        Assert.True(result.UpperBound > result.Mean);
    }

    [Fact]
    public void DistributionsFunctions_NormalSummary_ComputesExpectedValues()
    {
        var result = DistributionsFunctions.NormalSummary(new(0d, 1d), 0d, 0.5d);
        Assert.Equal(0.3989422804d, result.Pdf, 6);
        Assert.Equal(0.5d, result.Cdf, 6);
        Assert.Equal(0d, result.InverseCdf!.Value, 6);
    }

    [Fact]
    public void DistributionsFunctions_ExponentialInverseCdf_ComputesExpectedValue()
    {
        var result = DistributionsFunctions.ExponentialInverseCdf(new(2d), 0.6321205588d);
        Assert.Equal(0.5d, result, 6);
    }

    [Fact]
    public void PhysicsFunctions_ProjectileMotion_ComputesExpectedRange()
    {
        var result = PhysicsFunctions.ProjectileMotion(
            new(Velocity.FromMetersPerSecond(10d), Angle.FromDegrees(45d), Length.FromMeters(0d), Acceleration.FromMetersPerSecondSquared(9.80665d)));

        Assert.True(result.Range.Meters > 10d);
        Assert.True(result.MaximumHeight.Meters > 2d);
    }

    [Fact]
    public void PhysicsFunctions_IdealGasPressure_ComputesExpectedValue()
    {
        var result = PhysicsFunctions.IdealGasPressure(new(Pressure.FromPascals(101325d), Volume.FromCubicMeters(1d), Temperature.FromKelvin(273.15d), 1d));
        Assert.Equal(2271.095d, result.Pascals, 3);
    }

    [Fact]
    public void CalculusFunctions_TrapezoidalIntegration_ComputesExpectedArea()
    {
        var result = CalculusFunctions.TrapezoidalIntegration(new(x => x * x, 0d, 1d, 1000));
        Assert.Equal(1d / 3d, result, 3);
    }

    [Fact]
    public void CalculusFunctions_Bisection_FindsRoot()
    {
        var result = CalculusFunctions.Bisection(x => x * x - 2d, 1d, 2d, 1e-8, 100);
        Assert.True(result.Converged);
        Assert.Equal(Math.Sqrt(2d), result.Root, 6);
    }

    [Fact]
    public void CalculusFunctions_RungeKutta4Solve_ApproximatesExponentialGrowth()
    {
        var result = CalculusFunctions.RungeKutta4Solve(new((_, y) => y, 0d, 1d, 0.1d, 10));
        Assert.Equal(Math.E, result[^1].Y, 2);
    }

    [Fact]
    public void CalculusFunctions_AdaptiveIntegration_ComputesExpectedArea()
    {
        var result = CalculusFunctions.AdaptiveIntegration(new(Math.Sin, 0d, Math.PI, 1e-8d, 12));
        Assert.Equal(2d, result, 6);
    }

    [Fact]
    public void CalculusFunctions_Jacobian_ComputesExpectedDerivatives()
    {
        var result = CalculusFunctions.Jacobian(new(point => [point[0] * point[0] + point[1], point[0] * point[1]], [2d, 3d], 1e-5d));
        Assert.Equal(4d, result[0, 0], 3);
        Assert.Equal(1d, result[0, 1], 3);
        Assert.Equal(3d, result[1, 0], 3);
        Assert.Equal(2d, result[1, 1], 3);
    }

    [Fact]
    public void CalculusFunctions_Hessian_ComputesExpectedCurvature()
    {
        var result = CalculusFunctions.Hessian(new(point => point[0] * point[0] + 3d * point[0] * point[1] + point[1] * point[1], [1d, 1d], 1e-5d));
        Assert.Equal(2d, result[0, 0], 3);
        Assert.Equal(3d, result[0, 1], 3);
        Assert.Equal(3d, result[1, 0], 3);
        Assert.Equal(2d, result[1, 1], 3);
    }

    [Fact]
    public void TrigonometryFunctions_LawOfCosinesForSide_ComputesExpectedLength()
    {
        var result = TrigonometryFunctions.LawOfCosinesForSide(Length.FromMeters(3d), Length.FromMeters(4d), Angle.FromDegrees(90d));
        Assert.Equal(5d, result.Meters, 10);
    }

    [Fact]
    public void ComplexFunctions_Divide_ComputesExpectedValue()
    {
        var result = ComplexFunctions.Divide(new(1d, 2d), new(3d, -4d));
        Assert.Equal(-0.2d, result.Real, 10);
        Assert.Equal(0.4d, result.Imaginary, 10);
    }

    [Fact]
    public void InterpolationFunctions_PiecewiseLinear_ComputesExpectedValue()
    {
        var result = InterpolationFunctions.PiecewiseLinear([0d, 10d, 20d], [0d, 100d, 400d], 15d);
        Assert.Equal(250d, result, 10);
    }

    [Fact]
    public void FinancialFunctions_LoanPayment_ComputesExpectedValue()
    {
        var result = FinancialFunctions.LoanPayment(new(100_000d, 0.05d, 12, 30d));
        Assert.InRange(result, 530d, 540d);
    }

    [Fact]
    public void SignalFunctions_Convolution_ComputesExpectedSequence()
    {
        var result = SignalFunctions.Convolution([1d, 2d, 3d], [1d, 1d]);
        Assert.Equal([1d, 3d, 5d, 3d], result);
    }

    [Fact]
    public void TransformFunctions_FastFourierTransform_ComputesExpectedSpectrum()
    {
        var result = TransformFunctions.FastFourierTransform([new(1d, 0d), new(0d, 0d), new(-1d, 0d), new(0d, 0d)]);
        Assert.Equal(0d, result[0].Magnitude, 6);
        Assert.Equal(2d, result[1].Magnitude, 6);
        Assert.Equal(0d, result[2].Magnitude, 6);
        Assert.Equal(2d, result[3].Magnitude, 6);
    }

    [Fact]
    public void LinearAlgebraFunctions_QrDecomposition_ReconstructsMatrix()
    {
        var result = LinearAlgebraFunctions.QrDecomposition(new[,] { { 12d, -51d }, { 6d, 167d }, { -4d, 24d } });
        var q = result.Q;
        var r = result.R;
        var reconstructed00 = q[0, 0] * r[0, 0] + q[0, 1] * r[1, 0];
        var reconstructed01 = q[0, 0] * r[0, 1] + q[0, 1] * r[1, 1];
        Assert.Equal(12d, reconstructed00, 6);
        Assert.Equal(-51d, reconstructed01, 6);
    }

    [Fact]
    public void LinearAlgebraFunctions_Eigenvalues_ComputesExpectedValues()
    {
        var result = LinearAlgebraFunctions.Eigenvalues(new(2d, 1d, 1d, 2d));
        Assert.Equal(3d, result.Eigenvalue1.Real, 6);
        Assert.Equal(1d, result.Eigenvalue2.Real, 6);
    }

    [Fact]
    public void OptimizationFunctions_GradientDescent_MovesTowardMinimum()
    {
        var result = OptimizationFunctions.GradientDescent(new(x => 2d * (x - 3d), 10d, 0.1d, 50));
        Assert.InRange(result.Value, 2.99d, 3.01d);
    }

    [Fact]
    public void Vector3D_Project_ComputesExpectedProjection()
    {
        var result = Vector3D.Project(new(3d, 4d, 0d), new(1d, 0d, 0d));
        Assert.Equal(new(3d, 0d, 0d), result);
    }

    [Fact]
    public void MathematicsFormulaRegistry_ExposesNewProductionEntries()
    {
        Assert.Contains(MathematicsFormulaRegistry.All, item => item.Id == "signal.fft");
        Assert.Contains(MathematicsFormulaRegistry.All, item => item.Id == "linear_algebra.qr");
    }
}