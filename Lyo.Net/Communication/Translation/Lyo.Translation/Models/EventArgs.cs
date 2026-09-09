using System.Diagnostics;

namespace Lyo.Translation.Models;

/// <summary>Raised as a translation is about to begin.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class TranslationTranslatingEventArgs : EventArgs
{
    /// <summary>Request that is about to be translated.</summary>
    public TranslationRequest Request { get; }

    /// <summary>Builds args for a translation that is starting.</summary>
    public TranslationTranslatingEventArgs(TranslationRequest request) => Request = request;

    public override string ToString() => Request.ToString();
}

/// <summary>Raised after a translation finishes.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class TranslationTranslatedEventArgs : EventArgs
{
    /// <summary>Outcome of the finished translation.</summary>
    public TranslationResult Result { get; }

    /// <summary>Builds args for a completed translation.</summary>
    public TranslationTranslatedEventArgs(TranslationResult result) => Result = result;

    public override string ToString() => Result.ToString();
}

/// <summary>Raised as a bulk translation is about to begin.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class TranslationBulkTranslatingEventArgs : EventArgs
{
    /// <summary>Requests included in the bulk run.</summary>
    public IReadOnlyList<TranslationRequest> Requests { get; }

    /// <summary>Builds args for a bulk translation that is starting.</summary>
    public TranslationBulkTranslatingEventArgs(IReadOnlyList<TranslationRequest> requests) => Requests = requests;

    public override string ToString() => $"{Requests.Count} Requests";
}

/// <summary>Raised after a bulk translation finishes.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class TranslationBulkTranslatedEventArgs : EventArgs
{
    /// <summary>Results produced by the bulk run.</summary>
    public IReadOnlyList<TranslationResult> Results { get; }

    /// <summary>Wall time spent on the bulk operation.</summary>
    public TimeSpan ElapsedTime { get; }

    /// <summary>Builds args for a completed bulk translation.</summary>
    public TranslationBulkTranslatedEventArgs(IReadOnlyList<TranslationResult> results, TimeSpan elapsedTime)
    {
        Results = results;
        ElapsedTime = elapsedTime;
    }

    public override string ToString() => $"{Results.Count} Results, Elapsed Time: {ElapsedTime}";
}