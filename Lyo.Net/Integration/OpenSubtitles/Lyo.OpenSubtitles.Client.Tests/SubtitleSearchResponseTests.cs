using System.Text.Json;

namespace Lyo.OpenSubtitles.Client.Tests;

public sealed class SubtitleSearchResponseTests
{
    [Fact]
    public void SubtitleSearchResponse_ReadsFileIdFromAttributesFiles()
    {
        const string json = """
                            {
                              "total_pages": 1,
                              "total_count": 1,
                              "page": 1,
                              "data": [
                                {
                                  "id": "10139516",
                                  "type": "subtitle",
                                  "attributes": {
                                    "files": [
                                      { "file_id": 11047023, "cd_number": 1, "file_name": "Cheers.srt" }
                                    ]
                                  }
                                }
                              ]
                            }
                            """;

        var response = JsonSerializer.Deserialize<SubtitleSearchResponse>(json, OpenSubtitlesJson.Create());
        Assert.NotNull(response);
        Assert.Equal(11047023, response.Data![0].Attributes!.Files![0].FileId);
    }
}
