namespace Lyo.Job.Scheduler.Tests;

/// <summary>
/// The reference instant is the anchor every next-run calculation begins from, and the misfire scan now shares it with due-slot evaluation. These pin the later-of-success-or-slot
/// cursor and the never-run fallback, since anchoring a never-run schedule at <c>DateTime.MinValue</c> would make its whole history look missed.
/// </summary>
public class JobScheduleReferenceTests
{
    private static readonly DateTime Now = new(2026, 7, 7, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Resolve_UsesTheLaterOfLastSuccessAndLastAttemptedSlot()
    {
        var laterSlot = Now.AddMinutes(-10);
        var reference = JobScheduleReference.Resolve(Now.AddHours(-1), laterSlot, Now.AddMinutes(-5), Now.AddMinutes(-3), null, Now, 1440);
        Assert.Equal(laterSlot, reference);
    }

    [Fact]
    public void Resolve_PrefersLastSuccessWhenItIsLaterThanTheAttemptedSlot()
    {
        var laterSuccess = Now.AddMinutes(-2);
        var reference = JobScheduleReference.Resolve(laterSuccess, Now.AddHours(-1), Now.AddMinutes(-5), Now.AddMinutes(-3), null, Now, 1440);
        Assert.Equal(laterSuccess, reference);
    }

    [Fact]
    public void Resolve_FallsBackThroughSlotThenStartThenCreated()
    {
        Assert.Equal(Now.AddMinutes(-10), JobScheduleReference.Resolve(null, Now.AddMinutes(-10), Now.AddMinutes(-5), Now.AddMinutes(-3), null, Now, 1440));
        Assert.Equal(Now.AddMinutes(-5), JobScheduleReference.Resolve(null, null, Now.AddMinutes(-5), Now.AddMinutes(-3), null, Now, 1440));
        Assert.Equal(Now.AddMinutes(-3), JobScheduleReference.Resolve(null, null, null, Now.AddMinutes(-3), null, Now, 1440));
    }

    [Fact]
    public void Resolve_ForANeverRunSchedule_UsesTheMisfireLookback()
    {
        var reference = JobScheduleReference.Resolve(null, null, null, null, null, Now, 1440);
        Assert.Equal(Now.AddDays(-1), reference);
    }

    [Fact]
    public void Resolve_ForANeverRunSchedule_PrefersAStartDateInsideTheLookback()
    {
        var start = Now.AddHours(-2);
        Assert.Equal(start, JobScheduleReference.Resolve(null, null, null, null, start, Now, 1440));
    }

    [Fact]
    public void Resolve_ForANeverRunSchedule_IgnoresAStartDateOlderThanTheLookback()
    {
        var reference = JobScheduleReference.Resolve(null, null, null, null, Now.AddYears(-5), Now, 1440);
        Assert.Equal(Now.AddDays(-1), reference);
    }

    [Fact]
    public void Resolve_TreatsANegativeLookbackAsZero()
    {
        Assert.Equal(Now, JobScheduleReference.Resolve(null, null, null, null, null, Now, -60));
    }
}
