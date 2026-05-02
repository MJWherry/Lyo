using Lyo.Privacy.Policy;

namespace Lyo.Privacy.Abstractions;

/// <summary>Applies <see cref="RedactionPolicy" /> to plain text.</summary>
public interface ITextRedactor
{
    RedactionResult Redact(string? input);

    /// <summary>Same as <see cref="Redact(string?)" /> but accepts a span (copies to string once on .NET Standard 2.0; uses <c>new string(span)</c> on modern targets).</summary>
    RedactionResult Redact(ReadOnlySpan<char> input);
}