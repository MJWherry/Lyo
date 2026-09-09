namespace Lyo.Resilience;

/// <summary>Well-known pipeline names used by Lyo.Resilience.</summary>
public static class PipelineNames
{
    /// <summary>Default pipeline for IResilientExecutor (retry, timeout). Used when the caller omits a pipeline name.</summary>
    public const string Basic = "lyo-basic";

    /// <summary>Default pipeline for HttpClient resilience. Used by AddLyoResilienceHandler() when the caller omits a pipeline name.</summary>
    public const string Http = "lyo-http";
}