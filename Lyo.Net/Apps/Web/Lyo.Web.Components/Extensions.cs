using System.Collections;
using System.Text.Json;
using Lyo.Common.Core.Conversion;
using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Records;
using Lyo.Query.Models.Enums;
using Lyo.Web.Components.Models;

namespace Lyo.Web.Components;

internal static class Extensions
{
    public static string GetIcon(FileTypeFlags type)
        => type switch {
            FileTypeFlags.Csv => Icons.Custom.FileFormats.FileCode,
            FileTypeFlags.Txt => Icons.Material.Filled.TextFields,
            FileTypeFlags.Html => Icons.Material.Filled.Web,
            FileTypeFlags.Json => Icons.Custom.FileFormats.FileCode,
            FileTypeFlags.Xml => Icons.Custom.FileFormats.FileCode,
            FileTypeFlags.Js or FileTypeFlags.Graphql or FileTypeFlags.Gql or FileTypeFlags.UrlEncodedForm => Icons.Custom.FileFormats.FileCode,
            FileTypeFlags.Xlsx => Icons.Custom.FileFormats.FileExcel,
            FileTypeFlags.Nupkg or FileTypeFlags.Snupkg or FileTypeFlags.Jar or FileTypeFlags.War or FileTypeFlags.Ear or FileTypeFlags.Aar or FileTypeFlags.Deb
                or FileTypeFlags.Rpm or FileTypeFlags.Msi => Icons.Custom.FileFormats.FileCode,
            FileTypeFlags.Mp4 or FileTypeFlags.Mov or FileTypeFlags.Mkv or FileTypeFlags.Avi or FileTypeFlags.Mpeg or FileTypeFlags.Ts or FileTypeFlags.ThreeGp
                or FileTypeFlags.M3u8 => Icons.Custom.FileFormats.FileVideo,
            var _ => Icons.Material.Filled.Description
        };

    public static List<ComparisonOperatorEnum> GetAvailableComparisonOperators(LyoTypeInfo type)
        => type.Category switch {
            LyoTypeCategory.Text or LyoTypeCategory.Identifier => [
                ComparisonOperatorEnum.Contains, ComparisonOperatorEnum.NotContains, ComparisonOperatorEnum.Equals, ComparisonOperatorEnum.NotEquals,
                ComparisonOperatorEnum.StartsWith, ComparisonOperatorEnum.NotStartsWith, ComparisonOperatorEnum.EndsWith, ComparisonOperatorEnum.NotEndsWith,
                ComparisonOperatorEnum.In, ComparisonOperatorEnum.NotIn
            ],
            LyoTypeCategory.Integer or LyoTypeCategory.Number => [
                ComparisonOperatorEnum.Equals, ComparisonOperatorEnum.NotEquals, ComparisonOperatorEnum.GreaterThan, ComparisonOperatorEnum.GreaterThanOrEqual,
                ComparisonOperatorEnum.LessThan, ComparisonOperatorEnum.LessThanOrEqual, ComparisonOperatorEnum.In, ComparisonOperatorEnum.NotIn
            ],
            LyoTypeCategory.Enum => [ComparisonOperatorEnum.Equals, ComparisonOperatorEnum.NotEquals, ComparisonOperatorEnum.In, ComparisonOperatorEnum.NotIn],
            LyoTypeCategory.Temporal => [
                ComparisonOperatorEnum.Equals, ComparisonOperatorEnum.NotEquals, ComparisonOperatorEnum.GreaterThan, ComparisonOperatorEnum.GreaterThanOrEqual,
                ComparisonOperatorEnum.LessThan, ComparisonOperatorEnum.LessThanOrEqual
            ],
            LyoTypeCategory.Boolean => [ComparisonOperatorEnum.Equals, ComparisonOperatorEnum.NotEquals],
            var _ => Enum.GetValues<ComparisonOperatorEnum>().ToList()
        };

    public static bool IsMultiValueComparisonOperator(this ComparisonOperatorEnum comparison) => comparison is ComparisonOperatorEnum.In or ComparisonOperatorEnum.NotIn;

    /// <summary>
    /// Normalizes a condition <c>In</c>/<c>NotIn</c> value for chip and CSV editors. Handles JSON-deserialized <see cref="JsonElement" /> arrays and non-string enumerables,
    /// not only
    /// <see cref="IEnumerable{String}" />.
    /// </summary>
    public static List<string> ToMultiValueStrings(object? value)
    {
        if (value is null)
            return [];

        if (value is string s)
            return SplitCsv(s);

        if (value is JsonElement je) {
            return je.ValueKind switch {
                JsonValueKind.Array => je.EnumerateArray().Select(FormatJsonElementItem).Where(static x => x.Length > 0).ToList(),
                JsonValueKind.String => SplitCsv(je.GetString()),
                JsonValueKind.Null or JsonValueKind.Undefined => [],
                var _ => [je.ToString()]
            };
        }

        if (value is IEnumerable enumerable and not string and not byte[]) {
            var list = new List<string>();
            foreach (var item in enumerable) {
                var text = item switch {
                    null => null,
                    string str => str,
                    JsonElement el => FormatJsonElementItem(el),
                    var o => o.ToString()
                };

                if (!string.IsNullOrWhiteSpace(text))
                    list.Add(text.Trim());
            }

            return list;
        }

        var fallback = value.ToString();
        return string.IsNullOrWhiteSpace(fallback) ? [] : [fallback.Trim()];
    }

    private static List<string> SplitCsv(string? value)
        => string.IsNullOrWhiteSpace(value) ? [] : value.Split(',').Select(static part => part.Trim()).Where(static part => part.Length > 0).ToList();

    private static string FormatJsonElementItem(JsonElement el)
        => el.ValueKind is JsonValueKind.Array or JsonValueKind.Object or JsonValueKind.Number ? el.ToString() : TypeConversion.FromJsonElement(el)?.ToString() ?? "";
}