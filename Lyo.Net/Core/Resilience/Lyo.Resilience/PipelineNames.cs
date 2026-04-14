namespace Lyo.Resilience;

/// <summary>Standard pipeline names used by Lyo.Resilience.</summary>
public static class PipelineNames
{
    /// <summary>Default pipeline for IResilientExecutor (retry, timeout). Used when no pipeline name is specified.</summary>
    public const string Basic = "lyo-basic";

    /// <summary>Default pipeline for HttpClient resilience. Used by AddLyoResilienceHandler() when no pipeline name is specified.</summary>
    public const string Http = "lyo-http";
}