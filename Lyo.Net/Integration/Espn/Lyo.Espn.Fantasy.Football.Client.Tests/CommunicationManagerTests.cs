using System.Net;
using System.Text;
using System.Text.Json;
using Lyo.Espn.Fantasy.Football.Client.Models.Request;

namespace Lyo.Espn.Fantasy.Football.Client.Tests;

public sealed class CommunicationManagerTests
{
    private const int LeagueId = 1874757777;
    private const int SeasonId = 2026;
    private const string TopicId = "622adbe6-5001-33d1-82b0-d9d70c0cefcd";
    private const string DeletedTopicId = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";

    private const string ListUrl =
        "https://lm-api-communication.fantasy.espn.com/apis/v3/games/ffl/seasons/2026/segments/0/leagues/1874757777/communication/?view=chat_conversations&platform=chat";

    private const string ConversationUrl =
        "https://lm-api-communication.fantasy.espn.com/apis/v3/games/ffl/seasons/2026/segments/0/leagues/1874757777/communication/topics/622adbe6-5001-33d1-82b0-d9d70c0cefcd?view=chat_conversation&platform=chat";

    [Fact]
    public async Task GetLeagueChatAsync_ListsThenLoadsEachTopic_BindsConversation()
    {
        var handler = new RecordingHandler {
            Respond = call => call.Uri.AbsolutePath.EndsWith("/communication/", StringComparison.Ordinal) ? Json(ListJson()) : Json(ConversationJson())
        };
        var client = Client(handler);

        var conversations = await client.Communication.GetLeagueChatAsync(LeagueId, SeasonId, TestCt);

        Assert.Equal(2, handler.Calls.Count);
        var list = handler.Calls[0];
        Assert.Equal(HttpMethod.Get, list.Method);
        Assert.Equal(ListUrl, list.Uri.AbsoluteUri);
        Assert.Contains("espn_s2=s2-token", list.Headers["Cookie"]);
        Assert.Contains("SWID={SWID-1}", list.Headers["Cookie"]);
        using (var filter = JsonDocument.Parse(list.Headers["x-fantasy-filter"])) {
            var types = filter.RootElement.GetProperty("topics").GetProperty("filterType").GetProperty("value").EnumerateArray().Select(item => item.GetString()!).ToArray();
            Assert.Equal(["CHAT_ALL_MEMBERS", "CHAT_DIRECT_MESSAGE"], types);
        }

        var conversationCall = handler.Calls[1];
        Assert.Equal(HttpMethod.Get, conversationCall.Method);
        Assert.Equal(ConversationUrl, conversationCall.Uri.AbsoluteUri);
        Assert.DoesNotContain(handler.Calls, call => call.Uri.AbsoluteUri.Contains(DeletedTopicId, StringComparison.Ordinal));
        using (var filter = JsonDocument.Parse(conversationCall.Headers["x-fantasy-filter"])) {
            Assert.Equal(25, filter.RootElement.GetProperty("limit").GetInt32());
            Assert.Equal(0, filter.RootElement.GetProperty("offset").GetInt32());
            Assert.Equal(1, filter.RootElement.GetProperty("sortDate").GetProperty("sortPriority").GetInt32());
            Assert.False(filter.RootElement.GetProperty("sortDate").GetProperty("sortAsc").GetBoolean());
        }

        var conversation = Assert.Single(conversations!);
        Assert.Equal("CHAT_ALL_MEMBERS", conversation.Type);
        Assert.Equal(68, conversation.TotalMessageCount);
        Assert.Equal(0, conversation.UnreadMessageCount);
        Assert.True(conversation.IsEdited);
        Assert.Equal("{83B30A97-98B3-4CC8-A7E8-6616E3CF674E}", conversation.Author);
        Assert.Equal(1790187181538, conversation.DateEdited);
        var addDrop = Assert.Single(conversation.Messages!, message => message.MessageTypeId == 266);
        Assert.Null(addDrop.Content);
        Assert.Equal("12", addDrop.Attributes!["team1"]);
        Assert.Equal("4832955", addDrop.Attributes["addP1"]);
        Assert.Equal("3149687", addDrop.Attributes["dropP1"]);
        var recap = Assert.Single(conversation.Messages!, message => message.MessageTypeId == 270);
        Assert.Equal("What a matchup! Check out your league's recap in the Fantasy app.", recap.Content);
        Assert.Null(recap.Attributes);
    }

    [Fact]
    public async Task GetConversationAsync_Offset_SendsOffsetAndSort()
    {
        var handler = new RecordingHandler { Respond = _ => Json(ConversationJson()) };
        var client = Client(handler);

        await client.Communication.GetConversationAsync(LeagueId, SeasonId, TopicId, new ChatConversationQuery { Limit = 10, Offset = 25 }, TestCt);

        var call = Assert.Single(handler.Calls);
        Assert.Equal(HttpMethod.Get, call.Method);
        Assert.Equal(ConversationUrl, call.Uri.AbsoluteUri);
        using var filter = JsonDocument.Parse(call.Headers["x-fantasy-filter"]);
        Assert.Equal(10, filter.RootElement.GetProperty("limit").GetInt32());
        Assert.Equal(25, filter.RootElement.GetProperty("offset").GetInt32());
        Assert.Equal(1, filter.RootElement.GetProperty("sortDate").GetProperty("sortPriority").GetInt32());
        Assert.False(filter.RootElement.GetProperty("sortDate").GetProperty("sortAsc").GetBoolean());
    }

    private static FantasyFootballClient Client(HttpMessageHandler handler)
        => new(new FantasyFootballClientOptions { EspnS2 = "s2-token", Swid = "{SWID-1}" }, httpClient: new HttpClient(handler));

    private static HttpResponseMessage Json(string json)
        => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static string ListJson()
        => $$"""
            {
              "topics": [
                {"id":"{{TopicId}}","type":"CHAT_ALL_MEMBERS","isDeleted":false},
                {"id":"{{DeletedTopicId}}","type":"CHAT_DIRECT_MESSAGE","isDeleted":true}
              ]
            }
            """;

    private static string ConversationJson()
        => $$"""
            {
              "author":"{83B30A97-98B3-4CC8-A7E8-6616E3CF674E}",
              "date":1693303072165,
              "dateEdited":1790187181538,
              "id":"{{TopicId}}",
              "isDeleted":false,
              "isEdited":true,
              "messages":[
                {"attributes":{"team1":"12","addP1":"4832955","dropP1":"3149687"},"author":"LM","date":1789658639348,"id":"7ee88006-6ee1-46a6-8957-9d5b9eb1670a","isDeleted":false,"messageTypeId":266,"topicId":"{{TopicId}}"},
                {"author":"LM","content":"What a matchup! Check out your league's recap in the Fantasy app.","date":1790090068560,"id":"d5443bce-0d67-4351-b388-259b88e235a6","isDeleted":false,"messageTypeId":270,"topicId":"{{TopicId}}"}
              ],
              "totalMessageCount":68,
              "type":"CHAT_ALL_MEMBERS",
              "unreadMessageCount":0
            }
            """;
}
