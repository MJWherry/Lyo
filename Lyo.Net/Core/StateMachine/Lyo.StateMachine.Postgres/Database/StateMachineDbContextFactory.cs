using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.StateMachine.Postgres.Database;

/// <summary>Design-time factory for EF migrations. Needs <c>STATE_MACHINE_CONNECTION_STRING</c>.</summary>
public sealed class StateMachineDbContextFactory : IDesignTimeDbContextFactory<StateMachineDbContext>
{
    /// <inheritdoc />
    public StateMachineDbContext CreateDbContext(string[] args)
        => new(PostgresDesignTime.CreateOptions<StateMachineDbContext>("STATE_MACHINE_CONNECTION_STRING", PostgresStateMachineOptions.Schema));
}