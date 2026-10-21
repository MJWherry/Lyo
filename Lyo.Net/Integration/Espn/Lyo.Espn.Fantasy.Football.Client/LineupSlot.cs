namespace Lyo.Espn.Fantasy.Football.Client;

/// <summary>ESPN lineup slot ids for a standard fantasy roster. The numeric value is the <c>lineupSlotId</c> sent on a lineup move.</summary>
public enum LineupSlot
{
    /// <summary>Quarterback. Slot id 0.</summary>
    Quarterback = 0,

    /// <summary>Running back. Slot id 2.</summary>
    RunningBack = 2,

    /// <summary>Wide receiver. Slot id 4.</summary>
    WideReceiver = 4,

    /// <summary>Tight end. Slot id 6.</summary>
    TightEnd = 6,

    /// <summary>Team defense and special teams. Slot id 16.</summary>
    Defense = 16,

    /// <summary>Kicker. Slot id 17.</summary>
    Kicker = 17,

    /// <summary>Bench. Slot id 20.</summary>
    Bench = 20,

    /// <summary>Injured reserve. Slot id 21.</summary>
    InjuredReserve = 21,

    /// <summary>Flex (running back, wide receiver, or tight end). Slot id 23.</summary>
    Flex = 23
}