using Lyo.Common.Metadata.Records;
using Lyo.Parameters;
using Lyo.Web.Components.ParamTable;

namespace Lyo.Web.Components.Tests;

public class LyoParameterEntryTests
{
    public enum SampleEnum
    {
        First,
        Second
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void From_OptionalEnumWithoutDefault_StartsUnset()
    {
        var entry = Assert.Single(LyoParameterEntry.From([new StubDefinition(typeof(SampleEnum).FullName!, required: false)]));
        Assert.Null(entry.Value);
        Assert.True(entry.IsEmpty());
        Assert.False(entry.ShouldSubmit);
        Assert.True(LyoParameterEntry.RequiredSatisfied([entry]));
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void From_RequiredEnumWithoutDefault_SeedsTypeDefault()
    {
        var entry = Assert.Single(LyoParameterEntry.From([new StubDefinition(typeof(SampleEnum).FullName!, required: true)]));
        Assert.Equal(LyoTypeInfo.Enum.DefaultJson, entry.Value);
        Assert.False(entry.IsEmpty());
        Assert.True(entry.ShouldSubmit);
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void From_OptionalEnumWithLiteralDefault_UsesStoredValue()
    {
        var json = "\"Second\"";
        var entry = Assert.Single(LyoParameterEntry.From([new StubDefinition(typeof(SampleEnum).FullName!, required: false, value: json)]));
        Assert.Equal(json, entry.Value);
        Assert.True(entry.ShouldSubmit);
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void From_OptionalBooleanWithoutDefault_SeedsFalse()
    {
        var entry = Assert.Single(LyoParameterEntry.From([new StubDefinition(LyoTypeInfo.Bool.FullName, required: false)]));
        Assert.Equal(LyoTypeInfo.Bool.DefaultJson, entry.Value);
        Assert.True(entry.ShouldSubmit);
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void RequiredSatisfied_EmptyOptional_DoesNotBlock()
        => Assert.True(LyoParameterEntry.RequiredSatisfied(LyoParameterEntry.From([new StubDefinition(typeof(SampleEnum).FullName!, required: false)])));

    [Fact]
    [Trait("Category", "Fast")]
    public void RequiredSatisfied_EmptyRequired_Blocks()
    {
        var entry = Assert.Single(LyoParameterEntry.From([new StubDefinition(LyoTypeInfo.String.FullName, required: true)]));
        entry.Value = null;
        Assert.False(LyoParameterEntry.RequiredSatisfied([entry]));
    }

    private sealed class StubDefinition(string type, bool required, string? value = null) : ILyoParameterDefinition
    {
        public string Key => "Mode";

        public string? Value => value;

        public string? Description => null;

        public string Type => type;

        public byte[]? EncryptedValue => null;

        public bool Required => required;

        public string? ValidationRegex => null;

        public int? MinLength => null;

        public int? MaxLength => null;

        public string? AllowedValues => null;

        public string? Options => null;

        public LyoParameterDefaultKind DefaultKind => LyoParameterDefaultKind.Literal;

        public string? DefaultTemplate => null;
    }
}
