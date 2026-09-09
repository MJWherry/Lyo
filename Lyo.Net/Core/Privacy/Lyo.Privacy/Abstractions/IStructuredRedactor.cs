namespace Lyo.Privacy.Abstractions;

/// <summary>Redacts JSON using sensitive keys and optional text rules on string values.</summary>
public interface IStructuredRedactor
{
    RedactionResult RedactJson(string? json);
}