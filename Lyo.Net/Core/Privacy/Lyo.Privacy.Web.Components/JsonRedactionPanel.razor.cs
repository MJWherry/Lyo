using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Json;
using Lyo.Privacy.Policy;
using Lyo.Privacy.Text;
using Microsoft.AspNetCore.Components;

namespace Lyo.Privacy.Web.Components;

public partial class JsonRedactionPanel
{
    private bool _applyTextToStrings;

    private string _input = @"{
      ""user"": ""alice@example.com"",
      ""password"": ""secret123"",
      ""note"": ""Call +1-555-123-4567""
    }";

    private string _outputText = string.Empty;

    private RedactionResult? _result;

    [Parameter]
    public RedactionPolicy? Policy { get; set; }

    [Parameter]
    public string? PolicyNameLabel { get; set; }

    private void Redact()
    {
        if (Policy is null) {
            _result = null;
            _outputText = string.Empty;
            return;
        }

        var name = PolicyNameLabel ?? Policy.Name;
        var options = new JsonRedactorOptions { PolicyName = name, ApplyTextRulesToAllStringValues = _applyTextToStrings };
        ITextRedactor? textRedactor = _applyTextToStrings ? new TextRedactor(Policy) : null;
        var redactor = new JsonRedactor(options, textRedactor);
        _result = redactor.RedactJson(_input);
        _outputText = _result.Text ?? string.Empty;
    }
}