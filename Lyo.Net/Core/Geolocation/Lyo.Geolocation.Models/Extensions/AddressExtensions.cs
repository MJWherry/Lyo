using Lyo.Common.Enums;
using Lyo.Geolocation.Models.Addresses;

namespace Lyo.Geolocation.Models.Extensions;

/// <summary>Extension methods for Address model</summary>
public static class AddressExtensions
{
    /// <summary>Checks if the address is in the United States</summary>
    public static bool IsInUnitedStates(this Address address) => address?.CountryCode == CountryCode.US;

    /// <summary>Gets the state abbreviation (for US addresses)</summary>
    public static string? GetStateAbbreviation(this Address address)
    {
        if (address == null || !address.IsInUnitedStates())
            return null;

        return address.State?.Length == 2 ? address.State : address.State;
    }

    /// <summary>Converts address to mailing label format</summary>
    public static string ToMailingFormat(this Address address) => address?.GetFormattedAddress(AddressFormat.Postal) ?? string.Empty;

    /// <summary>Gets the postal code (handles both US and international)</summary>
    public static string? GetPostalCode(this Address address)
    {
        if (address == null)
            return null;

        if (!string.IsNullOrEmpty(address.Zipcode))
            return string.IsNullOrEmpty(address.Zipcode4) ? address.Zipcode : $"{address.Zipcode}-{address.Zipcode4}";

        return address.PostalCode;
    }

    /// <summary>Gets the state or province (whichever is available)</summary>
    public static string? GetStateOrProvince(this Address address) => address == null ? null : address.State ?? address.Province;
}