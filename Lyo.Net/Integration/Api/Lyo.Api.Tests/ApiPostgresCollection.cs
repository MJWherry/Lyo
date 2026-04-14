using Lyo.Api.Tests.Fixtures;

namespace Lyo.Api.Tests;

[CollectionDefinition(Name)]
public sealed class ApiPostgresCollection : ICollectionFixture<ApiPostgresFixture>
{
    public const string Name = "Api Postgres";
}