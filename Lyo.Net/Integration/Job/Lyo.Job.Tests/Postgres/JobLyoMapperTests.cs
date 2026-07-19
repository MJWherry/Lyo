using Lyo.Common.Enums;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Postgres.Database;
using Lyo.Job.Postgres.Mapping;
using Lyo.Query.Models.Enums;
using Lyo.Schedule.Models;

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
                new JobParameterReq { Key = "A", Type = JobParameterType.String, Value = "1", Enabled = true, Required = true }
            ],
            CreateSchedules = [
                new JobScheduleReq {
                    Type = ScheduleType.Interval,
                    MonthFlags = MonthFlags.EveryMonth,
                    DayFlags = DayFlags.Weekdays,
                    IntervalMinutes = 15,
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(17, 0),
                    Enabled = true,
                    CreateScheduleParameters = [
                        new JobScheduleParameterReq { Key = "ClientId", Type = JobParameterType.Guid, Value = Guid.NewGuid().ToString("D"), Enabled = true }
                    ]
                }
            ],
            CreateTriggers = [
                new JobTriggerReq {
                    TriggersJobDefinitionId = otherDefId,
                    JobResultKey = "Result",
                    Comparison = ComparisonOperatorEnum.Equals,
                    JobResultValue = "Success",
                    Enabled = true,
                    CreateTriggerParameters = [
                        new JobTriggerParameterReq { Key = "X", Type = JobParameterType.Int, Value = "2", Enabled = true }
                    ]
                }
            ],
            CreateParallelRestrictions = [
                new JobParallelRestrictionReq(otherDefId, "no overlap", true)
            ],
            CreateBlackoutCalendar = new JobBlackoutCalendarReq {
                Name = "Maint",
                CreateBlackoutWindows = [
                    new() { Name = "Night", DayFlags = DayFlags.EveryDay, StartTime = TimeOnly.Parse("02:00"), EndTime = TimeOnly.Parse("04:00") }
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
    public void Schedule_StartEndTime_UsesInvariantFormatting()
    {
        var entity = _mapper.Map<JobSchedule>(new JobScheduleReq {
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
