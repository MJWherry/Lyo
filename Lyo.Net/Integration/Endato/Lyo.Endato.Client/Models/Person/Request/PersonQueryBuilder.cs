using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Endato.Client.Models.Person.Request;

/// <summary>Builder for creating Person Search requests.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class PersonQueryBuilder
{
    private readonly List<PersonQueryAddress> _addresses = [];
    private readonly List<PersonQueryName> _akas = [];
    private readonly List<string> _filterOptions = [];
    private readonly List<string> _includes = [];
    private readonly PersonQuery _query = new();
    private readonly List<PersonQueryName> _relatives = [];
    private readonly List<string> _tahoeIds = [];

    public PersonQueryBuilder WithFirstName(string? firstName)
    {
        _query.FirstName = firstName;
        return this;
    }

    public PersonQueryBuilder WithMiddleName(string? middleName)
    {
        _query.MiddleName = middleName;
        return this;
    }

    public PersonQueryBuilder WithLastName(string? lastName)
    {
        _query.LastName = lastName;
        return this;
    }

    public PersonQueryBuilder WithDateOfBirth(string? dateOfBirth)
    {
        _query.DateOfBirth = dateOfBirth;
        return this;
    }

    public PersonQueryBuilder WithAge(int? age)
    {
        _query.Age = age;
        return this;
    }

    public PersonQueryBuilder WithAgeRange(string? ageRange)
    {
        _query.AgeRange = ageRange;
        return this;
    }

    public PersonQueryBuilder WithAgeRange(int? minAge, int? maxAge)
    {
        _query.AgeRangeMinAge = minAge;
        _query.AgeRangeMaxAge = maxAge;
        return this;
    }

    public PersonQueryBuilder WithPhone(string? phone)
    {
        _query.Phone = phone;
        return this;
    }

    public PersonQueryBuilder WithEmail(string? email)
    {
        _query.Email = email;
        return this;
    }

    public PersonQueryBuilder WithClientIp(string? clientIp)
    {
        _query.ClientIp = clientIp;
        return this;
    }

    public PersonQueryBuilder WithDobFormat(string? dobFormat)
    {
        _query.DobFormat = dobFormat;
        return this;
    }

    public PersonQueryBuilder WithCharOffsets(int? firstNameOffset, int? lastNameOffset)
    {
        _query.FirstNameCharOffset = firstNameOffset;
        _query.LastNameCharOffset = lastNameOffset;
        return this;
    }

    public PersonQueryBuilder WithMaxAddressYears(int? years)
    {
        _query.MaxAddressYears = years;
        return this;
    }

    public PersonQueryBuilder WithMaxPhoneYears(int? years)
    {
        _query.MaxPhoneYears = years;
        return this;
    }

    public PersonQueryBuilder WithPage(int? page)
    {
        _query.Page = page;
        return this;
    }

    public PersonQueryBuilder WithResultsPerPage(int resultsPerPage)
    {
        _query.ResultsPerPage = resultsPerPage;
        return this;
    }

    public PersonQueryBuilder WithTahoeIds(params string[] tahoeIds)
    {
        ArgumentHelpers.ThrowIfNull(tahoeIds);
        _tahoeIds.AddRange(tahoeIds.Where(static id => !string.IsNullOrWhiteSpace(id)));
        return this;
    }

    public PersonQueryBuilder WithIncludes(params string[] includes)
    {
        ArgumentHelpers.ThrowIfNull(includes);
        _includes.AddRange(includes.Where(static i => !string.IsNullOrWhiteSpace(i)));
        return this;
    }

    public PersonQueryBuilder WithFilterOptions(params string[] filterOptions)
    {
        ArgumentHelpers.ThrowIfNull(filterOptions);
        _filterOptions.AddRange(filterOptions.Where(static o => !string.IsNullOrWhiteSpace(o)));
        return this;
    }

    public PersonQueryBuilder AddAddress(string? addressLine1, string? addressLine2 = null, string? county = null)
        => AddAddress(address => {
            address.AddressLine1 = addressLine1;
            address.AddressLine2 = addressLine2;
            address.County = county;
        });

    public PersonQueryBuilder AddAddress(Action<PersonQueryAddress> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        var address = new PersonQueryAddress();
        configure(address);
        _addresses.Add(address);
        return this;
    }

    public PersonQueryBuilder AddAka(string? firstName, string? lastName, string? middleName = null, string? prefix = null, string? suffix = null)
        => AddAka(name => {
            name.Prefix = prefix;
            name.FirstName = firstName;
            name.MiddleName = middleName;
            name.LastName = lastName;
            name.Suffix = suffix;
        });

    public PersonQueryBuilder AddAka(Action<PersonQueryName> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        var aka = new PersonQueryName();
        configure(aka);
        _akas.Add(aka);
        return this;
    }

    public PersonQueryBuilder AddRelative(string? firstName, string? lastName, string? middleName = null, string? prefix = null, string? suffix = null)
        => AddRelative(name => {
            name.Prefix = prefix;
            name.FirstName = firstName;
            name.MiddleName = middleName;
            name.LastName = lastName;
            name.Suffix = suffix;
        });

    public PersonQueryBuilder AddRelative(Action<PersonQueryName> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        var relative = new PersonQueryName();
        configure(relative);
        _relatives.Add(relative);
        return this;
    }

    public PersonQuery Build()
    {
        if (_akas.Count > 0)
            _query.Akas = _akas;

        if (_relatives.Count > 0)
            _query.Relatives = _relatives;

        if (_addresses.Count > 0)
            _query.Addresses = _addresses;

        if (_tahoeIds.Count > 0)
            _query.TahoeIds = _tahoeIds;

        if (_includes.Count > 0)
            _query.Includes = _includes;

        if (_filterOptions.Count > 0)
            _query.FilterOptions = _filterOptions;

        return _query;
    }

    public static PersonQueryBuilder New() => new();

    public static PersonQueryBuilder Create(string firstName, string lastName, int? age = null, string? dateOfBirth = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(lastName);
        var builder = New().WithFirstName(firstName).WithLastName(lastName);
        if (age.HasValue)
            builder.WithAge(age);

        if (!string.IsNullOrWhiteSpace(dateOfBirth))
            builder.WithDateOfBirth(dateOfBirth);

        return builder;
    }

    public override string ToString() => _query.ToString();
}