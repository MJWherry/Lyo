using Lyo.Parameters;

namespace Lyo.Query.Models.Parameters;

/// <summary>Request body for resolving parameter Options into picker items and/or dataset rows.</summary>
public sealed class ParameterOptionsResolveReq
{
    /// <summary>Serialized <see cref="ParameterOptions" /> JSON.</summary>
    public string? OptionsJson { get; set; }

    /// <summary>Sibling parameter key → current value for <c>{{Key}}</c> binding.</summary>
    public Dictionary<string, string?> SiblingValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Picker items and optional raw rows from Query or Sproc Options.</summary>
public sealed class ParameterOptionsResolveRes
{
    /// <summary>Key/label pairs for a picker.</summary>
    public List<ParameterOptionsItem> Items { get; set; } = [];

    /// <summary>Raw row dictionaries (Sproc/Query datasets).</summary>
    public List<Dictionary<string, object?>> Rows { get; set; } = [];

    /// <summary>Fail-closed message when the source could not run.</summary>
    public string? Error { get; set; }
}
