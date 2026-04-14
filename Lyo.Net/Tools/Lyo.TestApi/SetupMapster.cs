using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Api.Mapping;
using Lyo.Common.Enums;
using Lyo.DateAndTime;
using Lyo.Discord.Postgres;
using Lyo.Geolocation.Models.Enums;
using Lyo.People.Models.Enum;
using Lyo.FileMetadataStore.Models;
using Lyo.People.Postgres.Database;
using Lyo.Query.Models.Common;
using Lyo.TestApi.Person.Request;
using Lyo.TestApi.Person.Response;
using Lyo.Web.Components.UniqueValueSelector;
using Mapster;
using MapsterMapper;

namespace Lyo.TestApi;

public static class SetupMapster
{
    private static readonly JsonSerializerOptions WhereClauseJsonOptions = new() { Converters = { new JsonStringEnumConverter() }, PropertyNameCaseInsensitive = true };

    private static NamePrefix? ParseNamePrefix(string? value) => Enum.TryParse<NamePrefix>(value, true, out var parsed) ? parsed : null;

    private static NameSuffix? ParseNameSuffix(string? value) => Enum.TryParse<NameSuffix>(value, true, out var parsed) ? parsed : null;

    private static ContactAddressType ParseContactAddressType(string? value) => Enum.TryParse<ContactAddressType>(value, true, out var parsed) ? parsed : ContactAddressType.Other;

    private static ContactEmailType ParseContactEmailType(string? value) => Enum.TryParse<ContactEmailType>(value, true, out var parsed) ? parsed : ContactEmailType.Other;

    private static ContactPhoneType ParseContactPhoneType(string? value) => Enum.TryParse<ContactPhoneType>(value, true, out var parsed) ? parsed : ContactPhoneType.Other;

    private static CountryCode ParseCountryCodeOrUs(string? value) => Enum.TryParse<CountryCode>(value, true, out var parsed) ? parsed : CountryCode.US;

    private static CountryCode? ParseNullableCountryCode(string? value) => Enum.TryParse<CountryCode>(value, true, out var parsed) ? parsed : null;

    private static PhoneType? ParseNullablePhoneType(string? value) => Enum.TryParse<PhoneType>(value, true, out var parsed) ? parsed : null;

    private static DateOnly? ToDateOnly(DateTime? value) => value.HasValue ? DateOnly.FromDateTime(value.Value) : null;

    private static DateTime? ToDateTime(DateOnly? value) => value.HasValue ? value.Value.ToDateTime(TimeOnly.MinValue) : null;

    private static SpUniqueValueCount MapSpUniqueValueCount(Dictionary<string, object?> src)
    {
        string? v = null;
        var c = 0;
        foreach (var kvp in src) {
            if (kvp.Key.Equals("value", StringComparison.OrdinalIgnoreCase))
                v = kvp.Value?.ToString();
            else if (kvp.Key.Equals("count", StringComparison.OrdinalIgnoreCase) && kvp.Value != null && kvp.Value != DBNull.Value) {
                var n = Convert.ToInt64(kvp.Value);
                c = n > int.MaxValue ? int.MaxValue : (int)n;
            }
        }

        return new(v, c);
    }

    public static IServiceCollection ConfigureMapster(this IServiceCollection services)
    {
        var config = new TypeAdapterConfig();
        config.Default.EnumMappingStrategy(EnumMappingStrategy.ByName);
        config.Default.MaxDepth(8);
        config.Default.Settings.NameMatchingStrategy = NameMatchingStrategy.IgnoreCase;
        config.Default.MapToConstructor(true);
        config.Default.IgnoreNullValues(true);
        // ensure concrete type mappings exist
        config.NewConfig<ConditionClause, ConditionClause>();
        config.NewConfig<GroupClause, GroupClause>();
        config.NewConfig<Dictionary<string, object?>, SpUniqueValueCount>().MapWith(src => MapSpUniqueValueCount(src));

        // polymorphic mapping for the abstract base
        // Map the abstract base to itself by returning the source instance.
        // This prevents Mapster from trying to instantiate the abstract type during Compile().
        config.NewConfig<WhereClause, WhereClause>().ConstructUsing(src => src);
        config.NewConfig<FileMetadataEntity, FileMetadataEntity>();
        config.ConfigureDateTimeMappings().ConfigurePersonMappings().ConfigureTwilioMappings().ConfigureDiscordMappings().Compile();
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();
        services.AddScoped<ILyoMapper, MapsterLyoMapper>();
        return services;
    }

    extension(TypeAdapterConfig config)
    {
        private TypeAdapterConfig ConfigureDateTimeMappings()
        {
            config.NewConfig<DateOnlyModel, DateOnly>().MapWith(src => DateOnly.FromDateTime(src.ToDateTime()));
            config.NewConfig<DateOnly, DateOnlyModel>().MapWith(src => new(src.Year, src.Month, src.Day));
            config.NewConfig<DateOnlyModel, DateTime>().MapWith(src => src.ToDateTime());
            config.NewConfig<DateTime, DateOnlyModel>().MapWith(src => DateOnlyModel.FromDateTime(src)!);
            config.NewConfig<DateOnly, DateTime>().MapWith(src => src.ToDateTime(TimeOnly.MinValue));
            config.NewConfig<DateTime, DateOnly>().MapWith(src => DateOnly.FromDateTime(src));
            config.NewConfig<TimeOnlyModel, TimeOnly>().MapWith(src => new(src.Hour, src.Minute, src.Second, src.Millisecond));
            config.NewConfig<TimeOnly, TimeOnlyModel>().MapWith(src => new(src.Hour, src.Minute, src.Second, src.Nanosecond / 100));
            //config.NewConfig<TimeOnlyModel, DateTime>()
            //    .MapWith(src => src.ToDateTime(DateTime.MinValue.Date));
            config.NewConfig<DateTime, TimeOnlyModel>().MapWith(src => TimeOnlyModel.FromDateTime(src));
            config.NewConfig<TimeOnly, DateTime>()
                .MapWith(t => DateTime.MinValue.AddHours(t.Hour).AddMinutes(t.Minute).AddSeconds(t.Second).AddTicks(t.Ticks % TimeSpan.TicksPerSecond));

            config.NewConfig<DateTime, TimeOnly>().MapWith(src => TimeOnly.FromDateTime(src));
            config.NewConfig<DateTimeOffset, DateTime>().MapWith(src => src.UtcDateTime);
            config.NewConfig<DateTimeOffset, DateTime?>().MapWith(src => src.UtcDateTime);
            return config;
        }

        private TypeAdapterConfig ConfigurePersonMappings()
        {
            config.NewConfig<PersonReq, PersonEntity>()
                .Map(dest => dest.NamePrefix, src => string.IsNullOrEmpty(src.Prefix) ? null : src.Prefix)
                .Map(dest => dest.MiddleName, src => string.IsNullOrEmpty(src.MiddleName) ? null : src.MiddleName)
                .Map(dest => dest.NameSuffix, src => string.IsNullOrEmpty(src.Suffix) ? null : src.Suffix)
                .Map(dest => dest.Source, src => string.IsNullOrEmpty(src.Source) ? "Manual" : src.Source)
                .IgnoreNonMapped(true);

            config.NewConfig<PersonAddressReq, AddressEntity>().IgnoreNonMapped(true);
            config.NewConfig<PersonEmailAddressReq, EmailAddressEntity>().Map(dest => dest.Email, src => src.Address).IgnoreNonMapped(true);
            config.NewConfig<PersonPhoneNumberReq, PhoneNumberEntity>().IgnoreNonMapped(true);
            config.NewConfig<PersonEntity, PersonRes>()
                .MapWith(src => new(
                    src.Id, null, src.NamePrefix, src.FirstName, src.MiddleName, src.LastName, src.NameSuffix, src.Source ?? "",
                    src.ContactAddresses.Where(ca => ca.Address != null)
                        .Select(ca => new PersonAddressRes(
                            ca.Address!.Id, ca.PersonId, ca.Address.HouseNumber, ca.Address.StreetPreDirection, ca.Address.StreetName, ca.Address.StreetPostDirection,
                            ca.Address.StreetType, ca.Address.Unit, ca.Address.UnitType, ca.Address.City, ca.Address.State, ca.Address.County, ca.Address.Zipcode,
                            ca.Address.Zipcode4, DateOnly.FromDateTime(ca.Address.CreatedTimestamp),
                            ca.Address.UpdatedTimestamp.HasValue ? DateOnly.FromDateTime(ca.Address.UpdatedTimestamp.Value) : DateOnly.FromDateTime(ca.Address.CreatedTimestamp)))
                        .ToList(),
                    src.ContactEmailAddresses.Where(ce => ce.EmailAddress != null)
                        .Select(ce => new PersonEmailAddressRes(ce.EmailAddress!.Id, ce.PersonId, ce.EmailAddress.Email))
                        .ToList(),
                    src.ContactPhoneNumbers.Where(cp => cp.PhoneNumber != null)
                        .Select(cp => new PersonPhoneNumberRes(
                            cp.PhoneNumber!.Id, cp.PersonId, cp.PhoneNumber.Number, cp.Type, DateOnly.FromDateTime(cp.PhoneNumber.CreatedTimestamp),
                            cp.PhoneNumber.UpdatedTimestamp.HasValue
                                ? DateOnly.FromDateTime(cp.PhoneNumber.UpdatedTimestamp.Value)
                                : DateOnly.FromDateTime(cp.PhoneNumber.CreatedTimestamp)))
                        .ToList()));

            return config;
        }

        private TypeAdapterConfig ConfigureTwilioMappings() => config;
    }
}