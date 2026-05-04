using Lyo.Diagnostic.Sanitisation;
using Lyo.Diagnostic.StackTrace;

namespace Lyo.Diagnostic.Tests;

public sealed class TraceSanitiserTests
{
    [Fact]
    public void Sanitise_CrashSite_FallsBack_ToMethod_WhenInnermostFrameHasNoFile()
    {
        const string raw = """
System.StackOverflowException: Stack overflow.
   at MyApp.Utils.TreeTraversal.FindNode(TreeNode node, Int32 targetId)
   at MyApp.Utils.TreeTraversal.FindNode(TreeNode node, Int32 targetId)
   at MyApp.Utils.TreeTraversal.FindNode(TreeNode node, Int32 targetId)
   at MyApp.Utils.TreeTraversal.FindNode(TreeNode node, Int32 targetId)
   at MyApp.Services.CategoryService.Search(Int32 rootId, Int32 targetId) in CategoryService.cs:line 88
""";
        var decoded = new StackTraceDecoder().Decode(raw);
        Assert.NotNull(decoded.LikelyCrashSite);
        Assert.Null(decoded.LikelyCrashSite!.SourceFile);

        var sanitised = new TraceSanitiser().Sanitise(decoded);
        Assert.False(string.IsNullOrWhiteSpace(sanitised.CrashSite));
        Assert.Contains("FindNode", sanitised.CrashSite!, StringComparison.Ordinal);
    }
}
