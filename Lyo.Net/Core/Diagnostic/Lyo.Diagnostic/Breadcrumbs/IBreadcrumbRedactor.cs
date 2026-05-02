namespace Lyo.Diagnostic.Breadcrumbs;

/// <summary>Optional transform applied to each breadcrumb before it is stored (strip PII, secrets, etc.).</summary>
public interface IBreadcrumbRedactor
{
    Breadcrumb Redact(Breadcrumb breadcrumb);
}