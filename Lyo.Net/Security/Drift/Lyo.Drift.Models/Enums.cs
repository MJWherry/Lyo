namespace Lyo.Drift.Models;

/// <summary>Lifecycle of a drift agent instance.</summary>
public enum DriftInstanceState
{
    Running = 0,
    Stopped = 1
}

/// <summary>What a structure snapshot contains.</summary>
public enum DriftSnapshotKind
{
    FileTree = 0,
    SystemInfo = 1
}

/// <summary>Who computed a diff snapshot.</summary>
public enum DriftDiffSource
{
    Agent = 0,
    Server = 1
}
