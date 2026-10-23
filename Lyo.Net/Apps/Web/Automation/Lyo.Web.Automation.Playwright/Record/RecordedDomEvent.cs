using System.Diagnostics;
using Lyo.Web.Automation.Models;
using Lyo.Web.Automation.Models.Enums;

namespace Lyo.Web.Automation.Playwright.Record;

/// <summary>What the page reported. The mapper turns a sequence of these into plan steps.</summary>
public enum RecordedDomEventKind
{
    Click,
    Input,
    Blur,
    Select,
    Key,
    Navigate,
    Pick,
    Insert
}

/// <summary>Armed panel tool that captured a click instead of recording it.</summary>
public enum RecordedPickTool
{
    Images,
    Links,
    Text,
    Attribute
}

/// <summary>A click made while a pick tool is armed. <see cref="Repeating" /> is true when a parent has two or more children of <see cref="DescendantTag" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record RecordedPick(
    RecordedPickTool Tool,
    bool Repeating,
    ElementLocator? ParentLocator,
    string? DescendantTag,
    ElementLocator? TargetLocator,
    string VariableName,
    string? AttributeName = null);

/// <summary>
/// A step typed into the panel. <see cref="LocatorKind" /> and <see cref="Value" /> locate an element. <see cref="Action" /> is click, input, or clear. <see cref="Text" />
/// is what an input action types.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record RecordedInsert(
    string StepType,
    string? VariableName = null,
    string? Value = null,
    int? DelayMilliseconds = null,
    string? Directory = null,
    IReadOnlyList<string>? Values = null,
    ElementLocatorKind? LocatorKind = null,
    string? Action = null,
    string? Text = null);

/// <summary>One browser event, already reduced to a locator by the page script.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record RecordedDomEvent(
    RecordedDomEventKind Kind,
    string? ElementKey = null,
    ElementLocator? Locator = null,
    string? Text = null,
    string? Value = null,
    string? Url = null,
    bool CausedByRecordedAction = false,
    RecordedPick? Pick = null,
    RecordedInsert? Insert = null);