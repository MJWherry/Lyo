using System.Reflection;

namespace Lyo.Common.Records;

/// <summary>Represents metadata about a programming language, using <see cref="ShortName" /> as the canonical identifier.</summary>
public sealed record ProgrammingLanguageInfo(
    string Name,
    string ShortName,
    string Slug,
    string Description,
    string[] FileExtensions,
    string RuntimeFamily,
    bool IsCompiled,
    bool IsInterpreted,
    bool IsMarkup,
    bool IsStylesheet,
    string[] Aliases)
{
    public static readonly ProgrammingLanguageInfo Unknown = new(
        "Unknown", "unknown", "unknown", "Unknown or unspecified language", [], "Unknown", false, false, false, false, ["unknown", "unspecified"]);

    public static readonly ProgrammingLanguageInfo CSharp = new(
        "C#", "cs", "csharp", "Modern .NET language commonly used for backend services, desktop apps, and game development.", [".cs"], ".NET", true, false, false, false,
        ["c#", "csharp", "dotnet"]);

    public static readonly ProgrammingLanguageInfo FSharp = new(
        "F#", "fs", "fsharp", "Functional-first .NET language used for data-heavy, domain-oriented, and analytical applications.", [".fs", ".fsi", ".fsx"], ".NET", true, false,
        false, false, ["f#", "fsharp"]);

    public static readonly ProgrammingLanguageInfo VisualBasic = new(
        "Visual Basic", "vb", "visual-basic", "Classic .NET language commonly seen in line-of-business applications and legacy enterprise systems.", [".vb"], ".NET", true, false,
        false, false, ["visual basic", "visualbasic", "vb.net"]);

    public static readonly ProgrammingLanguageInfo Java = new(
        "Java", "java", "java", "JVM language commonly used for enterprise systems, Android, and distributed services.", [".java"], "JVM", true, false, false, false, ["java"]);

    public static readonly ProgrammingLanguageInfo Kotlin = new(
        "Kotlin", "kt", "kotlin", "Modern JVM language also used for Android and multiplatform development.", [".kt", ".kts"], "JVM", true, false, false, false, ["kt", "kotlin"]);

    public static readonly ProgrammingLanguageInfo Scala = new(
        "Scala", "scala", "scala", "JVM language blending object-oriented and functional programming styles.", [".scala"], "JVM", true, false, false, false, ["scala"]);

    public static readonly ProgrammingLanguageInfo Groovy = new(
        "Groovy", "groovy", "groovy", "Dynamic JVM language often used for scripting, Gradle builds, and automation.", [".groovy", ".gradle"], "JVM", false, true, false, false,
        ["groovy", "gradle"]);

    public static readonly ProgrammingLanguageInfo Clojure = new(
        "Clojure", "clj", "clojure", "Functional Lisp dialect on the JVM used for data processing and backend systems.", [".clj", ".cljs", ".cljc"], "JVM", false, true, false,
        false, ["clj", "clojure"]);

    public static readonly ProgrammingLanguageInfo Python = new(
        "Python", "py", "python", "General-purpose interpreted language popular for scripting, automation, data work, and web services.", [".py"], "Python", false, true, false,
        false, ["py", "python"]);

    public static readonly ProgrammingLanguageInfo JavaScript = new(
        "JavaScript", "js", "javascript", "Dynamic language used heavily in browsers, Node.js services, and front-end tooling.", [".js", ".mjs", ".cjs"], "JavaScript", false, true,
        false, false, ["js", "javascript", "node"]);

    public static readonly ProgrammingLanguageInfo TypeScript = new(
        "TypeScript", "ts", "typescript", "Typed superset of JavaScript that compiles to JavaScript for browser and server runtimes.", [".ts", ".tsx"], "JavaScript", true, false,
        false, false, ["ts", "typescript"]);

    public static readonly ProgrammingLanguageInfo PHP = new(
        "PHP", "php", "php", "Server-side scripting language commonly used for web applications and content platforms.", [".php"], "PHP", false, true, false, false, ["php"]);

    public static readonly ProgrammingLanguageInfo Ruby = new(
        "Ruby", "rb", "ruby", "Interpreted language known for expressive syntax and web development with Rails.", [".rb"], "Ruby", false, true, false, false, ["rb", "ruby"]);

    public static readonly ProgrammingLanguageInfo Perl = new(
        "Perl", "pl", "perl", "Interpreted language known for text processing, scripting, and automation.", [".pl", ".pm"], "Perl", false, true, false, false, ["pl", "perl"]);

    public static readonly ProgrammingLanguageInfo Lua = new(
        "Lua", "lua", "lua", "Lightweight embeddable scripting language used in games, plugins, and automation.", [".lua"], "Lua", false, true, false, false, ["lua"]);

    public static readonly ProgrammingLanguageInfo Shell = new(
        "Shell", "sh", "shell", "Unix shell scripting language family used for automation and command-line workflows.", [".sh", ".bash", ".zsh"], "Shell", false, true, false,
        false, ["sh", "bash", "shell", "zsh"]);

    public static readonly ProgrammingLanguageInfo PowerShell = new(
        "PowerShell", "ps1", "powershell", "Task automation and configuration language built on .NET and used heavily in Windows and cloud operations.", [".ps1", ".psm1", ".psd1"],
        ".NET", false, true, false, false, ["ps", "ps1", "powershell", "pwsh"]);

    public static readonly ProgrammingLanguageInfo C = new(
        "C", "c", "c", "Compiled low-level language widely used for operating systems, embedded systems, and native libraries.", [".c", ".h"], "Native", true, false, false, false,
        ["c"]);

    public static readonly ProgrammingLanguageInfo Cpp = new(
        "C++", "cpp", "cpp", "Compiled systems language used for performance-sensitive software, engines, and native tooling.", [".cpp", ".cc", ".cxx", ".hpp", ".hh", ".hxx"],
        "Native", true, false, false, false, ["c++", "cpp"]);

    public static readonly ProgrammingLanguageInfo Go = new(
        "Go", "go", "go", "Compiled language designed for simple, concurrent, cloud-oriented services and tooling.", [".go"], "Go", true, false, false, false, ["go", "golang"]);

    public static readonly ProgrammingLanguageInfo Rust = new(
        "Rust", "rs", "rust", "Compiled systems language focused on memory safety, performance, and fearless concurrency.", [".rs"], "Native", true, false, false, false,
        ["rs", "rust"]);

    public static readonly ProgrammingLanguageInfo Swift = new(
        "Swift", "swift", "swift", "Compiled language from Apple used for iOS, macOS, and server-side Swift applications.", [".swift"], "Swift", true, false, false, false,
        ["swift"]);

    public static readonly ProgrammingLanguageInfo Dart = new(
        "Dart", "dart", "dart", "Language from Google used primarily with Flutter and capable of JIT or AOT compilation.", [".dart"], "Dart VM", true, true, false, false,
        ["dart"]);

    public static readonly ProgrammingLanguageInfo Julia = new(
        "Julia", "jl", "julia", "High-performance technical computing language used for numerical analysis and scientific workloads.", [".jl"], "Julia", true, true, false, false,
        ["jl", "julia"]);

    public static readonly ProgrammingLanguageInfo Elixir = new(
        "Elixir", "ex", "elixir", "Functional language on the Erlang VM used for fault-tolerant, concurrent systems.", [".ex", ".exs"], "BEAM", true, false, false, false,
        ["ex", "elixir"]);

    public static readonly ProgrammingLanguageInfo Erlang = new(
        "Erlang", "erl", "erlang", "Concurrent functional language on the BEAM VM used for telecom, messaging, and distributed systems.", [".erl", ".hrl"], "BEAM", true, false,
        false, false, ["erl", "erlang"]);

    public static readonly ProgrammingLanguageInfo Zig = new(
        "Zig", "zig", "zig", "Low-level compiled language focused on simplicity, control, and interoperability with C.", [".zig"], "Native", true, false, false, false, ["zig"]);

    public static readonly ProgrammingLanguageInfo Solidity = new(
        "Solidity", "sol", "solidity", "Contract-oriented language used for Ethereum and EVM-compatible smart contracts.", [".sol"], "EVM", true, false, false, false,
        ["sol", "solidity"]);

    public static readonly ProgrammingLanguageInfo Assembly = new(
        "Assembly", "asm", "assembly", "Low-level symbolic language used for hardware-near programming and platform-specific optimization.", [".asm", ".s"], "Native", true, false,
        false, false, ["asm", "assembly"]);

    public static readonly ProgrammingLanguageInfo Haskell = new(
        "Haskell", "hs", "haskell", "Purely functional compiled language used in academic, research, and correctness-focused domains.", [".hs"], "Native", true, false, false,
        false, ["hs", "haskell"]);

    public static readonly ProgrammingLanguageInfo R = new(
        "R", "r", "r", "Statistical computing language used for analytics, research, and data visualization.", [".r", ".rmd"], "R", false, true, false, false, ["r"]);

    public static readonly ProgrammingLanguageInfo SQL = new(
        "SQL", "sql", "sql", "Declarative query language used to define and manipulate relational data.", [".sql"], "Database", false, true, false, false, ["sql"]);

    public static readonly ProgrammingLanguageInfo Matlab = new(
        "MATLAB", "m", "matlab", "Numerical computing language and environment used in engineering and scientific workflows.", [".m"], "MATLAB", false, true, false, false,
        ["m", "matlab"]);

    public static readonly ProgrammingLanguageInfo HTML = new(
        "HTML", "html", "html", "Markup language used to structure content on the web.", [".html", ".htm"], "Web", false, false, true, false, ["html"]);

    public static readonly ProgrammingLanguageInfo CSS = new(
        "CSS", "css", "css", "Stylesheet language used to define presentation and layout on the web.", [".css"], "Web", false, false, false, true, ["css"]);

    private static readonly Dictionary<string, ProgrammingLanguageInfo> _byName = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, ProgrammingLanguageInfo> _byShortName = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, ProgrammingLanguageInfo> _bySlug = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, ProgrammingLanguageInfo> _byExtension = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, ProgrammingLanguageInfo> _byAlias = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<ProgrammingLanguageInfo> _allLanguages = new();

    public string CanonicalName => ShortName;

    public string? PrimaryExtension => FileExtensions.FirstOrDefault();

    /// <summary>Gets all registered programming language metadata records.</summary>
    public static IReadOnlyList<ProgrammingLanguageInfo> All => _allLanguages;

    static ProgrammingLanguageInfo()
    {
        var fields = typeof(ProgrammingLanguageInfo).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(ProgrammingLanguageInfo))
            .Select(f => (ProgrammingLanguageInfo)f.GetValue(null)!)
            .ToList();

        foreach (var language in fields) {
            _allLanguages.Add(language);
            _byName[Normalize(language.Name)] = language;
            _byShortName[Normalize(language.ShortName)] = language;
            _bySlug[Normalize(language.Slug)] = language;
            _byAlias[Normalize(language.Name)] = language;
            _byAlias[Normalize(language.ShortName)] = language;
            _byAlias[Normalize(language.Slug)] = language;
            foreach (var alias in language.Aliases.Where(a => !string.IsNullOrWhiteSpace(a)))
                _byAlias[Normalize(alias)] = language;

            foreach (var extension in language.FileExtensions.Where(e => !string.IsNullOrWhiteSpace(e)))
                _byExtension[NormalizeExtension(extension)] = language;
        }
    }

    public static ProgrammingLanguageInfo FromName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Unknown;

        var trimmedName = name.Trim();

        // Keep short-name resolution exclusive to FromShortName, even when
        // the lower-cased display name overlaps with the short name (for
        // example "zig" vs "Zig").
        if (_allLanguages.Any(language => string.Equals(language.ShortName, trimmedName, StringComparison.Ordinal)))
            return Unknown;

        return _byName.TryGetValue(Normalize(trimmedName), out var language) ? language : FromAlias(trimmedName);
    }

    public static ProgrammingLanguageInfo FromShortName(string? shortName)
    {
        if (string.IsNullOrWhiteSpace(shortName))
            return Unknown;

        return _byShortName.TryGetValue(Normalize(shortName!), out var language) ? language : Unknown;
    }

    public static ProgrammingLanguageInfo FromSlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return Unknown;

        return _bySlug.TryGetValue(Normalize(slug!), out var language) ? language : Unknown;
    }

    public static ProgrammingLanguageInfo FromAlias(string? alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
            return Unknown;

        return _byAlias.TryGetValue(Normalize(alias!), out var language) ? language : Unknown;
    }

    public static ProgrammingLanguageInfo FromExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return Unknown;

        return _byExtension.TryGetValue(NormalizeExtension(extension!), out var language) ? language : Unknown;
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();

    private static string NormalizeExtension(string value)
    {
        var trimmed = value.Trim();
        return trimmed.StartsWith(".", StringComparison.Ordinal) ? trimmed.ToLowerInvariant() : "." + trimmed.ToLowerInvariant();
    }
}