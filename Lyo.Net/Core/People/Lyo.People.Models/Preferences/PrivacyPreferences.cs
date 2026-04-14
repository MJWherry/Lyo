namespace Lyo.People.Models.Preferences;

/// <summary>Privacy and data sharing preferences for a person</summary>
public class PrivacyPreferences
{
    /// <summary>Whether the person allows their data to be shared with third parties</summary>
    public bool ShareDataWithThirdParties { get; set; }

    /// <summary>Whether the person allows their data to be used for analytics</summary>
    public bool AllowDataAnalytics { get; set; }

    /// <summary>Whether the person's information can be shown in public directories</summary>
    public bool ShowInPublicDirectory { get; set; }
}