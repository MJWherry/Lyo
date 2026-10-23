using System.Reflection;
using System.Text.Json;
using Lyo.Common.Core.Conversion;
using Lyo.Exceptions;

namespace Lyo.Algorithms;

/// <summary>In-process preview. Child <c>Algo</c> and <c>score</c> calls resolve on the live catalog. The draft itself is not saved.</summary>
public sealed class AlgorithmPreview : IAlgorithmPreview
{
    private static readonly MethodInfo RunOpen = typeof(AlgorithmPreview).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
        .Single(method => method.Name == nameof(Run) && method.IsGenericMethodDefinition);

    private readonly RuntimeAlgorithmStore _store;

    /// <summary>Creates a preview over the process runtime store.</summary>
    public AlgorithmPreview(RuntimeAlgorithmStore store)
    {
        ArgumentHelpers.ThrowIfNull(store);
        _store = store;
    }

    /// <inheritdoc />
    public async Task<AlgorithmPreviewResult> PreviewAsync(AlgorithmRevisionReq draft, JsonElement context, string? key = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(draft);
        var stepKey = string.IsNullOrWhiteSpace(key) ? "preview" : key;
        AlgorithmKeys.ThrowIfInvalidKey(stepKey);
        var binding = AlgorithmTypeBinding.ParsePayload(draft.Payload);
        var bag = Bind(binding, context);
        var task = (Task)RunOpen.MakeGenericMethod(binding.Result).Invoke(this, [stepKey, draft, bag, ct])!;
        await task.ConfigureAwait(false);
        var execution = task.GetType().GetProperty("Result")!.GetValue(task)!;
        return new() {
            Value = execution.GetType().GetProperty("Value")!.GetValue(execution),
            Result = AlgorithmTypeBinding.Alias(binding.Result),
            Steps = (IReadOnlyList<AlgorithmStep>)execution.GetType().GetProperty("Steps")!.GetValue(execution)!
        };
    }

    private async Task<AlgorithmExecution<TResult>> Run<TResult>(string key, AlgorithmRevisionReq draft, AlgorithmContextBag bag, CancellationToken ct)
        => await _store.ExecutePreviewAsync<AlgorithmContextBag, TResult>(key, draft, bag, ct).ConfigureAwait(false);

    private static AlgorithmContextBag Bind(AlgorithmTypeBinding binding, JsonElement context)
    {
        var bag = new AlgorithmContextBag();
        if (context.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return bag;

        AlgorithmHelpers.ThrowIf(context.ValueKind != JsonValueKind.Object, "Algorithm context must be a JSON object.", "algorithm.context.invalid");
        foreach (var property in context.EnumerateObject()) {
            AlgorithmHelpers.ThrowIf(!AlgorithmContextBag.IsIdentifier(property.Name), $"'{property.Name}' is not an expression identifier.", "algorithm.context.invalid");
            bag.Set(property.Name, Convert(property.Name, property.Value, binding.ParameterType(property.Name)));
        }

        return bag;
    }

    private static object Convert(string name, JsonElement value, Type type)
    {
        if (!TypeConversion.TryFromJsonElement(value, type, out var converted) || converted is null)
            AlgorithmHelpers.Throw($"'{name}' cannot be read as {AlgorithmTypeBinding.Alias(type)}.", "algorithm.context.invalid");

        return converted;
    }
}
