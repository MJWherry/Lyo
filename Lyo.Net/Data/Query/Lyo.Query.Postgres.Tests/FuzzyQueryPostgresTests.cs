using Lyo.Common.Core.Identifiers;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Query.Postgres.Tests;

public class FuzzyQueryPostgresTests(FuzzyQueryPostgresFixture fixture) : IClassFixture<FuzzyQueryPostgresFixture>
{
    [Fact]
    public async Task ApplyWhereClause_Fuzzy_EmitsSimilaritySqlAndMatchesClr()
    {
        var rows = new[] {
            new FuzzyPersonEntity { Id = LyoGuid.CreateV7(), Name = "Alice" },
            new FuzzyPersonEntity { Id = LyoGuid.CreateV7(), Name = "Alicia" },
            new FuzzyPersonEntity { Id = LyoGuid.CreateV7(), Name = "Bob" },
            new FuzzyPersonEntity { Id = LyoGuid.CreateV7(), Name = "Jonathan" }
        };

        await using (var seed = await fixture.ContextFactory.CreateDbContextAsync(TestCt)) {
            seed.People.RemoveRange(seed.People);
            seed.People.AddRange(rows);
            await seed.SaveChangesAsync(TestCt);
        }

        var where = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Fuzzy, "Alice", threshold: 0.3);
        await using var db = await fixture.ContextFactory.CreateDbContextAsync(TestCt);
        var query = fixture.WhereClause.ApplyWhereClause(db.People.AsNoTracking(), where);
        var sql = query.ToQueryString();
        Assert.Contains("similarity", sql, StringComparison.OrdinalIgnoreCase);
        var sqlNames = (await query.Select(p => p.Name).ToListAsync(TestCt)).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        var clrNames = fixture.WhereClause.ApplyWhereClause(rows.AsQueryable(), where).Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Assert.Equal(clrNames, sqlNames);
        Assert.Contains("Alice", sqlNames);
        Assert.DoesNotContain("Bob", sqlNames);
    }

    [Fact]
    public async Task ApplyWhereClause_FuzzyThreshold_ChangesMatches()
    {
        var rows = new[] { new FuzzyPersonEntity { Id = LyoGuid.CreateV7(), Name = "Alice" }, new FuzzyPersonEntity { Id = LyoGuid.CreateV7(), Name = "Alicia" } };
        await using (var seed = await fixture.ContextFactory.CreateDbContextAsync(TestCt)) {
            seed.People.RemoveRange(seed.People);
            seed.People.AddRange(rows);
            await seed.SaveChangesAsync(TestCt);
        }

        var loose = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Fuzzy, "Alice", threshold: 0.2);
        var tight = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Fuzzy, "Alice", threshold: 0.99);
        await using var db = await fixture.ContextFactory.CreateDbContextAsync(TestCt);
        var looseSql = await fixture.WhereClause.ApplyWhereClause(db.People.AsNoTracking(), loose).Select(p => p.Name).ToListAsync(TestCt);
        var tightSql = await fixture.WhereClause.ApplyWhereClause(db.People.AsNoTracking(), tight).Select(p => p.Name).ToListAsync(TestCt);
        Assert.Contains("Alice", looseSql);
        Assert.Contains("Alice", tightSql);
        Assert.DoesNotContain("Alicia", tightSql);
    }
}