using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Extensions;
using Lyo.Job.Models.Response;

namespace Lyo.Job.Tests;

public class JobRunParameterExtensionsTests
{
    public enum SampleEnum
    {
        Unknown = 0,
        First = 1,
        Second = 2
    }

    private static IReadOnlyList<JobRunParameterRes> Params(params (string Key, string? Value)[] entries)
        => entries.Select(e => new JobRunParameterRes(Guid.NewGuid(), Guid.NewGuid(), e.Key, JobParameterType.String, e.Value, null, null, true)).ToList();

    private static IReadOnlyList<JobRunResultRes> Results(params (string Key, string? Value)[] entries)
        => entries.Select(e => new JobRunResultRes(Guid.NewGuid(), Guid.NewGuid(), e.Key, JobParameterType.String, e.Value)).ToList();

    // ---- GetString / key handling ----

    [Fact]
    public void GetString_MatchesKeyCaseInsensitively() => Assert.Equal("abc", Params(("MyKey", "abc")).GetString("mykey"));

    [Fact]
    public void GetString_AbsentKey_ReturnsNull() => Assert.Null(Params(("A", "1")).GetString("B"));

    [Fact]
    public void GetString_NullList_ReturnsNull() => Assert.Null(((IReadOnlyList<JobRunParameterRes>?)null).GetString("A"));

    // ---- Scalar accessors ----

    [Fact]
    public void GetInt_ParsesAndMisses()
    {
        var parameters = Params(("N", "42"), ("Bad", "abc"));
        Assert.Equal(42, parameters.GetInt("N"));
        Assert.Null(parameters.GetInt("Bad"));
        Assert.Null(parameters.GetInt("Absent"));
    }

    [Fact]
    public void GetLong_ParsesAndMisses()
    {
        var parameters = Params(("N", "9223372036854775807"));
        Assert.Equal(long.MaxValue, parameters.GetLong("N"));
        Assert.Null(parameters.GetLong("Absent"));
    }

    [Fact]
    public void GetDecimal_ParsesAndMisses()
    {
        var parameters = Params(("N", "42.5"));
        Assert.Equal(42.5m, parameters.GetDecimal("N"));
        Assert.Null(parameters.GetDecimal("Absent"));
    }

    [Fact]
    public void GetGuid_ParsesAndMisses()
    {
        var guid = Guid.NewGuid();
        var parameters = Params(("Id", guid.ToString()), ("Bad", "nope"));
        Assert.Equal(guid, parameters.GetGuid("Id"));
        Assert.Null(parameters.GetGuid("Bad"));
        Assert.Null(parameters.GetGuid("Absent"));
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("1", true)]
    [InlineData("yes", true)]
    [InlineData("Y", true)]
    [InlineData("on", true)]
    [InlineData("false", false)]
    [InlineData("0", false)]
    [InlineData("no", false)]
    [InlineData("off", false)]
    public void GetBool_ParsesLenientTokens(string value, bool expected) => Assert.Equal(expected, Params(("B", value)).GetBool("B"));

    [Fact]
    public void GetBool_UnknownOrAbsent_ReturnsNull()
    {
        var parameters = Params(("B", "maybe"));
        Assert.Null(parameters.GetBool("B"));
        Assert.Null(parameters.GetBool("Absent"));
    }

    [Fact]
    public void GetDateTime_RoundtripString_PreservesUtcKind()
    {
        var utc = new DateTime(2026, 7, 26, 10, 0, 0, DateTimeKind.Utc);
        var parameters = Params(("When", utc.ToString("O")));
        var parsed = parameters.GetDateTime("When");
        Assert.Equal(utc, parsed);
        Assert.Equal(DateTimeKind.Utc, parsed!.Value.Kind);
    }

    [Fact]
    public void GetEnum_ParsesCaseInsensitively()
    {
        var parameters = Params(("E", "second"), ("Bad", "bogus"));
        Assert.Equal(SampleEnum.Second, parameters.GetEnum<SampleEnum>("E"));
        Assert.Null(parameters.GetEnum<SampleEnum>("Bad"));
        Assert.Null(parameters.GetEnum<SampleEnum>("Absent"));
    }

    // ---- GetRegex ----

    [Fact]
    public void GetRegex_CompilesPattern()
    {
        var parameters = Params(("Pattern", @"^\d+$"));
        var regex = parameters.GetRegex("Pattern");
        Assert.NotNull(regex);
        Assert.Matches(regex!, "12345");
    }

    [Fact]
    public void GetRegex_InvalidOrAbsent_ReturnsNull()
    {
        var parameters = Params(("Bad", "[unclosed"));
        Assert.Null(parameters.GetRegex("Bad"));
        Assert.Null(parameters.GetRegex("Absent"));
    }

    // ---- GetAs<T> ----

    [Fact]
    public void GetAs_Scalar_ConvertsViaTypeConversion()
    {
        var parameters = Params(("N", "42"));
        Assert.Equal(42, parameters.GetAs<int>("N"));
        Assert.Equal(42L, parameters.GetAs<long?>("N"));
    }

    [Fact]
    public void GetAs_JsonParameter_DeserializesComplexType()
    {
        var parameters = Params(("Payload", """{"Name":"abc","Count":3}"""));
        Assert.Equal(new("abc", 3), parameters.GetAs<SamplePayload>("Payload"));
    }

    [Fact]
    public void GetAs_JsonArrayParameter_DeserializesListType()
    {
        var parameters = Params(("Items", "[1,2,3]"));
        Assert.Equal([1, 2, 3], parameters.GetAs<List<int>>("Items"));
    }

    [Fact]
    public void GetAs_AbsentOrUnconvertible_ReturnsDefault()
    {
        var parameters = Params(("Bad", "abc"));
        Assert.Equal(default, parameters.GetAs<int>("Bad"));
        Assert.Null(parameters.GetAs<SamplePayload>("Absent"));
    }

    [Fact]
    public void GetAs_WithFormat_UsesFormatAwarePath()
    {
        var parameters = Params(("N", "42.567"));
        Assert.Equal(42.57, parameters.GetAs<double>("N", "F2"));
    }

    // ---- Result-side accessors ----

    [Fact]
    public void Results_ScalarAccessors_Work()
    {
        var results = Results(("Count", "7"), ("Ok", "yes"), ("E", "first"));
        Assert.Equal(7, results.GetInt("Count"));
        Assert.True(results.GetBool("Ok"));
        Assert.Equal(SampleEnum.First, results.GetEnum<SampleEnum>("E"));
        Assert.Null(results.GetInt("Absent"));
    }

    [Fact]
    public void Results_GetAs_JsonValue_Deserializes()
    {
        var results = Results(("Payload", """{"Name":"x","Count":1}"""));
        Assert.Equal(new("x", 1), results.GetAs<SamplePayload>("Payload"));
    }

    // ---- JobRunRes delegation ----

    [Fact]
    public void JobRunRes_GetParameterValueAs_DelegatesToExtensions()
    {
        var run = new JobRunRes { Id = Guid.NewGuid(), JobRunParameters = Params(("N", "42"), ("Payload", """{"Name":"abc","Count":3}""")) };
        Assert.Equal(42, run.GetParameterValueAs<int>("n"));
        Assert.Equal(new("abc", 3), run.GetParameterValueAs<SamplePayload>("Payload"));
    }

    [Fact]
    public void JobRunRes_GetResultValueAs_DelegatesToExtensions()
    {
        var run = new JobRunRes { Id = Guid.NewGuid(), JobRunResults = Results(("Result", "Success"), ("CreateCount", "5")) };
        Assert.Equal("Success", run.GetResultValueAs<string?>("Result"));
        Assert.Equal(5, run.GetResultValueAs<int?>("CreateCount"));
    }

    public sealed record SamplePayload(string Name, int Count);
}