using System.Text.Json;
using Lyo.Api.Mapping;
using Lyo.Common;
using Lyo.DateAndTime;
using Lyo.FileMetadataStore.Models;
using Lyo.Job.Postgres.Mapping;
using Lyo.People.Postgres.Database;
using Lyo.Portfolio.Api.Person.Request;
using Lyo.Portfolio.Api.Person.Response;
using Lyo.Query.Models.Common;
using Mapster;
using MapsterMapper;

namespace Lyo.Portfolio.Api;

/// <summary>Mapster configuration for Person DTOs and shared query types.</summary>
public static class SetupMapster
{
    private static readonly JsonSerializerOptions WhereClauseJsonOptions = LyoJsonSerializerOptions.Create();

    private static List<PersonAddressRes> MapPersonAddresses(PersonEntity src)
        => src.ContactAddresses.Where(ca => ca.Address != null)
            .Select(ca => {
                var a = ca.Address!;
                return new PersonAddressRes {
                    Id = a.Id,
                    PersonId = ca.PersonId,
                    HouseNumber = a.HouseNumber,
                    StreetPreDirection = a.StreetPreDirection,
                    StreetName = a.StreetName,
                    StreetPostDirection = a.StreetPostDirection,
                    StreetType = a.StreetType,
                    Unit = a.Unit,
                    UnitType = a.UnitType,
                    StreetAddress = a.StreetAddress,
                    StreetAddressLine2 = a.StreetAddressLine2,
                    City = a.City,
                    State = a.State,
                    County = a.County,
                    Zipcode = a.Zipcode,
                    Zipcode4 = a.Zipcode4,
                    PostalCode = a.PostalCode,
                    CountryCode = a.CountryCode,
                    FullAddress = a.FullAddress,
                    Coordinates = a.Coordinates,
                    SourceEntityType = a.SourceEntityType,
                    SourceEntityId = a.SourceEntityId,
                    ImportedAt = a.ImportedAt,
                    CreatedTimestamp = a.CreatedTimestamp,
                    UpdatedTimestamp = a.UpdatedTimestamp
                };
            })
            .ToList();

    private static List<PersonEmailAddressRes> MapPersonEmailAddresses(PersonEntity src)
        => src.ContactEmailAddresses.Where(ce => ce.EmailAddress != null).Select(ce => new PersonEmailAddressRes(ce.EmailAddress!.Id, ce.PersonId, ce.EmailAddress.Email)).ToList();

    private static List<PersonPhoneNumberRes> MapPersonPhoneNumbers(PersonEntity src)
        => src.ContactPhoneNumbers.Where(cp => cp.PhoneNumber != null)
            .Select(cp => {
                var p = cp.PhoneNumber!;
                return new PersonPhoneNumberRes {
                    Id = p.Id,
                    PersonId = cp.PersonId,
                    Number = p.Number,
                    CountryCode = p.CountryCode,
                    CountryCodeString = p.CountryCodeString,
                    TechnologyType = p.TechnologyType,
                    VerifiedAt = p.VerifiedAt,
                    Label = p.Label,
                    Type = cp.Type,
                    SourceEntityType = p.SourceEntityType,
                    SourceEntityId = p.SourceEntityId,
                    ImportedAt = p.ImportedAt,
                    CreatedTimestamp = p.CreatedTimestamp,
                    UpdatedTimestamp = p.UpdatedTimestamp
                };
            })
            .ToList();

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

    /// <summary>Registers Mapster + composite <see cref="ILyoMapper" /> (Job + Mapster).</summary>
    public static IServiceCollection ConfigureMapster(this IServiceCollection services)
    {
        var config = new TypeAdapterConfig();
        config.Default.EnumMappingStrategy(EnumMappingStrategy.ByName);
        config.Default.MaxDepth(8);
        config.Default.Settings.NameMatchingStrategy = NameMatchingStrategy.IgnoreCase;
        config.Default.MapToConstructor(true);
        config.Default.IgnoreNullValues(true);
        config.NewConfig<ConditionClause, ConditionClause>();
        config.NewConfig<GroupClause, GroupClause>();
        config.NewConfig<Dictionary<string, object?>, SpUniqueValueCount>().MapWith(src => MapSpUniqueValueCount(src));
        config.NewConfig<WhereClause, WhereClause>().ConstructUsing(src => src);
        config.NewConfig<FileMetadataEntity, FileMetadataEntity>();
        config.ConfigureDateTimeMappings().ConfigurePersonMappings().Compile();
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();
        services.AddSingleton<JobLyoMapper>();
        services.AddScoped<ILyoMapper>(sp => new CompositeLyoMapper(sp.GetRequiredService<JobLyoMapper>(), new MapsterLyoMapper(sp.GetRequiredService<IMapper>())));
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
                .IgnoreNonMapped(true);

            config.NewConfig<PersonAddressReq, AddressEntity>().IgnoreNonMapped(true);
            config.NewConfig<PersonEmailAddressReq, EmailAddressEntity>().Map(dest => dest.Email, src => src.Address).IgnoreNonMapped(true);
            config.NewConfig<PersonPhoneNumberReq, PhoneNumberEntity>().IgnoreNonMapped(true);
            config.NewConfig<PersonEntity, PersonRes>()
                .Ignore(dest => dest.EndatoPersonId)
                .Map(dest => dest.Addresses, src => MapPersonAddresses(src))
                .Map(dest => dest.EmailAddresses, src => MapPersonEmailAddresses(src))
                .Map(dest => dest.PhoneNumbers, src => MapPersonPhoneNumbers(src));

            return config;
        }
    }
}