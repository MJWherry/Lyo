namespace Lyo.EntityReference.Models;

/// <summary>Fluent builder for subject/actor relation endpoint pairs (<c>For</c> / <c>From</c>).</summary>
public sealed class EntityRelationBuilder
{
    private readonly EntityRef _subject;

    private EntityRelationBuilder(EntityRef subject) => _subject = subject;

    /// <summary>Starts a relation pair with a subject reference built from the logical type of <typeparamref name="T" /> and key(s).</summary>
    /// <typeparam name="T">CLR type used to resolve the subject entity type discriminator.</typeparam>
    /// <param name="keys">One or more non-empty key segments.</param>
    /// <returns>A builder awaiting an actor endpoint via <see cref="From{T}(object[])" />.</returns>
    public static EntityRelationBuilder For<T>(params object[] keys) => new(EntityRef.For<T>(keys));

    /// <summary>Starts a relation pair with a subject reference built from an entity instance.</summary>
    /// <typeparam name="T">CLR type used to resolve the subject entity type discriminator.</typeparam>
    /// <param name="entity">Non-null instance to read keys from.</param>
    /// <param name="selector">Returns a single key, a non-string <see cref="IEnumerable" /> of keys, or an <c>object[]</c> of keys.</param>
    /// <returns>A builder awaiting an actor endpoint via <see cref="From{T}(object[])" />.</returns>
    public static EntityRelationBuilder For<T>(T entity, Func<T, object?> selector)
        where T : class
        => new(EntityRef.For(entity, selector));

    /// <summary>Starts a relation pair with a subject reference built from an entity instance and collection-expression keys.</summary>
    /// <typeparam name="T">CLR type used to resolve the subject entity type discriminator.</typeparam>
    /// <param name="entity">Non-null instance to read keys from.</param>
    /// <param name="selector">Returns key segments, including via collection expressions (e.g. <c>e => [e.Id]</c>).</param>
    /// <returns>A builder awaiting an actor endpoint via <see cref="From{T}(object[])" />.</returns>
    public static EntityRelationBuilder For<T>(T entity, Func<T, object?[]> selector)
        where T : class
        => new(EntityRef.For(entity, selector));

    /// <summary>Starts a relation pair with an existing subject reference.</summary>
    /// <param name="subject">Entity the relation applies to.</param>
    /// <returns>A builder awaiting an actor endpoint via <see cref="From{T}(object[])" />.</returns>
    public static EntityRelationBuilder For(EntityRef subject) => new(subject);

    /// <summary>Completes the relation pair with an actor reference built from the logical type of <typeparamref name="T" /> and a <see cref="Guid" /> key.</summary>
    /// <typeparam name="T">CLR type used to resolve the actor entity type discriminator.</typeparam>
    /// <param name="actorId">Identifier stored using the GUID's default string format.</param>
    /// <returns>The subject/actor endpoint pair.</returns>
    public EntityRelationEndpoints From<T>(Guid actorId) => From(EntityRef.For<T>(actorId));

    /// <summary>Completes the relation pair with an actor reference built from the logical type of <typeparamref name="T" /> and a string key.</summary>
    /// <typeparam name="T">CLR type used to resolve the actor entity type discriminator.</typeparam>
    /// <param name="actorId">Non-empty identifier string.</param>
    /// <returns>The subject/actor endpoint pair.</returns>
    public EntityRelationEndpoints From<T>(string actorId) => From(EntityRef.For<T>(actorId));

    /// <summary>Completes the relation pair with an actor reference built from the logical type of <typeparamref name="T" /> and key(s).</summary>
    /// <typeparam name="T">CLR type used to resolve the actor entity type discriminator.</typeparam>
    /// <param name="keys">One or more non-empty key segments.</param>
    /// <returns>The subject/actor endpoint pair.</returns>
    public EntityRelationEndpoints From<T>(params object[] keys) => From(EntityRef.For<T>(keys));

    /// <summary>Completes the relation pair with an actor reference built from an entity instance.</summary>
    /// <typeparam name="T">CLR type used to resolve the actor entity type discriminator.</typeparam>
    /// <param name="entity">Non-null instance to read keys from.</param>
    /// <param name="selector">Returns a single key, a non-string <see cref="IEnumerable" /> of keys, or an <c>object[]</c> of keys.</param>
    /// <returns>The subject/actor endpoint pair.</returns>
    public EntityRelationEndpoints From<T>(T entity, Func<T, object?> selector)
        where T : class
        => From(EntityRef.For(entity, selector));

    /// <summary>Completes the relation pair with an actor reference built from an entity instance and collection-expression keys.</summary>
    /// <typeparam name="T">CLR type used to resolve the actor entity type discriminator.</typeparam>
    /// <param name="entity">Non-null instance to read keys from.</param>
    /// <param name="selector">Returns key segments, including via collection expressions (e.g. <c>e => [e.Id]</c>).</param>
    /// <returns>The subject/actor endpoint pair.</returns>
    public EntityRelationEndpoints From<T>(T entity, Func<T, object?[]> selector)
        where T : class
        => From(EntityRef.For(entity, selector));

    /// <summary>Completes the relation pair with an existing actor reference.</summary>
    /// <param name="actor">Entity that performed or owns the relation.</param>
    /// <returns>The subject/actor endpoint pair.</returns>
    public EntityRelationEndpoints From(EntityRef actor) => new(_subject, actor);
}