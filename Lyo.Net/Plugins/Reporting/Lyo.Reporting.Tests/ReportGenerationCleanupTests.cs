using Lyo.Common.Core.Identifiers;
using Lyo.Reporting.Models.Rendering;
using Lyo.Reporting.Postgres;
using Lyo.Reporting.Postgres.Database;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Reporting.Tests;

public sealed class ReportGenerationCleanupTests
{
    [Fact]
    public async Task CleanupAsync_GenerationsWithOutputFiles_InvokesHook()
    {
        var cleaned = new List<Guid?>();
        var hooks = new ReportGenerationHooks {
            OnCleanupAsync = (ctx, _) => {
                cleaned.Add(ctx.OutputFileId);
                return ValueTask.CompletedTask;
            }
        };

        var withOutput = new ReportGeneration {
            Id = LyoGuid.CreateV7(),
            OutputFileId = LyoGuid.CreateV7(),
            ReportDataJson = "{}",
            Format = "Csv",
            Status = "Succeeded"
        };

        var withoutOutput = new ReportGeneration {
            Id = LyoGuid.CreateV7(),
            ReportDataJson = "{}",
            Format = "Csv",
            Status = "Failed"
        };

        await using var provider = new ServiceCollection().BuildServiceProvider();
        await ReportGenerationCleanup.InvokeCleanupHooksAsync([withOutput, withoutOutput], hooks, provider, TestCt);
        Assert.Single(cleaned);
        Assert.Equal(withOutput.OutputFileId, cleaned[0]);
    }

    [Fact]
    public async Task CleanupAsync_NoHooks_IsNoOp()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        await ReportGenerationCleanup.InvokeCleanupHooksAsync(
        [
            new() {
                Id = LyoGuid.CreateV7(),
                OutputFileId = LyoGuid.CreateV7(),
                ReportDataJson = "{}",
                Format = "Csv",
                Status = "Succeeded"
            }
        ], null, provider, TestCt);
    }

    [Fact]
    public async Task CleanupAsync_HookFailure_Propagates()
    {
        var hooks = new ReportGenerationHooks { OnCleanupAsync = (_, _) => throw new IOException("storage down") };
        await using var provider = new ServiceCollection().BuildServiceProvider();
        await Assert.ThrowsAsync<IOException>(async () => await ReportGenerationCleanup.InvokeCleanupHooksAsync(
        [
            new() {
                Id = LyoGuid.CreateV7(),
                OutputFileId = LyoGuid.CreateV7(),
                ReportDataJson = "{}",
                Format = "Csv",
                Status = "Succeeded"
            }
        ], hooks, provider, TestCt));
    }
}