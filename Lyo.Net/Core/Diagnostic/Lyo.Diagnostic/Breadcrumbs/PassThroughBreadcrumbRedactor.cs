namespace Lyo.Diagnostic.Breadcrumbs;

/// <summary>Default redactor that returns breadcrumbs unchanged.</summary>
public sealed class PassThroughBreadcrumbRedactor : IBreadcrumbRedactor
{
    /// <summary>Singleton instance.</summary>
    public static PassThroughBreadcrumbRedactor Instance { get; } = new();

    /// <inheritdoc />
    public Breadcrumb Redact(Breadcrumb breadcrumb) => breadcrumb;
}