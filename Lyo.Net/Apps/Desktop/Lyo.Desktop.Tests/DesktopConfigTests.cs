namespace Lyo.Desktop.Tests;

public class DesktopConfigTests
{
    [Fact]
    public void File_Windows_UsesAppData()
    {
        var path = DesktopConfig.File("screenshot.json", windows: true, xdg: "/ignored", appData: @"C:\Users\ada\AppData\Roaming", profile: @"C:\Users\ada");
        Assert.Equal(Path.Combine(@"C:\Users\ada\AppData\Roaming", "Lyo", "screenshot.json"), path);
    }

    [Fact]
    public void File_XdgSet_UsesThatDirectory()
    {
        var path = DesktopConfig.File("file-manager.json", windows: false, xdg: "/home/ada/.config", appData: "", profile: "/home/ada");
        Assert.Equal(Path.Combine("/home/ada/.config", "lyo", "file-manager.json"), path);
    }

    [Fact]
    public void File_XdgMissing_UsesProfileConfig()
    {
        var path = DesktopConfig.File("automation-studio.json", windows: false, xdg: "  ", appData: "", profile: "/home/ada");
        Assert.Equal(Path.Combine("/home/ada", ".config", "lyo", "automation-studio.json"), path);
    }
}
