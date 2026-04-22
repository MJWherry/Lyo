using Lyo.Common;
using Lyo.Common.Identifiers;

namespace Lyo.Web.Automation.Core;

/// <summary>Time-ordered identifiers for automation runs and steps.</summary>
public static class AutomationGuid
{
    /// <summary>Creates a new time-ordered version 7 GUID.</summary>
    public static Guid CreateTimeOrdered() => LyoGuid.CreateV7();
}
