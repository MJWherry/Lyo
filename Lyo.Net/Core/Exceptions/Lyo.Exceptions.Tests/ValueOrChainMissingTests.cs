namespace Lyo.Exceptions.Tests;

public class ValueOrChainMissingTests
{
    [Fact]
    public void IfNull_SubjectNull_ReturnsChosen()
    {
        string? name = null;
        Assert.Equal("missing", name.IfNull("missing").OrDefault("present"));
    }

    [Fact]
    public void IfNotNull_SubjectPresent_ReturnsChosen()
    {
        string? name = "a";
        Assert.Equal("present", name.IfNotNull("present").OrDefault("missing"));
    }

    [Fact]
    public void IfDefault_DefaultDateTime_ReturnsChosen()
    {
        var createdAt = default(DateTime);
        Assert.Equal("unset", createdAt.IfDefault("unset").OrDefault("set"));
    }

    [Fact]
    public void IfNotDefault_SetDateTime_ReturnsChosen()
    {
        var createdAt = new DateTime(2024, 6, 1);
        Assert.Equal("set", createdAt.IfNotDefault("set").OrDefault("unset"));
    }

    [Fact]
    public void IfEmpty_GuidEmpty_ReturnsChosen() => Assert.Equal("empty", Guid.Empty.IfEmpty("empty").OrDefault("set"));

    [Fact]
    public void IfNotEmpty_Guid_ReturnsChosen() => Assert.Equal("set", new Guid("11111111-1111-1111-1111-111111111111").IfNotEmpty("set").OrDefault("empty"));

    [Fact]
    public void IfDefined_NamedMember_ReturnsChosen() => Assert.Equal("ok", DayOfWeek.Friday.IfDefined("ok").OrDefault("bad"));

    [Fact]
    public void IfNotDefined_UndefinedCast_ReturnsChosen()
    {
        var day = (DayOfWeek)99;
        Assert.Equal("bad", day.IfNotDefined("bad").OrDefault("ok"));
    }

    [Fact]
    public void IfNullOrEmpty_Empty_ReturnsChosen() => Assert.Equal("empty", "".IfNullOrEmpty("empty").OrDefault("set"));

    [Fact]
    public void IfNullOrWhiteSpace_Whitespace_ReturnsChosen() => Assert.Equal("blank", " ".IfNullOrWhiteSpace("blank").OrDefault("set"));

    [Fact]
    public void IfLengthInRange_Inside_ReturnsChosen() => Assert.Equal("ok", "ab".IfLengthInRange(1, 3, "ok").OrDefault("no"));

    [Fact]
    public void IfLengthInRange_Outside_ReturnsDefault() => Assert.Equal("no", "abcd".IfLengthInRange(1, 3, "ok").OrDefault("no"));

    [Fact]
    public void OrIfNull_SubjectNull_PicksAlternateResult()
    {
        string? name = null;
        Assert.Equal("alt", name.IfNotNull("present").OrIfNull("alt").OrDefault("fallback"));
    }
}
