using System.Reflection;
using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Records;
using Lyo.Formatter;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Parameters;
using Lyo.Schedule.Models;

namespace Lyo.Job.Scheduler.Tests;

/// <summary>
/// Schedule-level parameters must be merged into the run request when a schedule fires: they override definition defaults by key (case-insensitive), disabled parameters are
/// ignored, and String/Json values go through the same template formatting as definition/trigger parameters. Regression coverage for required definition parameters with null
/// defaults
/// whose values live on the schedule (per-client schedules).
/// </summary>
public class JobSchedulerScheduleParameterTests
{
    [Fact]
    public async Task ScheduleParameters_OverrideDefinitionDefaultsByKey()
    {
        var definitionId = Guid.NewGuid();
        var clientId = Guid.NewGuid().ToString("D");
        var schedule = BuildSchedule(
            definitionId, new JobScheduleParameterRes(Guid.NewGuid(), Guid.NewGuid(), "ClientId", LyoTypeInfo.Guid.FullName, clientId, "ClientName client", null, true),
            new JobScheduleParameterRes(Guid.NewGuid(), Guid.NewGuid(), "emailto", LyoTypeInfo.String.FullName, "to@example.com", "ClientName To", null, true));

        var definition = BuildDefinition(
            definitionId, schedule, BuildDefinitionParameter(definitionId, "ClientId", LyoTypeInfo.Guid.FullName, null, true),
            BuildDefinitionParameter(definitionId, "EmailTo", LyoTypeInfo.String.FullName, null, true),
            BuildDefinitionParameter(definitionId, "PageSize", LyoTypeInfo.Int.FullName, "200", false));

        var runReq = await BuildRunRequestAsync(definition, schedule);

        // Overridden by the schedule. exactly once per key, no empty definition default left alongside.
        var clientParam = Assert.Single(runReq.JobRunParameters, p => p.Key.Equals("ClientId", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(clientId, clientParam.Value);
        var emailParam = Assert.Single(runReq.JobRunParameters, p => p.Key.Equals("EmailTo", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("to@example.com", emailParam.Value);

        // Untouched definition default survives.
        var pageSizeParam = Assert.Single(runReq.JobRunParameters, p => p.Key == "PageSize");
        Assert.Equal("200", pageSizeParam.Value);
    }

    [Fact]
    public async Task DisabledScheduleParameters_AreIgnored()
    {
        var definitionId = Guid.NewGuid();
        var schedule = BuildSchedule(
            definitionId, new JobScheduleParameterRes(Guid.NewGuid(), Guid.NewGuid(), "EmailTo", LyoTypeInfo.String.FullName, "to@example.com", null, null, true),
            new JobScheduleParameterRes(Guid.NewGuid(), Guid.NewGuid(), "EmailToCc", LyoTypeInfo.String.FullName, "cc@example.com", null, null, false));

        var definition = BuildDefinition(definitionId, schedule, BuildDefinitionParameter(definitionId, "EmailTo", LyoTypeInfo.String.FullName, null, true));
        var runReq = await BuildRunRequestAsync(definition, schedule);
        Assert.Single(runReq.JobRunParameters, p => p.Key == "EmailTo");
        Assert.DoesNotContain(runReq.JobRunParameters, p => p.Key == "EmailToCc");
    }

    [Fact]
    public async Task ScheduleParameterStringValues_AreTemplateFormatted()
    {
        var definitionId = Guid.NewGuid();
        var schedule = BuildSchedule(
            definitionId, new JobScheduleParameterRes(Guid.NewGuid(), Guid.NewGuid(), "Note", LyoTypeInfo.String.FullName, "Run of {Definition.Name}", null, null, true));

        var definition = BuildDefinition(definitionId, schedule);
        var runReq = await BuildRunRequestAsync(definition, schedule);
        var noteParam = Assert.Single(runReq.JobRunParameters, p => p.Key == "Note");
        Assert.Equal("Run of ScheduleParamDef", noteParam.Value);
    }

    [Fact]
    public async Task ScheduleParameterStringValues_UnwrapLegacyDoubleBracePlaceholders()
    {
        var definitionId = Guid.NewGuid();
        var schedule = BuildSchedule(
            definitionId, new JobScheduleParameterRes(Guid.NewGuid(), Guid.NewGuid(), "Note", LyoTypeInfo.String.FullName, "Run of {{Definition.Name}}", null, null, true));

        var definition = BuildDefinition(definitionId, schedule);
        var runReq = await BuildRunRequestAsync(definition, schedule);
        var noteParam = Assert.Single(runReq.JobRunParameters, p => p.Key == "Note");
        Assert.Equal("Run of ScheduleParamDef", noteParam.Value);
    }

    [Fact]
    public async Task ScheduleParameterStringValues_LeaveUnresolvedClientTokens()
    {
        var definitionId = Guid.NewGuid();
        var schedule = BuildSchedule(
            definitionId,
            new JobScheduleParameterRes(Guid.NewGuid(), Guid.NewGuid(), "EmailTo", LyoTypeInfo.String.FullName, "{client.contact.emailAddress}", null, null, true));

        var definition = BuildDefinition(definitionId, schedule);
        var runReq = await BuildRunRequestAsync(definition, schedule);
        var emailParam = Assert.Single(runReq.JobRunParameters, p => p.Key == "EmailTo");
        Assert.Equal("{client.contact.emailAddress}", emailParam.Value);
    }

    [Fact]
    public async Task ScheduleParameterJsonValues_AreNotFormatted()
    {
        var definitionId = Guid.NewGuid();
        var json = "{\"name\":\"{Definition.Name}\"}";
        var schedule = BuildSchedule(
            definitionId, new JobScheduleParameterRes(Guid.NewGuid(), Guid.NewGuid(), "Payload", LyoTypeInfo.JsonNode.FullName, json, null, null, true));

        var definition = BuildDefinition(definitionId, schedule);
        var runReq = await BuildRunRequestAsync(definition, schedule);
        var payload = Assert.Single(runReq.JobRunParameters, p => p.Key == "Payload");
        Assert.Equal(json, payload.Value);
    }

    [Fact]
    public async Task ScheduleParameterFormatterValues_AreTemplateFormatted()
    {
        var definitionId = Guid.NewGuid();
        var schedule = BuildSchedule(
            definitionId,
            new JobScheduleParameterRes(
                Guid.NewGuid(), Guid.NewGuid(), "When", FormatterLyoType.Template.FullName, FormatterLyoType.Template.ToJson("{Definition.Name}"), null, null, true));

        var definition = BuildDefinition(definitionId, schedule);
        var runReq = await BuildRunRequestAsync(definition, schedule);
        var when = Assert.Single(runReq.JobRunParameters, p => p.Key == "When");
        Assert.Equal(FormatterLyoType.Template.ToJson("ScheduleParamDef"), when.Value);
    }

    [Fact]
    public async Task ExpressionDefault_ResolvesToAValueOfTheDeclaredType()
    {
        var definitionId = Guid.NewGuid();
        var schedule = BuildSchedule(definitionId);
        var definition = BuildDefinition(definitionId, schedule, YesterdayParameter(definitionId));

        var runReq = await BuildRunRequestAsync(definition, schedule, FrozenClock);
        var asOfDate = Assert.Single(runReq.JobRunParameters, p => p.Key == "AsOfDate");

        // The declared type stays DateTime, so the run parameter has to carry a date the validator accepts instead of the template that produced it.
        Assert.Equal(LyoTypeInfo.DateTime.FullName, asOfDate.Type);
        Assert.Equal("\"2026-08-25\"", asOfDate.Value);
        Assert.Empty(LyoParameterValidator.Validate([LyoParameterSpec.From(definition.JobParameters![0])], [new(asOfDate.Key, asOfDate.Value)]));
    }

    [Fact]
    public async Task ExpressionDefault_IsOverriddenByAScheduleParameter()
    {
        var definitionId = Guid.NewGuid();
        var schedule = BuildSchedule(
            definitionId, new JobScheduleParameterRes(Guid.NewGuid(), Guid.NewGuid(), "AsOfDate", LyoTypeInfo.DateTime.FullName, "2020-01-01", null, null, true));

        var definition = BuildDefinition(definitionId, schedule, YesterdayParameter(definitionId));
        var runReq = await BuildRunRequestAsync(definition, schedule, FrozenClock);
        var asOfDate = Assert.Single(runReq.JobRunParameters, p => p.Key == "AsOfDate");
        Assert.Equal("2020-01-01", asOfDate.Value);
    }

    private static DateTimeOffset FrozenClock() => new(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);

    private static JobParameterRes YesterdayParameter(Guid definitionId)
        => BuildDefinitionParameter(definitionId, "AsOfDate", LyoTypeInfo.DateTime.FullName, null, false) with {
            DefaultKind = LyoParameterDefaultKind.Expression,
            DefaultTemplate = "{DateTime.UtcNow.AddDays(-1):yyyy-MM-dd}"
        };

    /// <summary>Loads the definition into the scheduler cache via the fake API, then invokes the private BuildRunRequest as CreateScheduledRunAsync does for a due slot.</summary>
    private static async Task<JobRunReq> BuildRunRequestAsync(JobDefinitionRes definition, JobScheduleRes schedule, Func<DateTimeOffset>? clock = null)
    {
        var api = new FakeSchedulerApiClient(definition);
        var scheduler = new JobScheduler(
            new() {
                ApiBaseUrl = "http://localhost/api",
                DefinitionRefreshIntervalSeconds = 3600,
                ScheduleCheckIntervalSeconds = 3600,
                EnableMisfireCatchUp = false
            }, api, clock is null ? new FormatterService() : new FormatterService(clock), new FakeEventPublisher());

        await scheduler.RefreshDefinitionsAsync(TestContext.Current.CancellationToken);
        var method = typeof(JobScheduler).GetMethod("BuildRunRequest", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (JobRunReq)method.Invoke(scheduler, [definition.Id, schedule, null, null])!;
    }

    private static JobDefinitionRes BuildDefinition(Guid definitionId, JobScheduleRes schedule, params JobParameterRes[] parameters)
        => new(definitionId, "ScheduleParamDef", null, "Test", "cs", true, parameters, [schedule], [], null);

    private static JobParameterRes BuildDefinitionParameter(Guid definitionId, string key, string type, string? value, bool required)
        => new(Guid.NewGuid(), definitionId, key, null, type, value, null, true, required);

    private static JobScheduleRes BuildSchedule(Guid definitionId, params JobScheduleParameterRes[] parameters)
        => new(Guid.NewGuid(), definitionId, MonthFlags.None, DayFlags.None, ScheduleType.SetTimes, [new(12, 0)], null, null, null, "test schedule", true, parameters);
}