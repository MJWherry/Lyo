using System.Net.Http.Json;
using Lyo.Privacy.Abstractions;
using Lyo.Privacy.Json;
using Lyo.Privacy.Policy;
using Lyo.Privacy.Text;
using Lyo.Web.Primitives;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Privacy.Web.Components;

public partial class JsonRedactionPanel
{
        [Parameter]
        public RedactionPolicy? Policy { get; set; }

        [Parameter]
        public string? PolicyNameLabel { get; set; }

        private string _input = @"{
      ""user"": ""alice@example.com"",
      ""password"": ""secret123"",
      ""note"": ""Call +1-555-123-4567""
    }";

        private string _outputText = string.Empty;

        private bool _applyTextToStrings;

        private RedactionResult? _result;

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
