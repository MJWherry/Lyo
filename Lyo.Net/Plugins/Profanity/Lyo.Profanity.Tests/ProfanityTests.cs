using Lyo.Profanity.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Profanity.Tests;

public sealed class ProfanityTests
{
    [Fact]
    public void Service_Can_Be_Created_With_Default_Options()
    {
        var services = new ServiceCollection();
        services.AddProfanityFilterService();
        using var sp = services.BuildServiceProvider();
        var service = sp.GetRequiredService<IProfanityFilterService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void Filter_Replaces_Profanity_With_AdditionalWords()
    {
        var options = new FileProfanityFilterOptions { AdditionalWords = ["badword", "naughty"] };
        var service = new FileProfanityFilterService(options);
        var result = service.Filter("This is badword and naughty text", TestContext.Current.CancellationToken);
        Assert.True(result.HasProfanity);
        Assert.Equal("This is ******* and ******* text", result.FilteredText);
        Assert.Equal(2, result.Matches.Count);
    }

    [Fact]
    public void Filter_Returns_Unchanged_When_No_Profanity()
    {
        var options = new FileProfanityFilterOptions { AdditionalWords = ["badword"] };
        var service = new FileProfanityFilterService(options);
        var result = service.Filter("Clean text here", TestContext.Current.CancellationToken);
        Assert.False(result.HasProfanity);
        Assert.Equal("Clean text here", result.FilteredText);
        Assert.Empty(result.Matches);
    }

    [Fact]
    public async Task FilterAsync_Replaces_Profanity()
    {
        var options = new FileProfanityFilterOptions { AdditionalWords = ["badword"] };
        var service = new FileProfanityFilterService(options);
        var result = await service.FilterAsync("Some badword here", TestContext.Current.CancellationToken);
        Assert.True(result.HasProfanity);
        Assert.Equal("Some ******* here", result.FilteredText);
    }

    [Fact]
    public void ContainsProfanity_Returns_True_When_Profanity_Found()
    {
        var options = new FileProfanityFilterOptions { AdditionalWords = ["badword"] };
        var service = new FileProfanityFilterService(options);
        Assert.True(service.ContainsProfanity("This has badword in it", TestContext.Current.CancellationToken));
    }

    [Fact]
    public void ContainsProfanity_Returns_False_When_Clean()
    {
        var options = new FileProfanityFilterOptions { AdditionalWords = ["badword"] };
        var service = new FileProfanityFilterService(options);
        Assert.False(service.ContainsProfanity("Clean content", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ContainsProfanityAsync_Returns_True_When_Profanity_Found()
    {
        var options = new FileProfanityFilterOptions { AdditionalWords = ["naughty"] };
        var service = new FileProfanityFilterService(options);
        Assert.True(await service.ContainsProfanityAsync("Something naughty", TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Filter_Uses_ExcludedWords_To_Ignore_False_Positives()
    {
        var options = new FileProfanityFilterOptions { AdditionalWords = ["ass", "class"], ExcludedWords = ["class"] };
        var service = new FileProfanityFilterService(options);
        var result = service.Filter("In class we learn about ass", TestContext.Current.CancellationToken);
        Assert.True(result.HasProfanity);
        Assert.Equal("In class we learn about ***", result.FilteredText);
    }

    [Fact]
    public void Filter_Handles_Null_Or_Empty_Input()
    {
        var options = new FileProfanityFilterOptions { AdditionalWords = ["badword"] };
        var service = new FileProfanityFilterService(options);
        var nullResult = service.Filter(null, TestContext.Current.CancellationToken);
        Assert.False(nullResult.HasProfanity);
        Assert.Equal(string.Empty, nullResult.FilteredText);
        var emptyResult = service.Filter("", TestContext.Current.CancellationToken);
        Assert.False(emptyResult.HasProfanity);
        Assert.Equal("", emptyResult.FilteredText);
    }

    [Fact]
    public void Filter_Uses_ReplaceWithWord_Strategy()
    {
        var options = new FileProfanityFilterOptions { AdditionalWords = ["badword"], ReplacementStrategy = ProfanityReplacementStrategy.ReplaceWithWord, ReplacementWord = "***" };
        var service = new FileProfanityFilterService(options);
        var result = service.Filter("Text with badword", TestContext.Current.CancellationToken);
        Assert.True(result.HasProfanity);
        Assert.Equal("Text with ***", result.FilteredText);
    }

    [Fact]
    public void Filter_Loads_From_Temp_File()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "lyo_profanity_test_" + Guid.NewGuid().ToString("N") + ".json");
        try {
            File.WriteAllText(tempFile, "fileword1\nfileword2");
            Assert.True(File.Exists(tempFile));
            var options = new FileProfanityFilterOptions { WordsFilePath = tempFile };
            var service = new FileProfanityFilterService(options);
            var result = service.Filter("Contains fileword1 and fileword2", TestContext.Current.CancellationToken);
            Assert.True(result.HasProfanity);
            Assert.Equal("Contains ********* and *********", result.FilteredText);
        }
        finally {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void RefreshWords_Reloads_From_File_When_AllowRefresh_True()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "lyo_profanity_refresh_" + Guid.NewGuid().ToString("N") + ".json");
        try {
            File.WriteAllText(tempFile, "oldword");
            var options = new FileProfanityFilterOptions { WordsFilePath = tempFile, AllowRefresh = true };
            var service = new FileProfanityFilterService(options);
            Assert.True(service.ContainsProfanity("Has oldword", TestContext.Current.CancellationToken));
            File.WriteAllText(tempFile, "newword");
            service.RefreshWords(TestContext.Current.CancellationToken);
            Assert.False(service.ContainsProfanity("Has oldword", TestContext.Current.CancellationToken));
            Assert.True(service.ContainsProfanity("Has newword", TestContext.Current.CancellationToken));
        }
        finally {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void ProfanityEntry_FromPlainWord_Creates_Default_Entry()
    {
        var entry = ProfanityEntry.FromPlainWord("test");
        Assert.Equal("test", entry.Id);
        Assert.Equal("test", entry.Match);
        Assert.Empty(entry.Tags);
        Assert.Equal(1, entry.Severity);
        Assert.Empty(entry.Exceptions);
        Assert.True(entry.IsLiteral);
    }
}