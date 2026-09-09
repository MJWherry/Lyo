namespace Lyo.Parameters;

/// <summary>A supplied value that can be turned off without being removed, used where parameters hang off a schedule, trigger, or run rather than a definition.</summary>
public abstract class LyoParameterOverrideBase : LyoParameterValueBase
{
    /// <summary>True when this value is applied. Disabled entries stay on the record for history but do not reach the worker.</summary>
    public bool Enabled { get; set; }
}
