using System.Text.Json;
using Lyo.Api.Models.Common.Response;
using Lyo.Common;
using Lyo.Query.Models.Builders;

namespace Lyo.Api.Tests;

/// <summary>Cache payload round-trip for <see cref="ProjectedQueryRes{T}" /> must deserialize polymorphic <c>QueryRequest</c>.</summary>
public sealed class ProjectedQueryResCacheSerializeTests
{
    [Fact]
    public void RoundTrip_RootQueryReq_ViaProjectedQueryRes()
    {
        var req = QueryReqBuilder.New()
            .From("p", "Person")
            .AddSelects("p.FirstName")
            .SetPagination(0, 10)
            .Build();
        var original = ResultFactory.ProjectedQuerySuccess<object?>(req, ["Ann"], 0, 10, 1, false, entityTypes: ["PersonEntity"]);

        var options = LyoJsonSerializerOptions.Create();
        var bytes = JsonSerializer.SerializeToUtf8Bytes(original, options);
        var restored = JsonSerializer.Deserialize<ProjectedQueryRes<object?>>(bytes, options);

        Assert.NotNull(restored);
        Assert.True(restored.IsSuccess);
        Assert.IsType<Lyo.Query.Models.Common.Request.QueryReq>(restored.QueryRequest);
        Assert.Equal("Person", ((Lyo.Query.Models.Common.Request.QueryReq)restored.QueryRequest).From.EntityType);
    }
}
