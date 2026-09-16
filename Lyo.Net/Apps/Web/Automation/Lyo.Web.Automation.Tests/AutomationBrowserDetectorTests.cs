using Lyo.Web.Automation.Models;

namespace Lyo.Web.Automation.Tests;

public sealed class AutomationBrowserDetectorTests
{
    [Fact]
    public void Detect_EnvVar_WinsOverInstallPath()
    {
        var file = Path.GetTempFileName();
        var previous = Environment.GetEnvironmentVariable("CHROME_PATH");
        try {
            Environment.SetEnvironmentVariable("CHROME_PATH", file);
            Assert.Equal(file, AutomationBrowserDetector.Detect(AutomationBrowserKind.Chrome));
        }
        finally {
            Environment.SetEnvironmentVariable("CHROME_PATH", previous);
            File.Delete(file);
        }
    }

    [Fact]
    public void Detect_Edge_UsesEdgePathEnv()
    {
        var file = Path.GetTempFileName();
        var previous = Environment.GetEnvironmentVariable("EDGE_PATH");
        try {
            Environment.SetEnvironmentVariable("EDGE_PATH", file);
            Assert.Equal(file, AutomationBrowserDetector.Detect(AutomationBrowserKind.Edge));
        }
        finally {
            Environment.SetEnvironmentVariable("EDGE_PATH", previous);
            File.Delete(file);
        }
    }
}
