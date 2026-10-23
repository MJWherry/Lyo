using System.ComponentModel.DataAnnotations;
using Lyo.Common.Core.Identifiers;
using Lyo.Job.Postgres;
using Lyo.Job.Postgres.Database;

namespace Lyo.Job.Tests;

/// <summary>
/// A schedule fires exactly one thing: a job definition or a workflow. The database CHECK is the backstop; these lock the write-time message so the API answers with a
/// validation error naming the problem rather than a constraint-violation 500.
/// </summary>
public class JobScheduleWriteValidatorTests
{
    [Fact]
    public void Validate_ScheduleWithBothDefinitionAndWorkflow_Throws()
    {
        var schedule = new JobSchedule { JobDefinitionId = LyoGuid.CreateV7(), JobWorkflowId = LyoGuid.CreateV7() };
        var ex = Assert.Throws<ValidationException>(() => JobScheduleWriteValidator.ValidateOwner(schedule));
        Assert.Contains("exactly one", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ScheduleWithNeither_Throws()
    {
        var ex = Assert.Throws<ValidationException>(() => JobScheduleWriteValidator.ValidateOwner(new()));
        Assert.Contains("exactly one", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ScheduleWithOneOwner_DoesNotThrow()
    {
        JobScheduleWriteValidator.ValidateOwner(new() { JobDefinitionId = LyoGuid.CreateV7() });
        JobScheduleWriteValidator.ValidateOwner(new() { JobWorkflowId = LyoGuid.CreateV7() });
    }
}