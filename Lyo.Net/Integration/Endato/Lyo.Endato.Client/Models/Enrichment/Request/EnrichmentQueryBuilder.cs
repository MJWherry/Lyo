using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Endato.Client.Models.Enrichment.Request;

/// <summary>Builder for creating Contact Enrichment requests.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class EnrichmentQueryBuilder
{
    private readonly EnrichmentQuery _query = new();

    public EnrichmentQueryBuilder WithName(string? firstName, string? lastName, string? middleName = null)
    {
        _query.FirstName = firstName;
        _query.LastName = lastName;
        _query.MiddleName = middleName;
        return this;
    }

    public EnrichmentQueryBuilder WithDateOfBirth(string? dateOfBirth)
    {
        _query.DateOfBirth = dateOfBirth;
        return this;
    }

    public EnrichmentQueryBuilder WithAge(int? age)
    {
        _query.Age = age;
        return this;
    }

    public EnrichmentQueryBuilder WithPhone(string? phone)
    {
        _query.Phone = phone;
        return this;
    }

    public EnrichmentQueryBuilder WithEmail(string? email)
    {
        _query.Email = email;
        return this;
    }

    public EnrichmentQueryBuilder WithAddress(string? addressLine1, string? addressLine2 = null)
        => WithAddress(address => {
            address.AddressLine1 = addressLine1;
            address.AddressLine2 = addressLine2;
        });

    public EnrichmentQueryBuilder WithAddress(Action<Address> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        var address = new Address();
        configure(address);
        _query.Address = address;
        return this;
    }

    public EnrichmentQuery Build()
    {
        var identifierCount = 0;
        if (HasName())
            identifierCount++;

        if (!string.IsNullOrWhiteSpace(_query.Phone))
            identifierCount++;

        if (!string.IsNullOrWhiteSpace(_query.Email))
            identifierCount++;

        if (HasAddress())
            identifierCount++;

        if (identifierCount < 2)
            throw new InvalidOperationException("Contact Enrichment requires at least two of: name, phone, email, or address.");

        return _query;
    }

    public static EnrichmentQueryBuilder New() => new();

    public static EnrichmentQueryBuilder Create(string firstName, string lastName)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(lastName);
        return New().WithName(firstName, lastName);
    }

    private bool HasName()
        => !string.IsNullOrWhiteSpace(_query.FirstName)
            || !string.IsNullOrWhiteSpace(_query.LastName)
            || !string.IsNullOrWhiteSpace(_query.MiddleName);

    private bool HasAddress()
        => _query.Address != null
            && (!string.IsNullOrWhiteSpace(_query.Address.AddressLine1)
                || !string.IsNullOrWhiteSpace(_query.Address.AddressLine2));

    public override string ToString() => _query.ToString() ?? nameof(EnrichmentQueryBuilder);
}
