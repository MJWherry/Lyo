using Lyo.Web.Primitives;

namespace Lyo.Web.Primitives.Tests;

public sealed class LyoTabSetTests
{
    [Theory]
    [InlineData("Overview", "overview")]
    [InlineData("Job Logs", "job-logs")]
    [InlineData("  SQL / JSON  ", "sql-json")]
    [InlineData("", "")]
    public void Slug_Label_Hyphenates(string text, string expected) => Assert.Equal(expected, LyoTabSet.Slug(text));
}
