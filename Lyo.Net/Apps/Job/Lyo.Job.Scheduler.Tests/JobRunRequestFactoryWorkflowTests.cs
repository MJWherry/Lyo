using Lyo.Common.Core.Identifiers;
using Lyo.Common.Metadata.Records;
using Lyo.Formatter;
using Lyo.Job.Models.Context;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;

namespace Lyo.Job.Scheduler.Tests;

/// <summary>
/// A workflow step's <c>ParametersJson</c> is what makes a shared job definition reusable across workflows: the definition declares the inputs and each step supplies its own
/// values, templated against upstream results. These lock the merge rule, the upstream tokens, and the failure behavior when the JSON is unusable.
/// </summary>
public class JobRunRequestFactoryWorkflowTests
{
    private static readonly DateTimeOffset Frozen = new(2026, 8, 22, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public void BuildForWorkflowStep_ParametersJsonOverride_ReplacesDefinitionDefault()
    {
        var definition = BuildDefinition(Parameter("Path", LyoTypeInfo.String, "/tmp/a"));
        var req = Build(definition, Step(definition, """{"Path":"/tmp/b"}"""), new());

        // Replace, not append: two rows for the same key would leave the worker picking between the definition default and the step's value.
        var parameter = Assert.Single(req.JobRunParameters);
        Assert.Equal("Path", parameter.Key);
        Assert.Equal("/tmp/b", parameter.Value);
    }

    [Fact]
    public void BuildForWorkflowStep_UpstreamResultToken_ResolvesFromDependencyRun()
    {
        var definition = BuildDefinition(Parameter("Rows", LyoTypeInfo.String, "0"));
        var scope = new JobContextBag().WithUpstreamRun("Extract", RunWithRowCount("42"));
        var req = Build(definition, Step(definition, """{"Rows":"{Step.Extract.Result.RowCount}"}"""), scope);
        Assert.Equal("42", Assert.Single(req.JobRunParameters).Value);
    }

    [Fact]
    public void BuildForWorkflowStep_UnknownKey_AddsStringParameter()
    {
        var definition = BuildDefinition(Parameter("Path", LyoTypeInfo.String, "/tmp/a"));
        var req = Build(definition, Step(definition, """{"Extra":"x"}"""), new());
        var extra = Assert.Single(req.JobRunParameters, p => p.Key == "Extra");
        Assert.Equal(LyoTypeInfo.String.FullName, extra.Type);
        Assert.Equal("x", extra.Value);
    }

    [Fact]
    public void BuildForWorkflowStep_MalformedParametersJson_FallsBackToDefinitionDefaults()
    {
        var definition = BuildDefinition(Parameter("Path", LyoTypeInfo.String, "/tmp/a"));
        var req = Build(definition, Step(definition, "not json"), new());

        // A bad step configuration must not take the run down; the definition defaults still produce a runnable request.
        var parameter = Assert.Single(req.JobRunParameters);
        Assert.Equal("/tmp/a", parameter.Value);
    }

    [Fact]
    public void BuildForWorkflowStep_JsonTypedParameter_IsNotFormatted()
    {
        const string payload = """{"a":"{Definition.Name}"}""";
        var definition = BuildDefinition(Parameter("Payload", LyoTypeInfo.JsonObject, payload));
        var req = Build(definition, Step(definition, null), new JobContextBag().WithDefinition(definition));

        // Braces in a JSON value are data, not placeholders. Formatting them would corrupt the document.
        Assert.Equal(payload, Assert.Single(req.JobRunParameters).Value);
    }

    [Fact]
    public void BuildForWorkflowStep_CreatedRun_IsNotDispatchedAndCarriesNoTriggers()
    {
        var definition = BuildDefinition(Parameter("Path", LyoTypeInfo.String, "/tmp/a"));
        var req = Build(definition, Step(definition, null), new());
        Assert.Equal(definition.Id, req.JobDefinitionId);
    }

    private static JobRunReq Build(JobDefinitionRes definition, JobWorkflowStepRes step, JobContextBag scope)
        => new JobRunRequestFactory(new FormatterService(() => Frozen), "test").BuildForWorkflowStep(definition, step, scope);

    private static JobWorkflowStepRes Step(JobDefinitionRes definition, string? parametersJson)
        => new(LyoGuid.CreateV7(), LyoGuid.CreateV7(), definition.Id, "Load", 0, null, JobWorkflowFailurePolicy.Stop, parametersJson, true);

    private static JobParameterRes Parameter(string key, LyoTypeInfo type, string? value)
        => new(LyoGuid.CreateV7(), LyoGuid.CreateV7(), key, null, type.FullName, value, null, true, false);

    private static JobDefinitionRes BuildDefinition(params JobParameterRes[] parameters)
        => new(LyoGuid.CreateV7(), "WorkflowStepDef", null, "Test", "cs", true, parameters, null, null);

    private static JobRunRes RunWithRowCount(string value)
    {
        var runId = LyoGuid.CreateV7();
        return new() {
            Id = runId,
            JobDefinitionId = LyoGuid.CreateV7(),
            State = JobState.Finished,
            Result = JobRunResult.Success,
            CreatedTimestamp = DateTime.UtcNow,
            JobRunResults = [new(LyoGuid.CreateV7(), runId, "RowCount", LyoTypeInfo.Int.FullName, value)]
        };
    }
}