using System.Net;
using System.Net.Http.Headers;
using Lyo.Email.Models;
using Lyo.Exceptions.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SendGrid;
using SendGridMessage = SendGrid.Helpers.Mail.SendGridMessage;

namespace Lyo.Email.SendGrid.Tests;

public class SendGridEmailServiceTests
{
    [Fact]
    public async Task SendEmailAsync_MapsRecipientsNamesAttachmentAndTracking()
    {
        var client = new FakeSendGridClient { Send = Accepted("msg-1") };
        var service = Service(client);
        var request = new EmailRequest(
            FromAddress: "from@example.com",
            FromName: "From",
            ToAddresses: [new EmailAddress("ada@example.com", "Ada"), new EmailAddress("bea@example.com")],
            CcAddresses: [new EmailAddress("cee@example.com", "Cee")],
            BccAddresses: [new EmailAddress("dee@example.com")],
            Subject: "Hello",
            Attachments: [new EmailAttachment("note.txt", "file"u8.ToArray(), "text/plain", """{"keep":true}""")],
            TextBody: "plain",
            HtmlBody: "<p>html</p>",
            ReplyTo: [new EmailAddress("reply@example.com", "Reply")],
            Headers: [new EmailHeader("X-Custom", "yes")],
            Priority: EmailPriority.Urgent);

        var result = await service.SendEmailAsync(request, TestCt);
        Assert.True(result.IsSuccess);
        Assert.Equal("msg-1", result.MessageId);
        Assert.StartsWith("202", result.ProviderResponse);
        var message = client.Message!;
        var people = Assert.Single(message.Personalizations);
        Assert.Equal(["ada@example.com", "bea@example.com"], people.Tos.Select(static address => address.Email));
        Assert.Equal("Ada", people.Tos[0].Name);
        Assert.Equal("bea@example.com", people.Tos[1].Name);
        Assert.Equal("Cee", people.Ccs.Single().Name);
        Assert.Equal("dee@example.com", people.Bccs.Single().Name);
        Assert.Equal("dee@example.com", people.Bccs.Single().Email);
        Assert.Equal("reply@example.com", message.ReplyTo.Email);
        Assert.Null(message.ReplyTos);
        var attachment = Assert.Single(message.Attachments);
        Assert.Equal("note.txt", attachment.Filename);
        Assert.Equal("text/plain", attachment.Type);
        Assert.Equal("attachment", attachment.Disposition);
        Assert.Equal(Convert.ToBase64String("file"u8.ToArray()), attachment.Content);
        Assert.DoesNotContain("keep", attachment.Content);
        Assert.Equal("yes", message.Headers["X-Custom"]);
        Assert.Equal("urgent", message.Headers["Priority"]);
        Assert.False(message.TrackingSettings.ClickTracking.Enable);
        Assert.False(message.TrackingSettings.OpenTracking.Enable);
        Assert.False(message.TrackingSettings.SubscriptionTracking.Enable);
    }

    [Fact]
    public async Task SendEmailAsync_SeveralReplyTo_UsesReplyToListOnly()
    {
        var client = new FakeSendGridClient { Send = Accepted(null) };
        var service = Service(client);
        var request = new EmailRequest(
            ToAddresses: [new EmailAddress("ada@example.com", "Ada")],
            ReplyTo: [new EmailAddress("one@example.com", "One"), new EmailAddress("two@example.com", "Two")]);

        await service.SendEmailAsync(request, TestCt);
        Assert.Null(client.Message!.ReplyTo);
        Assert.Equal(["one@example.com", "two@example.com"], client.Message.ReplyTos.Select(static address => address.Email));
    }

    [Fact]
    public async Task SendEmailAsync_AcceptedWithoutMessageId_SucceedsWithNullMessageId()
    {
        var client = new FakeSendGridClient { Send = Accepted(null) };
        var result = await Service(client).SendEmailAsync(new EmailRequest(ToAddresses: [new EmailAddress("ada@example.com")]), TestCt);
        Assert.True(result.IsSuccess);
        Assert.Null(result.MessageId);
        Assert.Equal("202", result.ProviderResponse);
    }

    [Fact]
    public async Task SendEmailAsync_NonSuccess_PutsBodyOnProviderResponse()
    {
        var client = new FakeSendGridClient { Send = Failed(HttpStatusCode.TooManyRequests, "slow down") };
        var result = await Service(client).SendEmailAsync(new EmailRequest(ToAddresses: [new EmailAddress("ada@example.com")]), TestCt);
        Assert.False(result.IsSuccess);
        Assert.Contains("429", result.ProviderResponse);
        Assert.Contains("slow down", result.ProviderResponse);
    }

    [Fact]
    public async Task TestConnectionAsync_ScopesSuccess_ReturnsTrue()
    {
        var client = new FakeSendGridClient { Probe = Accepted(null) };
        var ok = await Service(client).TestConnectionAsync(TestCt);
        Assert.True(ok);
        Assert.Equal(BaseClient.Method.GET, client.ProbeMethod);
        Assert.Equal("scopes", client.ProbePath);
    }

    [Fact]
    public void Validate_BlankApiKey_Throws()
    {
        var options = Options();
        options.ApiKey = " ";
        var ex = Assert.Throws<ArgumentException>(options.Validate);
        Assert.Equal("ApiKey", ex.ParamName);
    }

    [Fact]
    public void Validate_BlankFromName_Throws()
    {
        var options = Options();
        options.DefaultFromName = "";
        Assert.Throws<ArgumentException>(options.Validate);
    }

    [Fact]
    public void Validate_AttachmentLimit_Throws()
    {
        var options = Options();
        options.MaxAttachmentCountPerEmail = 0;
        Assert.Throws<ArgumentOutsideRangeException>(options.Validate);
    }

    [Fact]
    public void ToString_OmitsApiKey()
    {
        var options = Options();
        Assert.DoesNotContain("secret-key", options.ToString());
    }

    [Fact]
    public void AddSendGridEmailService_RegistersTheSameInstanceForTheInterface()
    {
        var services = new ServiceCollection();
        services.AddSendGridEmailService(Options());
        using var provider = services.BuildServiceProvider();
        Assert.Same(provider.GetRequiredService<SendGridEmailService>(), provider.GetRequiredService<IEmailService>());
    }

    [Fact]
    public void AddSendGridEmailServiceFromConfiguration_BindsTheSection()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> {
                    ["SendGridEmailOptions:ApiKey"] = "secret-key",
                    ["SendGridEmailOptions:DefaultFromAddress"] = "from@example.com",
                    ["SendGridEmailOptions:DefaultFromName"] = "From"
                })
            .Build();

        var services = new ServiceCollection();
        services.AddSendGridEmailServiceFromConfiguration(config);
        using var provider = services.BuildServiceProvider();
        Assert.IsType<SendGridEmailService>(provider.GetRequiredService<IEmailService>());
    }

    private static SendGridEmailService Service(FakeSendGridClient client) => new(Options(), client);

    private static SendGridEmailOptions Options()
        => new() { ApiKey = "secret-key", DefaultFromAddress = "from@example.com", DefaultFromName = "From" };

    private static Response Accepted(string? messageId)
    {
        var http = new HttpResponseMessage(HttpStatusCode.Accepted) { Content = new StringContent(string.Empty) };
        if (messageId != null)
            http.Headers.TryAddWithoutValidation("X-Message-Id", messageId);

        return new Response(http.StatusCode, http.Content, http.Headers);
    }

    private static Response Failed(HttpStatusCode status, string body)
    {
        var http = new HttpResponseMessage(status) { Content = new StringContent(body) };
        return new Response(http.StatusCode, http.Content, http.Headers);
    }

    private sealed class FakeSendGridClient : ISendGridClient
    {
        public Response Send { get; init; } = null!;

        public Response Probe { get; init; } = null!;

        public SendGridMessage? Message { get; private set; }

        public BaseClient.Method? ProbeMethod { get; private set; }

        public string? ProbePath { get; private set; }

        public string UrlPath { get; set; } = "";

        public string Version { get; set; } = "v3";

        public string MediaType { get; set; } = "application/json";

        public AuthenticationHeaderValue AddAuthorization(KeyValuePair<string, string> header) => new("Bearer", header.Value);

        public Task<Response> MakeRequest(HttpRequestMessage request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Response> RequestAsync(
            BaseClient.Method method, string? requestBody = null, string? queryParams = null, string? urlPath = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ProbeMethod = method;
            ProbePath = urlPath;
            return Task.FromResult(Probe);
        }

        public Task<Response> SendEmailAsync(SendGridMessage msg, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Message = msg;
            return Task.FromResult(Send);
        }
    }
}
