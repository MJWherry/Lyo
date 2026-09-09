using Lyo.Reporting.Models.Controls;
using Lyo.Reporting.Models.Models;
using Lyo.Reporting.Web;
using Lyo.Reporting.Web.Components;

namespace Lyo.Web.WebRenderer.Tests;

public sealed class ReportEmbedBinderTests
{
    [Fact]
    public void TryResolveComponent_HelloRenderComponent_Succeeds()
    {
        var type = ReportEmbedBinder.TryResolveComponent(typeof(HelloRenderComponent).FullName, out var error);
        Assert.Null(error);
        Assert.Equal(typeof(HelloRenderComponent), type);
    }

    [Fact]
    public void TryResolveComponent_String_IsRejected()
    {
        var type = ReportEmbedBinder.TryResolveComponent(typeof(string).FullName, out var error);
        Assert.Null(type);
        Assert.Contains("IComponent", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Bind_ParamAndLiteral_Resolves()
    {
        var type = typeof(HelloRenderComponent);
        var bindings = new Dictionary<string, ComponentBinding> {
            ["Name"] = new() { Kind = ComponentBindingKind.Param, Value = "Who" }
        };
        var bound = ReportEmbedBinder.Bind(type, bindings, new Dictionary<string, string?> { ["Who"] = "Ada" }, out var error);
        Assert.Null(error);
        Assert.Equal("Ada", bound["Name"]);

        bindings["Name"] = new() { Kind = ComponentBindingKind.Literal, Value = "Lin" };
        bound = ReportEmbedBinder.Bind(type, bindings, new Dictionary<string, string?>(), out error);
        Assert.Null(error);
        Assert.Equal("Lin", bound["Name"]);
    }

    [Fact]
    public async Task RenderToHtmlAsync_MissingBlockType_StillRenders()
    {
        var renderer = WebRendererTestHost.CreateService();
        var report = new Report<object> {
            Title = "Hybrid",
            Sections = [
                new() {
                    Controls = [
                        new Block {
                            ContentType = ContentType.Component,
                            ComponentType = "Lyo.Does.Not.Exist.Invoice"
                        }
                    ]
                }
            ]
        };

        var html = await renderer.RenderToHtmlAsync<ReportViewer<object>>(new Dictionary<string, object> { ["Report"] = report }, TestContext.Current.CancellationToken);
        Assert.Contains("Hybrid", html, StringComparison.Ordinal);
        Assert.Contains("Could not resolve", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Unhandled", html, StringComparison.OrdinalIgnoreCase);
    }
}
