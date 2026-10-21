namespace Lyo.StateMachine;

/// <summary>A trigger that carries a typed payload, for example <c>Trigger&lt;JobTrigger, RetryPayload&gt;</c>.</summary>
/// <remarks>
/// Declare one static instance per payload trigger and use it for both <c>Permit</c>/<c>PermitIf</c> and <c>FireAsync</c>. The compiler then requires a
/// <typeparamref name="TPayload" /> wherever the trigger is fired, and guards and <c>OnEntryFrom</c> hooks receive it typed.
/// </remarks>
/// <typeparam name="TTrigger">Trigger type, usually an enum.</typeparam>
/// <typeparam name="TPayload">Data that travels with the trigger.</typeparam>
/// <param name="Value">The underlying trigger value.</param>
public sealed record Trigger<TTrigger, TPayload>(TTrigger Value)
    where TTrigger : notnull;