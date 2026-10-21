using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Policy;
using Lyo.Privacy.Text;
using Microsoft.AspNetCore.Components;

namespace Lyo.Privacy.Web.Components;

public partial class TextRedactionPanel
{
    private string _input = string.Empty;

    private string _outputText = string.Empty;

    private RedactionResult? _result;

    [Parameter]
    public RedactionPolicy? Policy { get; set; }

    private void Redact()
    {
        if (Policy is null) {
            _result = null;
            _outputText = string.Empty;
            return;
        }

        var redactor = new TextRedactor(Policy);
        _result = redactor.Redact(_input);
        _outputText = _result.Text ?? string.Empty;
    }
}