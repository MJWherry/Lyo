namespace Lyo.Api.Services.Crud;

/// <summary>Consistent quoting for field/path names in validation messages (matches select-style <c>'field'</c> wording).</summary>
internal static class ValidationFieldFormatter
{
    public static string Quote(string value)
        => $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";
}
