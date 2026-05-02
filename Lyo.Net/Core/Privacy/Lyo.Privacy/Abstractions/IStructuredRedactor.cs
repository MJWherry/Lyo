namespace Lyo.Privacy.Abstractions;

/// <summary>Redacts JSON by sensitive keys and optional text rules on string values.</summary>
public interface IStructuredRedactor
{
    RedactionResult RedactJson(string? json);
}