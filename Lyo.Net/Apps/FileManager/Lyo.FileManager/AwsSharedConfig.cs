using System.Diagnostics;

namespace Lyo.FileManager;

/// <summary>One profile from the AWS shared credentials and config files.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record AwsProfile(string Name, string? AccessKeyId, string? SecretAccessKey, string? Region)
{
    /// <summary>Profile name and region. Access key id and secret are omitted.</summary>
    public override string ToString() => $"Name={Name} Region={Region} AccessKeySet={!string.IsNullOrEmpty(AccessKeyId)} SecretSet={!string.IsNullOrEmpty(SecretAccessKey)}";
}

/// <summary>Reads <c>~/.aws/credentials</c> and <c>~/.aws/config</c>. Region from config fills a credentials profile that omitted it.</summary>
public static class AwsSharedConfig
{
    /// <summary>Profiles from the current user's AWS files. A missing file is an empty profile list for that file.</summary>
    public static IReadOnlyList<AwsProfile> LoadDefault()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var root = Path.Combine(home, ".aws");
        return Read(ReadFile(Path.Combine(root, "credentials")), ReadFile(Path.Combine(root, "config")));
    }

    /// <summary>Merges credential text and config text. Config sections named <c>profile foo</c> match the credentials section <c>foo</c>.</summary>
    public static IReadOnlyList<AwsProfile> Read(string? credentialsText, string? configText)
    {
        var credentials = Parse(credentialsText, false);
        var config = Parse(configText, true);
        var names = new List<string>();
        foreach (var name in credentials.Keys.Concat(config.Keys)) {
            if (!names.Contains(name, StringComparer.Ordinal))
                names.Add(name);
        }

        names.Sort(static (left, right) => {
            var leftDefault = left.Equals("default", StringComparison.OrdinalIgnoreCase);
            var rightDefault = right.Equals("default", StringComparison.OrdinalIgnoreCase);
            if (leftDefault != rightDefault)
                return leftDefault ? -1 : 1;

            return string.Compare(left, right, StringComparison.Ordinal);
        });

        var profiles = new List<AwsProfile>(names.Count);
        foreach (var name in names) {
            credentials.TryGetValue(name, out var cred);
            config.TryGetValue(name, out var cfg);
            profiles.Add(new(name, Value(cred, "aws_access_key_id"), Value(cred, "aws_secret_access_key"), Value(cred, "region") ?? Value(cfg, "region")));
        }

        return profiles;
    }

    private static string? ReadFile(string path) => File.Exists(path) ? File.ReadAllText(path) : null;

    private static Dictionary<string, Dictionary<string, string>> Parse(string? text, bool stripProfilePrefix)
    {
        var sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(text))
            return sections;

        Dictionary<string, string>? current = null;
        foreach (var raw in text.Split('\r', '\n')) {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] is '#' or ';')
                continue;

            if (line[0] == '[' && line[^1] == ']') {
                var name = line[1..^1].Trim();
                if (stripProfilePrefix && name.StartsWith("profile ", StringComparison.OrdinalIgnoreCase))
                    name = name["profile ".Length..].Trim();

                if (!sections.TryGetValue(name, out current)) {
                    current = new(StringComparer.OrdinalIgnoreCase);
                    sections[name] = current;
                }

                continue;
            }

            if (current == null)
                continue;

            var eq = line.IndexOf('=');
            if (eq <= 0)
                continue;

            current[line[..eq].Trim()] = line[(eq + 1)..].Trim();
        }

        return sections;
    }

    private static string? Value(Dictionary<string, string>? section, string key)
    {
        if (section == null || !section.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            return null;

        return value;
    }
}