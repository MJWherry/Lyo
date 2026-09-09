using Lyo.Common.Core.Enums;
using Lyo.Geolocation.Models.Addresses;

namespace Lyo.Geolocation.Models.Extensions;

/// <summary>Helpers for <see cref="Address" />.</summary>
public static class AddressExtensions
{
    extension(Address? address)
    {
        /// <summary>True when the address is in the United States.</summary>
        public bool IsInUnitedStates() => address?.CountryCode == CountryCode.US;

        /// <summary>US state abbreviation, or null when the address is not US.</summary>
        public string? GetStateAbbreviation()
        {
            if (address == null || !address.IsInUnitedStates())
                return null;

            return address.State;
        }

        /// <summary>Formats the address as a mailing label.</summary>
        public string ToMailingFormat() => address?.GetFormattedAddress(AddressFormat.Postal) ?? string.Empty;

        /// <summary>US ZIP (plus +4 when present) or international postal code.</summary>
        public string? GetPostalCode()
        {
            if (address == null)
                return null;

            if (!string.IsNullOrEmpty(address.Zipcode))
                return string.IsNullOrEmpty(address.Zipcode4) ? address.Zipcode : $"{address.Zipcode}-{address.Zipcode4}";

            return address.PostalCode;
        }

        /// <summary>State if set, otherwise province.</summary>
        public string? GetStateOrProvince() => address == null ? null : address.State ?? address.Province;
    }
}