using System.Net.Http.Json;
using Lyo.Diagnostic.Classification;
using Lyo.Web.Primitives;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Diagnostic.Web.Components;

public partial class ExceptionTypeClassifierPanel
{
    private readonly IExceptionClassifier _classifier = new ExceptionClassifier();

    private string _typeName = string.Empty;

    private ClassifiedExceptionResult? _result;

    private void Classify()
    {
        var name = _typeName.Trim();
        _result = string.IsNullOrEmpty(name) ? null : _classifier.ClassifyByTypeName(name);
    }
}
