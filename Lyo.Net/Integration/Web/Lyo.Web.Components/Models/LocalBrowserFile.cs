namespace Lyo.Web.Components.Models;

/// <summary>File bytes read in the browser after pick (no server upload).</summary>
public sealed record LocalBrowserFile(string FileName, byte[] Content);