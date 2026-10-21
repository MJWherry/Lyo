using Lyo.StateMachine.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.StateMachine.Postgres.Tests;

public enum OrderState
{
    Pending,
    Paid,
    Shipped
}

/// <summary>Shared Postgres container with state machine migrations applied and an <see cref="OrderState" /> store registered.</summary>
public sealed class StateMachinePostgresFixture : PostgresServiceFixtureBase<StateMachineDbContext>
{
    /// <inheritdoc />
    protected override void ConfigureServices(IServiceCollection services, string connectionString)
    {
        services.AddStateMachinePostgres(new PostgresStateMachineOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
        services.AddPostgresStateStore<OrderState>();
        services.AddPostgresStateMachineDefinitionStore();
    }
}