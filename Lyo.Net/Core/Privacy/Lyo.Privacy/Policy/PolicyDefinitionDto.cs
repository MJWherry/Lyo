namespace Lyo.Privacy.Policy;

/// <summary>JSON model for <see cref="PolicyJson" /> (ops-owned policy files). YAML can be converted to JSON externally.</summary>
public sealed class PolicyDefinitionDto
{
    public string? Placeholder { get; set; }

    public string? Name { get; set; }

    public bool? MergeAdjacentRuns { get; set; }

    public List<string>? NeverRedactSubstrings { get; set; }

    public List<PolicyRuleDto>? Rules { get; set; }
}