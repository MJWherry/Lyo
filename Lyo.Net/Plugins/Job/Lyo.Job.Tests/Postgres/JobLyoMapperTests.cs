using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Records;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Postgres.Database;
using Lyo.Job.Postgres.Mapping;
using Lyo.Parameters;
using Lyo.Query.Models.Enums;
using Lyo.Schedule.Models;
using JobRunResult = Lyo.Job.Models.Enums.JobRunResult;

namespace Lyo.Job.Tests.Postgres;

public class JobLyoMapperTests
{
    private readonly JobLyoMapper _mapper = new();

    [Fact]
    public void Definition_ReqToEntityToRes_RoundTripsNestedGraph()
    {
        var otherDefId = Guid.NewGuid();
        var req = new JobDefinitionReq("Mapped Job", "desc") {
            Type = "Test",
            WorkerType = "cs",
            RetryBackoffType = JobRetryBackoffType.Exponential,
            CreateParameters = [
                new() {
                    Key = "A",
                    Type = LyoTypeInfo.String.FullName,
                    Value = "1",
                    Enabled = true,
                    Required = true
                }
            ],
            CreateSchedules = [
                new() {
                    Type = ScheduleType.Interval,
                    MonthFlags = MonthFlags.EveryMonth,
                    DayFlags = DayFlags.Weekdays,
                    IntervalMinutes = 15,
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(17, 0),
                    Enabled = true,
                    CreateScheduleParameters = [
                        new() {
                            Key = "ClientId",
                            Type = LyoTypeInfo.Guid.FullName,
                            Value = Guid.NewGuid().ToString("D"),
                            Enabled = true
                        }
                    ]
                }
            ],
            CreateTriggers = [
                new() {
                    TriggersJobDefinitionId = otherDefId,
                    JobResultKey = "Result",
                    Comparison = ComparisonOperatorEnum.Equals,
                    JobResultValue = "Success",
                    Enabled = true,
                    CreateTriggerParameters = [
                        new() {
                            Key = "X",
                            Type = LyoTypeInfo.Int.FullName,
                            Value = "2",
                            Enabled = true
                        }
                    ]
                }
            ],
            CreateParallelRestrictions = [new(otherDefId, "no overlap")],
            CreateBlackoutCalendar = new() {
                Name = "Maint",
                CreateBlackoutWindows = [
                    new() {
                        Name = "Night",
                        DayFlags = DayFlags.EveryDay,
                        StartTime = TimeOnly.Parse("02:00"),
                        EndTime = TimeOnly.Parse("04:00")
                    }
                ]
            }
        };

        req.CreateSchedules[0].CreateBlackoutCalendar = req.CreateBlackoutCalendar;
        var entity = _mapper.Map<JobDefinition>(req);
        Assert.NotEqual(default, entity.CreatedTimestamp);
        Assert.Equal(nameof(JobRetryBackoffType.Exponential), entity.RetryBackoffType);
        Assert.Single(entity.JobParameters);
        Assert.NotEqual(default, entity.JobParameters.First().CreatedTimestamp);
        Assert.Single(entity.JobSchedules);
        Assert.NotEqual(default, entity.JobSchedules.First().CreatedTimestamp);
        Assert.NotNull(entity.JobSchedules.First().JobBlackoutCalendar);
        Assert.Single(entity.JobTriggerJobDefinitions);
        Assert.Single(entity.JobTriggerJobDefinitions.First().JobTriggerParameters);
        Assert.Single(entity.JobParallelRestrictionBaseJobDefinitions);
        var res = _mapper.Map<JobDefinitionRes>(entity);
        Assert.Equal("Mapped Job", res.Name);
        Assert.Equal(JobRetryBackoffType.Exponential, res.RetryBackoffType);
        Assert.Single(res.JobParameters!);
        Assert.Single(res.JobSchedules!);
        Assert.NotNull(res.JobSchedules![0].JobBlackoutCalendar);
        Assert.Equal("Maint", res.JobSchedules[0].JobBlackoutCalendar!.Name);
        Assert.Single(res.JobTriggers!);
        Assert.Single(res.JobTriggers![0].TriggerParameters!);
        Assert.Single(res.JobParallelRestrictions!);
    }

    [Fact]
    public void RunResToReq_DoesNotCopyIdempotencyKeyOrScheduledSlot()
    {
        var res = new JobRunRes {
            Id = Guid.NewGuid(),
            JobDefinitionId = Guid.NewGuid(),
            State = JobState.Finished,
            Result = JobRunResult.Success,
            RetryAttempt = 1,
            Priority = 3,
            AllowTriggers = true,
            IdempotencyKey = "retry:abc:1",
            ScheduledSlotUtc = DateTime.UtcNow.AddMinutes(-5)
        };

        var req = _mapper.Map<JobRunReq>(res);

        // Both fields are backed by unique indexes; copying them onto a rerun/child clone silently resolves to the original run
        // (idempotency lookup) or fails the insert (slot constraint).
        Assert.Null(req.IdempotencyKey);
        Assert.Null(req.ScheduledSlotUtc);
        Assert.Equal(res.JobDefinitionId, req.JobDefinitionId);
        Assert.Equal(res.RetryAttempt, req.RetryAttempt);
        Assert.Equal(res.Priority, req.Priority);
    }

    [Fact]
    public void Parameter_ReqToEntityToRes_RoundTripsTheDefaultChannel()
    {
        var req = new JobParameterReq {
            Key = "AsOfDate",
            Type = LyoTypeInfo.DateTime.FullName,
            DefaultKind = LyoParameterDefaultKind.Expression,
            DefaultTemplate = "{DateTime.UtcNow.AddDays(-1):yyyy-MM-dd}"
        };

        var entity = _mapper.Map<JobParameter>(req);
        Assert.Equal(nameof(LyoParameterDefaultKind.Expression), entity.DefaultKind);
        Assert.Equal(req.DefaultTemplate, entity.DefaultTemplate);

        // Apply has to overwrite and also populate, or clearing an expression default on an existing row would leave the template behind.
        _mapper.Map(
            new JobParameterReq {
                Key = "AsOfDate",
                Type = LyoTypeInfo.DateTime.FullName
            }, entity);

        Assert.Equal(nameof(LyoParameterDefaultKind.Literal), entity.DefaultKind);
        Assert.Null(entity.DefaultTemplate);

        entity.DefaultKind = nameof(LyoParameterDefaultKind.Expression);
        entity.DefaultTemplate = req.DefaultTemplate;
        var res = _mapper.Map<JobParameterRes>(entity);
        Assert.Equal(LyoParameterDefaultKind.Expression, res.DefaultKind);
        Assert.Equal(req.DefaultTemplate, res.DefaultTemplate);
    }

    [Fact]
    public void Parameter_UnreadableDefaultKind_ReadsAsLiteral()
    {
        // Rows written before the column existed, or by a client sending garbage, cannot be read as expression-authored.
        var res = _mapper.Map<JobParameterRes>(
            new JobParameter {
                Key = "Code",
                Type = LyoTypeInfo.String.FullName,
                DefaultKind = "nonsense"
            });

        Assert.Equal(LyoParameterDefaultKind.Literal, res.DefaultKind);
    }

    [Fact]
    public void Schedule_StartEndTime_UsesInvariantFormatting()
    {
        var entity = _mapper.Map<JobSchedule>(
            new JobScheduleReq {
                Type = ScheduleType.Interval,
                MonthFlags = MonthFlags.EveryMonth,
                DayFlags = DayFlags.EveryDay,
                StartTime = new TimeOnly(2, 5, 7),
                EndTime = new TimeOnly(14, 30),
                IntervalMinutes = 5
            });

        Assert.Equal("02:05:07", entity.StartTime);
        Assert.Equal("14:30:00", entity.EndTime);
        var res = _mapper.Map<JobScheduleRes>(entity);
        Assert.Equal(new TimeOnly(2, 5, 7), res.StartTime);
        Assert.Equal(new TimeOnly(14, 30), res.EndTime);
    }
}