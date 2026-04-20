using Lyo.Web.WebRenderer.Models;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.WebRenderer;

public interface IWebRendererService
{
    /// <summary>Event fired when a component is rendered to HTML.</summary>
    event EventHandler<ComponentRenderedResult>? ComponentRendered;

    /// <summary>Event fired when a component is rendered to HTML bytes.</summary>
    event EventHandler<ComponentRenderedToBytesResult>? ComponentRenderedToBytes;

    /// <summary>Event fired when a component is saved to a file.</summary>
    event EventHandler<ComponentSavedToFileResult>? ComponentSavedToFile;

    Task<string> RenderToHtmlAsync<T>(Dictionary<string, object>? parameterDictionary = null, CancellationToken ct = default)
        where T : IComponent;

    string RenderToHtml<T>(Dictionary<string, object>? parameterDictionary = null)
        where T : IComponent;

    Task<byte[]> RenderToHtmlBytesAsync<T>(Dictionary<string, object>? parameterDictionary = null, CancellationToken ct = default)
        where T : IComponent;

    byte[] RenderToHtmlBytes<T>(Dictionary<string, object>? parameterDictionary = null)
        where T : IComponent;

    Task RenderToFileAsync<T>(string filePath, Dictionary<string, object>? parameterDictionary = null, CancellationToken ct = default)
        where T : IComponent;

    void RenderToFile<T>(string filePath, Dictionary<string, object>? parameterDictionary = null)
        where T : IComponent;

    void RenderToFile<T, TOptions>(string filePath, TOptions options)
        where T : IComponent where TOptions : class;

    Task RenderToFileAsync<T, TOptions>(string filePath, TOptions options, CancellationToken ct = default)
        where T : IComponent where TOptions : class;

    Task<string> RenderToHtmlAsync<T, TOptions>(TOptions options, CancellationToken ct = default)
        where T : IComponent where TOptions : class;

    string RenderToHtml<T, TOptions>(TOptions options)
        where T : IComponent where TOptions : class;

    Task<byte[]> RenderToHtmlBytesAsync<T, TOptions>(TOptions options, CancellationToken ct = default)
        where T : IComponent where TOptions : class;

    byte[] RenderToHtmlBytes<T, TOptions>(TOptions options)
        where T : IComponent where TOptions : class;

    Task<byte[]> ConvertHtmlToPdfAsync(string htmlContent, CancellationToken ct = default);

    byte[] ConvertHtmlToPdf(string htmlContent);

    Task<byte[]> ConvertHtmlToPdfAsync(byte[] htmlBytes, CancellationToken ct = default);

    byte[] ConvertHtmlToPdf(byte[] htmlBytes);

    Task<byte[]> ConvertHtmlToPdfFromFileAsync(string htmlFilePath, CancellationToken ct = default);

    byte[] ConvertHtmlToPdfFromFile(string htmlFilePath);

    Task ConvertHtmlToPdfFileAsync(string htmlContent, string pdfFilePath, CancellationToken ct = default);

    void ConvertHtmlToPdfFile(string htmlContent, string pdfFilePath);

    Task ConvertHtmlToPdfFileAsync(byte[] htmlBytes, string pdfFilePath, CancellationToken ct = default);

    void ConvertHtmlToPdfFile(byte[] htmlBytes, string pdfFilePath);

    Task ConvertHtmlFileToPdfFileAsync(string htmlFilePath, string? pdfFilePath = null, CancellationToken ct = default);

    void ConvertHtmlFileToPdfFile(string htmlFilePath, string? pdfFilePath = null);
}