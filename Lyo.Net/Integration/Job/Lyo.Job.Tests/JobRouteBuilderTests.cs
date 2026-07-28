using Lyo.Job.Client;

namespace Lyo.Job.Tests;

public class JobRouteBuilderTests
{
    [Fact]
    public void Build_WithoutPrefix_ReturnsRelativePath() => Assert.Equal("Job/Run/abc/Started", JobRouteBuilder.Build(null, "Job/Run/abc/Started"));

    [Fact]
    public void Build_WithPrefix_JoinsPaths() => Assert.Equal("https://localhost:5074/Job/Run/abc/Started", JobRouteBuilder.Build("https://localhost:5074", "Job/Run/abc/Started"));

    [Fact]
    public void Build_WithTrailingSlashOnPrefix_TrimsSlash() => Assert.Equal("https://localhost:5074/Job/Run", JobRouteBuilder.Build("https://localhost:5074/", "Job/Run"));

    [Fact]
    public void WithIncludes_AppendsQueryString()
        => Assert.Equal("Job/Run/id?include=JobRunParameters&include=JobDefinition", JobRouteBuilder.WithIncludes("Job/Run/id", ["JobRunParameters", "JobDefinition"]));

    [Fact]
    public void WithIncludes_NullOrEmpty_ReturnsRouteUnchanged()
    {
        Assert.Equal("Job/Run/id", JobRouteBuilder.WithIncludes("Job/Run/id", null));
        Assert.Equal("Job/Run/id", JobRouteBuilder.WithIncludes("Job/Run/id", []));
    }
}