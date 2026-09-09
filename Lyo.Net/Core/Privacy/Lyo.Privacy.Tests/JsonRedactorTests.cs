using System.Text.Json;
using Lyo.Exceptions.Models;
using Lyo.Privacy.Configuration;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Json;
using Lyo.Privacy.Text;

namespace Lyo.Privacy.Tests;

public sealed class JsonRedactorTests
{
    [Fact]
    public void RedactJson_Hashes_Password_And_Placeholders_Email()
    {
        var opts = new JsonRedactorOptions { StableHashSalt = [1, 2, 3], ApplyTextRulesToAllStringValues = false };
        var r = new JsonRedactor(opts);
        var json = """
                   {"user":"alice","password":"secret1","email":"a@b.co"}
                   """;

        var res = r.RedactJson(json);
        using var doc = JsonDocument.Parse(res.Text!);
        var root = doc.RootElement;
        Assert.Equal("alice", root.GetProperty("user").GetString());
        Assert.Equal("[redacted]", root.GetProperty("email").GetString());
        var pass = root.GetProperty("password").GetString();
        Assert.NotEqual("secret1", pass);
        Assert.Equal(16, pass!.Length);
        Assert.Equal(2, res.CountsByKind[RedactionKind.JsonKey]);
    }

    [Fact]
    public void RedactJson_Remove_Key_Omits_Property()
    {
        var map = new Dictionary<string, JsonKeyRedactionStrategy>(StringComparer.OrdinalIgnoreCase) { ["ssn"] = JsonKeyRedactionStrategy.Remove };
        var r = new JsonRedactor(new() { SensitiveKeys = map });
        var res = r.RedactJson("""{"ssn":"123"}""");
        using var doc = JsonDocument.Parse(res.Text!);
        Assert.False(doc.RootElement.TryGetProperty("ssn", out var _));
        Assert.Equal(1, res.CountsByKind[RedactionKind.JsonKey]);
    }

    [Fact]
    public void RedactJson_Nested_String_Applies_Text_Redactor_When_Enabled()
    {
        var text = new TextRedactor(PrivacyPolicies.Logging());
        var r = new JsonRedactor(new() { ApplyTextRulesToAllStringValues = true }, text);
        var res = r.RedactJson("""{"msg":"mail x@y.co ok"}""");
        using var doc = JsonDocument.Parse(res.Text!);
        Assert.DoesNotContain("@", doc.RootElement.GetProperty("msg").GetString());
        Assert.True(res.CountsByKind.GetValueOrDefault(RedactionKind.Email) > 0);
    }

    [Fact]
    public void RedactJson_Requires_Text_Redactor_When_Apply_Text_Rules_Enabled()
    {
        var ex = Assert.Throws<ArgumentException>(() => new JsonRedactor(new() { ApplyTextRulesToAllStringValues = true }));
        Assert.Contains("ApplyTextRulesToAllStringValues", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RedactJson_Invalid_Json_Throws_Without_Fallback_Text_Redactor()
    {
        var r = new JsonRedactor(new());
        var ex = Assert.Throws<InvalidFormatException>(() => r.RedactJson("{not json"));
        Assert.IsAssignableFrom<JsonException>(ex.InnerException);
    }

    [Fact]
    public void RedactJson_Invalid_Json_Falls_Back_To_Text_Redactor()
    {
        var text = new TextRedactor(PrivacyPolicies.Logging());
        var r = new JsonRedactor(new(), text);
        var raw = "not json a@b.co";
        var res = r.RedactJson(raw);
        Assert.DoesNotContain("@", res.Text);
    }
}