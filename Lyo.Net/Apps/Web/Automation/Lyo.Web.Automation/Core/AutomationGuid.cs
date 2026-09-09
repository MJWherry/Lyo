using Lyo.Common.Core.Identifiers;

namespace Lyo.Web.Automation.Core;

/// <summary>Time-ordered identifiers for automation runs and for steps.</summary>
public static class AutomationGuid
{
    /// <summary>Creates a fresh time-ordered version 7 GUID.</summary>
    public static Guid CreateTimeOrdered() => LyoGuid.CreateV7();
}