namespace Lyo.Endato.Client.Models.Person.Response;

public sealed record PersonQueryResponse(
    IReadOnlyList<Person> Persons,
    /*"smartSearchStatistics": {
        "userInput": null,
        "criteriaGroupId": null,
        "isSuccessful": false,
        "successfulPattern": null,
        "totalTimeInMS": 0,
        "resultCount": 0,
        "patterns": [],
        "searches": []
    },
    "searchCriteria": [],*/
    int TotalRequestExecutionTimeMs,
    Guid RequestId,
    string RequestType,
    DateTime RequestTime,
    bool IsError
    /*"error": {
        "inputErrors": [],
        "warnings": []
    }*/
);