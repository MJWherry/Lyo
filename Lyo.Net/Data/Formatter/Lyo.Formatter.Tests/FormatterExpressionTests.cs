using System.Globalization;

namespace Lyo.Formatter.Tests;

public class FormatterExpressionTests
{
    private static readonly DateTimeOffset Frozen = new(2026, 8, 22, 16, 0, 0, TimeSpan.Zero);

    private readonly FormatterService _service = new(() => Frozen);

    [Fact]
    public void Format_DateTimeNowAddDays_UsesInjectedClock()
    {
        var expected = Frozen.LocalDateTime.AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var result = _service.Format("{DateTime.Now.AddDays(-1):yyyy-MM-dd}", (object?)null);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Format_UtcNowMinusTimeSpan_UsesInjectedClock()
    {
        var expected = Frozen.UtcDateTime.AddHours(-24).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var result = _service.Format("{DateTime.UtcNow - TimeSpan.FromHours(24):yyyy-MM-dd}", (object?)null);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Format_UtcNowMinusContextTimestamp()
    {
        var ctx = new { lastSuccessJobRun = new { Timestamp = Frozen.AddHours(-6).UtcDateTime } };
        var result = _service.Format("{(DateTime.UtcNow - lastSuccessJobRun.Timestamp).TotalHours}", ctx);
        Assert.Equal("6", result);
    }

    [Fact]
    public void Format_NestedBraceTimestamp_SameAsUnnested()
    {
        var ctx = new { lastSuccessJobRun = new { Timestamp = Frozen.AddHours(-6).UtcDateTime } };
        var nested = _service.Format("{(DateTime.UtcNow - {lastSuccessJobRun.Timestamp}).TotalHours}", ctx);
        var plain = _service.Format("{(DateTime.UtcNow - lastSuccessJobRun.Timestamp).TotalHours}", ctx);
        Assert.Equal(plain, nested);
        Assert.Equal("6", nested);
    }

    [Fact]
    public void Format_TernaryComparison_SelectsBranch()
    {
        var result = _service.Format("{this.amount > 2 ? \"true\" : \"false\"}", new { amount = 5 });
        Assert.Equal("true", result);
        var low = _service.Format("{this.amount > 2 ? \"true\" : \"false\"}", new { amount = 1 });
        Assert.Equal("false", low);
    }

    [Fact]
    public void Format_NestedDictionaryPath_InComparisonAndTernary()
    {
        var ctx = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) {
            ["Order"] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) {
                ["Total"] = 99.50m
            }
        };
        Assert.Equal("True", _service.Format("{Order.Total > 10}", ctx));
        Assert.Equal("0", _service.Format("{Order.Total > 100 ? 1 : 0}", ctx));
        Assert.Equal("True", _service.Format("{(Order.Total > 10 ? true : false)}", ctx));
    }

    [Fact]
    public void Format_NestedDictionary_UserAddressCity()
    {
        var ctx = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) {
            ["User"] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) {
                ["Address"] = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["City"] = "London" }
            }
        };
        Assert.Equal("True", _service.Format("{User.Address.City == \"London\"}", ctx));
    }

    [Fact]
    public void Format_NullCoalesce_UsesFallback()
    {
        var result = _service.Format("{this.nickname ?? this.name}", new { nickname = (string?)null, name = "Ada" });
        Assert.Equal("Ada", result);
    }

    [Fact]
    public void Format_IsNullOrWhiteSpace_UsesFallback()
    {
        var result = _service.Format("{string.IsNullOrWhiteSpace(this.nickname) ? this.name : this.nickname}", new { nickname = "  ", name = "Ada" });
        Assert.Equal("Ada", result);
    }

    [Fact]
    public void Format_StringJoin_SelectsNames()
    {
        var items = new[] { new Item("a", 1, true), new Item("b", 3, true) };
        var result = _service.Format("{string.Join(\", \", this.items.Select(x => x.Name))}", new { items });
        Assert.Equal("a, b", result);
    }

    [Fact]
    public void Format_Indexer_ListAndDictionary()
    {
        var items = new[] { new Item("first", 1, true) };
        var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["key"] = "value" };
        var listResult = _service.Format("{this.items[0].Name}", new { items });
        Assert.Equal("first", listResult);
        var dictResult = _service.Format("{this.map[\"key\"]}", new { map });
        Assert.Equal("value", dictResult);
    }

    [Fact]
    public void Format_ConvertToInt32_FromStringParam()
    {
        var result = _service.Format("{Convert.ToInt32(this.qty) + 1}", new { qty = "42" });
        Assert.Equal("43", result);
    }

    [Fact]
    public void Format_LinqWhereCount()
    {
        var items = new[] { new Item("a", 1, true), new Item("b", 3, true), new Item("c", 5, false) };
        var result = _service.Format("{this.items.Where(x => x.Amount > 2).Count()}", new { items });
        Assert.Equal("2", result);
    }

    [Fact]
    public void Format_ExistingSmartFormat_StillWorks()
    {
        _service.Culture = CultureInfo.InvariantCulture;
        Assert.Equal("1,234", _service.Format("{Count:N0}", new { Count = 1234 }));
        var items = new[] { "a", "b" };
        var listed = _service.Format("{Items:list:{}|, }", new { Items = items });
        Assert.Equal("a, b", listed);
    }

    [Fact]
    public void Format_UnknownMethod_LeavesToken()
    {
        var result = _service.Format("{this.Delete()}", new Deletable());
        Assert.Equal("{this.Delete()}", result);
    }

    [Fact]
    public void TryValidateTemplate_InvalidExpression_ReturnsError()
    {
        var valid = _service.TryValidateTemplate("Hi {amount >}", out var errorMessage);
        Assert.False(valid);
        Assert.False(string.IsNullOrWhiteSpace(errorMessage));
    }

    [Fact]
    public void TryValidateTemplate_DottedContextPath_IsValid()
    {
        Assert.True(_service.TryValidateTemplate("{Order.Total > 6}", out var error));
        Assert.True(string.IsNullOrWhiteSpace(error));
        Assert.True(_service.TryValidateTemplate("{Order.Total > 6 ? \"a\" : \"b\"}", out error));
        Assert.True(string.IsNullOrWhiteSpace(error));
        Assert.True(_service.TryValidateTemplate("{(Order.Total > 10 ? true : false)}", out error));
        Assert.True(string.IsNullOrWhiteSpace(error));
        Assert.True(_service.TryValidateTemplate("{User.Address.City == \"London\"}", out error));
        Assert.True(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void TryValidateTemplate_IncompleteComparison_ReturnsError()
    {
        Assert.False(_service.TryValidateTemplate("{Order.Total >}", out var errorMessage));
        Assert.False(string.IsNullOrWhiteSpace(errorMessage));
        Assert.DoesNotContain("Unknown identifier", errorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetPlaceholders_Expression_ReturnsFieldPaths()
    {
        var placeholders = _service.GetPlaceholders("{this.amount > 2 ? \"true\" : \"false\"}");
        Assert.Contains("amount", placeholders);
        Assert.DoesNotContain(placeholders, p => p.Contains('>') || p.Contains('?'));
    }

    [Fact]
    public void GetPlaceholders_Linq_DropsLambdaParams()
    {
        var placeholders = _service.GetPlaceholders("{this.items.Where(x => x.Amount > 2).Count()}");
        Assert.Contains("items", placeholders);
        Assert.DoesNotContain("x", placeholders);
    }

    [Fact]
    public void Format_FlatDottedDictionaryKey_ResolvesWithoutNesting()
    {
        IReadOnlyDictionary<string, object?> row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["Docket.Number"] = "A-1" };
        Assert.Equal("A-1", _service.Format("{Docket.Number}", row));
    }

    [Fact]
    public void FormatSegments_Ternary_OnePlaceholderSpan()
    {
        var segments = _service.FormatSegments("x={this.amount > 2 ? \"true\" : \"false\"}", new { amount = 5 });
        var placeholder = Assert.Single(segments, s => s.Kind == FormatterSegmentKind.Placeholder);
        Assert.Equal("true", placeholder.Text);
        Assert.Equal("{this.amount > 2 ? \"true\" : \"false\"}", placeholder.RawToken);
    }

    private sealed record Item(string Name, int Amount, bool Active);

    private sealed class Deletable
    {
        public string Delete() => "gone";
    }
}
