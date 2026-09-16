using Bogus;
using Lyo.Authentication.Format;
using Lyo.Authentication.Models.Audit;
using Lyo.Authentication.Models.Format;
using Lyo.Authentication.Postgres;
using Lyo.Authentication.Postgres.Database;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Seed;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Authentication.Seed;

/// <summary>
/// EF seed of local users. <see cref="Password" /> is hashed with <see cref="ApiTokenCodec.ComputeSecretHash" /> and stored as an expired token secret hash.
/// The plaintext token is not persisted. Schema <c>user</c>.
/// </summary>
public sealed class AuthenticationEfSeedContributor : SeedContributor, IEfSeedSource
{
    /// <summary>Constant local password. Stored only as a hash on the expired token.</summary>
    public const string Password = "seed-password";

    /// <inheritdoc />
    public override string Name => "Authentication";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    public string Schema => PostgresUserOptions.Schema;

    /// <inheritdoc />
    public DbContext CreateContext(string connectionString)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(connectionString);
        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseNpgsql(connectionString, n => n.MigrationsHistoryTable("__EFMigrationsHistory", Schema))
            .Options;
        return new UserDbContext(options);
    }

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        graph.OnClear(ClearAsync);
        var faker = options.RandomSeed is { } seed ? new Faker { Random = new(seed) } : new Faker();
        var now = DateTime.UtcNow;
        var hash = ApiTokenCodec.ComputeSecretHash(Password);
        var userIndex = new Dictionary<Guid, int>();
        var users = graph.Entity(options.Count, i => {
            var local = faker.Internet.UserName();
            if (local.Length > 40)
                local = local[..40];
            var user = new UserEntity {
                Id = LyoGuid.CreateV7(),
                DisplayName = faker.Name.FullName(),
                Email = $"{local}.{i}@example.com",
                EmailVerified = true,
                ScopesJson = "[\"boards.read\"]",
                CreatedTimestamp = now
            };
            userIndex[user.Id] = i;
            return user;
        });
        users.ForEach(_ => 1, (user, _) => new TokenEntity {
            Id = TokenId(userIndex[user.Id]),
            SecretHash = hash,
            Kind = ApiTokenKind.Pat,
            Ring = ApiTokenRing.Test,
            UserId = user.Id,
            DisplayName = "Laptop",
            ScopesJson = "[]",
            CreatedTimestamp = now.AddDays(-2),
            ExpiresTimestamp = now.AddDays(-1)
        });
        users.ForEach(_ => 1, (user, _) => new LinkedIdentityEntity {
            Id = LyoGuid.CreateV7(),
            UserId = user.Id,
            Provider = "local",
            Subject = user.Id.ToString("N"),
            EmailAtLink = user.Email,
            ScopesJson = "[]",
            LinkedTimestamp = now
        });
        users.ForEach(_ => 1, (user, _) => new UserClaimEntity {
            Id = LyoGuid.CreateV7(),
            UserId = user.Id,
            Type = "locale",
            Value = "en",
            CreatedTimestamp = now
        });
        users.ForEach(_ => 1, (user, _) => new UserScopeEntity {
            Id = LyoGuid.CreateV7(),
            UserId = user.Id,
            Name = "boards.read",
            CreatedTimestamp = now
        });
        users.ForEach(_ => 1, (user, _) => new UserEventEntity {
            Id = LyoGuid.CreateV7(),
            Timestamp = now,
            Kind = AuthAuditEventKind.UserProvisioned,
            UserId = user.Id,
            Provider = "local",
            Outcome = "success"
        });
    }

    private static string TokenId(int index)
    {
        Span<byte> bytes = stackalloc byte[8];
        BitConverter.TryWriteBytes(bytes, index + 1);
        return Base32Crockford.EncodeRandom11(bytes);
    }

    private static async Task ClearAsync(SeedSession session, CancellationToken ct)
    {
        if (session.Transport is not EfSeedTransport<UserDbContext> ef)
            throw new SeedException("Authentication OnClear requires EfSeedTransport<UserDbContext>.");

        var db = ef.Context;
        await db.Events.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Scopes.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Claims.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.LinkedIdentities.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Tokens.ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await db.Users.ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}
