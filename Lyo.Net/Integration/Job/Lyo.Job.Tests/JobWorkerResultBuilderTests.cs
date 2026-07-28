using Lyo.Job.Models.Enums;
using Lyo.Job.Worker;
using Constants = Lyo.Job.Models.Constants;

namespace Lyo.Job.Tests;

public class JobWorkerResultBuilderTests
{
    [Fact]
    public void Build_DefaultOutcome_IsSuccess()
    {
        var results = new JobWorkerResultBuilder().Build();
        Assert.Contains(results, r => r.Key == Constants.Data.JobRunResultKey.Result && r.Value == nameof(JobRunResult.Success));
    }

    [Fact]
    public void Fail_SetsFailureOutcome()
    {
        var results = new JobWorkerResultBuilder().Fail().Build();
        Assert.Equal(JobRunResult.Failure, new JobWorkerResultBuilder().Fail().CurrentOutcome);
        Assert.Contains(results, r => r.Key == Constants.Data.JobRunResultKey.Result && r.Value == nameof(JobRunResult.Failure));
    }

    [Fact]
    public void Cancel_SetsCancelledOutcome()
    {
        var results = new JobWorkerResultBuilder().Cancel().Build();
        Assert.Contains(results, r => r.Key == Constants.Data.JobRunResultKey.Result && r.Value == nameof(JobRunResult.Cancelled));
    }

    [Fact]
    public void SucceedWithWarnings_SetsSuccessWithWarningsOutcome()
    {
        var results = new JobWorkerResultBuilder().SucceedWithWarnings().Build();
        Assert.Contains(results, r => r.Key == Constants.Data.JobRunResultKey.Result && r.Value == nameof(JobRunResult.SuccessWithWarnings));
    }

    [Fact]
    public void AddResult_AddsCustomEntry()
    {
        var results = new JobWorkerResultBuilder().AddResult("CustomKey", "value").Build();
        Assert.Contains(results, r => r.Key == "CustomKey" && r.Value == "value" && r.Type == JobParameterType.String);
    }

    [Fact]
    public void AddCount_AddsIntegerEntry()
    {
        var results = new JobWorkerResultBuilder().AddCount("Processed", 42).Build();
        Assert.Contains(results, r => r.Key == "Processed" && r.Value == "42" && r.Type == JobParameterType.Int);
    }

    [Fact]
    public void AddError_RecordsFailureReasonAndFails()
    {
        var results = new JobWorkerResultBuilder().AddError("Something broke", 2).Build();
        Assert.Contains(results, r => r.Key == Constants.Data.JobRunResultKey.FailureReason(2) && r.Value == "Something broke");
        Assert.Contains(results, r => r.Key == Constants.Data.JobRunResultKey.Result && r.Value == nameof(JobRunResult.Failure));
    }

    [Fact]
    public void AddFailedItem_RecordsItemAndOptionalReason()
    {
        var results = new JobWorkerResultBuilder().AddFailedItem(1, "item-42", "bad data").Build();
        Assert.Contains(results, r => r.Key == Constants.Data.JobRunResultKey.FailedItem(1) && r.Value == "item-42");
        Assert.Contains(results, r => r.Key == Constants.Data.JobRunResultKey.FailureReason(1) && r.Value == "bad data");
        Assert.Contains(results, r => r.Key == Constants.Data.JobRunResultKey.Result && r.Value == nameof(JobRunResult.Failure));
    }

    [Fact]
    public void AddApiCallTime_UsesApiCallTimeKey()
    {
        var results = new JobWorkerResultBuilder().AddApiCallTime("ExternalApi", 1234).Build();
        Assert.Contains(results, r => r.Key == Constants.Data.JobRunResultKey.ApiCallTime("ExternalApi") && r.Value == "1234" && r.Type == JobParameterType.Long);
    }

    [Theory]
    [InlineData(nameof(JobWorkerResultBuilder.AddCreateCount), Constants.Data.JobRunResultKey.CreateCount)]
    [InlineData(nameof(JobWorkerResultBuilder.AddUpdateCount), Constants.Data.JobRunResultKey.UpdateCount)]
    [InlineData(nameof(JobWorkerResultBuilder.AddDeleteCount), Constants.Data.JobRunResultKey.DeleteCount)]
    [InlineData(nameof(JobWorkerResultBuilder.AddFailedCount), Constants.Data.JobRunResultKey.FailedCount)]
    [InlineData(nameof(JobWorkerResultBuilder.AddNoChangeCount), Constants.Data.JobRunResultKey.NoChangeCount)]
    public void AddStandardCounts_UseExpectedKeys(string methodName, string expectedKey)
    {
        var builder = new JobWorkerResultBuilder();
        switch (methodName) {
            case nameof(JobWorkerResultBuilder.AddCreateCount):
                builder.AddCreateCount(3);
                break;
            case nameof(JobWorkerResultBuilder.AddUpdateCount):
                builder.AddUpdateCount(3);
                break;
            case nameof(JobWorkerResultBuilder.AddDeleteCount):
                builder.AddDeleteCount(3);
                break;
            case nameof(JobWorkerResultBuilder.AddFailedCount):
                builder.AddFailedCount(3);
                break;
            case nameof(JobWorkerResultBuilder.AddNoChangeCount):
                builder.AddNoChangeCount(3);
                break;
        }

        var results = builder.Build();
        Assert.Contains(results, r => r.Key == expectedKey && r.Value == "3");
    }
}