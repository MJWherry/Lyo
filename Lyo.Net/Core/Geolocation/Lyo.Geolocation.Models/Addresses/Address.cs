using Lyo.Common;
using Lyo.Common.Enums;
using Lyo.Geolocation.Models.Coordinates;

namespace Lyo.Geolocation.Models.Addresses;

/// <summary>Unified address model that handles both US and international addresses</summary>
public class Address : IEquatable<Address>
{
    /// <summary>Unique identifier for the address</summary>
    public Guid Id { get; set; }

    // Street address components (US-style)
    /// <summary>Street number</summary>
    public string? StreetNumber { get; set; }

    /// <summary>Street pre-direction (N, S, E, W)</summary>
    public CardinalDirection? StreetPreDirection { get; set; }

    /// <summary>Street name</summary>
    public string? StreetName { get; set; }

    /// <summary>Street post-direction (N, S, E, W)</summary>
    public CardinalDirection? StreetPostDirection { get; set; }

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

    // Additional properties
    /// <summary>Time zone information</summary>
    public GeoTimeZone? TimeZone { get; set; }

    /// <summary>Address type (Residential, Commercial, POBox, etc.)</summary>
    public AddressType? AddressType { get; set; }

    /// <summary>Validation status</summary>
    public AddressValidationStatus ValidationStatus { get; set; } = AddressValidationStatus.Unvalidated;

    /// <summary>Date when address was last validated</summary>
    public DateTime? LastValidated { get; set; }

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
        if (!string.IsNullOrEmpty(StreetAddress))
            return StreetAddress;

        // Otherwise, build from components
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(StreetNumber))
            parts.Add(StreetNumber);

        if (StreetPreDirection.HasValue)
            parts.Add(StreetPreDirection.Value.GetDescription());

        if (!string.IsNullOrEmpty(StreetName))
            parts.Add(StreetName);

        if (StreetPostDirection.HasValue)
            parts.Add(StreetPostDirection.Value.GetDescription());

        if (!string.IsNullOrEmpty(StreetType))
            parts.Add(StreetType);

        var street = string.Join(" ", parts);

        // Add unit if present
        if (!string.IsNullOrEmpty(Unit)) {
            var unitStr = string.IsNullOrEmpty(UnitType) ? $"Apt {Unit}" : $"{UnitType} {Unit}";
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
        if (!string.IsNullOrEmpty(street))
            parts.Add(street);

        if (!string.IsNullOrEmpty(StreetAddressLine2))
            parts.Add(StreetAddressLine2);

        // City
        if (!string.IsNullOrEmpty(City))
            parts.Add(City);

        // State/Province
        var stateProvince = !string.IsNullOrEmpty(State) ? State : Province;
        if (!string.IsNullOrEmpty(stateProvince))
            parts.Add(stateProvince);

        // Postal code
        var postalCode = !string.IsNullOrEmpty(Zipcode) ? !string.IsNullOrEmpty(Zipcode4) ? $"{Zipcode}-{Zipcode4}" : Zipcode : PostalCode;
        if (!string.IsNullOrEmpty(postalCode))
            parts.Add(postalCode);

        // Country
        if (CountryCode != CountryCode.UU)
            parts.Add(CountryCode.GetDescription());

        return string.Join(", ", parts);
    }

    /// <summary>Single line format (compact)</summary>
    private string GetSingleLineFormat() => GetStandardFormat().Replace(", ", " ");

    /// <summary>Multi-line format (for mailing labels)</summary>
    private string GetMultiLineFormat()
    {
        var lines = new List<string>();
        var street = GetFormattedStreet();
        if (!string.IsNullOrEmpty(street))
            lines.Add(street);

        if (!string.IsNullOrEmpty(StreetAddressLine2))
            lines.Add(StreetAddressLine2);

        var cityStateZip = new List<string>();
        if (!string.IsNullOrEmpty(City))
            cityStateZip.Add(City);

        var stateProvince = !string.IsNullOrEmpty(State) ? State : Province;
        if (!string.IsNullOrEmpty(stateProvince))
            cityStateZip.Add(stateProvince);

        var postalCode = !string.IsNullOrEmpty(Zipcode) ? !string.IsNullOrEmpty(Zipcode4) ? $"{Zipcode}-{Zipcode4}" : Zipcode : PostalCode;
        if (!string.IsNullOrEmpty(postalCode))
            cityStateZip.Add(postalCode);

        if (cityStateZip.Any())
            lines.Add(string.Join(" ", cityStateZip));

        if (CountryCode != CountryCode.UU)
            lines.Add(CountryCode.GetDescription());

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>Postal format (for mailing labels)</summary>
    private string GetPostalFormat() => GetMultiLineFormat();

    /// <summary>Checks if address is valid (has minimum required fields)</summary>
    public bool IsValid()
        => (!string.IsNullOrEmpty(StreetAddress) || (!string.IsNullOrEmpty(StreetNumber) && !string.IsNullOrEmpty(StreetName))) && !string.IsNullOrEmpty(City) &&
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
            StreetNumber = StreetNumber?.Trim(),
            StreetPreDirection = StreetPreDirection,
            StreetName = StreetName?.Trim(),
            StreetPostDirection = StreetPostDirection,
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
            LastValidated = LastValidated
        };

        return normalized;
    }

    private static string? NormalizeStreetType(string? streetType)
    {
        if (string.IsNullOrEmpty(streetType))
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

        return abbreviations.TryGetValue(streetType.Trim(), out var abbrev) ? abbrev : streetType.Trim();
    }

    private static string? NormalizeUnitType(string? unitType)
    {
        if (string.IsNullOrEmpty(unitType))
            return unitType;

        var abbreviations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "apartment", "Apt" },
            { "suite", "Ste" },
            { "unit", "Unit" },
            { "building", "Bldg" },
            { "floor", "Fl" },
            { "room", "Rm" }
        };

        return abbreviations.TryGetValue(unitType.Trim(), out var abbrev) ? abbrev : unitType.Trim();
    }

    private static string? NormalizeCity(string? city)
    {
        if (string.IsNullOrEmpty(city))
            return city;

        // Capitalize first letter of each word
        return string.Join(" ", city.Split([' '], StringSplitOptions.RemoveEmptyEntries).Select(word => char.ToUpperInvariant(word[0]) + word.Substring(1).ToLowerInvariant()));
    }

    /// <summary>Gets canonical form (standardized string representation)</summary>
    public string GetCanonicalForm() => Normalize().GetFormattedAddress(AddressFormat.SingleLine).ToUpperInvariant();

    /// <summary>Checks if this address is similar to another (within tolerance)</summary>
    public bool IsSimilarTo(Address other, double toleranceMeters = 100)
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