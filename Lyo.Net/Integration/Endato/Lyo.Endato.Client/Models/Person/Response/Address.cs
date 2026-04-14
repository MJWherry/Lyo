namespace Lyo.Endato.Client.Models.Person.Response;

public sealed record Address(
    bool IsDeliverable,
    bool IsMergedAddress,
    bool IsPublic,
    //"addressQualityCodes": [],
    string AddressHash,
    string HouseNumber,
    string StreetPreDirection,
    string StreetName,
    string StreetPostDirection,
    string StreetType,
    string Unit,
    //"unitType": null,
    string City,
    string State,
    string County,
    string Zip,
    string Zip4,
    string FullAddress,
    string Latitude,
    string Longitude,
    int AddressOrder,
    /*"highRiskMarker": {
        "isHighRisk": false,
        "sic": "",
        "addressType": ""
    },
    */
    string PropertyIndicator,
    string BldgCode,
    string UtilityCode,
    int UnitCount,
    string FirstReportedDate,
    string LastReportedDate,
    string PublicFirstSeenDate
    //string? PublicLastSeenDate,

    //"phoneNumbers": [],
    //"neighbors": [],
    //"neighborSummaryRecords": []
);