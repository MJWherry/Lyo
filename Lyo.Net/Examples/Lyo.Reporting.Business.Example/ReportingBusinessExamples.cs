using Lyo.Reporting.Web;

namespace Lyo.Reporting.Business.Example;

/// <summary>Registers sample business-document templates with <see cref="ReportEmbedBinder"/> so workbench pickers and generate can resolve them.</summary>
public static class ReportingBusinessExamples
{
    /// <summary>FullNames of the example business document components in this assembly.</summary>
    public static IReadOnlyList<string> ComponentTypes { get; } = [
        typeof(Invoice).FullName!,
        typeof(InvoiceCompact).FullName!,
        typeof(InvoiceProfessional).FullName!,
        typeof(Receipt).FullName!,
        typeof(PurchaseOrder).FullName!,
        typeof(Estimate).FullName!,
        typeof(StatementOfWork).FullName!
    ];

    /// <summary>Assigns <see cref="ComponentTypes"/> onto <see cref="ReportEmbedBinder.KnownComponentTypes"/> so this assembly is loaded for FullName resolve.</summary>
    public static void Register()
        => ReportEmbedBinder.KnownComponentTypes = ComponentTypes;
}
