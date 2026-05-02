namespace Lyo.Diagnostic.StackTrace;

/// <summary>Renders a <see cref="DecodedStackTrace" /> to the console. Separated from the model so you can write your own JSON / HTML renderer.</summary>
public static class StackTracePrinter
{
    public static void Print(DecodedStackTrace trace, int indentLevel = 0)
    {
        var pad = new string(' ', indentLevel * 2);
        Divider('═', pad);
        Console.WriteLine($"{pad}  C# STACK TRACE DECODER");
        Divider('═', pad);

        // Exception message
        if (!string.IsNullOrWhiteSpace(trace.ExceptionMessage)) {
            Header("EXCEPTION", ConsoleColor.Red, pad);
            Console.WriteLine($"{pad}{trace.ExceptionMessage}");
        }

        // Summary
        Header("SUMMARY", ConsoleColor.Cyan, pad);
        Kv("Total frames", trace.TotalFrameCount.ToString(), pad);
        Kv("Your code", trace.UserFrameCount.ToString(), pad);
        Kv("System / 3rd P", trace.SystemFrameCount.ToString(), pad);
        Kv("Test framework", trace.TestFrames.Count.ToString(), pad);
        Kv("Async frames", trace.AsyncFrameCount.ToString(), pad);
        Kv("Lambda frames", trace.LambdaFrameCount.ToString(), pad);
        Kv("Namespaces hit", trace.UserNamespaces.Count.ToString(), pad);
        Kv("Inner exceptions", trace.InnerExceptionDepth.ToString(), pad);
        Kv("Fingerprint", trace.Fingerprint, pad);

        // Key sites
        Header("KEY SITES", ConsoleColor.Cyan, pad);
        Site($"Likely crash [{trace.CrashSiteConfidence}]", trace.LikelyCrashSite, ConsoleColor.Red, pad);
        Site("Deepest user frame", trace.DeepestUserFrame, ConsoleColor.Yellow, pad);
        Site("Last system call", trace.LastSystemFrame, ConsoleColor.DarkGray, pad);

        // Recursion
        if (trace.HasRecursion) {
            Header("RECURSION DETECTED", ConsoleColor.Magenta, pad);
            foreach (var r in trace.RecursionPatterns) {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"{pad}  ⚠ Depth {r.Depth} × {r.Frame.ShortMethod}  (starts at frame {r.StartIndex})");
                Console.ResetColor();
            }
        }

        // Namespaces
        if (trace.UserNamespaces.Count > 0) {
            Header("YOUR NAMESPACES", ConsoleColor.Cyan, pad);
            foreach (var ns in trace.UserNamespaces)
                Console.WriteLine($"{pad}  · {ns}");
        }

        // Frames
        Header("FRAMES (grouped by category)", ConsoleColor.Cyan, pad);
        var idx = 0;
        foreach (var group in trace.Groups) {
            var (label, color) = group.Category switch {
                FrameCategory.UserCode => ("YOUR CODE", ConsoleColor.Red),
                FrameCategory.SystemOrThirdParty => ("SYSTEM / 3RD PARTY", ConsoleColor.DarkGray),
                FrameCategory.TestFramework => ("TEST FRAMEWORK", ConsoleColor.Green),
                var _ => ("UNKNOWN", ConsoleColor.White)
            };

            Console.ForegroundColor = color;
            Console.WriteLine($"\n{pad}  ┌─ {label} ({group.Count} frame{(group.Count > 1 ? "s" : "")}) ───");
            Console.ResetColor();
            foreach (var frame in group.Frames) {
                Console.ForegroundColor = color;
                Console.Write($"{pad}  │ [{idx,3}] ");
                Console.ResetColor();
                Console.ForegroundColor = frame.Category == FrameCategory.UserCode ? ConsoleColor.White : ConsoleColor.DarkGray;
                Console.WriteLine(frame.ShortMethod);
                Console.ResetColor();
                if (frame.LocationSummary is not null) {
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine($"{pad}  │       → {frame.LocationSummary}");
                    Console.ResetColor();
                }

                var tags = Tags(frame);
                if (tags.Length > 0) {
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine($"{pad}  │       ⚑ {tags}");
                    Console.ResetColor();
                }

                idx++;
            }
        }

        Divider('─', pad);

        // Inner exceptions (recursive)
        for (var i = 0; i < trace.InnerExceptions.Count; i++) {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"\n{pad}  ↳ INNER EXCEPTION [{i + 1}]");
            Console.ResetColor();
            Print(trace.InnerExceptions[i], indentLevel + 1);
        }
    }

    private static string Tags(StackFrame f)
    {
        var t = new List<string>(3);
        if (f.IsAsync)
            t.Add("async");

        if (f.IsLambda)
            t.Add("lambda");

        if (f.IsCompilerGenerated)
            t.Add("compiler-generated");

        return string.Join("  ", t);
    }

    private static void Site(string label, StackFrame? frame, ConsoleColor color, string pad)
    {
        Console.Write($"{pad}  {label,-35}: ");
        if (frame is null) {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("(none)");
        }
        else {
            Console.ForegroundColor = color;
            Console.Write(frame.ShortMethod);
            Console.ResetColor();
            if (frame.LocationSummary is not null) {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"  [{frame.LocationSummary}]");
            }

            Console.WriteLine();
        }

        Console.ResetColor();
    }

    private static void Kv(string key, string value, string pad)
    {
        Console.Write($"{pad}  {key,-20}: ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(value);
        Console.ResetColor();
    }

    private static void Header(string title, ConsoleColor color, string pad)
    {
        Console.ForegroundColor = color;
        Console.WriteLine($"\n{pad}[{title}]");
        Console.ResetColor();
    }

    private static void Divider(char ch, string pad) => Console.WriteLine($"{pad}{new string(ch, Math.Max(10, 80 - pad.Length))}");
}