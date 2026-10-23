using System.Diagnostics;
using Lyo.Desktop.Components;

namespace Lyo.Web.Automation.Studio;

/// <summary>Label and hue for one network chip.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct NetworkChipSpec(string Label, int Hue);

/// <summary>Chips for a captured request: method, status, and resource type.</summary>
public static class NetworkChip
{
    /// <summary>HTTP method. GET is green, POST is blue, and an unknown method is neutral.</summary>
    public static NetworkChipSpec Method(string? method)
    {
        var label = string.IsNullOrWhiteSpace(method) ? "—" : method.Trim().ToUpperInvariant();
        var hue = label switch {
            "GET" => ChipHues.Green,
            "POST" => ChipHues.Blue,
            "PUT" => ChipHues.Amber,
            "PATCH" => ChipHues.Violet,
            "DELETE" => ChipHues.Rose,
            "HEAD" => ChipHues.Cyan,
            var _ => -1
        };

        return new(label, hue);
    }

    /// <summary>Response status. 2xx is green, 4xx is amber, and a missing status is a dash.</summary>
    public static NetworkChipSpec Status(int? status)
    {
        if (status is not int code)
            return new("—", -1);

        var hue = code switch {
            >= 200 and < 300 => ChipHues.Green,
            >= 300 and < 400 => ChipHues.Cyan,
            >= 400 and < 500 => ChipHues.Amber,
            >= 500 => ChipHues.Rose,
            var _ => -1
        };

        return new(code.ToString(), hue);
    }

    /// <summary>Browser resource type, lowercased. XHR and fetch share a hue.</summary>
    public static NetworkChipSpec Type(string? resourceType)
    {
        var label = string.IsNullOrWhiteSpace(resourceType) ? "other" : resourceType.Trim().ToLowerInvariant();
        var hue = label switch {
            "document" => ChipHues.Blue,
            "xhr" or "fetch" => ChipHues.Teal,
            "script" => ChipHues.Indigo,
            "stylesheet" => ChipHues.Purple,
            "image" => ChipHues.Lime,
            "font" => ChipHues.Magenta,
            "media" => ChipHues.Amber,
            var _ => -1
        };

        return new(label, hue);
    }
}

/// <summary>Quick filter for the network list. A blank selection or <c>all</c> keeps every row.</summary>
public static class NetworkTypeFilter
{
    /// <summary>Types offered as quick filters, in list order.</summary>
    public static readonly string[] Types = ["all", "document", "xhr", "fetch", "script", "stylesheet", "image", "font", "media", "other"];

    /// <summary>True when <paramref name="resourceType" /> should stay visible for <paramref name="selected" />. A missing type matches <c>other</c>.</summary>
    public static bool Matches(string? resourceType, string? selected)
    {
        if (string.IsNullOrWhiteSpace(selected) || selected.Trim().Equals("all", StringComparison.OrdinalIgnoreCase))
            return true;

        var type = string.IsNullOrWhiteSpace(resourceType) ? "other" : resourceType.Trim();
        return type.Equals(selected.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}