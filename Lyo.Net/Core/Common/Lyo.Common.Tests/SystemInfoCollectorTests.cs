using Lyo.Common.SystemInformation;
using Microsoft.Extensions.Logging;

namespace Lyo.Common.Tests;

public class SystemInfoCollectorTests
{
    [Fact]
    public void Collect_PopulatesAllSections()
    {
        var info = SystemInfoCollector.Collect();
        Assert.NotNull(info.Hardware);
        Assert.NotNull(info.Software);
        Assert.NotNull(info.Network);
        Assert.NotNull(info.Environment);
        Assert.True(info.CollectedAtUtc > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void GetHardwareInfo_ReturnsSaneValues()
    {
        var hardware = SystemInfoCollector.GetHardwareInfo();
        Assert.True(hardware.ProcessorCount > 0);
        Assert.False(string.IsNullOrEmpty(hardware.ProcessArchitecture));
        Assert.False(string.IsNullOrEmpty(hardware.OsArchitecture));
        Assert.NotNull(hardware.Drives);
        Assert.NotNull(hardware.Monitors);
        Assert.All(hardware.Monitors, monitor => Assert.False(string.IsNullOrEmpty(monitor.Connector)));
    }

    [Fact]
    public void GetSoftwareInfo_ReturnsSaneValues()
    {
        var software = SystemInfoCollector.GetSoftwareInfo();
        Assert.False(string.IsNullOrEmpty(software.OsDescription));
        Assert.False(string.IsNullOrEmpty(software.FrameworkDescription));
        Assert.False(string.IsNullOrEmpty(software.OsVersion));
        Assert.False(string.IsNullOrEmpty(software.ClrVersion));
        Assert.True(software.ProcessId > 0);
        Assert.False(string.IsNullOrEmpty(software.ProcessName));
    }

    [Fact]
    public void GetNetworkInfo_ReturnsSaneValues()
    {
        var network = SystemInfoCollector.GetNetworkInfo();
        Assert.False(string.IsNullOrEmpty(network.HostName));
        Assert.NotNull(network.Interfaces);
        Assert.All(
            network.Interfaces, nic => {
                Assert.NotNull(nic.UnicastAddresses);
                Assert.NotNull(nic.GatewayAddresses);
                Assert.NotNull(nic.DnsAddresses);
            });
    }

    [Fact]
    public void GetEnvironmentInfo_ReturnsSaneValues()
    {
        var environment = SystemInfoCollector.GetEnvironmentInfo();
        Assert.False(string.IsNullOrEmpty(environment.MachineName));
        Assert.False(string.IsNullOrEmpty(environment.TimeZoneId));
        Assert.True(environment.SystemUptime > TimeSpan.Zero);
        Assert.NotNull(environment.Variables);
        Assert.NotEmpty(environment.Variables);
    }

    [Fact]
    public void GetEnvironmentInfo_RedactsSecretLikeVariables()
    {
        Environment.SetEnvironmentVariable("LYO_TEST_SECRET", "super-secret-value");
        Environment.SetEnvironmentVariable("LYO_TEST_PLAIN", "plain-value");
        try {
            var environment = SystemInfoCollector.GetEnvironmentInfo();
            Assert.Equal("********", environment.Variables["LYO_TEST_SECRET"]);
            Assert.Equal("plain-value", environment.Variables["LYO_TEST_PLAIN"]);
        }
        finally {
            Environment.SetEnvironmentVariable("LYO_TEST_SECRET", null);
            Environment.SetEnvironmentVariable("LYO_TEST_PLAIN", null);
        }
    }

    [Theory]
    [InlineData("MY_API_KEY")]
    [InlineData("DB_PASSWORD")]
    [InlineData("AUTH_HEADER")]
    [InlineData("CONNECTION_STRING")]
    [InlineData("some_token")]
    public void GetEnvironmentInfo_RedactsCommonSecretKeyPatterns(string key)
    {
        Environment.SetEnvironmentVariable(key, "sensitive");
        try {
            var environment = SystemInfoCollector.GetEnvironmentInfo();
            Assert.Equal("********", environment.Variables[key]);
        }
        finally {
            Environment.SetEnvironmentVariable(key, null);
        }
    }

    [Fact]
    public void LogSystemInfo_EmitsAllEntriesAtSuppliedLevel()
    {
        var logger = new CapturingLogger(LogLevel.Trace);
        var info = SystemInfoCollector.Collect();
        logger.LogSystemInfo(info, LogLevel.Warning);
        Assert.NotEmpty(logger.Entries);
        Assert.All(logger.Entries, entry => Assert.Equal(LogLevel.Warning, entry.Level));
        Assert.Contains(logger.Entries, entry => entry.Message.StartsWith("Hardware:"));
        Assert.Contains(logger.Entries, entry => entry.Message.StartsWith("Software:"));
        Assert.Contains(logger.Entries, entry => entry.Message.StartsWith("Network:"));
        Assert.Contains(logger.Entries, entry => entry.Message.StartsWith("Environment:"));
    }

    [Fact]
    public void LogSystemInfo_LogsNothingWhenLevelDisabled()
    {
        var logger = new CapturingLogger(LogLevel.Warning);
        var info = SystemInfoCollector.Collect();
        logger.LogSystemInfo(info, LogLevel.Debug);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void LogEnvironmentInfo_IncludesRedactedVariables()
    {
        Environment.SetEnvironmentVariable("LYO_TEST_LOG_SECRET", "should-not-appear");
        try {
            var logger = new CapturingLogger(LogLevel.Trace);
            logger.LogEnvironmentInfo(SystemInfoCollector.GetEnvironmentInfo(), LogLevel.Trace);
            var variablesEntry = Assert.Single(logger.Entries, entry => entry.Message.StartsWith("Environment variables:"));
            Assert.DoesNotContain("should-not-appear", variablesEntry.Message);
            Assert.Contains("LYO_TEST_LOG_SECRET=********", variablesEntry.Message);
        }
        finally {
            Environment.SetEnvironmentVariable("LYO_TEST_LOG_SECRET", null);
        }
    }

    private sealed class CapturingLogger(LogLevel minLevel) : ILogger
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= minLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
                Entries.Add((logLevel, formatter(state, exception)));
        }
    }
}