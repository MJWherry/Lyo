using Microsoft.Extensions.Logging;

namespace Lyo.Desktop.Components.Tests;

public class LogLineDetailTests
{
    private static readonly DateTimeOffset Stamp = new(2026, 10, 7, 14, 30, 0, TimeSpan.Zero);

    [Fact]
    public void From_Error_DecodesContextMessageAndCrashSite()
    {
        const string trace = """
                             System.InvalidOperationException: Navigation failed
                                at Lyo.Web.Automation.Playwright.Go() in Browser.cs:line 12
                                at System.Threading.Tasks.Task.Wait()
                             """;
        var line = new LogLine(
            Stamp,
            LogLevel.Error,
            "Plan failed",
            "studio",
            [new("Url", "https://example.com/search"), new("automation_run_id", "run-1")],
            trace);

        var detail = LogLineDetail.From(line);

        Assert.Equal("Plan failed", detail.Message);
        Assert.Equal("https://example.com/search", Assert.Single(detail.Context, field => field.Name == "Url").Value);
        Assert.Equal("run-1", Assert.Single(detail.Context, field => field.Name == "automation_run_id").Value);
        Assert.Contains("Navigation failed", detail.ExceptionMessage);
        Assert.Contains("Go", detail.CrashSite);
        Assert.Contains("Browser.cs:12", detail.CrashSite);
        Assert.False(string.IsNullOrWhiteSpace(detail.Confidence));
        Assert.False(string.IsNullOrWhiteSpace(detail.Fingerprint));
        Assert.Contains("Go", detail.Frames[0].Method);
        Assert.Equal("Browser.cs:12", detail.Frames[0].Location);
        Assert.Equal(trace, detail.ExceptionText);
    }

    [Fact]
    public void From_Information_HasMessageAndContextWithoutAStack()
    {
        var line = new LogLine(Stamp, LogLevel.Information, "Session started.", "studio", [new("session_id", "abc")]);

        var detail = LogLineDetail.From(line);

        Assert.Equal("Session started.", detail.Message);
        Assert.Equal("abc", Assert.Single(detail.Context).Value);
        Assert.Null(detail.ExceptionText);
        Assert.Null(detail.ExceptionMessage);
        Assert.Null(detail.CrashSite);
        Assert.Empty(detail.Frames);
    }
}
