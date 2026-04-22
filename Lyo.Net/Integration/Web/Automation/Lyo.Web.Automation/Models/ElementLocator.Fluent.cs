using Lyo.Exceptions;
using Lyo.Web.Automation.Models.Enums;

namespace Lyo.Web.Automation.Models;

/// <summary>Factory methods and fluent chaining (<see cref="Then" />) for <see cref="ElementLocator" />.</summary>
public sealed partial record ElementLocator
{
    public static ElementLocator Id(string value) => New(ElementLocatorKind.Id, value);

    public static ElementLocator Name(string value) => New(ElementLocatorKind.Name, value);

    public static ElementLocator CssSelector(string value) => New(ElementLocatorKind.CssSelector, value);

    public static ElementLocator XPath(string value) => New(ElementLocatorKind.XPath, value);

    public static ElementLocator LinkText(string value) => New(ElementLocatorKind.LinkText, value);

    public static ElementLocator PartialLinkText(string value) => New(ElementLocatorKind.PartialLinkText, value);

    public static ElementLocator ClassName(string value) => New(ElementLocatorKind.ClassName, value);

    public static ElementLocator TagName(string value) => New(ElementLocatorKind.TagName, value);

    /// <summary>Builds a path; continue with <see cref="ElementLocatorChain.Then" /> on the returned chain.</summary>
    public ElementLocatorChain Then(ElementLocator next)
    {
        ArgumentHelpers.ThrowIfNull(next, nameof(next));
        return new ElementLocatorChain(this, next);
    }

    private static ElementLocator New(ElementLocatorKind kind, string value)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value, nameof(value));
        return new ElementLocator(kind, value);
    }
}
