using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using Lyo.Exceptions;

namespace Lyo.Http.Client.Extract;

/// <summary>HTML/JSON extract over a response body (AngleSharp). HTTP never loads subresources; this only reads the document you already fetched.</summary>
public static class LyoHttpDocumentExtractor
{
    /// <summary>Extracts <c>src</c>/<c>href</c>/lazy attributes from <paramref name="html" />.</summary>
    public static IReadOnlyList<LyoHttpExtractedItem> ExtractSources(string html, LyoHttpExtractOptions options, Uri? baseUri = null)
        => ExtractAttributes(html, options.Selector ?? "[src],[href],[data-src]", options, baseUri);

    /// <summary>Images: <c>img</c>, <c>source</c>, <c>picture source</c>, SVG <c>image</c>.</summary>
    public static IReadOnlyList<LyoHttpExtractedItem> ExtractImages(string html, LyoHttpExtractOptions? options = null, Uri? baseUri = null)
    {
        options ??= new();
        options.Selector ??= "img, source, picture source, image";
        return ExtractAttributes(html, options.Selector, options, baseUri);
    }

    /// <summary>Links: <c>a[href]</c> with optional extension/host/rel filters.</summary>
    public static IReadOnlyList<LyoHttpExtractedItem> ExtractLinks(string html, LyoHttpExtractOptions? options = null, Uri? baseUri = null)
    {
        options ??= new();
        options.Selector ??= "a[href]";
        options.Attributes = ["href"];
        var items = ExtractAttributes(html, options.Selector, options, baseUri);
        return ApplyLinkFilters(items, options);
    }

    /// <summary>Inner text of matches.</summary>
    public static IReadOnlyList<LyoHttpExtractedItem> ExtractText(string html, LyoHttpExtractOptions options, Uri? baseUri = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.Selector);
        var document = Parse(html, baseUri ?? TryUri(options.LinkResolutionBaseUri));
        var list = new List<LyoHttpExtractedItem>();
        foreach (var el in document.QuerySelectorAll(options.Selector!)) {
            var text = el.TextContent?.Trim();
            if (string.IsNullOrEmpty(text))
                continue;

            list.Add(new() { Text = text, Selector = options.Selector, Raw = text });
        }

        return Dedupe(list, options);
    }

    /// <summary>Inner HTML of matches.</summary>
    public static IReadOnlyList<LyoHttpExtractedItem> ExtractHtml(string html, LyoHttpExtractOptions options, Uri? baseUri = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.Selector);
        var document = Parse(html, baseUri ?? TryUri(options.LinkResolutionBaseUri));
        var list = new List<LyoHttpExtractedItem>();
        foreach (var el in document.QuerySelectorAll(options.Selector!)) {
            var inner = el.InnerHtml;
            if (string.IsNullOrEmpty(inner))
                continue;

            list.Add(new() { Raw = inner, Selector = options.Selector });
        }

        return Dedupe(list, options);
    }

    /// <summary>Named regex groups or a numbered group from <paramref name="input" /> (HTML or any string).</summary>
    public static IReadOnlyList<LyoHttpExtractedItem> ExtractRegex(string input, LyoHttpExtractOptions options)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.Pattern);
        var regex = new Regex(options.Pattern!, RegexOptions.CultureInvariant);
        var list = new List<LyoHttpExtractedItem>();
        foreach (Match match in regex.Matches(input)) {
            string? value = null;
            if (!string.IsNullOrWhiteSpace(options.Group)) {
                if (int.TryParse(options.Group, out var index) && index < match.Groups.Count)
                    value = match.Groups[index].Value;
                else if (match.Groups[options.Group!] != null)
                    value = match.Groups[options.Group!].Value;
            }
            else
                value = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;

            if (string.IsNullOrEmpty(value))
                continue;

            var captured = value;
            list.Add(new() { Raw = captured, Text = captured, Url = LooksLikeUrl(captured) ? captured : null });
        }

        return Dedupe(list, options);
    }

    /// <summary>Open Graph / Twitter / description / canonical.</summary>
    public static IReadOnlyList<LyoHttpExtractedItem> ExtractMeta(string html, LyoHttpExtractOptions? options = null, Uri? baseUri = null)
    {
        options ??= new();
        var document = Parse(html, baseUri ?? TryUri(options.LinkResolutionBaseUri));
        var list = new List<LyoHttpExtractedItem>();
        foreach (var meta in document.QuerySelectorAll("meta[property], meta[name]")) {
            var key = meta.GetAttribute("property") ?? meta.GetAttribute("name");
            var content = meta.GetAttribute("content");
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(content))
                continue;

            var metaKey = key;
            var metaContent = content;
            list.Add(new() {
                Text = metaKey,
                Raw = metaContent,
                Url = Resolve(metaContent, document.DocumentUri, options),
                Attribute = "content",
                AttributeValue = metaContent,
                Selector = "meta",
                Metadata = new() { ["name"] = key }
            });
        }

        var canonical = document.QuerySelector("link[rel=canonical]")?.GetAttribute("href");
        if (!string.IsNullOrWhiteSpace(canonical)) {
            var canonicalHref = canonical;
            list.Add(new() {
                Text = "canonical",
                Raw = canonicalHref,
                Url = Resolve(canonicalHref, document.DocumentUri, options),
                Attribute = "href",
                Selector = "link[rel=canonical]"
            });
        }

        return Dedupe(list, options);
    }

    /// <summary>JSON-LD blocks.</summary>
    public static IReadOnlyList<LyoHttpExtractedItem> ExtractJsonLd(string html, LyoHttpExtractOptions? options = null)
    {
        options ??= new();
        var document = Parse(html, TryUri(options.LinkResolutionBaseUri));
        var list = new List<LyoHttpExtractedItem>();
        foreach (var script in document.QuerySelectorAll("script[type='application/ld+json']")) {
            var raw = script.TextContent;
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            list.Add(new() { Raw = raw, Selector = "script[type=application/ld+json]", Metadata = new() { ["@type"] = TryJsonLdType(raw) } });
            foreach (var url in FindUrlsInJson(raw)) {
                list.Add(new() { Url = url, Raw = url, Selector = "script[type=application/ld+json]" });
            }
        }

        return Dedupe(list, options);
    }

    /// <summary>HTML table rows as maps serialized into <see cref="LyoHttpExtractedItem.Metadata" />.</summary>
    public static IReadOnlyList<LyoHttpExtractedItem> ExtractTable(string html, LyoHttpExtractOptions options, Uri? baseUri = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.Selector);
        var document = Parse(html, baseUri ?? TryUri(options.LinkResolutionBaseUri));
        var list = new List<LyoHttpExtractedItem>();
        foreach (var table in document.QuerySelectorAll(options.Selector!)) {
            var headers = table.QuerySelectorAll("th").Select(th => th.TextContent.Trim()).ToArray();
            foreach (var row in table.QuerySelectorAll("tr")) {
                var cells = row.QuerySelectorAll("td").Select(td => td.TextContent.Trim()).ToArray();
                if (cells.Length == 0)
                    continue;

                var map = new Dictionary<string, object?>();
                for (var i = 0; i < cells.Length; i++) {
                    var key = i < headers.Length && !string.IsNullOrEmpty(headers[i]) ? headers[i] : $"c{i}";
                    map[key] = cells[i];
                }

                list.Add(new() { Selector = options.Selector, Metadata = map, Raw = string.Join(",", cells) });
            }
        }

        return list;
    }

    /// <summary>Reads attributes from matches, including srcset expansion.</summary>
    public static IReadOnlyList<LyoHttpExtractedItem> ExtractAttributes(string html, string selector, LyoHttpExtractOptions options, Uri? baseUri = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(html);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(selector);
        var document = Parse(html, baseUri ?? TryUri(options.LinkResolutionBaseUri));
        var list = new List<LyoHttpExtractedItem>();
        foreach (var el in document.QuerySelectorAll(selector)) {
            foreach (var attr in options.Attributes) {
                var value = el.GetAttribute(attr);
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                var attrValue = value;
                var isSrcset = attr.Contains("srcset", StringComparison.OrdinalIgnoreCase);
                var candidates = isSrcset || options.SplitCommaSeparatedValues ? SplitCandidates(attrValue, isSrcset, options.SrcsetMode) : [attrValue!];
                foreach (var candidate in candidates) {
                    var token = candidate.Trim();
                    if (token.Length == 0)
                        continue;

                    var urlPart = isSrcset ? token.Split([' '], StringSplitOptions.RemoveEmptyEntries)[0] : token;
                    list.Add(new() {
                        Url = Resolve(urlPart, document.DocumentUri, options),
                        Text = el.GetAttribute("alt") ?? el.GetAttribute("title") ?? el.TextContent?.Trim(),
                        Attribute = attr,
                        AttributeValue = attrValue,
                        Selector = selector,
                        Raw = token,
                        Metadata = isSrcset && token.IndexOf(' ') >= 0 ? new() { ["descriptor"] = token[(urlPart.Length)..].Trim() } : null
                    });
                }
            }
        }

        return Dedupe(list, options);
    }

    /// <summary>URL strings for <c>DownloadUrls</c>.</summary>
    public static IReadOnlyList<string> ToUrlList(IEnumerable<LyoHttpExtractedItem> items)
        => items.Select(i => i.Url ?? i.Raw).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!).Distinct(StringComparer.Ordinal).ToArray();

    private static IReadOnlyList<LyoHttpExtractedItem> ApplyLinkFilters(IReadOnlyList<LyoHttpExtractedItem> items, LyoHttpExtractOptions options)
    {
        IEnumerable<LyoHttpExtractedItem> q = items;
        if (options.FileExtensionFilter is { Length: > 0 }) {
            var exts = options.FileExtensionFilter.Select(NormalizeExt).ToArray();
            q = q.Where(i => {
                var url = i.Url ?? i.Raw;
                if (string.IsNullOrEmpty(url))
                    return false;

                var ext = Path.GetExtension(url!.Split('?', '#')[0]);
                return exts.Any(e => string.Equals(e, NormalizeExt(ext), StringComparison.OrdinalIgnoreCase));
            });
        }

        if (options.HostFilter is { Length: > 0 }) {
            q = q.Where(i => {
                if (!Uri.TryCreate(i.Url ?? i.Raw, UriKind.Absolute, out var uri))
                    return false;

                return options.HostFilter.Any(h => uri.Host.Equals(h, StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith("." + h, StringComparison.OrdinalIgnoreCase));
            });
        }

        return q.ToArray();
    }

    private static string NormalizeExt(string ext)
    {
        var t = ext.Trim();
        return t.StartsWith(".", StringComparison.Ordinal) ? t.ToLowerInvariant() : "." + t.ToLowerInvariant();
    }

    private static AngleSharp.Html.Dom.IHtmlDocument Parse(string html, Uri? baseUri)
    {
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);
        if (baseUri != null)
            document.DocumentElement.SetAttribute("data-lyo-base", baseUri.ToString());

        return document;
    }

    private static string? Resolve(string? token, string? documentUri, LyoHttpExtractOptions options)
    {
        if (token == null || !options.ResolveRelativeUrls)
            return token;

        var baseUri = TryUri(options.LinkResolutionBaseUri) ?? TryUri(documentUri);
        if (baseUri != null && Uri.TryCreate(baseUri, token, out var abs))
            return abs.ToString();

        return token;
    }

    private static Uri? TryUri(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : null;

    private static IEnumerable<string> SplitCandidates(string? value, bool isSrcset, LyoHttpSrcsetMode mode)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];
        if (isSrcset) {
            var parts = value!.Split([','], StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).Where(p => p.Length > 0);
            return mode == LyoHttpSrcsetMode.First ? parts.Take(1) : parts;
        }

        return value!.Split([','], StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim());
    }

    private static IReadOnlyList<LyoHttpExtractedItem> Dedupe(List<LyoHttpExtractedItem> list, LyoHttpExtractOptions options)
    {
        if (!options.Deduplicate)
            return list;

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<LyoHttpExtractedItem>();
        foreach (var item in list) {
            var key = item.Url ?? item.Raw ?? item.Text ?? "";
            if (key.Length == 0 || seen.Add(key))
                result.Add(item);
        }

        return result;
    }

    private static bool LooksLikeUrl(string? value)
        => !string.IsNullOrEmpty(value) && (value!.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || value.StartsWith("/"));

    private static string? TryJsonLdType(string raw)
    {
        var idx = raw.IndexOf("\"@type\"", StringComparison.Ordinal);
        if (idx < 0)
            return null;

        var colon = raw.IndexOf(':', idx);
        if (colon < 0)
            return null;

        var quote = raw.IndexOf('"', colon + 1);
        var quote2 = quote >= 0 ? raw.IndexOf('"', quote + 1) : -1;
        return quote >= 0 && quote2 > quote ? raw.Substring(quote + 1, quote2 - quote - 1) : null;
    }

    private static IEnumerable<string> FindUrlsInJson(string raw)
    {
        foreach (Match match in Regex.Matches(raw, "https?://[^\"\\s]+"))
            yield return match.Value.TrimEnd('\\', ',', '}');
    }
}
