using Lyo.Algorithms;
using Lyo.Algorithms.Models;
using Lyo.Algorithms.Models.Request;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Formatter.Algorithms.Tests;

public sealed class AlgorithmFormatterTests
{
    [Fact]
    public async Task Format_AlgoCall_UsesRegistry()
    {
        var services = new ServiceCollection();
        services.AddAlgorithms();
        services.AddMemoryAlgorithmStore();
        services.AddFormatterService();
        services.AddFormatterAlgorithms();
        await using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IAlgorithmCatalog>();
        var formatter = provider.GetRequiredService<IFormatterService>();
        var ct = TestCt;
        Assert.True((await catalog.SaveRevisionAsync("attack.physical", new AlgorithmRevisionReq { Kind = AlgorithmKind.Expression, Source = "Atk * 2" }, ct)).IsSuccess);

        var text = formatter.Format("{Algo(\"attack.physical\")}", new Dictionary<string, object?> { ["Atk"] = 10d });
        Assert.Equal("20", text);
        Assert.Equal("3", formatter.Format("{Convert.ToInt32(1)+2}", (object?)null));
    }

    [Fact]
    public void Format_WithoutAddon_LeavesAlgoToken()
    {
        var formatter = new FormatterService();
        Assert.Equal("{Algo(\"attack.physical\")}", formatter.Format("{Algo(\"attack.physical\")}", (object?)null));
        Assert.Equal("3", formatter.Format("{Convert.ToInt32(1)+2}", (object?)null));
    }

    [Fact]
    public void JobAndApi_DoNotReferenceAlgorithms()
    {
        var root = FindRepoRoot();
        Assert.DoesNotContain("Lyo.Algorithms", File.ReadAllText(Path.Combine(root, "Lyo.Net/Apps/Api/Lyo.Api/Lyo.Api.csproj")), StringComparison.Ordinal);
        Assert.DoesNotContain("Lyo.Algorithms", File.ReadAllText(Path.Combine(root, "Lyo.Net/Apps/Job/Lyo.Job.Worker/Lyo.Job.Worker.csproj")), StringComparison.Ordinal);
        Assert.DoesNotContain("Lyo.Algorithms", File.ReadAllText(Path.Combine(root, "Lyo.Net/Data/Formatter/Lyo.Formatter/Lyo.Formatter.csproj")), StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null) {
            if (File.Exists(Path.Combine(dir.FullName, "Lyo.Net", "Lyo.slnx")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
