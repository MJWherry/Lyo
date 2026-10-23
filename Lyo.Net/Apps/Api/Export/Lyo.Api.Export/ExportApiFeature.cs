using System.Diagnostics;
using Lyo.Api.ApiEndpoint;

namespace Lyo.Api.Export;

/// <summary>CRUD feature that turns on export endpoints.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ExportApiFeature : ApiFeature
{
    public static readonly ExportApiFeature Instance = new();

    private ExportApiFeature()
        : base("Export") { }
}