using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lyo.Api.EntityFramework;

/// <summary>Fluent registration of same-context and cross-schema navigations for <typeparamref name="TContext" />.</summary>
public sealed class CrossSchemaNavigationBuilder<TContext>
    where TContext : DbContext
{
    private readonly CrossSchemaNavigationOptions<TContext> _options;

    internal CrossSchemaNavigationBuilder(CrossSchemaNavigationOptions<TContext> options) => _options = options;

    /// <summary>
    /// Adds a relationship to a related type already mapped on <typeparamref name="TContext" /> (same database model).
    /// Does not change table mapping or migrations.
    /// </summary>
    public CrossSchemaNavigationBuilder<TContext> AddSameContext<TRoot, TRelated>(
        Expression<Func<TRoot, TRelated?>> navigation,
        Expression<Func<TRoot, object?>> foreignKey)
        where TRoot : class
        where TRelated : class
    {
        var navName = GetMemberName(navigation);
        var fkName = GetMemberName(foreignKey);
        _options.Registrations.Add(
            new() {
                RootEntityType = typeof(TRoot),
                RelatedEntityType = typeof(TRelated),
                NavigationName = navName,
                ForeignKeyPropertyName = fkName,
                SameContext = true,
                Apply = mb => {
                    mb.Entity<TRoot>()
                        .HasOne(navigation)
                        .WithMany()
                        .HasForeignKey(fkName)
                        .IsRequired(false)
                        .OnDelete(DeleteBehavior.ClientNoAction);
                }
            });
        return this;
    }

    /// <summary>
    /// Maps <typeparamref name="TRelated" /> onto <typeparamref name="TContext" /> at <paramref name="schema" />.<paramref name="table" />
    /// with <c>ExcludeFromMigrations</c>, then adds the <paramref name="navigation" /> relationship.
    /// Related tables must live in the same database; ownership stays with the related module's migrations.
    /// </summary>
    public CrossSchemaNavigationBuilder<TContext> AddCrossSchema<TRoot, TRelated>(
        Expression<Func<TRoot, TRelated?>> navigation,
        Expression<Func<TRoot, object?>> foreignKey,
        string table,
        string schema,
        Action<EntityTypeBuilder<TRelated>>? configureRelated = null)
        where TRoot : class
        where TRelated : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(table);
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);

        var navName = GetMemberName(navigation);
        var fkName = GetMemberName(foreignKey);
        _options.Registrations.Add(
            new() {
                RootEntityType = typeof(TRoot),
                RelatedEntityType = typeof(TRelated),
                NavigationName = navName,
                ForeignKeyPropertyName = fkName,
                SameContext = false,
                Apply = mb => {
                    var related = mb.Entity<TRelated>();
                    configureRelated?.Invoke(related);
                    related.ToTable(table, schema, t => t.ExcludeFromMigrations());
                    mb.Entity<TRoot>()
                        .HasOne(navigation)
                        .WithMany()
                        .HasForeignKey(fkName)
                        .IsRequired(false)
                        .OnDelete(DeleteBehavior.ClientNoAction);
                }
            });
        return this;
    }

    private static string GetMemberName<T, TValue>(Expression<Func<T, TValue>> expression)
    {
        var body = expression.Body;
        if (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
            body = unary.Operand;

        return body switch {
            MemberExpression member => member.Member.Name,
            _ => throw new ArgumentException(
                $"Expression '{expression}' must be a simple property access (e.g. e => e.Person).",
                nameof(expression))
        };
    }
}
