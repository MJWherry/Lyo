namespace Lyo.Web.Automation.Playwright.Record;

/// <summary>Result of replacing the in-memory plan from the panel JSON editor.</summary>
public sealed record RecordedPlanEdit(bool Applied, string? Error);
