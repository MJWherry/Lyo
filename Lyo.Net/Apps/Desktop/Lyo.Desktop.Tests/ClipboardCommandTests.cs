namespace Lyo.Desktop.Tests;

public class ClipboardCommandTests
{
    [Fact]
    public void TextWrite_LinuxWithWlCopy_UsesWlCopy()
    {
        var tool = Tool(ClipboardCommand.TextWrite(DesktopOs.Linux, Has("wl-copy", "xclip", "xsel")));
        Assert.Equal("wl-copy", tool.FileName);
        Assert.Equal("", tool.Arguments);
    }

    [Fact]
    public void TextWrite_LinuxWithoutWlCopy_UsesXclip()
    {
        var tool = Tool(ClipboardCommand.TextWrite(DesktopOs.Linux, Has("xclip", "xsel")));
        Assert.Equal("xclip", tool.FileName);
        Assert.Equal("-selection clipboard", tool.Arguments);
    }

    [Fact]
    public void TextWrite_LinuxWithOnlyXsel_UsesXsel()
    {
        var tool = Tool(ClipboardCommand.TextWrite(DesktopOs.Linux, Has("xsel")));
        Assert.Equal("xsel", tool.FileName);
        Assert.Equal("--clipboard --input", tool.Arguments);
    }

    [Fact]
    public void TextWrite_Windows_UsesClip()
    {
        var tool = Tool(ClipboardCommand.TextWrite(DesktopOs.Windows, Has("clip.exe")));
        Assert.Equal("clip", tool.FileName);
        Assert.Equal("", tool.Arguments);
    }

    [Fact]
    public void TextWrite_Mac_UsesPbcopy()
    {
        var tool = Tool(ClipboardCommand.TextWrite(DesktopOs.Mac, Has("pbcopy")));
        Assert.Equal("pbcopy", tool.FileName);
        Assert.Equal("", tool.Arguments);
    }

    [Fact]
    public void TextWrite_LinuxWithNoTool_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => ClipboardCommand.TextWrite(DesktopOs.Linux, _ => false));
        Assert.Equal("Clipboard tool not found. Install one of: wl-copy (Wayland), xclip, or xsel.", ex.Message);
    }

    [Fact]
    public void TextWrite_WindowsWithNoTool_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => ClipboardCommand.TextWrite(DesktopOs.Windows, _ => false));
        Assert.Equal("Clipboard tool not found. Expected 'clip.exe' on PATH.", ex.Message);
    }

    [Fact]
    public void TextWrite_MacWithNoTool_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => ClipboardCommand.TextWrite(DesktopOs.Mac, _ => false));
        Assert.Equal("Clipboard tool not found. Expected 'pbcopy' on PATH.", ex.Message);
    }

    [Fact]
    public void TextRead_LinuxWithWlPaste_UsesWlPaste()
    {
        var tool = Tool(ClipboardCommand.TextRead(DesktopOs.Linux, Has("wl-paste", "xclip")));
        Assert.Equal("wl-paste", tool.FileName);
        Assert.Equal("", tool.Arguments);
    }

    [Fact]
    public void TextRead_LinuxWithXclip_UsesClipboardOutput()
    {
        var tool = Tool(ClipboardCommand.TextRead(DesktopOs.Linux, Has("xclip", "xsel")));
        Assert.Equal("xclip", tool.FileName);
        Assert.Equal("-selection clipboard -o", tool.Arguments);
    }

    [Fact]
    public void TextRead_LinuxWithXsel_UsesClipboardOutput()
    {
        var tool = Tool(ClipboardCommand.TextRead(DesktopOs.Linux, Has("xsel")));
        Assert.Equal("xsel", tool.FileName);
        Assert.Equal("--clipboard --output", tool.Arguments);
    }

    [Fact]
    public void TextRead_Mac_UsesPbpaste()
    {
        var tool = Tool(ClipboardCommand.TextRead(DesktopOs.Mac, Has("pbpaste")));
        Assert.Equal("pbpaste", tool.FileName);
        Assert.Equal("", tool.Arguments);
    }

    [Fact]
    public void TextRead_Windows_UsesNativeClipboard() => Assert.IsType<ClipboardRoute.Native>(ClipboardCommand.TextRead(DesktopOs.Windows, _ => false));

    [Fact]
    public void PngWrite_LinuxWayland_PrefersWlCopyImagePng()
    {
        var tool = Tool(ClipboardCommand.PngWrite(DesktopOs.Linux, true, Has("wl-copy", "xclip")));
        Assert.Equal("wl-copy", tool.FileName);
        Assert.Equal("--type image/png", tool.Arguments);
    }

    [Fact]
    public void PngWrite_LinuxWithXclip_SetsImagePng()
    {
        var tool = Tool(ClipboardCommand.PngWrite(DesktopOs.Linux, false, Has("xclip", "wl-copy")));
        Assert.Equal("xclip", tool.FileName);
        Assert.Equal("-selection clipboard -t image/png", tool.Arguments);
    }

    [Fact]
    public void PngWrite_LinuxWithOnlyWlCopy_SetsImagePng()
    {
        var tool = Tool(ClipboardCommand.PngWrite(DesktopOs.Linux, false, Has("wl-copy")));
        Assert.Equal("wl-copy", tool.FileName);
        Assert.Equal("--type image/png", tool.Arguments);
    }

    [Fact]
    public void PngWrite_Windows_UsesNativeBitmap() => Assert.IsType<ClipboardRoute.Native>(ClipboardCommand.PngWrite(DesktopOs.Windows, false, _ => false));

    private static ClipboardRoute.Tool Tool(ClipboardRoute route) => Assert.IsType<ClipboardRoute.Tool>(route);

    private static Func<string, bool> Has(params string[] names) => name => names.Contains(name);
}