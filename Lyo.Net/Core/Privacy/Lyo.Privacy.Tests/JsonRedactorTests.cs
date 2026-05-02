using System.Text.Json;
using Lyo.Privacy.Configuration;
using Lyo.Privacy.Enums;
using Lyo.Privacy.Json;
using Lyo.Privacy.Text;

namespace Lyo.Privacy.Tests;

public sealed class JsonRedactorTests
{
    [Fact]
    public void RedactJson_hashes_password_and_placeholders_email()
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
    public void RedactJson_remove_key_omits_property()
    {
        var map = new Dictionary<string, JsonKeyRedactionStrategy>(StringComparer.OrdinalIgnoreCase) { ["ssn"] = JsonKeyRedactionStrategy.Remove };
        var r = new JsonRedactor(new() { SensitiveKeys = map });
        var res = r.RedactJson("""{"ssn":"123"}""");
        using var doc = JsonDocument.Parse(res.Text!);
        Assert.False(doc.RootElement.TryGetProperty("ssn", out var _));
        Assert.Equal(1, res.CountsByKind[RedactionKind.JsonKey]);
    }

    [Fact]
    public void RedactJson_nested_string_applies_text_redactor_when_enabled()
    {
        var text = new TextRedactor(PrivacyPolicies.Logging());
        var r = new JsonRedactor(new() { ApplyTextRulesToAllStringValues = true }, text);
        var res = r.RedactJson("""{"msg":"mail x@y.co ok"}""");
        using var doc = JsonDocument.Parse(res.Text!);
        Assert.DoesNotContain("@", doc.RootElement.GetProperty("msg").GetString());
        Assert.True(res.CountsByKind.GetValueOrDefault(RedactionKind.Email) > 0);
    }

    [Fact]
    public void RedactJson_requires_text_redactor_when_apply_text_rules_enabled()
    {
        var ex = Assert.Throws<ArgumentException>(() => new JsonRedactor(new() { ApplyTextRulesToAllStringValues = true }));
        Assert.Contains("ApplyTextRulesToAllStringValues", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RedactJson_invalid_json_throws_without_fallback_text_redactor()
    {
        var r = new JsonRedactor(new());
        var ex = Assert.Throws<InvalidOperationException>(() => r.RedactJson("{not json"));
        Assert.IsAssignableFrom<JsonException>(ex.InnerException);
    }

    [Fact]
    public void RedactJson_invalid_json_falls_back_to_text_redactor()
    {
        var text = new TextRedactor(PrivacyPolicies.Logging());
        var r = new JsonRedactor(new(), text);
        var raw = "not json a@b.co";
        var res = r.RedactJson(raw);
        Assert.DoesNotContain("@", res.Text);
    }
}