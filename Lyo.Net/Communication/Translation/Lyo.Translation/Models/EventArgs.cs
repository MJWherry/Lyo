using System.Diagnostics;

namespace Lyo.Translation.Models;

/// <summary>Event arguments for when translation starts.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class TranslationTranslatingEventArgs : EventArgs
{
    /// <summary>Gets the translation request.</summary>
    public TranslationRequest Request { get; }

    /// <summary>Initializes a new instance of the TranslationTranslatingEventArgs class.</summary>
    public TranslationTranslatingEventArgs(TranslationRequest request) => Request = request;

    public override string ToString() => Request.ToString();
}

/// <summary>Event arguments for when translation completes.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class TranslationTranslatedEventArgs : EventArgs
{
    /// <summary>Gets the translation result.</summary>
    public TranslationResult Result { get; }

    /// <summary>Initializes a new instance of the TranslationTranslatedEventArgs class.</summary>
    public TranslationTranslatedEventArgs(TranslationResult result) => Result = result;

    public override string ToString() => Result.ToString();
}

/// <summary>Event arguments for when bulk translation starts.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class TranslationBulkTranslatingEventArgs : EventArgs
{
    /// <summary>Gets the collection of translation requests.</summary>
    public IReadOnlyList<TranslationRequest> Requests { get; }

    /// <summary>Initializes a new instance of the TranslationBulkTranslatingEventArgs class.</summary>
    public TranslationBulkTranslatingEventArgs(IReadOnlyList<TranslationRequest> requests) => Requests = requests;

    public override string ToString() => $"{Requests.Count} Requests";
}

/// <summary>Event arguments for when bulk translation completes.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class TranslationBulkTranslatedEventArgs : EventArgs
{
    /// <summary>Gets the collection of translation results.</summary>
    public IReadOnlyList<TranslationResult> Results { get; }

    /// <summary>Gets the elapsed time for the bulk operation.</summary>
    public TimeSpan ElapsedTime { get; }

    /// <summary>Initializes a new instance of the TranslationBulkTranslatedEventArgs class.</summary>
    public TranslationBulkTranslatedEventArgs(IReadOnlyList<TranslationResult> results, TimeSpan elapsedTime)
    {
        Results = results;
        ElapsedTime = elapsedTime;
    }

    public override string ToString() => $"{Results.Count} Results, Elapsed Time: {ElapsedTime}";
}