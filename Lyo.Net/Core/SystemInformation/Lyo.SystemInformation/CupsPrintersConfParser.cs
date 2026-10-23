namespace Lyo.SystemInformation;

/// <summary>Parses a CUPS <c>printers.conf</c> document. Driver stays null; the default name comes from <c>lpoptions</c>.</summary>
internal static class CupsPrintersConfParser
{
    /// <summary>Reads printer blocks. <paramref name="defaultName" /> marks <see cref="PrinterInfo.IsDefault" /> when it matches a printer name.</summary>
    internal static IReadOnlyList<PrinterInfo> Parse(string? conf, string? defaultName)
    {
        if (conf is null || string.IsNullOrWhiteSpace(conf))
            return [];

        var printers = new List<PrinterInfo>();
        string? name = null;
        string? port = null;
        string? status = null;
        foreach (var raw in conf.Split('\n')) {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                continue;

            if (line.StartsWith("<Printer ", StringComparison.Ordinal) && line.EndsWith(">", StringComparison.Ordinal)) {
                name = line.Substring("<Printer ".Length, line.Length - "<Printer ".Length - 1).Trim();
                port = null;
                status = null;
                continue;
            }

            if (line.Equals("</Printer>", StringComparison.Ordinal)) {
                if (name is { Length: > 0 }) {
                    var isDefault = defaultName is not null && string.Equals(name, defaultName, StringComparison.Ordinal);
                    printers.Add(new(name, null, port, isDefault, status));
                }

                name = null;
                continue;
            }

            if (name is null)
                continue;

            var space = line.IndexOf(' ');
            if (space <= 0)
                continue;

            var key = line.Substring(0, space);
            var value = line.Substring(space + 1).Trim();
            if (key == "DeviceURI")
                port = value;
            else if (key == "State")
                status = value;
        }

        return printers;
    }

    /// <summary>Returns the printer named on the first <c>Default</c> line of an <c>lpoptions</c> file, or null when none is present.</summary>
    internal static string? ReadDefaultName(string? lpoptions)
    {
        if (lpoptions is null || string.IsNullOrWhiteSpace(lpoptions))
            return null;

        foreach (var raw in lpoptions.Split('\n')) {
            var line = raw.Trim();
            if (!line.StartsWith("Default ", StringComparison.Ordinal))
                continue;

            var name = line.Substring("Default ".Length).Trim();
            return name.Length == 0 ? null : name;
        }

        return null;
    }
}