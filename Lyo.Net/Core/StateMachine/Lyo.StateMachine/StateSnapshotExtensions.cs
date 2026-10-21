using System.Text.Json;
using Lyo.Exceptions;
using Lyo.Json;
using Lyo.StateMachine.Models;

namespace Lyo.StateMachine;

/// <summary>Typed access to <see cref="StateSnapshot{TState}.DataJson" />.</summary>
public static class StateSnapshotExtensions
{
    internal static readonly JsonSerializerOptions DefaultJson = LyoJsonSerializerOptions.Create();

    /// <summary>Deserializes the snapshot's data, or returns default when it has none.</summary>
    /// <param name="snapshot">Snapshot to read.</param>
    /// <param name="options">Serializer options. Defaults to <c>LyoJsonSerializerOptions.Create()</c>; use the same options that wrote the data.</param>
    public static TData? GetData<TState, TData>(this StateSnapshot<TState> snapshot, JsonSerializerOptions? options = null)
        where TState : notnull
    {
        ArgumentHelpers.ThrowIfNull(snapshot);
        return snapshot.DataJson is null ? default : JsonSerializer.Deserialize<TData>(snapshot.DataJson, options ?? DefaultJson);
    }

    /// <summary>Returns a copy of the snapshot with <paramref name="data" /> serialized into <see cref="StateSnapshot{TState}.DataJson" />. The version is unchanged.</summary>
    public static StateSnapshot<TState> WithData<TState, TData>(this StateSnapshot<TState> snapshot, TData data, JsonSerializerOptions? options = null)
        where TState : notnull
    {
        ArgumentHelpers.ThrowIfNull(snapshot);
        return snapshot with { DataJson = JsonSerializer.Serialize(data, options ?? DefaultJson) };
    }
}