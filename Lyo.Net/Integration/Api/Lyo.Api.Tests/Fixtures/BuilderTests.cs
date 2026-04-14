using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Api.Models;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Error;
using Lyo.Common.Enums;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;

namespace Lyo.Api.Tests.Fixtures;

public class BuilderTests
{
    [Fact]
    public void LyoProblemDetailsBuilder_Builds_WithMessage()
    {
        var err = LyoProblemDetailsBuilder.CreateWithTrace("t1", "s1").WithErrorCode(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidRequest).WithMessage("bad request").Build();
        Assert.Equal("bad request", err.Detail);
        Assert.Equal(Constants.ApiErrorCodes.InvalidRequest, err.Errors[0].Code);
        Assert.Equal("t1", err.TraceId);
    }

    [Fact]
    public void LyoProblemDetailsBuilder_AddValidation_CollectsErrors()
    {
        var err = LyoProblemDetailsBuilder.CreateWithActivity()
            .WithErrorCode(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidField)
            .AddValidation("FieldA", "bad")
            .AddValidation("FieldB", "worse")
            .Build();

        Assert.NotEmpty(err.Errors);
        Assert.Equal(2, err.Errors.Count);
        Assert.Equal(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidField, err.Errors[0].Code);
        Assert.Contains("FieldA", err.Errors[0].Description);
        Assert.Contains(LyoProblemDetailsBuilder.DefaultValidationDetailSummary, err.Detail);
    }

    [Fact]
    public void LyoProblemDetails_RoundTrips_Json_WithErrorsArray()
    {
        var original = LyoProblemDetailsBuilder.CreateWithActivity()
            .WithErrorCode(Constants.ApiErrorCodes.InvalidQuery)
            .AddApiError(Constants.ApiErrorCodes.InvalidSelectField, "Entity Person does not have field Foo")
            .WithMessage("custom summary")
            .Build();

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, Converters = { new JsonStringEnumConverter() } };
        var json = JsonSerializer.Serialize(original, options);
        Assert.Contains("\"errors\"", json);
        Assert.Contains("\"description\"", json);
        Assert.DoesNotContain("errorCode", json);

        var back = JsonSerializer.Deserialize<LyoProblemDetails>(json, options);
        Assert.NotNull(back);
        Assert.Equal(original.Detail, back!.Detail);
        Assert.NotEmpty(back.Errors);
        Assert.Single(back.Errors);
        Assert.Equal(Lyo.Api.Models.Constants.ApiErrorCodes.InvalidSelectField, back.Errors[0].Code);
    }

    [Fact]
    public void LyoProblemDetails_Json_WithoutErrorsArray_DeserializesWithEmptyErrors()
    {
        const string json = """
            {
              "type": "about:blank",
              "title": "Bad Request",
              "status": 400,
              "detail": "oops",
              "timestamp": "2026-01-01T00:00:00.0000000Z"
            }
            """;
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };
        var back = JsonSerializer.Deserialize<LyoProblemDetails>(json, options);
        Assert.NotNull(back);
        Assert.True(back!.Errors is null or { Count: 0 });
    }

    [Fact]
    public void DeleteRequestBuilder_Builds_WithKey()
    {
        var req = DeleteRequestBuilder.New().WithKey(1).Build();
        Assert.NotNull(req.Keys);
        Assert.Single(req.Keys!);
    }

    [Fact]
    public void DeleteRequestBuilder_Throws_WhenEmpty()
    {
        var b = DeleteRequestBuilder.New();
        Assert.Throws<InvalidOperationException>(() => b.Build());
    }

    [Fact]
    public void PatchRequestBuilder_Builds()
    {
        var p = PatchRequestBuilder.New().WithId(123).SetProperty("Name", "Bob").Build();
        Assert.True(p.Properties.ContainsKey("Name"));
    }

    [Fact]
    public void PatchRequestBuilder_Throws_When_NoProps()
    {
        var b = PatchRequestBuilder.New().WithId(1);
        Assert.Throws<InvalidOperationException>(() => b.Build());
    }

    [Fact]
    public void UpsertRequestBuilder_Builds()
    {
        var data = new { Id = 1, Name = "x" };
        var u = UpsertRequestBuilder<object>.New().WithData(data).WithId(1).Build();
        Assert.NotNull(u.NewData);
    }

    [Fact]
    public void UpsertRequestBuilder_Throws_WithoutData()
    {
        var b = UpsertRequestBuilder<object>.New().WithId(1);
        Assert.Throws<InvalidOperationException>(() => b.Build());
    }

    [Fact]
    public void QueryReqBuilder_ForT_AddsIncludeAndSort()
    {
        var qb = QueryReqBuilder.New().For<Person>().Include(p => p.Name).AddSort(p => p.Name, SortDirection.Asc).Done().Build();
        Assert.Contains("Name", qb.Include);
        Assert.True(qb.SortBy.Any());
    }

    [Fact]
    public void DeleteRequestBuilder_WithIdentifiers_Works()
    {
        var b = DeleteRequestBuilder.New().WithIdentifier("Id", ComparisonOperatorEnum.Equals, 5).AllowMultiple();
        var req = b.Build();
        Assert.NotNull(req.Query);
        Assert.True(req.AllowMultiple);
    }

    [Fact]
    public void PatchRequestBuilder_SetProperties_FromObject()
    {
        var obj = new { Foo = "x", Bar = 2 };
        var b = PatchRequestBuilder.New().WithId(1).SetProperties(obj).SetProperty("Extra", 7);
        var r = b.Build();
        Assert.True(r.Properties.ContainsKey("Foo"));
        Assert.True(r.Properties.ContainsKey("Extra"));
    }

    [Fact]
    public void UpsertRequestBuilder_IgnoredProperties()
    {
        var data = new { Id = 9, Name = "n" };
        var b = UpsertRequestBuilder<object>.New().WithData(data).WithId(9).WithIgnoredProperties("Name");
        var r = b.Build();
        Assert.NotEmpty(r.IgnoredCompareProperties);
    }

    [Fact]
    public void LyoProblemDetailsBuilder_FromException_IncludesInner()
    {
        var ex = new InvalidOperationException("boom", new("inner"));
        var err = LyoProblemDetailsBuilder.FromException(ex, Lyo.Api.Models.Constants.ApiErrorCodes.InvalidOperation).Build();
        Assert.Contains("boom", err.Detail);
        Assert.True(err.Errors.Count >= 2);
    }

    [Fact]
    public void DeleteRequestBuilder_WithKeysAndIdentifiers()
    {
        var d = DeleteRequestBuilder.New().WithKey(1).WithKey(2).WithIdentifier("A", ComparisonOperatorEnum.Equals, 10).WithIdentifier("B", ComparisonOperatorEnum.Equals, 20);
        var ex = Record.Exception(() => d.Build());
        Assert.Null(ex);
        var req = d.Build();
        Assert.NotNull(req.Keys);
        Assert.Equal(2, req.Keys!.Count);
        Assert.NotNull(req.Query);
        var fields = GetConditionFields(req.Query);
        Assert.NotEmpty(fields);
        d.RemoveIdentifier("A");
        var req2 = d.Build();
        Assert.DoesNotContain("A", GetConditionFields(req2.Query), StringComparer.OrdinalIgnoreCase);
        d.ClearIdentifiers();
        var built = d.Build();
        Assert.True(built.Query == null || !GetConditionFields(built.Query).Any());
    }

    private static IEnumerable<string> GetConditionFields(WhereClause? node)
    {
        if (node is null)
            return [];

        if (node is ConditionClause c)
            return [c.Field];

        if (node is GroupClause ln)
            return ln.Children.SelectMany(GetConditionFields);

        return [];
    }

    [Fact]
    public void PatchRequestBuilder_WithIds_AddsKeys()
    {
        var p = PatchRequestBuilder.New().WithId(42).WithIds([1, 2, 3]).SetProperty("X", 1);
        var r = p.Build();
        Assert.NotNull(r.Keys);
        Assert.NotEmpty(r.Keys);
        Assert.True(r.Keys.Count >= 1);
    }

    [Fact]
    public void UpsertRequestBuilder_FactoryHelpers()
    {
        var data = new { Id = 55 };
        var r1 = UpsertRequestBuilder<object>.ForDataWithId(data, 55).Build();
        Assert.NotNull(r1.NewData);
        var r2 = UpsertRequestBuilder<object>.ForDataWithIdentifier(data, "Id", 55).Build();
        Assert.NotNull(r2.NewData);
    }

    [Fact]
    public void QueryReqBuilder_AddIncludes_Multiple()
    {
        var q = QueryReqBuilder.New().AddIncludes("a", "b", "c").Build();
        Assert.True(q.Include.Count >= 3);
    }

    private sealed class Person
    {
        public string Name { get; } = "";
    }
}
