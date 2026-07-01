using System.Text.Json;
using Lyo.Endato.Client.Models.Person.Response;

namespace Lyo.Endato.Client.Tests;

public class EndatoJsonDeserializationTests
{
    private static readonly JsonSerializerOptions Options = EndatoJsonSerializerOptions.Create();

    [Fact]
    public void Phone_DeserializesStringLatitudeToDecimal()
    {
        const string json = """
                            {
                              "phoneNumber": "5125550100",
                              "company": "",
                              "location": "Austin, TX",
                              "phoneType": "Mobile",
                              "isConnected": true,
                              "isPublic": true,
                              "latitude": "30.267153",
                              "longitude": "-97.743061",
                              "phoneOrder": 1,
                              "firstReportedDate": "01/01/2020",
                              "lastReportedDate": "01/01/2024",
                              "publicFirstSeenDate": "01/01/2020"
                            }
                            """;

        var phone = JsonSerializer.Deserialize<Phone>(json, Options);
        Assert.NotNull(phone);
        Assert.Equal(30.267153m, phone.Latitude);
        Assert.Equal(-97.743061m, phone.Longitude);
    }

    [Fact]
    public void Phone_DeserializesNumericLatitudeToDecimal()
    {
        const string json = """
                            {
                              "phoneNumber": "5125550100",
                              "company": "",
                              "location": "Austin, TX",
                              "phoneType": "Mobile",
                              "isConnected": true,
                              "isPublic": true,
                              "latitude": 30.267153,
                              "longitude": -97.743061,
                              "phoneOrder": 1,
                              "firstReportedDate": "01/01/2020",
                              "lastReportedDate": "01/01/2024",
                              "publicFirstSeenDate": "01/01/2020"
                            }
                            """;

        var phone = JsonSerializer.Deserialize<Phone>(json, Options);
        Assert.NotNull(phone);
        Assert.Equal(30.267153m, phone.Latitude);
        Assert.Equal(-97.743061m, phone.Longitude);
    }

    [Fact]
    public void PersonQueryResponse_DeserializesSearchCriteriaAndPagination()
    {
        const string json = """
                            {
                              "persons": [],
                              "pagination": {
                                "currentPageNumber": 1,
                                "resultsPerPage": 10,
                                "totalPages": 0,
                                "totalResults": 0
                              },
                              "searchCriteria": [{ "phone": true, "email": false }],
                              "totalRequestExecutionTimeMs": 12,
                              "requestId": "11111111-1111-1111-1111-111111111111",
                              "requestType": "PersonSearch",
                              "requestTime": "2026-01-01T00:00:00Z",
                              "isError": false
                            }
                            """;

        var response = JsonSerializer.Deserialize<PersonQueryResponse>(json, Options);
        Assert.NotNull(response);
        Assert.NotNull(response.Pagination);
        Assert.Equal(1, response.Pagination!.CurrentPageNumber);
        Assert.NotNull(response.SearchCriteria);
        Assert.True(response.SearchCriteria![0].Phone);
    }

    [Fact]
    public void Address_DeserializesStringPhoneNumbers()
    {
        const string json = """
                            {
                              "isDeliverable": true,
                              "isMergedAddress": false,
                              "isPublic": true,
                              "addressHash": "abc",
                              "houseNumber": "123",
                              "streetPreDirection": "",
                              "streetName": "Main",
                              "streetPostDirection": "",
                              "streetType": "St",
                              "unit": "",
                              "city": "Louisville",
                              "state": "KY",
                              "county": "Jefferson",
                              "zip": "40202",
                              "zip4": "",
                              "fullAddress": "123 Main St, Louisville, KY 40202",
                              "latitude": "38.2527",
                              "longitude": "-85.7585",
                              "addressOrder": 1,
                              "propertyIndicator": "",
                              "bldgCode": "",
                              "utilityCode": "",
                              "unitCount": 1,
                              "firstReportedDate": "01/01/2020",
                              "lastReportedDate": "01/01/2024",
                              "publicFirstSeenDate": "01/01/2020",
                              "phoneNumbers": [ "(502) 370-4422" ]
                            }
                            """;

        var address = JsonSerializer.Deserialize<Address>(json, Options);
        Assert.NotNull(address);
        Assert.NotNull(address.PhoneNumbers);
        Assert.Single(address.PhoneNumbers!);
        Assert.Equal("(502) 370-4422", address.PhoneNumbers![0]);
    }

    [Fact]
    public void Address_DeserializesEmptyStringLatitudeAsNull()
    {
        const string json = """
                            {
                              "isDeliverable": true,
                              "isMergedAddress": false,
                              "isPublic": true,
                              "addressHash": "abc",
                              "houseNumber": "123",
                              "streetPreDirection": "",
                              "streetName": "Main",
                              "streetPostDirection": "",
                              "streetType": "St",
                              "unit": "",
                              "city": "Louisville",
                              "state": "KY",
                              "county": "Jefferson",
                              "zip": "40202",
                              "zip4": "",
                              "fullAddress": "123 Main St, Louisville, KY 40202",
                              "latitude": "",
                              "longitude": "",
                              "addressOrder": 1,
                              "propertyIndicator": "",
                              "bldgCode": "",
                              "utilityCode": "",
                              "unitCount": 1,
                              "firstReportedDate": "01/01/2020",
                              "lastReportedDate": "01/01/2024",
                              "publicFirstSeenDate": "01/01/2020"
                            }
                            """;

        var address = JsonSerializer.Deserialize<Address>(json, Options);
        Assert.NotNull(address);
        Assert.Null(address.Latitude);
        Assert.Null(address.Longitude);
    }

    [Fact]
    public void RelativeSummary_DeserializesNumericSpouseAndOldSpouse()
    {
        const string json = """
                            {
                              "tahoeId": "abc-123",
                              "prefix": "",
                              "firstName": "Jane",
                              "middleName": "",
                              "lastName": "Doe",
                              "suffix": "",
                              "dob": "01/01/1980",
                              "relativeLevel": "ab",
                              "relativeType": "Spouse",
                              "spouse": 0,
                              "sharedHouseholdIds": [],
                              "score": 100,
                              "oldSpouse": false
                            }
                            """;

        var relative = JsonSerializer.Deserialize<RelativeSummary>(json, Options);
        Assert.NotNull(relative);
        Assert.False(relative.Spouse);
        Assert.False(relative.OldSpouse);
        Assert.Equal("ab", relative.RelativeLevel);
        Assert.Equal(100, relative.Score);
    }
}