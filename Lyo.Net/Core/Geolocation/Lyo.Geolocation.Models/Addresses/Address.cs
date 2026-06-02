using System.Diagnostics;
using Lyo.Common.Enums;
using Lyo.Common.Extensions;
using Lyo.EntityReference.Models;
using Lyo.Geolocation.Models.Coordinates;

namespace Lyo.Geolocation.Models.Addresses;

/// <summary>Unified address model that handles both US and international addresses</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class Address : IEquatable<Address>, IEntitySourceDerived
{
    /// <summary>Unique identifier for the address</summary>
    public Guid Id { get; set; }

    // Street address components (US-style, Endato-aligned)
    /// <summary>House or street number</summary>
    public string? HouseNumber { get; set; }

    /// <summary>Street pre-direction (N, S, E, W)</summary>
    public string? StreetPreDirection { get; set; }

    /// <summary>Street name</summary>
    public string? StreetName { get; set; }

    /// <summary>Street post-direction (N, S, E, W)</summary>
    public string? StreetPostDirection { get; set; }

    /// <summary>Street type (St, Ave, Blvd, etc.)</summary>
    public string? StreetType { get; set; }

    // Alternative street address (international-style)
    /// <summary>Full street address line (for international addresses)</summary>
    public string? StreetAddress { get; set; }

    /// <summary>Additional address line</summary>
    public string? StreetAddressLine2 { get; set; }

    // Unit/Apartment
    /// <summary>Unit or apartment number</summary>
    public string? Unit { get; set; }

    /// <summary>Unit type (Apt, Unit, Suite, etc.)</summary>
    public string? UnitType { get; set; }

    // City and locality
    /// <summary>City name</summary>
    public string? City { get; set; }

    /// <summary>Sub-locality (neighborhood, district, borough)</summary>
    public string? SubLocality { get; set; }

    // State/Province (now string instead of enum)
    /// <summary>State (US) or Province/State (international)</summary>
    public string? State { get; set; }

    /// <summary>Province (for countries that use provinces)</summary>
    public string? Province { get; set; }

    // Postal codes
    /// <summary>US zipcode</summary>
    public string? Zipcode { get; set; }

    /// <summary>US zipcode+4 extension</summary>
    public string? Zipcode4 { get; set; }

    /// <summary>International postal code</summary>
    public string? PostalCode { get; set; }

    // Country
    /// <summary>Country code</summary>
    public CountryCode CountryCode { get; set; }

    // Administrative areas
    /// <summary>County name</summary>
    public string? County { get; set; }

    /// <summary>Sub-administrative area (county, region)</summary>
    public string? SubAdministrativeArea { get; set; }

    // Geographic coordinate
    /// <summary>Geographic coordinate</summary>
    public GeoCoordinate? Coordinate { get; set; }

    /// <summary>Single-line formatted address when known.</summary>
    public string? FullAddress { get; set; }

    // Endato-style enrichment (nullable)
    public bool? IsDeliverable { get; set; }

    public bool? IsMergedAddress { get; set; }

    public bool? IsPublic { get; set; }

    public string? PropertyIndicator { get; set; }

    public string? BldgCode { get; set; }

    public string? UtilityCode { get; set; }

    public int? UnitCount { get; set; }

    public DateTime? FirstReportedDate { get; set; }

    public DateTime? LastReportedDate { get; set; }

    public DateTime? PublicFirstSeenDate { get; set; }

    public double? GeocodeConfidence { get; set; }

    /// <summary>Overflow vendor-specific fields (serialized at persistence boundary).</summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; set; }

    // Additional properties
    /// <summary>Time zone information</summary>
    public GeoTimeZone? TimeZone { get; set; }

    /// <summary>Address type (Residential, Commercial, POBox, etc.)</summary>
    public AddressType? AddressType { get; set; }

    /// <summary>Validation status</summary>
    public AddressValidationStatus ValidationStatus { get; set; } = AddressValidationStatus.Unvalidated;

    /// <summary>Date when address was last validated</summary>
    public DateTime? LastValidated { get; set; }

    /// <summary>Import provenance (Google place, Endato hash, etc.).</summary>
    public EntitySourceRecord? Source { get; set; }

    /// <inheritdoc />
    public DateTime? LocallyModifiedAt { get; set; }

    public bool Equals(Address? other)
    {
        if (other == null)
            return false;

        return GetCanonicalForm() == other.GetCanonicalForm();
    }

    /// <summary>Gets formatted street address</summary>
    public string GetFormattedStreet()
    {
        // If StreetAddress is provided, use it
        if (!StreetAddress.IsNullOrEmpty())
            return StreetAddress;

        // Otherwise, build from components
        var parts = new List<string>();
        if (!HouseNumber.IsNullOrEmpty())
            parts.Add(HouseNumber!);

        if (!StreetPreDirection.IsNullOrEmpty())
            parts.Add(StreetPreDirection!);

        if (!StreetName.IsNullOrEmpty())
            parts.Add(StreetName!);

        if (!StreetPostDirection.IsNullOrEmpty())
            parts.Add(StreetPostDirection!);

        if (!StreetType.IsNullOrEmpty())
            parts.Add(StreetType!);

        var street = string.Join(" ", parts);

        // Add unit if present
        if (!Unit.IsNullOrEmpty()) {
            var unitStr = UnitType.IsNullOrEmpty() ? $"Apt {Unit}" : $"{UnitType} {Unit}";
            street += $" {unitStr}";
        }

        return street;
    }

    /// <summary>Gets formatted address based on format type</summary>
    public string GetFormattedAddress(AddressFormat format = AddressFormat.Standard)
    {
        switch (format) {
            case AddressFormat.SingleLine:
                return GetSingleLineFormat();
            case AddressFormat.MultiLine:
                return GetMultiLineFormat();
            case AddressFormat.Postal:
                return GetPostalFormat();
            case AddressFormat.Standard:
            default:
                return GetStandardFormat();
        }
    }

    /// <summary>Standard format (comma-separated)</summary>
    private string GetStandardFormat()
    {
        var parts = new List<string>();

        // Street address
        var street = GetFormattedStreet();
        if (!street.IsNullOrEmpty())
            parts.Add(street);

        if (!StreetAddressLine2.IsNullOrEmpty())
            parts.Add(StreetAddressLine2!);

        // City
        if (!City.IsNullOrEmpty())
            parts.Add(City!);

        // State/Province
        var stateProvince = !State.IsNullOrEmpty() ? State : Province;
        if (!stateProvince.IsNullOrEmpty())
            parts.Add(stateProvince);

        // Postal code
        var postalCode = !Zipcode.IsNullOrEmpty() ? !Zipcode4.IsNullOrEmpty() ? $"{Zipcode}-{Zipcode4}" : Zipcode : PostalCode;
        if (!postalCode.IsNullOrEmpty())
            parts.Add(postalCode);

        // Country
        if (CountryCode != CountryCode.UU) {
            var country = CountryCode.GetDescription();
            if (!country.IsNullOrEmpty())
                parts.Add(country);
        }

        return string.Join(", ", parts);
    }

    /// <summary>Single line format (compact)</summary>
    private string GetSingleLineFormat() => GetStandardFormat().Replace(", ", " ");

    /// <summary>Multi-line format (for mailing labels)</summary>
    private string GetMultiLineFormat()
    {
        var lines = new List<string>();
        var street = GetFormattedStreet();
        if (!street.IsNullOrEmpty())
            lines.Add(street);

        if (!StreetAddressLine2.IsNullOrEmpty())
            lines.Add(StreetAddressLine2!);

        var cityStateZip = new List<string>();
        if (!City.IsNullOrEmpty())
            cityStateZip.Add(City!);

        var stateProvince = !State.IsNullOrEmpty() ? State : Province;
        if (!stateProvince.IsNullOrEmpty())
            cityStateZip.Add(stateProvince);

        var postalCode = !Zipcode.IsNullOrEmpty() ? !Zipcode4.IsNullOrEmpty() ? $"{Zipcode}-{Zipcode4}" : Zipcode : PostalCode;
        if (!postalCode.IsNullOrEmpty())
            cityStateZip.Add(postalCode);

        if (cityStateZip.Any())
            lines.Add(string.Join(" ", cityStateZip));

        if (CountryCode != CountryCode.UU) {
            var country = CountryCode.GetDescription();
            if (!country.IsNullOrEmpty())
                lines.Add(country);
        }

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>Postal format (for mailing labels)</summary>
    private string GetPostalFormat() => GetMultiLineFormat();

    /// <summary>Checks if address is valid (has minimum required fields)</summary>
    public bool IsValid()
        => (!string.IsNullOrEmpty(StreetAddress) || (!string.IsNullOrEmpty(HouseNumber) && !string.IsNullOrEmpty(StreetName))) && !string.IsNullOrEmpty(City) &&
            CountryCode != CountryCode.UU;

    /// <summary>Checks if address is complete (has all recommended fields)</summary>
    public bool IsComplete()
        => IsValid() && (!string.IsNullOrEmpty(State) || !string.IsNullOrEmpty(Province)) && (!string.IsNullOrEmpty(Zipcode) || !string.IsNullOrEmpty(PostalCode));

    /// <summary>Checks if address is in the United States</summary>
    public bool IsInUnitedStates() => CountryCode == CountryCode.US;

    /// <summary>Normalizes address formatting (standardizes abbreviations, casing, etc.)</summary>
    public Address Normalize()
    {
        var normalized = new Address {
            Id = Id,
            Source = Source,
            HouseNumber = HouseNumber?.Trim(),
            StreetPreDirection = StreetPreDirection?.Trim(),
            StreetName = StreetName?.Trim(),
            StreetPostDirection = StreetPostDirection?.Trim(),
            StreetType = NormalizeStreetType(StreetType),
            StreetAddress = StreetAddress?.Trim(),
            StreetAddressLine2 = StreetAddressLine2?.Trim(),
            Unit = Unit?.Trim(),
            UnitType = NormalizeUnitType(UnitType),
            City = NormalizeCity(City),
            SubLocality = SubLocality?.Trim(),
            State = State?.Trim().ToUpperInvariant(),
            Province = Province?.Trim(),
            Zipcode = Zipcode?.Trim(),
            Zipcode4 = Zipcode4?.Trim(),
            PostalCode = PostalCode?.Trim().ToUpperInvariant(),
            CountryCode = CountryCode,
            County = County?.Trim(),
            SubAdministrativeArea = SubAdministrativeArea?.Trim(),
            Coordinate = Coordinate,
            TimeZone = TimeZone,
            AddressType = AddressType,
            ValidationStatus = ValidationStatus,
            LastValidated = LastValidated,
            FullAddress = FullAddress?.Trim(),
            IsDeliverable = IsDeliverable,
            IsMergedAddress = IsMergedAddress,
            IsPublic = IsPublic,
            PropertyIndicator = PropertyIndicator,
            BldgCode = BldgCode,
            UtilityCode = UtilityCode,
            UnitCount = UnitCount,
            FirstReportedDate = FirstReportedDate,
            LastReportedDate = LastReportedDate,
            PublicFirstSeenDate = PublicFirstSeenDate,
            GeocodeConfidence = GeocodeConfidence,
            Metadata = Metadata
        };

        return normalized;
    }

    private static string? NormalizeStreetType(string? streetType)
    {
        if (streetType.IsNullOrEmpty())
            return streetType;

        var abbreviations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "street", "St" },
            { "avenue", "Ave" },
            { "road", "Rd" },
            { "boulevard", "Blvd" },
            { "drive", "Dr" },
            { "lane", "Ln" },
            { "court", "Ct" },
            { "circle", "Cir" },
            { "way", "Way" },
            { "parkway", "Pkwy" },
            { "highway", "Hwy" },
            { "terrace", "Ter" }
        };

        var trimmed = streetType.Trim();
        return abbreviations.TryGetValue(trimmed, out var abbrev) ? abbrev : trimmed;
    }

    private static string? NormalizeUnitType(string? unitType)
    {
        if (unitType.IsNullOrEmpty())
            return unitType;

        var abbreviations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "apartment", "Apt" },
            { "suite", "Ste" },
            { "unit", "Unit" },
            { "building", "Bldg" },
            { "floor", "Fl" },
            { "room", "Rm" }
        };

        var trimmed = unitType.Trim();
        return abbreviations.TryGetValue(trimmed, out var abbrev) ? abbrev : trimmed;
    }

    private static string? NormalizeCity(string? city)
    {
        if (city.IsNullOrEmpty())
            return city;

        // Capitalize first letter of each word
        return string.Join(" ", city.Split([' '], StringSplitOptions.RemoveEmptyEntries).Select(word => char.ToUpperInvariant(word[0]) + word.Substring(1).ToLowerInvariant()));
    }

    /// <summary>Gets canonical form (standardized string representation)</summary>
    public string GetCanonicalForm() => Normalize().GetFormattedAddress(AddressFormat.SingleLine).ToUpperInvariant();

    /// <summary>Checks if this address is similar to another (within tolerance)</summary>
    public bool IsSimilarTo(Address? other, double toleranceMeters = 100)
    {
        if (other == null)
            return false;

        // If both have coordinates, check distance
        if (Coordinate != null && other.Coordinate != null)
            return Coordinate.DistanceTo(other.Coordinate) <= toleranceMeters;

        // Otherwise, compare canonical forms
        return GetCanonicalForm() == other.GetCanonicalForm();
    }

    /// <summary>Creates an address from components</summary>
    public static Address FromComponents(string? street, string? city, string? state, string? zip, CountryCode country)
        => new() {
            StreetAddress = street,
            City = city,
            State = state,
            Zipcode = zip,
            CountryCode = country
        };

    /// <summary>Creates a US address</summary>
    public static Address CreateUSAddress(string street, string city, string state, string zip) => FromComponents(street, city, state, zip, CountryCode.US);

    /// <summary>Creates an international address</summary>
    public static Address CreateInternationalAddress(string street, string city, string country)
        => FromComponents(street, city, null, null, (CountryCode)Enum.Parse(typeof(CountryCode), country));

    public override bool Equals(object? obj) => obj is Address other && Equals(other);

    public override int GetHashCode() => GetCanonicalForm().GetHashCode();

    public override string ToString() => GetFormattedAddress();

    public static bool operator ==(Address? left, Address? right) => Equals(left, right);

    public static bool operator !=(Address? left, Address? right) => !Equals(left, right);
}

/// <summary>Address formatting options</summary>
public enum AddressFormat
{
    /// <summary>Standard comma-separated format</summary>
    Standard,

    /// <summary>Single line compact format</summary>
    SingleLine,

    /// <summary>Multi-line format for mailing labels</summary>
    MultiLine,

    /// <summary>Postal format (same as MultiLine)</summary>
    Postal
}

/// <summary>Address type classification</summary>
public enum AddressType
{
    /// <summary>Residential address</summary>
    Residential,

    /// <summary>Commercial/business address</summary>
    Commercial,

    /// <summary>PO Box</summary>
    POBox,

    /// <summary>Military address</summary>
    Military,

    /// <summary>Other type</summary>
    Other
}

/// <summary>Address validation status</summary>
public enum AddressValidationStatus
{
    /// <summary>Not yet validated</summary>
    Unvalidated,

    /// <summary>Validated and confirmed</summary>
    Validated,

    /// <summary>Validation failed - address is invalid</summary>
    Invalid,

    /// <summary>Validation returned suggestions</summary>
    SuggestionsAvailable
}