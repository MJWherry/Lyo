namespace Lyo.Formatter;

internal enum FormatterTokenKind
{
    Literal,
    SmartFormat,
    Expression
}

internal readonly record struct FormatterToken(FormatterTokenKind Kind, string Raw, string Inner, string? FormatSpec);
