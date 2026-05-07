namespace Lyo.PackageMetadata;

internal static class SpdxLicenseExpressionParser
{
    internal static SpdxLicenseExpressionSyntax? TryParse(string? input)
    {
        if (input is null)
            return null;

        var trimmed = input.Trim();
        if (trimmed.Length == 0)
            return null;

        try {
            var tokens = Tokenize(trimmed);
            var p = new ParseState(tokens);
            var expr = p.ParseOr();
            if (expr is null || !p.Match(TokenKind.End))
                return null;

            return expr;
        }
        catch {
            return null;
        }
    }

    private static List<Token> Tokenize(string s)
    {
        var list = new List<Token>(16);
        var i = 0;
        while (i < s.Length) {
            if (char.IsWhiteSpace(s[i])) {
                i++;
                continue;
            }

            var c = s[i];
            if (c == '(') {
                list.Add(new(TokenKind.LeftParen));
                i++;
                continue;
            }

            if (c == ')') {
                list.Add(new(TokenKind.RightParen));
                i++;
                continue;
            }

            var start = i;
            while (i < s.Length) {
                c = s[i];
                if (char.IsWhiteSpace(c) || c == '(' || c == ')')
                    break;

                if (!(char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '+' || c == ':'))
                    throw new FormatException();

                i++;
            }

            if (start == i)
                throw new FormatException();

            var lex = s.Substring(start, i - start);
            if (string.Equals(lex, "AND", StringComparison.Ordinal))
                list.Add(new(TokenKind.And));
            else if (string.Equals(lex, "OR", StringComparison.Ordinal))
                list.Add(new(TokenKind.Or));
            else if (string.Equals(lex, "WITH", StringComparison.Ordinal))
                list.Add(new(TokenKind.With));
            else {
                ValidateIdentifierLexeme(lex);
                list.Add(new(TokenKind.Identifier, lex));
            }
        }

        list.Add(new(TokenKind.End));
        return list;
    }

    private static void ValidateIdentifierLexeme(string lex)
    {
        if (lex.Length == 0)
            throw new FormatException();

        var plusCount = 0;
        for (var j = 0; j < lex.Length; j++) {
            if (lex[j] == '+')
                plusCount++;
        }

        if (plusCount > 1)
            throw new FormatException();

        if (plusCount == 1 && lex[lex.Length - 1] != '+')
            throw new FormatException();
    }

    private enum TokenKind
    {
        End,
        LeftParen,
        RightParen,
        And,
        Or,
        With,
        Identifier
    }

    private readonly record struct Token(TokenKind Kind, string? Text = null);

    private sealed class ParseState(List<Token> tokens)
    {
        private int _index;

        internal SpdxLicenseExpressionSyntax? ParseOr()
        {
            var left = ParseAnd();
            if (left is null)
                return null;

            while (Match(TokenKind.Or)) {
                var right = ParseAnd();
                if (right is null)
                    return null;

                left = new("or", Left: left, Right: right);
            }

            return left;
        }

        private SpdxLicenseExpressionSyntax? ParseAnd()
        {
            var left = ParseWith();
            if (left is null)
                return null;

            while (Match(TokenKind.And)) {
                var right = ParseWith();
                if (right is null)
                    return null;

                left = new("and", Left: left, Right: right);
            }

            return left;
        }

        private SpdxLicenseExpressionSyntax? ParseWith()
        {
            var left = ParseUnary();
            if (left is null)
                return null;

            while (Match(TokenKind.With)) {
                var exc = ParseExceptionLeaf();
                if (exc is null)
                    return null;

                left = new("with", InnerLicense: left, InnerException: exc);
            }

            return left;
        }

        private SpdxLicenseExpressionSyntax? ParseUnary()
        {
            if (Match(TokenKind.LeftParen)) {
                var inner = ParseOr();
                if (inner is null || !Match(TokenKind.RightParen))
                    return null;

                return inner;
            }

            if (!Match(TokenKind.Identifier, out var lex) || lex is null)
                return null;

            ValidateIdentifierLexeme(lex);
            return LicenseLeaf(lex);
        }

        private SpdxLicenseExpressionSyntax? ParseExceptionLeaf()
        {
            if (!Match(TokenKind.Identifier, out var lex) || lex is null)
                return null;

            ValidateIdentifierLexeme(lex);
            return new("exception", StripPlus(lex, out var plus), plus ? true : null);
        }

        private static SpdxLicenseExpressionSyntax LicenseLeaf(string lex) => new("license", StripPlus(lex, out var plus), plus ? true : null);

        private static string StripPlus(string lex, out bool plusSuffix)
        {
            if (lex.Length > 0 && lex[lex.Length - 1] == '+') {
                plusSuffix = true;
                return lex.Substring(0, lex.Length - 1);
            }

            plusSuffix = false;
            return lex;
        }

        internal bool Match(TokenKind kind)
        {
            if (_index >= tokens.Count || tokens[_index].Kind != kind)
                return false;

            _index++;
            return true;
        }

        private bool Match(TokenKind kind, out string? text)
        {
            text = null;
            if (_index >= tokens.Count || tokens[_index].Kind != kind)
                return false;

            text = tokens[_index].Text;
            _index++;
            return true;
        }
    }
}