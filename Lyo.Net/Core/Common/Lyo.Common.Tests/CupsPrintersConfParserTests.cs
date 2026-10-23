using Lyo.SystemInformation;

namespace Lyo.Common.Tests;

public class CupsPrintersConfParserTests
{
    [Fact]
    public void Parse_PrinterBlock_ReadsNamePortAndState()
    {
        const string conf = """
                            <Printer Office>
                            Info Office printer
                            DeviceURI ipp://print.example/ipp
                            State Idle
                            </Printer>
                            """;

        var printer = Assert.Single(CupsPrintersConfParser.Parse(conf, null));
        Assert.Equal("Office", printer.Name);
        Assert.Equal("ipp://print.example/ipp", printer.Port);
        Assert.Equal("Idle", printer.Status);
        Assert.Null(printer.Driver);
        Assert.False(printer.IsDefault);
    }

    [Fact]
    public void Parse_EmptyConf_ReturnsEmpty()
    {
        Assert.Empty(CupsPrintersConfParser.Parse("", null));
        Assert.Empty(CupsPrintersConfParser.Parse(null, null));
    }
}