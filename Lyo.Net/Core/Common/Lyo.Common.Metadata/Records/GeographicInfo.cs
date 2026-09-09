using Lyo.Common.Core;
using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Extensions;
using Lyo.Common.Metadata.Extensions;

namespace Lyo.Common.Metadata.Records;

/// <summary>Catalog row for a place: state or province, country, and timezone.</summary>
public record GeographicInfo(USState? State, string? Province, CountryCode Country, string CountryName, string? TimeZoneId)
{
    // Unrecognized location
    public static readonly GeographicInfo Unknown = new(null, null, CountryCode.UU, "Unknown", null);

    // US states
    public static readonly GeographicInfo Alabama = new(USState.AL, null, CountryCode.US, "United States", "America/Chicago");
    public static readonly GeographicInfo Alaska = new(USState.AK, null, CountryCode.US, "United States", "America/Anchorage");
    public static readonly GeographicInfo Arizona = new(USState.AZ, null, CountryCode.US, "United States", "America/Phoenix");
    public static readonly GeographicInfo Arkansas = new(USState.AR, null, CountryCode.US, "United States", "America/Chicago");
    public static readonly GeographicInfo California = new(USState.CA, null, CountryCode.US, "United States", "America/Los_Angeles");
    public static readonly GeographicInfo Colorado = new(USState.CO, null, CountryCode.US, "United States", "America/Denver");
    public static readonly GeographicInfo Connecticut = new(USState.CT, null, CountryCode.US, "United States", "America/New_York");
    public static readonly GeographicInfo Delaware = new(USState.DE, null, CountryCode.US, "United States", "America/New_York");
    public static readonly GeographicInfo DistrictOfColumbia = new(USState.DC, null, CountryCode.US, "United States", "America/New_York");
    public static readonly GeographicInfo Florida = new(USState.FL, null, CountryCode.US, "United States", "America/New_York");
    public static readonly GeographicInfo Georgia = new(USState.GA, null, CountryCode.US, "United States", "America/New_York");
    public static readonly GeographicInfo Hawaii = new(USState.HI, null, CountryCode.US, "United States", "Pacific/Honolulu");
    public static readonly GeographicInfo Idaho = new(USState.ID, null, CountryCode.US, "United States", "America/Denver");
    public static readonly GeographicInfo Illinois = new(USState.IL, null, CountryCode.US, "United States", "America/Chicago");
    public static readonly GeographicInfo Indiana = new(USState.IN, null, CountryCode.US, "United States", "America/Indiana/Indianapolis");
    public static readonly GeographicInfo Iowa = new(USState.IA, null, CountryCode.US, "United States", "America/Chicago");
    public static readonly GeographicInfo Kansas = new(USState.KS, null, CountryCode.US, "United States", "America/Chicago");
    public static readonly GeographicInfo Kentucky = new(USState.KY, null, CountryCode.US, "United States", "America/New_York");
    public static readonly GeographicInfo Louisiana = new(USState.LA, null, CountryCode.US, "United States", "America/Chicago");
    public static readonly GeographicInfo Maine = new(USState.ME, null, CountryCode.US, "United States", "America/New_York");
    public static readonly GeographicInfo Maryland = new(USState.MD, null, CountryCode.US, "United States", "America/New_York");
    public static readonly GeographicInfo Massachusetts = new(USState.MA, null, CountryCode.US, "United States", "America/New_York");
    public static readonly GeographicInfo Michigan = new(USState.MI, null, CountryCode.US, "United States", "America/Detroit");
    public static readonly GeographicInfo Minnesota = new(USState.MN, null, CountryCode.US, "United States", "America/Chicago");
    public static readonly GeographicInfo Mississippi = new(USState.MS, null, CountryCode.US, "United States", "America/Chicago");
    public static readonly GeographicInfo Missouri = new(USState.MO, null, CountryCode.US, "United States", "America/Chicago");
    public static readonly GeographicInfo Montana = new(USState.MT, null, CountryCode.US, "United States", "America/Denver");
    public static readonly GeographicInfo Nebraska = new(USState.NE, null, CountryCode.US, "United States", "America/Chicago");
    public static readonly GeographicInfo Nevada = new(USState.NV, null, CountryCode.US, "United States", "America/Los_Angeles");
    public static readonly GeographicInfo NewHampshire = new(USState.NH, null, CountryCode.US, "United States", "America/New_York");
    public static readonly GeographicInfo NewJersey = new(USState.NJ, null, CountryCode.US, "United States", "America/New_York");
    public static readonly GeographicInfo NewMexico = new(USState.NM, null, CountryCode.US, "United States", "America/Denver");
    public static readonly GeographicInfo NewYork = new(USState.NY, null, CountryCode.US, "United States", "America/New_York");
    public static readonly GeographicInfo NorthCarolina = new(USState.NC, null, CountryCode.US, "United States", "America/New_York");
    public static readonly GeographicInfo NorthDakota = new(USState.ND, null, CountryCode.US, "United States", "America/Chicago");
    public static readonly GeographicInfo Ohio = new(USState.OH, null, CountryCode.US, "United States", "America/New_York");
    public static readonly GeographicInfo Oklahoma = new(USState.OK, null, CountryCode.US, "United States", "America/Chicago");
    public static readonly GeographicInfo Oregon = new(USState.OR, null, CountryCode.US, "United States", "America/Los_Angeles");
    public static readonly GeographicInfo Pennsylvania = new(USState.PA, null, CountryCode.US, "United States", "America/New_York");
    public static readonly GeographicInfo RhodeIsland = new(USState.RI, null, CountryCode.US, "United States", "America/New_York");
    public static readonly GeographicInfo SouthCarolina = new(USState.SC, null, CountryCode.US, "United States", "America/New_York");
    public static readonly GeographicInfo SouthDakota = new(USState.SD, null, CountryCode.US, "United States", "America/Chicago");
    public static readonly GeographicInfo Tennessee = new(USState.TN, null, CountryCode.US, "United States", "America/Chicago");
    public static readonly GeographicInfo Texas = new(USState.TX, null, CountryCode.US, "United States", "America/Chicago");
    public static readonly GeographicInfo Utah = new(USState.UT, null, CountryCode.US, "United States", "America/Denver");
    public static readonly GeographicInfo Vermont = new(USState.VT, null, CountryCode.US, "United States", "America/New_York");
    public static readonly GeographicInfo Virginia = new(USState.VA, null, CountryCode.US, "United States", "America/New_York");
    public static readonly GeographicInfo Washington = new(USState.WA, null, CountryCode.US, "United States", "America/Los_Angeles");
    public static readonly GeographicInfo WestVirginia = new(USState.WV, null, CountryCode.US, "United States", "America/New_York");
    public static readonly GeographicInfo Wisconsin = new(USState.WI, null, CountryCode.US, "United States", "America/Chicago");
    public static readonly GeographicInfo Wyoming = new(USState.WY, null, CountryCode.US, "United States", "America/Denver");

    // Locations outside the US
    public static readonly GeographicInfo Canada = new(null, null, CountryCode.CA, "Canada", "America/Toronto");
    public static readonly GeographicInfo UnitedKingdom = new(null, null, CountryCode.GB, "United Kingdom", "Europe/London");
    public static readonly GeographicInfo Australia = new(null, null, CountryCode.AU, "Australia", "Australia/Sydney");
    public static readonly GeographicInfo Germany = new(null, null, CountryCode.DE, "Germany", "Europe/Berlin");
    public static readonly GeographicInfo France = new(null, null, CountryCode.FR, "France", "Europe/Paris");
    public static readonly GeographicInfo Italy = new(null, null, CountryCode.IT, "Italy", "Europe/Rome");
    public static readonly GeographicInfo Spain = new(null, null, CountryCode.ES, "Spain", "Europe/Madrid");
    public static readonly GeographicInfo Japan = new(null, null, CountryCode.JP, "Japan", "Asia/Tokyo");
    public static readonly GeographicInfo China = new(null, null, CountryCode.CN, "China", "Asia/Shanghai");
    public static readonly GeographicInfo India = new(null, null, CountryCode.IN, "India", "Asia/Kolkata");
    public static readonly GeographicInfo Brazil = new(null, null, CountryCode.BR, "Brazil", "America/Sao_Paulo");
    public static readonly GeographicInfo Mexico = new(null, null, CountryCode.MX, "Mexico", "America/Mexico_City");
    public static readonly GeographicInfo Argentina = new(null, null, CountryCode.AR, "Argentina", "America/Argentina/Buenos_Aires");
    public static readonly GeographicInfo SouthAfrica = new(null, null, CountryCode.ZA, "South Africa", "Africa/Johannesburg");
    public static readonly GeographicInfo NewZealand = new(null, null, CountryCode.NZ, "New Zealand", "Pacific/Auckland");
    public static readonly GeographicInfo Singapore = new(null, null, CountryCode.SG, "Singapore", "Asia/Singapore");
    public static readonly GeographicInfo SouthKorea = new(null, null, CountryCode.KR, "South Korea", "Asia/Seoul");
    public static readonly GeographicInfo Netherlands = new(null, null, CountryCode.NL, "Netherlands", "Europe/Amsterdam");
    public static readonly GeographicInfo Switzerland = new(null, null, CountryCode.CH, "Switzerland", "Europe/Zurich");
    public static readonly GeographicInfo Sweden = new(null, null, CountryCode.SE, "Sweden", "Europe/Stockholm");
    public static readonly GeographicInfo Norway = new(null, null, CountryCode.NO, "Norway", "Europe/Oslo");
    public static readonly GeographicInfo Denmark = new(null, null, CountryCode.DK, "Denmark", "Europe/Copenhagen");
    public static readonly GeographicInfo Finland = new(null, null, CountryCode.FI, "Finland", "Europe/Helsinki");
    public static readonly GeographicInfo Poland = new(null, null, CountryCode.PL, "Poland", "Europe/Warsaw");
    public static readonly GeographicInfo Ireland = new(null, null, CountryCode.IE, "Ireland", "Europe/Dublin");
    public static readonly GeographicInfo Portugal = new(null, null, CountryCode.PT, "Portugal", "Europe/Lisbon");
    public static readonly GeographicInfo Greece = new(null, null, CountryCode.GR, "Greece", "Europe/Athens");
    public static readonly GeographicInfo Turkey = new(null, null, CountryCode.TR, "Turkey", "Europe/Istanbul");
    public static readonly GeographicInfo Israel = new(null, null, CountryCode.IL, "Israel", "Asia/Jerusalem");
    public static readonly GeographicInfo UnitedArabEmirates = new(null, null, CountryCode.AE, "United Arab Emirates", "Asia/Dubai");
    public static readonly GeographicInfo Vietnam = new(null, null, CountryCode.VN, "Vietnam", "Asia/Ho_Chi_Minh");
    public static readonly GeographicInfo Philippines = new(null, null, CountryCode.PH, "Philippines", "Asia/Manila");
    public static readonly GeographicInfo Indonesia = new(null, null, CountryCode.ID, "Indonesia", "Asia/Jakarta");
    public static readonly GeographicInfo Malaysia = new(null, null, CountryCode.MY, "Malaysia", "Asia/Kuala_Lumpur");
    public static readonly GeographicInfo Taiwan = new(null, null, CountryCode.TW, "Taiwan", "Asia/Taipei");
    public static readonly GeographicInfo HongKong = new(null, null, CountryCode.HK, "Hong Kong", "Asia/Hong_Kong");

    // Lookup tables keyed by state, country, and timezone
    private static readonly Dictionary<USState, List<GeographicInfo>> _byState = new();
    private static readonly Dictionary<CountryCode, List<GeographicInfo>> _byCountry = new();
    private static readonly Dictionary<string, List<GeographicInfo>> _byTimeZone = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<GeographicInfo> AllLocations = [];

    /// <summary>Every registered geographic catalog row except the unknown sentinel.</summary>
    public static IReadOnlyList<GeographicInfo> All => AllLocations;

    /// <summary>Resolves a <see cref="TimeZoneInfo" /> for this location's timezone id.</summary>
    /// <returns>The zone, or <see langword="null" /> when <see cref="TimeZoneId" /> is missing or invalid.</returns>
    public TimeZoneInfo? TimeZone {
        get {
            if (TimeZoneId.IsNullOrWhitespace())
                return null;

            try {
                return TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
            }
            catch (TimeZoneNotFoundException) {
                // Missing zone: return null rather than throw.
                return null;
            }
        }
    }

    /// <summary>True when the timezone id resolves to a <see cref="TimeZoneInfo" />.</summary>
    public bool HasValidTimeZone => TimeZone != null;

    /// <summary>True when <see cref="Country" /> is the United States.</summary>
    public bool IsUnitedStates => Country == CountryCode.US;

    /// <summary>State description or province string, whichever is present.</summary>
    public string? StateOrProvinceName => State?.GetDescription() ?? Province;

    static GeographicInfo()
    {
        var fields = typeof(GeographicInfo).PublicStaticFields<GeographicInfo>();

        foreach (var location in fields) {
            if (location == Unknown)
                continue;

            AllLocations.Add(location);
            if (location.State.HasValue) {
                if (!_byState.ContainsKey(location.State.Value))
                    _byState[location.State.Value] = [];

                _byState[location.State.Value].Add(location);
            }

            if (!_byCountry.ContainsKey(location.Country))
                _byCountry[location.Country] = [];

            _byCountry[location.Country].Add(location);
            if (location.TimeZoneId.IsNullOrWhitespace())
                continue;

            var timeZoneKey = location.TimeZoneId.Trim();
            if (!_byTimeZone.ContainsKey(timeZoneKey))
                _byTimeZone[timeZoneKey] = [];

            _byTimeZone[timeZoneKey].Add(location);
        }
    }

    /// <summary>Lists catalog rows for <paramref name="state" />.</summary>
    /// <param name="state">The US state.</param>
    /// <returns>Matching locations, or an empty sequence when none are registered.</returns>
    public static IEnumerable<GeographicInfo> ByState(USState state) => _byState.TryGetValue(state, out var locations) ? locations : [];

    /// <summary>Returns the first catalog row for <paramref name="state" />.</summary>
    /// <param name="state">The US state.</param>
    /// <returns>The first match, or <see cref="Unknown" /> when the state is not registered.</returns>
    public static GeographicInfo FromState(USState state) => _byState.TryGetValue(state, out var locations) && locations.Count > 0 ? locations[0] : Unknown;

    /// <summary>Lists catalog rows for <paramref name="country" />.</summary>
    /// <param name="country">The country code.</param>
    /// <returns>Matching locations, or an empty sequence when none are registered.</returns>
    public static IEnumerable<GeographicInfo> ByCountry(CountryCode country) => _byCountry.TryGetValue(country, out var locations) ? locations : [];

    /// <summary>Returns the first catalog row for <paramref name="country" />.</summary>
    /// <param name="country">The country code.</param>
    /// <returns>The first match, or <see cref="Unknown" /> when the country is not registered.</returns>
    public static GeographicInfo FromCountry(CountryCode country) => _byCountry.TryGetValue(country, out var locations) && locations.Count > 0 ? locations[0] : Unknown;

    /// <summary>Lists catalog rows for an IANA timezone id.</summary>
    /// <param name="timeZoneId">IANA identifier, for example <c>America/New_York</c>.</param>
    /// <returns>Matching locations, or an empty sequence when none are registered.</returns>
    public static IEnumerable<GeographicInfo> ByTimeZone(string? timeZoneId)
    {
        if (timeZoneId.IsNullOrWhitespace())
            return [];

        var timeZoneKey = timeZoneId.Trim();
        return _byTimeZone.TryGetValue(timeZoneKey, out var locations) ? locations : [];
    }

    /// <summary>Returns the first catalog row for an IANA timezone id.</summary>
    /// <param name="timeZoneId">IANA identifier, for example <c>America/New_York</c>.</param>
    /// <returns>The first match, or <see cref="Unknown" /> when the id is not registered.</returns>
    public static GeographicInfo FromTimeZone(string? timeZoneId)
    {
        if (timeZoneId.IsNullOrWhitespace())
            return Unknown;

        var timeZoneKey = timeZoneId.Trim();
        return _byTimeZone.TryGetValue(timeZoneKey, out var locations) && locations.Count > 0 ? locations[0] : Unknown;
    }

    /// <summary>Returns the first catalog row for a <see cref="TimeZoneInfo" />.</summary>
    /// <param name="timeZone">The <see cref="TimeZoneInfo" /> instance.</param>
    /// <returns>The first match, or <see cref="Unknown" /> when the zone is not registered.</returns>
    public static GeographicInfo FromTimeZone(TimeZoneInfo timeZone) => FromTimeZone(timeZone.Id);
}