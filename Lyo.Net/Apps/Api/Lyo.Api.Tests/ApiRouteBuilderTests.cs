using Lyo.Api.Client;

namespace Lyo.Api.Tests;

public class ApiRouteBuilderTests
{
    [Fact]
    public void Build_WithoutPrefix_ReturnsRelativePath() => Assert.Equal("Job/Run/abc/Started", ApiRouteBuilder.Build(null, "Job/Run/abc/Started"));

    [Fact]
    public void Build_WithPrefix_JoinsPaths() => Assert.Equal("https://localhost:5074/Job/Run/abc/Started", ApiRouteBuilder.Build("https://localhost:5074", "Job/Run/abc/Started"));

    [Fact]
    public void Build_WithTrailingSlashOnPrefix_TrimsSlash() => Assert.Equal("https://localhost:5074/Job/Run", ApiRouteBuilder.Build("https://localhost:5074/", "Job/Run"));

    [Fact]
    public void WithIncludes_AppendsQueryString()
        => Assert.Equal("Job/Run/id?include=JobRunParameters&include=JobDefinition", ApiRouteBuilder.WithIncludes("Job/Run/id", ["JobRunParameters", "JobDefinition"]));

    [Fact]
    public void WithIncludes_NullOrEmpty_ReturnsRouteUnchanged()
    {
        Assert.Equal("Job/Run/id", ApiRouteBuilder.WithIncludes("Job/Run/id", null));
        Assert.Equal("Job/Run/id", ApiRouteBuilder.WithIncludes("Job/Run/id", []));
    }

    [Fact]
    public void QueryPath_JoinsSuffix()
    {
        Assert.Equal("person/QueryProject", ApiClient.QueryPath("person", "QueryProject"));
        Assert.Equal("person/QueryConcrete", ApiClient.QueryPath("/person/", "QueryConcrete"));
        Assert.Equal("QueryProject", ApiClient.QueryPath("", "QueryProject"));
    }
}
