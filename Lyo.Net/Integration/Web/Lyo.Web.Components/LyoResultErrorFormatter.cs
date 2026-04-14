using Lyo.Common;

namespace Lyo.Web.Components;

public static class LyoResultErrorFormatter
{
    public static string FormatErrors(IReadOnlyList<Error>? errors)
        => errors == null || errors.Count == 0 ? "Unknown error." : string.Join(Environment.NewLine, errors.Select(i => $"{i.Code}: {i.Message}"));
}