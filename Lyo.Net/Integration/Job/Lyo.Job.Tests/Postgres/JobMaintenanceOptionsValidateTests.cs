using Lyo.Exceptions.Models;
using Lyo.Job.Postgres;

namespace Lyo.Job.Tests.Postgres;

public class JobMaintenanceOptionsValidateTests
{
    [Fact]
    public void Validate_WhenValid_DoesNotThrow()
    {
        var options = new JobMaintenanceOptions();
        options.Validate();
    }

    [Fact]
    public void Validate_WhenInvalid_ThrowsValidationException()
    {
        var options = new JobMaintenanceOptions { PurgeBatchSize = 0 };
        var ex = Assert.Throws<ValidationException>(() => options.Validate());
        Assert.Contains(nameof(JobMaintenanceOptions.PurgeBatchSize), ex.Message, StringComparison.Ordinal);
    }
}
