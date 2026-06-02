using Lyo.Common.Enums;
using Lyo.Geolocation.Models.Addresses;

namespace Lyo.Geolocation.Models.Extensions;

/// <summary>Extension methods for Address model</summary>
public static class AddressExtensions
{
    extension(Address? address)
    {
        /// <summary>Checks if the address is in the United States</summary>
        public bool IsInUnitedStates() => address?.CountryCode == CountryCode.US;

        /// <summary>Gets the state abbreviation (for US addresses)</summary>
        public string? GetStateAbbreviation()
        {
            if (address == null || !address.IsInUnitedStates())
                return null;

            return address.State;
        }

        /// <summary>Converts address to mailing label format</summary>
        public string ToMailingFormat() => address?.GetFormattedAddress(AddressFormat.Postal) ?? string.Empty;

        /// <summary>Gets the postal code (handles both US and international)</summary>
        public string? GetPostalCode()
        {
            if (address == null)
                return null;

            if (!string.IsNullOrEmpty(address.Zipcode))
                return string.IsNullOrEmpty(address.Zipcode4) ? address.Zipcode : $"{address.Zipcode}-{address.Zipcode4}";

            return address.PostalCode;
        }

        /// <summary>Gets the state or province (whichever is available)</summary>
        public string? GetStateOrProvince() => address == null ? null : address.State ?? address.Province;
    }
}