using System.Linq.Expressions;
using System.Text;
using DynamicExpresso;
using DynamicExpresso.Exceptions;

namespace Lyo.Formatter;

/// <summary>Evaluates C# subset expressions against a <see cref="FormatterContextBag"/> using DynamicExpresso.</summary>
internal sealed class FormatterExpressionEngine
{
    internal const string ThisName = "__this";

    private static readonly HashSet<string> BuiltinIdentifiers = new(StringComparer.OrdinalIgnoreCase) {
        "DateTime", "DateTimeOffset", "TimeSpan", "Math", "Convert", "string", "String", "Enumerable",
        "true", "false", "null", ThisName, "this"
    };

    private static readonly HashSet<string> AllowedMethods = CreateAllowedMethods();

    private static readonly string[] TimeSpanFactories = ["FromDays", "FromHours", "FromMinutes", "FromSeconds", "FromMilliseconds", "FromTicks"];

    private readonly Func<DateTimeOffset> _clock;

    public FormatterExpressionEngine(Func<DateTimeOffset> clock) => _clock = clock;

    /// <summary>
    /// Parses <paramref name="expression"/> without evaluating against live context. Returns an error message when syntax is invalid.
    /// Unknown names (<c>Order</c> in <c>Order.Total &gt; 6</c>) are treated as context, not syntax errors.
    /// </summary>
    public string? TryValidate(string expression)
    {
        try {
            var rewritten = Rewrite(expression, bindClock: false);
            rewritten = RewriteNestedPlaceholders(rewritten);
            var interpreter = CreateInterpreter(lateBindObject: true);
            rewritten = BindUnknownPaths(rewritten, interpreter);
            BindUnknownIdentifiers(rewritten, interpreter);
            interpreter.Parse(rewritten, new Parameter(ThisName, typeof(FormatterContextBag), new FormatterContextBag()));
            return null;
        }
        catch (UnknownIdentifierException) {
            return null;
        }
        catch (ParseException ex) {
            return IsUnknownIdentifier(ex) ? null : ex.Message;
        }
        catch (Exception ex) {
            return IsUnknownIdentifierMessage(ex.Message) ? null : ex.Message;
        }
    }

    /// <summary>Member paths the host must supply (drops types, <c>this</c>, and lambda parameters).</summary>
    public IReadOnlyList<string> GetIdentifiers(string expression)
    {
        try {
            var rewritten = Rewrite(expression, bindClock: false);
            var info = CreateInterpreter().DetectIdentifiers(rewritten);
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in info.UnknownIdentifiers) {
                if (ShouldKeepIdentifier(name))
                    result.Add(name);
            }

            foreach (var id in info.Identifiers) {
                if (ShouldKeepIdentifier(id.Name))
                    result.Add(id.Name);
            }

            AddThisMembers(rewritten, result);
            RemoveLambdaParameters(rewritten, result);
            return result.ToList();
        }
        catch {
            return [];
        }
    }

    /// <summary>Evaluates <paramref name="expression"/>. Returns false when the expression cannot run (unknown method, parse error, missing data).</summary>
    public bool TryEvaluate(string expression, FormatterContextBag context, out object? value, out string? error)
    {
        value = null;
        error = null;
        try {
            var rewritten = Rewrite(expression, bindClock: true);
            rewritten = RewriteNestedPlaceholders(rewritten);
            var interpreter = CreateInterpreter();
            BindClock(interpreter);
            interpreter.SetVariable(ThisName, context.ToExpando());
            foreach (var kv in context) {
                if (!IsValidIdentifier(kv.Key))
                    continue;
                interpreter.SetVariable(kv.Key, kv.Value, context.GetBindingType(kv.Key));
            }

            rewritten = BindResolvedPaths(rewritten, context, interpreter);

            var lambda = interpreter.Parse(rewritten);
            if (!IsAllowed(lambda.Expression, out error))
                return false;

            value = lambda.Invoke();
            return true;
        }
        catch (PlatformNotSupportedException) {
            error = "Expressions are not supported on this platform.";
            return false;
        }
        catch (Exception ex) {
            error = ex.Message;
            return false;
        }
    }

    private Interpreter CreateInterpreter(bool lateBindObject = false)
    {
        var options = InterpreterOptions.DefaultCaseInsensitive | InterpreterOptions.LambdaExpressions;
        if (lateBindObject)
            options |= InterpreterOptions.LateBindObject;
        var interpreter = new Interpreter(options)
            .Reference(typeof(DateTime))
            .Reference(typeof(DateTimeOffset))
            .Reference(typeof(TimeSpan))
            .Reference(typeof(Math))
            .Reference(typeof(Convert))
            .Reference(typeof(string))
            .Reference(typeof(Enumerable));
        return interpreter;
    }

    private void BindClock(Interpreter interpreter)
    {
        var now = _clock();
        interpreter.SetVariable("__now", now.LocalDateTime);
        interpreter.SetVariable("__utcNow", now.UtcDateTime);
        interpreter.SetVariable("__today", now.LocalDateTime.Date);
        interpreter.SetVariable("__dtoNow", now);
        interpreter.SetVariable("__dtoUtcNow", now.ToUniversalTime());
    }

    private static string Rewrite(string expression, bool bindClock)
    {
        var text = RewriteNullCoalesce(expression);
        text = RewriteThis(text);
        text = RewriteTimeSpanNumericArgs(text);
        if (bindClock)
            text = RewriteClock(text);
        return text;
    }

    private static string RewriteNestedPlaceholders(string expression)
    {
        var text = expression;
        while (true) {
            var start = IndexOfNestedPlaceholder(text);
            if (start < 0)
                break;
            var end = text.IndexOf('}', start + 1);
            if (end < 0)
                break;
            var inner = text.Substring(start + 1, end - start - 1).Trim();
            text = text.Substring(0, start) + inner + text.Substring(end + 1);
        }

        return text;
    }

    /// <summary>
    /// <c>Order.Total</c> is a dictionary key or nested member, not a CLR property on <c>Dictionary</c>.
    /// Bind each resolvable dotted path to a typed variable so comparisons see <c>decimal</c> not <c>object</c>.
    /// </summary>
    private static string BindResolvedPaths(string expression, FormatterContextBag context, Interpreter interpreter)
    {
        var paths = FindDottedPaths(expression);
        paths.Sort((a, b) => b.Length.CompareTo(a.Length));
        var text = expression;
        var n = 0;
        foreach (var path in paths) {
            if (!ContainsPath(text, path) || !context.TryResolve(path, out var value))
                continue;
            var name = "__p" + n++;
            interpreter.SetVariable(name, value, value?.GetType() ?? typeof(object));
            text = ReplacePath(text, path, name);
        }

        return text;
    }

    /// <summary>
    /// Validation has no live bag. Replace dotted paths with <c>object</c> dummies (late-bound) so
    /// <c>Order.Total &gt; 6</c> parses, while <c>Order.Total &gt;</c> still fails as incomplete syntax.
    /// </summary>
    private static string BindUnknownPaths(string expression, Interpreter interpreter)
    {
        var lambda = GetLambdaParameters(expression);
        var paths = FindDottedPaths(expression);
        paths.Sort((a, b) => b.Length.CompareTo(a.Length));
        var text = expression;
        var n = 0;
        foreach (var path in paths) {
            var root = path.Split('.')[0];
            if (lambda.Contains(root) || !ContainsPath(text, path))
                continue;
            var name = "__d" + n++;
            interpreter.SetVariable(name, null, typeof(object));
            text = ReplacePath(text, path, name);
        }

        return text;
    }

    private static void BindUnknownIdentifiers(string expression, Interpreter interpreter)
    {
        var info = interpreter.DetectIdentifiers(expression);
        var names = new HashSet<string>(info.UnknownIdentifiers, StringComparer.OrdinalIgnoreCase);
        foreach (var name in GetLambdaParameters(expression))
            names.Remove(name);
        foreach (var name in names) {
            if (!IsValidIdentifier(name) || BuiltinIdentifiers.Contains(name))
                continue;
            interpreter.SetVariable(name, null, typeof(object));
        }
    }

    private static bool IsUnknownIdentifier(ParseException ex)
        => ex is UnknownIdentifierException || IsUnknownIdentifierMessage(ex.Message);

    private static bool IsUnknownIdentifierMessage(string? message)
        => !string.IsNullOrEmpty(message) && message.StartsWith("Unknown identifier", StringComparison.OrdinalIgnoreCase);

    private static List<string> FindDottedPaths(string expression)
    {
        var paths = new List<string>();
        var inString = false;
        var stringChar = '\0';
        var escaped = false;
        for (var i = 0; i < expression.Length;) {
            var c = expression[i];
            if (inString) {
                if (escaped)
                    escaped = false;
                else if (c == '\\')
                    escaped = true;
                else if (c == stringChar)
                    inString = false;
                i++;
                continue;
            }

            if (c is '"' or '\'') {
                inString = true;
                stringChar = c;
                i++;
                continue;
            }

            if (char.IsLetter(c) || c == '_') {
                var start = i;
                i++;
                while (i < expression.Length && (char.IsLetterOrDigit(expression[i]) || expression[i] == '_'))
                    i++;
                var dots = 0;
                while (i < expression.Length && expression[i] == '.') {
                    var afterDot = i + 1;
                    if (afterDot >= expression.Length || !(char.IsLetter(expression[afterDot]) || expression[afterDot] == '_'))
                        break;
                    dots++;
                    i = afterDot + 1;
                    while (i < expression.Length && (char.IsLetterOrDigit(expression[i]) || expression[i] == '_'))
                        i++;
                }

                if (dots == 0)
                    continue;
                var j = i;
                while (j < expression.Length && char.IsWhiteSpace(expression[j]))
                    j++;
                if (j < expression.Length && expression[j] == '(')
                    continue;
                paths.Add(expression.Substring(start, i - start));
                continue;
            }

            i++;
        }

        return paths;
    }

    private static bool ContainsPath(string expression, string path)
    {
        var start = 0;
        while (true) {
            var idx = expression.IndexOf(path, start, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return false;
            var after = idx + path.Length;
            var beforeOk = idx == 0 || !(char.IsLetterOrDigit(expression[idx - 1]) || expression[idx - 1] == '_');
            var afterOk = after >= expression.Length || !(char.IsLetterOrDigit(expression[after]) || expression[after] == '_');
            if (beforeOk && afterOk)
                return true;
            start = idx + 1;
        }
    }

    private static string ReplacePath(string expression, string path, string replacement)
    {
        var builder = new StringBuilder(expression.Length);
        var inString = false;
        var stringChar = '\0';
        var escaped = false;
        for (var i = 0; i < expression.Length;) {
            var c = expression[i];
            if (inString) {
                builder.Append(c);
                if (escaped)
                    escaped = false;
                else if (c == '\\')
                    escaped = true;
                else if (c == stringChar)
                    inString = false;
                i++;
                continue;
            }

            if (c is '"' or '\'') {
                inString = true;
                stringChar = c;
                builder.Append(c);
                i++;
                continue;
            }

            if (MatchPath(expression, i, path)) {
                builder.Append(replacement);
                i += path.Length;
                continue;
            }

            builder.Append(c);
            i++;
        }

        return builder.ToString();
    }

    private static bool MatchPath(string expression, int index, string path)
    {
        if (index + path.Length > expression.Length)
            return false;
        if (index > 0 && (char.IsLetterOrDigit(expression[index - 1]) || expression[index - 1] == '_'))
            return false;
        if (string.Compare(expression, index, path, 0, path.Length, StringComparison.OrdinalIgnoreCase) != 0)
            return false;
        var after = index + path.Length;
        return after >= expression.Length || !(char.IsLetterOrDigit(expression[after]) || expression[after] == '_');
    }

    private static int IndexOfNestedPlaceholder(string text)
    {
        var inString = false;
        var stringChar = '\0';
        var escaped = false;
        for (var i = 0; i < text.Length; i++) {
            var c = text[i];
            if (inString) {
                if (escaped) {
                    escaped = false;
                    continue;
                }

                if (c == '\\') {
                    escaped = true;
                    continue;
                }

                if (c == stringChar)
                    inString = false;
                continue;
            }

            if (c is '"' or '\'') {
                inString = true;
                stringChar = c;
                continue;
            }

            if (c == '{')
                return i;
        }

        return -1;
    }

    private static string RewriteThis(string expression)
    {
        var withoutDot = ReplaceThisDot(expression);
        return ReplaceIdentifier(withoutDot, "this", ThisName);
    }

    private static string ReplaceThisDot(string expression)
    {
        var builder = new StringBuilder(expression.Length);
        var inString = false;
        var stringChar = '\0';
        var escaped = false;
        for (var i = 0; i < expression.Length;) {
            var c = expression[i];
            if (inString) {
                builder.Append(c);
                if (escaped)
                    escaped = false;
                else if (c == '\\')
                    escaped = true;
                else if (c == stringChar)
                    inString = false;
                i++;
                continue;
            }

            if (c is '"' or '\'') {
                inString = true;
                stringChar = c;
                builder.Append(c);
                i++;
                continue;
            }

            if (MatchWord(expression, i, "this") && i + 4 < expression.Length && expression[i + 4] == '.') {
                i += 5;
                continue;
            }

            builder.Append(c);
            i++;
        }

        return builder.ToString();
    }

    /// <summary>
    /// DynamicExpresso's default numeric type is int, which makes <c>TimeSpan.FromHours(24)</c> ambiguous (double vs int overloads).
    /// Suffix integer literals in those factories as doubles. Array indexes stay ints.
    /// </summary>
    private static string RewriteTimeSpanNumericArgs(string expression)
    {
        var text = expression;
        foreach (var name in TimeSpanFactories) {
            var needle = name + "(";
            var start = 0;
            while (true) {
                var idx = IndexOfWord(text, needle, start);
                if (idx < 0)
                    break;
                var argStart = idx + needle.Length;
                var i = argStart;
                while (i < text.Length && char.IsWhiteSpace(text[i]))
                    i++;
                if (i < text.Length && text[i] == '-')
                    i++;
                if (i >= text.Length || !char.IsDigit(text[i])) {
                    start = argStart;
                    continue;
                }

                while (i < text.Length && char.IsDigit(text[i]))
                    i++;
                if (i < text.Length && (text[i] is '.' or 'd' or 'D' or 'm' or 'M' or 'f' or 'F' or 'L')) {
                    start = i;
                    continue;
                }

                var j = i;
                while (j < text.Length && char.IsWhiteSpace(text[j]))
                    j++;
                if (j >= text.Length || text[j] != ')') {
                    start = i;
                    continue;
                }

                text = text.Substring(0, i) + "d" + text.Substring(i);
                start = i + 1;
            }
        }

        return text;
    }

    private static int IndexOfWord(string text, string needle, int start)
    {
        while (true) {
            var idx = text.IndexOf(needle, start, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return -1;
            if (idx == 0 || !(char.IsLetterOrDigit(text[idx - 1]) || text[idx - 1] == '_'))
                return idx;
            start = idx + 1;
        }
    }

    private static string RewriteClock(string expression)
    {
        var text = ReplaceMember(expression, "DateTime", "Now", "__now");
        text = ReplaceMember(text, "DateTime", "UtcNow", "__utcNow");
        text = ReplaceMember(text, "DateTime", "Today", "__today");
        text = ReplaceMember(text, "DateTimeOffset", "Now", "__dtoNow");
        text = ReplaceMember(text, "DateTimeOffset", "UtcNow", "__dtoUtcNow");
        return text;
    }

    private static string RewriteNullCoalesce(string expression)
    {
        if (expression.IndexOf("??", StringComparison.Ordinal) < 0)
            return expression;

        try {
            return RewriteNullCoalesceCore(expression);
        }
        catch {
            return expression;
        }
    }

    private static string RewriteNullCoalesceCore(string expression)
    {
        var idx = IndexOfOperator(expression, "??");
        if (idx < 0)
            return expression;

        var left = expression[..idx].Trim();
        var right = expression[(idx + 2)..].Trim();
        left = RewriteNullCoalesceCore(left);
        right = RewriteNullCoalesceCore(right);
        return $"(({left}) != null ? ({left}) : ({right}))";
    }

    private static int IndexOfOperator(string expression, string op)
    {
        var depth = 0;
        var inString = false;
        var stringChar = '\0';
        var escaped = false;
        for (var i = 0; i <= expression.Length - op.Length; i++) {
            var c = expression[i];
            if (inString) {
                if (escaped) {
                    escaped = false;
                    continue;
                }

                if (c == '\\') {
                    escaped = true;
                    continue;
                }

                if (c == stringChar)
                    inString = false;
                continue;
            }

            if (c is '"' or '\'') {
                inString = true;
                stringChar = c;
                continue;
            }

            if (c == '(' || c == '[' || c == '{')
                depth++;
            else if ((c == ')' || c == ']' || c == '}') && depth > 0)
                depth--;

            if (depth == 0 && string.Compare(expression, i, op, 0, op.Length, StringComparison.Ordinal) == 0)
                return i;
        }

        return -1;
    }

    private static string ReplaceIdentifier(string expression, string from, string to)
    {
        var builder = new StringBuilder(expression.Length);
        var inString = false;
        var stringChar = '\0';
        var escaped = false;
        for (var i = 0; i < expression.Length;) {
            var c = expression[i];
            if (inString) {
                builder.Append(c);
                if (escaped)
                    escaped = false;
                else if (c == '\\')
                    escaped = true;
                else if (c == stringChar)
                    inString = false;
                i++;
                continue;
            }

            if (c is '"' or '\'') {
                inString = true;
                stringChar = c;
                builder.Append(c);
                i++;
                continue;
            }

            if (MatchWord(expression, i, from)) {
                builder.Append(to);
                i += from.Length;
                continue;
            }

            builder.Append(c);
            i++;
        }

        return builder.ToString();
    }

    private static string ReplaceMember(string expression, string typeName, string member, string replacement)
    {
        var needle = typeName + "." + member;
        var builder = new StringBuilder(expression.Length);
        var inString = false;
        var stringChar = '\0';
        var escaped = false;
        for (var i = 0; i < expression.Length;) {
            var c = expression[i];
            if (inString) {
                builder.Append(c);
                if (escaped)
                    escaped = false;
                else if (c == '\\')
                    escaped = true;
                else if (c == stringChar)
                    inString = false;
                i++;
                continue;
            }

            if (c is '"' or '\'') {
                inString = true;
                stringChar = c;
                builder.Append(c);
                i++;
                continue;
            }

            if (MatchWord(expression, i, typeName)
                && i + needle.Length <= expression.Length
                && string.Compare(expression, i, needle, 0, needle.Length, StringComparison.OrdinalIgnoreCase) == 0) {
                var after = i + needle.Length;
                if (after >= expression.Length || !(char.IsLetterOrDigit(expression[after]) || expression[after] == '_')) {
                    builder.Append(replacement);
                    i += needle.Length;
                    continue;
                }
            }

            builder.Append(c);
            i++;
        }

        return builder.ToString();
    }

    private static bool MatchWord(string expression, int index, string word)
    {
        if (index + word.Length > expression.Length)
            return false;
        if (index > 0 && (char.IsLetterOrDigit(expression[index - 1]) || expression[index - 1] == '_'))
            return false;
        if (string.Compare(expression, index, word, 0, word.Length, StringComparison.OrdinalIgnoreCase) != 0)
            return false;
        var after = index + word.Length;
        return after >= expression.Length || !(char.IsLetterOrDigit(expression[after]) || expression[after] == '_');
    }

    private static void RemoveLambdaParameters(string expression, HashSet<string> result)
    {
        foreach (var name in GetLambdaParameters(expression))
            result.Remove(name);
    }

    private static HashSet<string> GetLambdaParameters(string expression)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var start = 0;
        while (true) {
            var arrow = expression.IndexOf("=>", start, StringComparison.Ordinal);
            if (arrow < 0)
                return names;

            var i = arrow - 1;
            while (i >= 0 && char.IsWhiteSpace(expression[i]))
                i--;
            if (i >= 0 && expression[i] == ')') {
                var close = i;
                var depth = 1;
                i--;
                while (i >= 0 && depth > 0) {
                    if (expression[i] == ')')
                        depth++;
                    else if (expression[i] == '(')
                        depth--;
                    i--;
                }

                var innerStart = i + 2;
                if (innerStart < close) {
                    var inner = expression.Substring(innerStart, close - innerStart);
                    foreach (var part in inner.Split(',')) {
                        var name = part.Trim();
                        if (IsValidIdentifier(name))
                            names.Add(name);
                    }
                }
            }
            else if (i >= 0) {
                var end = i + 1;
                while (i >= 0 && (char.IsLetterOrDigit(expression[i]) || expression[i] == '_'))
                    i--;
                var name = expression.Substring(i + 1, end - (i + 1));
                if (IsValidIdentifier(name))
                    names.Add(name);
            }

            start = arrow + 2;
        }
    }

    private static void AddThisMembers(string rewritten, HashSet<string> result)
    {
        var needle = ThisName + ".";
        var start = 0;
        while (true) {
            var idx = rewritten.IndexOf(needle, start, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                break;
            idx += needle.Length;
            var end = idx;
            while (end < rewritten.Length && (char.IsLetterOrDigit(rewritten[end]) || rewritten[end] is '_' or '.'))
                end++;
            if (end > idx)
                result.Add(rewritten[idx..end]);
            start = end;
        }
    }

    private static bool ShouldKeepIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name) || BuiltinIdentifiers.Contains(name) || name.StartsWith("__", StringComparison.Ordinal))
            return false;
        return IsValidIdentifier(name) || name.Contains('.');
    }

    private static bool IsValidIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name) || !(char.IsLetter(name[0]) || name[0] == '_'))
            return false;
        for (var i = 1; i < name.Length; i++) {
            if (!(char.IsLetterOrDigit(name[i]) || name[i] == '_'))
                return false;
        }

        return true;
    }

    private static bool IsAllowed(Expression expression, out string? error)
    {
        error = null;
        var visitor = new AllowlistVisitor();
        visitor.Visit(expression);
        if (visitor.Rejected is null)
            return true;
        error = visitor.Rejected;
        return false;
    }

    private static HashSet<string> CreateAllowedMethods()
    {
        var names = new HashSet<string>(StringComparer.Ordinal) {
            "AddDays", "AddHours", "AddMinutes", "AddSeconds", "AddMilliseconds", "AddTicks", "AddMonths", "AddYears", "Subtract",
            "FromDays", "FromHours", "FromMinutes", "FromSeconds", "FromMilliseconds", "FromTicks",
            "Abs", "Min", "Max", "Round", "Floor", "Ceiling", "Pow",
            "ToInt32", "ToInt64", "ToDecimal", "ToDouble", "ToString", "ToBoolean", "ToDateTime",
            "IsNullOrWhiteSpace", "IsNullOrEmpty", "Join", "Concat", "Format",
            "Trim", "ToLower", "ToUpper", "Contains", "StartsWith", "EndsWith", "Substring", "ToLowerInvariant", "ToUpperInvariant",
            "Where", "Select", "SelectMany", "OrderBy", "OrderByDescending", "ThenBy", "ThenByDescending",
            "Any", "All", "Count", "Sum", "Average", "Min", "Max", "First", "FirstOrDefault", "Last", "LastOrDefault",
            "Single", "SingleOrDefault", "Take", "Skip", "Distinct", "GroupBy", "ToList", "ToArray",
            "get_Item", "get_Chars", "get_Length", "get_Count", "get_Keys", "get_Values",
            "get_Year", "get_Month", "get_Day", "get_Hour", "get_Minute", "get_Second", "get_Millisecond",
            "get_DayOfWeek", "get_Date", "get_TimeOfDay", "get_Kind", "get_Ticks",
            "get_TotalHours", "get_TotalDays", "get_TotalMinutes", "get_TotalSeconds", "get_TotalMilliseconds",
            "get_Days", "get_Hours", "get_Minutes", "get_Seconds",
            "get_LocalDateTime", "get_UtcDateTime", "get_Offset", "get_UtcTicks",
            "ToUniversalTime", "ToLocalTime", "ToOffset"
        };
        return names;
    }

    private sealed class AllowlistVisitor : ExpressionVisitor
    {
        public string? Rejected { get; private set; }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (Rejected is not null)
                return node;

            var name = node.Method.Name;
            if (node.Method.IsSpecialName && (name.StartsWith("get_", StringComparison.Ordinal) || name.StartsWith("op_", StringComparison.Ordinal)))
                return base.VisitMethodCall(node);

            if (!AllowedMethods.Contains(name)) {
                Rejected = $"Method '{name}' is not allowed in formatter expressions.";
                return node;
            }

            return base.VisitMethodCall(node);
        }

        protected override Expression VisitNew(NewExpression node)
        {
            if (Rejected is not null)
                return node;
            var type = node.Type;
            if (type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || type == typeof(decimal) || type.IsPrimitive)
                return base.VisitNew(node);
            Rejected = $"Creating '{type.Name}' is not allowed in formatter expressions.";
            return node;
        }
    }
}
