namespace Lyo.Diagnostic.Breadcrumbs;

/// <summary>Optional transform applied to each breadcrumb before it is stored (strip PII, secrets, and similar).</summary>
public interface IBreadcrumbRedactor
{
    Breadcrumb Redact(Breadcrumb breadcrumb);
}