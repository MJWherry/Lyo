using Lyo.Drift.Models;
using Lyo.SystemInformation;
using Lyo.Testing;

namespace Lyo.Drift.Tests;

public sealed class SystemInfoDriftProjectionTests
{
    [Fact]
    public void From_Identity_OmitsVolatileFieldsAndEnvVars()
    {
        var info = SampleInfo();
        var projection = SystemInfoDriftProjection.From(info);
        projection.HostName.ShouldBe("host");
        projection.MachineName.ShouldBe("box");
        projection.TotalPhysicalMemoryBytes.ShouldNotBeNull();
        projection.TotalPhysicalMemoryBytes!.Value.ShouldBe(1024L);
        projection.Drives.Select(d => d.Name).ToArray().ShouldBe(["A:", "Z:"]);
        projection.Drives[0].TotalSizeBytes.ShouldBe(100);
        projection.Interfaces.Select(i => i.Name).ToArray().ShouldBe(["eth0", "eth1"]);
        projection.Interfaces[0].MacAddress.ShouldBeNull();
        projection.Interfaces[0].UnicastAddresses.ShouldBeEmpty();
        projection.Variables.ShouldBeEmpty();
        projection.Monitors.ShouldBeEmpty();
        projection.CultureName.ShouldBeNull();
        typeof(SystemInfoDriftProjection).GetProperty("CollectedAtUtc").ShouldBeNull();
        typeof(SystemInfoDriftProjection).GetProperty("ProcessUptime").ShouldBeNull();
        typeof(SystemInfoDriftProjection).GetProperty("SystemUptime").ShouldBeNull();
    }

    [Fact]
    public void From_Identity_IgnoresEnvAndFreeDiskChurn()
    {
        var first = SystemInfoDriftProjection.From(SampleInfo(freeSpace: 10, env: new Dictionary<string, string> { ["A"] = "1" }));
        var second = SystemInfoDriftProjection.From(SampleInfo(freeSpace: 99, env: new Dictionary<string, string> { ["A"] = "2", ["B"] = "3" }));
        first.Drives[0].TotalSizeBytes.ShouldBe(second.Drives[0].TotalSizeBytes);
        first.Variables.ShouldBeEmpty();
        second.Variables.ShouldBeEmpty();
        first.HostName.ShouldBe(second.HostName);
        first.Interfaces[0].UnicastAddresses.ShouldBeEmpty();
    }

    [Fact]
    public void From_Variables_IncludesEnvVars()
    {
        var projection = SystemInfoDriftProjection.From(
            SampleInfo(),
            [..SystemInfoDriftGroups.Identity, SystemInfoDriftGroup.Variables, SystemInfoDriftGroup.InterfaceAddresses]);
        projection.Variables.Select(v => v.Key).ToArray().ShouldBe(["A_VAR", "Z_VAR"]);
        projection.Interfaces[1].UnicastAddresses.ShouldBe(["10.0.0.1", "10.0.0.2"]);
        projection.Interfaces[1].MacAddress.ShouldBe("mac");
    }

    [Fact]
    public void Parse_UnknownGroup_Throws()
        => Assert.Throws<ArgumentException>(() => SystemInfoDriftGroups.Parse(["not-a-group"]));

    [Fact]
    public void Parse_Empty_ReturnsIdentity()
        => SystemInfoDriftGroups.Parse([]).ShouldBe(SystemInfoDriftGroups.Identity);

    private static SystemInfo SampleInfo(long freeSpace = 10, IReadOnlyDictionary<string, string>? env = null)
        => new(
            new HardwareInfo(
                8, "cpu", "X64", "X64", 1024, [
                    new DriveSpaceInfo("Z:", "Fixed", "ext4", 200, freeSpace),
                    new DriveSpaceInfo("A:", "Fixed", "ext4", 100, freeSpace)
                ], []),
            new SoftwareInfo("Linux", "Linux", "1", "net", "linux-x64", "10.0", true, true, false, 1, "proc", DateTime.UtcNow, TimeSpan.FromHours(1)),
            new NetworkInfo("host", true, [
                new NetworkInterfaceInfo("eth1", "d", "Ethernet", "Up", 1, "mac", ["10.0.0.2", "10.0.0.1"], [], []),
                new NetworkInterfaceInfo("eth0", "d", "Ethernet", "Up", 1, "mac0", ["1.1.1.1"], [], [])
            ]),
            new EnvironmentInfo(
                "box", "user", "dom", "/", "", "/tmp", "cmd", "en", "en", "UTC", TimeSpan.Zero, TimeSpan.FromDays(2),
                env ?? new Dictionary<string, string> { ["Z_VAR"] = "z", ["A_VAR"] = "a" }),
            DateTime.UtcNow);
}
