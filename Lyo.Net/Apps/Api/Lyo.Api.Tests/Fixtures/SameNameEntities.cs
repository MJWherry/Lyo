// Two entity types sharing a short CLR name in different namespaces. Cache tags and the dynamic entity registry have to keep them distinct.

namespace Lyo.Api.Tests.Fixtures.NamespaceA;

public sealed class Widget
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
