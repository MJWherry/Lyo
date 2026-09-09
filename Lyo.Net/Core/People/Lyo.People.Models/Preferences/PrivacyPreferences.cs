using System.Diagnostics;

namespace Lyo.People.Models.Preferences;

/// <summary>Data-sharing and directory visibility choices for a person.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class PrivacyPreferences
{
    /// <summary>True when third-party sharing is allowed.</summary>
    public bool ShareDataWithThirdParties { get; set; }

    /// <summary>True when analytics use of the data is allowed.</summary>
    public bool AllowDataAnalytics { get; set; }

    /// <summary>True when the person may appear in public directories.</summary>
    public bool ShowInPublicDirectory { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"PrivacyPreferences: thirdParty={ShareDataWithThirdParties}, analytics={AllowDataAnalytics}, directory={ShowInPublicDirectory}";
}
