using Lyo.Diff.ObjectGraph;
using Lyo.Diff.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Diff.Tests;

public sealed class DiffServiceCollectionTests
{
    [Fact]
    public void AddLyoDiff_registers_all_services()
    {
        var services = new ServiceCollection();
        services.AddLyoDiff();
        var sp = services.BuildServiceProvider();
        Assert.NotNull(sp.GetService<ITextTokenizer>());
        Assert.NotNull(sp.GetService<ITextDiffService>());
        Assert.NotNull(sp.GetService<IObjectGraphDiffService>());
        Assert.NotNull(sp.GetService<IDiffService>());
    }
}