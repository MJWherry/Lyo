using System.Text.Json;
using Lyo.Common.Enums;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.Geolocation.Models.Addresses;
using Lyo.Geolocation.Postgres.Database;
using NpgsqlTypes;

namespace Lyo.Geolocation.Postgres.Mapping;

internal static class AddressEntityMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static Address ToModel(AddressEntity entity)
    {
        var address = new Address {
            Id = entity.Id,
            HouseNumber = entity.HouseNumber,
            StreetPreDirection = entity.StreetPreDirection,
            StreetName = entity.StreetName,
            StreetPostDirection = entity.StreetPostDirection,
            StreetType = entity.StreetType,
            StreetAddress = entity.StreetAddress,
            StreetAddressLine2 = entity.StreetAddressLine2,
            Unit = entity.Unit,
            UnitType = entity.UnitType,
            City = entity.City,
            SubLocality = entity.SubLocality,
            State = entity.State,
            Province = entity.Province,
            Zipcode = entity.Zipcode,
            Zipcode4 = entity.Zipcode4,
            PostalCode = entity.PostalCode,
            County = entity.County,
            SubAdministrativeArea = entity.SubAdministrativeArea,
            FullAddress = entity.FullAddress,
            CountryCode = Enum.TryParse<CountryCode>(entity.CountryCode, out var code) ? code : CountryCode.UU,
            IsDeliverable = entity.IsDeliverable,
            IsMergedAddress = entity.IsMergedAddress,
            IsPublic = entity.IsPublic,
            PropertyIndicator = entity.PropertyIndicator,
            BldgCode = entity.BldgCode,
            UtilityCode = entity.UtilityCode,
            UnitCount = entity.UnitCount,
            FirstReportedDate = entity.FirstReportedDate?.ToDateTime(TimeOnly.MinValue),
            LastReportedDate = entity.LastReportedDate?.ToDateTime(TimeOnly.MinValue),
            PublicFirstSeenDate = entity.PublicFirstSeenDate?.ToDateTime(TimeOnly.MinValue),
            GeocodeConfidence = entity.GeocodeConfidence,
            Metadata = DeserializeMetadata(entity.MetadataJson)
        };

        if (entity.Coordinates is { } point)
            address.Coordinate = new(point.Y, point.X);

        foreach (var source in entity.Sources)
            address.Sources.Add(EntitySourceMapping.ToRecord(source));

        return address;
    }

    public static AddressEntity ToEntity(Address address)
    {
        var entity = new AddressEntity {
            Id = address.Id == default ? Guid.NewGuid() : address.Id,
            HouseNumber = address.HouseNumber,
            StreetPreDirection = address.StreetPreDirection,
            StreetName = address.StreetName,
            StreetPostDirection = address.StreetPostDirection,
            StreetType = address.StreetType,
            StreetAddress = address.StreetAddress,
            StreetAddressLine2 = address.StreetAddressLine2,
            Unit = address.Unit,
            UnitType = address.UnitType,
            City = address.City,
            SubLocality = address.SubLocality,
            State = address.State,
            Province = address.Province,
            Zipcode = address.Zipcode,
            Zipcode4 = address.Zipcode4,
            PostalCode = address.PostalCode,
            County = address.County,
            SubAdministrativeArea = address.SubAdministrativeArea,
            FullAddress = address.FullAddress,
            CountryCode = address.CountryCode.ToString(),
            IsDeliverable = address.IsDeliverable,
            IsMergedAddress = address.IsMergedAddress,
            IsPublic = address.IsPublic,
            PropertyIndicator = address.PropertyIndicator,
            BldgCode = address.BldgCode,
            UtilityCode = address.UtilityCode,
            UnitCount = address.UnitCount,
            FirstReportedDate = ToDateOnly(address.FirstReportedDate),
            LastReportedDate = ToDateOnly(address.LastReportedDate),
            PublicFirstSeenDate = ToDateOnly(address.PublicFirstSeenDate),
            GeocodeConfidence = address.GeocodeConfidence,
            MetadataJson = SerializeMetadata(address.Metadata)
        };

        if (address.Coordinate != null)
            entity.Coordinates = new NpgsqlPoint(address.Coordinate.Longitude, address.Coordinate.Latitude);

        return entity;
    }

    public static void ApplySources(AddressEntity entity, IEnumerable<EntitySourceRecord> sources)
        => EntitySourceMapping.SyncSources(entity.Sources, sources, entity.Id, parentId => new() { AddressId = parentId });

    private static DateOnly? ToDateOnly(DateTime? value) => value.HasValue ? DateOnly.FromDateTime(value.Value) : null;

    private static IReadOnlyDictionary<string, string>? DeserializeMetadata(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);
    }

    private static string? SerializeMetadata(IReadOnlyDictionary<string, string>? metadata) => metadata is { Count: > 0 } ? JsonSerializer.Serialize(metadata, JsonOptions) : null;
}