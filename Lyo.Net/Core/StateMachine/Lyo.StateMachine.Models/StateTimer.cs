namespace Lyo.StateMachine.Models;

/// <summary>A timer trigger that is due, as returned by <c>StateMachineDefinition.GetDueTrigger</c>.</summary>
/// <param name="Trigger">Trigger to fire.</param>
/// <param name="DueAtUtc">When it became due.</param>
public sealed record StateTimer<TTrigger>(TTrigger Trigger, DateTime DueAtUtc)
    where TTrigger : notnull;