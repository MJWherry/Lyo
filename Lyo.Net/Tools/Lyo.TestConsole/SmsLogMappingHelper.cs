using System.Text.Json;
using Lyo.Sms.Models;
using Lyo.Sms.Twilio;
using Lyo.Sms.Twilio.Postgres.Database;

namespace Lyo.TestConsole;

internal static class SmsLogMappingHelper
{
    public static TwilioSmsResult MapToTwilioSmsResult(TwilioSmsLogEntity src)
    {
        var request = new SmsRequest(src.To, src.Body, src.From);
        if (!string.IsNullOrEmpty(src.MediaUrlsJson)) {
            try {
                var urls = JsonSerializer.Deserialize<List<string>>(src.MediaUrlsJson);
                if (urls != null) {
                    foreach (var url in urls) {
                        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                            request.MediaUrls.Add(uri);
                    }
                }
            }
            catch { /* ignore */
            }
        }

        var direction = src.Direction == MessageDirection.Inbound ? Direction.Inbound : Direction.OutboundApi;
        return TwilioSmsResult.FromLog(
            request, src.IsSuccess, src.Id, src.Status, src.DateCreated, src.DateSent, src.DateUpdated, src.NumSegments, src.AccountSid, src.Price, src.PriceUnit, src.ErrorCode,
            src.ErrorMessage, direction);
    }
}