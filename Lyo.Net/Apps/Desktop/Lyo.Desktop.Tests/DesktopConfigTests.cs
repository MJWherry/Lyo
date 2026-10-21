namespace Lyo.Desktop.Tests;

public class DesktopConfigTests
{
    [Fact]
    public void File_Windows_UsesAppData()
    {
        var path = DesktopConfig.File("screenshot.json", true, "/ignored", @"C:\Users\ada\AppData\Roaming", @"C:\Users\ada");
        Assert.Equal(Path.Combine(@"C:\Users\ada\AppData\Roaming", "Lyo", "screenshot.json"), path);
    }

    [Fact]
    public void File_XdgSet_UsesThatDirectory()
    {
        var path = DesktopConfig.File("file-manager.json", false, "/home/ada/.config", "", "/home/ada");
        Assert.Equal(Path.Combine("/home/ada/.config", "lyo", "file-manager.json"), path);
    }

    [Fact]
    public void File_XdgMissing_UsesProfileConfig()
    {
        var path = DesktopConfig.File("automation-studio.json", false, "  ", "", "/home/ada");
        Assert.Equal(Path.Combine("/home/ada", ".config", "lyo", "automation-studio.json"), path);
    }
}