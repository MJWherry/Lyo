using System.Net;
using System.Reflection;
using Lyo.Common.Enums;

namespace Lyo.Common.Records;

/// <summary>Represents HTTP status code information with code, name, description, and category. Wraps System.Net.HttpStatusCode.</summary>
public record HttpStatusCodeInfo(int Code, string Name, string Description, HttpStatusCodeCategory Category, HttpStatusCode HttpStatusCode)
{
    // Unknown
    public static readonly HttpStatusCodeInfo Unknown = new(0, "Unknown", "Unknown or unsupported HTTP status code", HttpStatusCodeCategory.Unknown, 0);

    // 1xx Informational
    public static readonly HttpStatusCodeInfo Continue = new(
        100, "Continue", "The server has received the request headers and the client should proceed to send the request body", HttpStatusCodeCategory.Informational,
        HttpStatusCode.Continue);

    public static readonly HttpStatusCodeInfo SwitchingProtocols = new(
        101, "Switching Protocols", "The requester has asked the server to switch protocols", HttpStatusCodeCategory.Informational, HttpStatusCode.SwitchingProtocols);

    public static readonly HttpStatusCodeInfo Processing = new(
        102, "Processing", "The server has received and is processing the request, but no response is available yet", HttpStatusCodeCategory.Informational, (HttpStatusCode)102);

    public static readonly HttpStatusCodeInfo EarlyHints = new(
        103, "Early Hints", "Used to return some response headers before final HTTP message", HttpStatusCodeCategory.Informational, (HttpStatusCode)103);

    // 2xx Success
    public static readonly HttpStatusCodeInfo Ok = new(200, "OK", "The request has succeeded", HttpStatusCodeCategory.Success, HttpStatusCode.OK);

    public static readonly HttpStatusCodeInfo Created = new(
        201, "Created", "The request has been fulfilled and resulted in a new resource being created", HttpStatusCodeCategory.Success, HttpStatusCode.Created);

    public static readonly HttpStatusCodeInfo Accepted = new(
        202, "Accepted", "The request has been accepted for processing, but the processing has not been completed", HttpStatusCodeCategory.Success, HttpStatusCode.Accepted);

    public static readonly HttpStatusCodeInfo NonAuthoritativeInformation = new(
        203, "Non-Authoritative Information", "The request was successful but the information may have been modified", HttpStatusCodeCategory.Success,
        HttpStatusCode.NonAuthoritativeInformation);

    public static readonly HttpStatusCodeInfo NoContent = new(
        204, "No Content", "The server successfully processed the request and is not returning any content", HttpStatusCodeCategory.Success, HttpStatusCode.NoContent);

    public static readonly HttpStatusCodeInfo ResetContent = new(
        205, "Reset Content", "The server successfully processed the request, asks that the requester reset its document view", HttpStatusCodeCategory.Success,
        HttpStatusCode.ResetContent);

    public static readonly HttpStatusCodeInfo PartialContent = new(
        206, "Partial Content", "The server is delivering only part of the resource due to a range header sent by the client", HttpStatusCodeCategory.Success,
        HttpStatusCode.PartialContent);

    public static readonly HttpStatusCodeInfo MultiStatus = new(
        207, "Multi-Status", "The message body that follows is an XML message and can contain a number of separate response codes", HttpStatusCodeCategory.Success,
        (HttpStatusCode)207);

    public static readonly HttpStatusCodeInfo AlreadyReported = new(
        208, "Already Reported", "The members of a DAV binding have already been enumerated in a previous reply", HttpStatusCodeCategory.Success, (HttpStatusCode)208);

    public static readonly HttpStatusCodeInfo ImUsed = new(
        226, "IM Used", "The server has fulfilled a request for the resource, and the response is a representation of the result of one or more instance-manipulations",
        HttpStatusCodeCategory.Success, (HttpStatusCode)226);

    // 3xx Redirection
    public static readonly HttpStatusCodeInfo MultipleChoices = new(
        300, "Multiple Choices", "The request has more than one possible response", HttpStatusCodeCategory.Redirection, HttpStatusCode.MultipleChoices);

    public static readonly HttpStatusCodeInfo MovedPermanently = new(
        301, "Moved Permanently", "The URL of the requested resource has been changed permanently", HttpStatusCodeCategory.Redirection, HttpStatusCode.MovedPermanently);

    public static readonly HttpStatusCodeInfo Found = new(
        302, "Found", "The URI of requested resource has been changed temporarily", HttpStatusCodeCategory.Redirection, HttpStatusCode.Found);

    public static readonly HttpStatusCodeInfo SeeOther = new(
        303, "See Other", "The server sent this response to direct the client to get the requested resource at another URI with a GET request", HttpStatusCodeCategory.Redirection,
        HttpStatusCode.SeeOther);

    public static readonly HttpStatusCodeInfo NotModified = new(
        304, "Not Modified", "The client has made a conditional request and the resource has not been modified", HttpStatusCodeCategory.Redirection, HttpStatusCode.NotModified);

    public static readonly HttpStatusCodeInfo UseProxy = new(
        305, "Use Proxy", "The requested resource is available only through a proxy", HttpStatusCodeCategory.Redirection, HttpStatusCode.UseProxy);

    public static readonly HttpStatusCodeInfo TemporaryRedirect = new(
        307, "Temporary Redirect", "The server is redirecting the user agent to a different resource", HttpStatusCodeCategory.Redirection, HttpStatusCode.TemporaryRedirect);

    public static readonly HttpStatusCodeInfo PermanentRedirect = new(
        308, "Permanent Redirect", "The resource is now permanently located at another URI", HttpStatusCodeCategory.Redirection, (HttpStatusCode)308);

    // 4xx Client Error
    public static readonly HttpStatusCodeInfo BadRequest = new(
        400, "Bad Request", "The server cannot or will not process the request due to an apparent client error", HttpStatusCodeCategory.ClientError, HttpStatusCode.BadRequest);

    public static readonly HttpStatusCodeInfo Unauthorized = new(
        401, "Unauthorized", "The client must authenticate itself to get the requested response", HttpStatusCodeCategory.ClientError, HttpStatusCode.Unauthorized);

    public static readonly HttpStatusCodeInfo PaymentRequired = new(
        402, "Payment Required", "Reserved for future use", HttpStatusCodeCategory.ClientError, HttpStatusCode.PaymentRequired);

    public static readonly HttpStatusCodeInfo Forbidden = new(
        403, "Forbidden", "The client does not have access rights to the content", HttpStatusCodeCategory.ClientError, HttpStatusCode.Forbidden);

    public static readonly HttpStatusCodeInfo NotFound = new(
        404, "Not Found", "The server cannot find the requested resource", HttpStatusCodeCategory.ClientError, HttpStatusCode.NotFound);

    public static readonly HttpStatusCodeInfo MethodNotAllowed = new(
        405, "Method Not Allowed", "The request method is known by the server but is not supported by the target resource", HttpStatusCodeCategory.ClientError,
        HttpStatusCode.MethodNotAllowed);

    public static readonly HttpStatusCodeInfo NotAcceptable = new(
        406, "Not Acceptable", "The server cannot produce a response matching the list of acceptable values", HttpStatusCodeCategory.ClientError, HttpStatusCode.NotAcceptable);

    public static readonly HttpStatusCodeInfo ProxyAuthenticationRequired = new(
        407, "Proxy Authentication Required", "The client must first authenticate itself with the proxy", HttpStatusCodeCategory.ClientError,
        HttpStatusCode.ProxyAuthenticationRequired);

    public static readonly HttpStatusCodeInfo RequestTimeout = new(
        408, "Request Timeout", "The server timed out waiting for the request", HttpStatusCodeCategory.ClientError, HttpStatusCode.RequestTimeout);

    public static readonly HttpStatusCodeInfo Conflict = new(
        409, "Conflict", "The request could not be completed due to a conflict with the current state of the resource", HttpStatusCodeCategory.ClientError,
        HttpStatusCode.Conflict);

    public static readonly HttpStatusCodeInfo Gone = new(
        410, "Gone", "The requested resource is no longer available and will not be available again", HttpStatusCodeCategory.ClientError, HttpStatusCode.Gone);

    public static readonly HttpStatusCodeInfo LengthRequired = new(
        411, "Length Required", "The server refuses to accept the request without a defined Content-Length", HttpStatusCodeCategory.ClientError, HttpStatusCode.LengthRequired);

    public static readonly HttpStatusCodeInfo PreconditionFailed = new(
        412, "Precondition Failed", "The server does not meet one of the preconditions that the requester put on the request", HttpStatusCodeCategory.ClientError,
        HttpStatusCode.PreconditionFailed);

    public static readonly HttpStatusCodeInfo PayloadTooLarge = new(
        413, "Payload Too Large", "The request is larger than the server is willing or able to process", HttpStatusCodeCategory.ClientError, (HttpStatusCode)413);

    public static readonly HttpStatusCodeInfo UriTooLong = new(
        414, "URI Too Long", "The URI provided was too long for the server to process", HttpStatusCodeCategory.ClientError, (HttpStatusCode)414);

    public static readonly HttpStatusCodeInfo UnsupportedMediaType = new(
        415, "Unsupported Media Type", "The request entity has a media type which the server or resource does not support", HttpStatusCodeCategory.ClientError,
        HttpStatusCode.UnsupportedMediaType);

    public static readonly HttpStatusCodeInfo RangeNotSatisfiable = new(
        416, "Range Not Satisfiable", "The client has asked for a portion of the file, but the server cannot supply that portion", HttpStatusCodeCategory.ClientError,
        (HttpStatusCode)416);

    public static readonly HttpStatusCodeInfo ExpectationFailed = new(
        417, "Expectation Failed", "The server cannot meet the requirements of the Expect request-header field", HttpStatusCodeCategory.ClientError,
        HttpStatusCode.ExpectationFailed);

    public static readonly HttpStatusCodeInfo ImATeapot = new(
        418, "I'm a teapot", "The server refuses the attempt to brew coffee with a teapot", HttpStatusCodeCategory.ClientError, (HttpStatusCode)418);

    public static readonly HttpStatusCodeInfo MisdirectedRequest = new(
        421, "Misdirected Request", "The request was directed at a server that is not able to produce a response", HttpStatusCodeCategory.ClientError, (HttpStatusCode)421);

    public static readonly HttpStatusCodeInfo UnprocessableEntity = new(
        422, "Unprocessable Entity", "The request was well-formed but was unable to be followed due to semantic errors", HttpStatusCodeCategory.ClientError, (HttpStatusCode)422);

    public static readonly HttpStatusCodeInfo Locked = new(423, "Locked", "The resource that is being accessed is locked", HttpStatusCodeCategory.ClientError, (HttpStatusCode)423);

    public static readonly HttpStatusCodeInfo FailedDependency = new(
        424, "Failed Dependency", "The request failed because it depended on another request and that request failed", HttpStatusCodeCategory.ClientError, (HttpStatusCode)424);

    public static readonly HttpStatusCodeInfo TooEarly = new(
        425, "Too Early", "The server is unwilling to risk processing a request that might be replayed", HttpStatusCodeCategory.ClientError, (HttpStatusCode)425);

    public static readonly HttpStatusCodeInfo UpgradeRequired = new(
        426, "Upgrade Required", "The server refuses to perform the request using the current protocol", HttpStatusCodeCategory.ClientError, HttpStatusCode.UpgradeRequired);

    public static readonly HttpStatusCodeInfo PreconditionRequired = new(
        428, "Precondition Required", "The origin server requires the request to be conditional", HttpStatusCodeCategory.ClientError, (HttpStatusCode)428);

    public static readonly HttpStatusCodeInfo TooManyRequests = new(
        429, "Too Many Requests", "The user has sent too many requests in a given amount of time", HttpStatusCodeCategory.ClientError, (HttpStatusCode)429);

    public static readonly HttpStatusCodeInfo RequestHeaderFieldsTooLarge = new(
        431, "Request Header Fields Too Large", "The server is unwilling to process the request because its header fields are too large", HttpStatusCodeCategory.ClientError,
        (HttpStatusCode)431);

    public static readonly HttpStatusCodeInfo UnavailableForLegalReasons = new(
        451, "Unavailable For Legal Reasons", "The server is denying access to the resource as a consequence of a legal demand", HttpStatusCodeCategory.ClientError,
        (HttpStatusCode)451);

    // 5xx Server Error
    public static readonly HttpStatusCodeInfo InternalServerError = new(
        500, "Internal Server Error", "The server has encountered a situation it doesn't know how to handle", HttpStatusCodeCategory.ServerError,
        HttpStatusCode.InternalServerError);

    public static readonly HttpStatusCodeInfo NotImplemented = new(
        501, "Not Implemented", "The request method is not supported by the server and cannot be handled", HttpStatusCodeCategory.ServerError, HttpStatusCode.NotImplemented);

    public static readonly HttpStatusCodeInfo BadGateway = new(
        502, "Bad Gateway", "The server was acting as a gateway or proxy and received an invalid response", HttpStatusCodeCategory.ServerError, HttpStatusCode.BadGateway);

    public static readonly HttpStatusCodeInfo ServiceUnavailable = new(
        503, "Service Unavailable", "The server is not ready to handle the request", HttpStatusCodeCategory.ServerError, HttpStatusCode.ServiceUnavailable);

    public static readonly HttpStatusCodeInfo GatewayTimeout = new(
        504, "Gateway Timeout", "The server was acting as a gateway or proxy and did not receive a timely response", HttpStatusCodeCategory.ServerError,
        HttpStatusCode.GatewayTimeout);

    public static readonly HttpStatusCodeInfo HttpVersionNotSupported = new(
        505, "HTTP Version Not Supported", "The HTTP version used in the request is not supported by the server", HttpStatusCodeCategory.ServerError,
        HttpStatusCode.HttpVersionNotSupported);

    public static readonly HttpStatusCodeInfo VariantAlsoNegotiates = new(
        506, "Variant Also Negotiates", "The server has an internal configuration error", HttpStatusCodeCategory.ServerError, (HttpStatusCode)506);

    public static readonly HttpStatusCodeInfo InsufficientStorage = new(
        507, "Insufficient Storage", "The method could not be performed on the resource because the server is unable to store the representation",
        HttpStatusCodeCategory.ServerError, (HttpStatusCode)507);

    public static readonly HttpStatusCodeInfo LoopDetected = new(
        508, "Loop Detected", "The server detected an infinite loop while processing the request", HttpStatusCodeCategory.ServerError, (HttpStatusCode)508);

    public static readonly HttpStatusCodeInfo NotExtended = new(
        510, "Not Extended", "Further extensions to the request are required for the server to fulfill it", HttpStatusCodeCategory.ServerError, (HttpStatusCode)510);

    public static readonly HttpStatusCodeInfo NetworkAuthenticationRequired = new(
        511, "Network Authentication Required", "The client needs to authenticate to gain network access", HttpStatusCodeCategory.ServerError, (HttpStatusCode)511);

    // Static registry with fast lookups
    private static readonly Dictionary<int, HttpStatusCodeInfo> _byCode = new();
    private static readonly Dictionary<HttpStatusCode, HttpStatusCodeInfo> _byHttpStatusCode = new();
    private static readonly List<HttpStatusCodeInfo> _allCodes = new();

    /// <summary>Gets all registered HTTP status codes.</summary>
    public static IReadOnlyList<HttpStatusCodeInfo> All => _allCodes;

    /// <summary>Determines if the status code represents a successful response (2xx).</summary>
    public bool IsSuccess => Category == HttpStatusCodeCategory.Success;

    /// <summary>Determines if the status code represents a client error (4xx).</summary>
    public bool IsClientError => Category == HttpStatusCodeCategory.ClientError;

    /// <summary>Determines if the status code represents a server error (5xx).</summary>
    public bool IsServerError => Category == HttpStatusCodeCategory.ServerError;

    /// <summary>Determines if the status code represents a redirection (3xx).</summary>
    public bool IsRedirection => Category == HttpStatusCodeCategory.Redirection;

    /// <summary>Determines if the status code represents an informational response (1xx).</summary>
    public bool IsInformational => Category == HttpStatusCodeCategory.Informational;

    static HttpStatusCodeInfo()
    {
        // Register all codes using reflection to find static fields
        var type = typeof(HttpStatusCodeInfo);
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(HttpStatusCodeInfo))
            .Select(f => (HttpStatusCodeInfo)f.GetValue(null)!)
            .ToList();

        foreach (var statusCode in fields) {
            _allCodes.Add(statusCode);
            _byCode[statusCode.Code] = statusCode;
            _byHttpStatusCode[statusCode.HttpStatusCode] = statusCode;
        }
    }

    /// <summary>Finds an HTTP status code by its numeric code.</summary>
    /// <param name="code">The HTTP status code (e.g., 200, 404, 500).</param>
    /// <returns>The status code info, or Unknown if not found.</returns>
    public static HttpStatusCodeInfo FromCode(int code) => _byCode.TryGetValue(code, out var statusCode) ? statusCode : Unknown;

    /// <summary>Finds an HTTP status code by System.Net.HttpStatusCode enum.</summary>
    /// <param name="httpStatusCode">The System.Net.HttpStatusCode enum value.</param>
    /// <returns>The status code info, or Unknown if not found.</returns>
    public static HttpStatusCodeInfo FromHttpStatusCode(HttpStatusCode httpStatusCode) => _byHttpStatusCode.TryGetValue(httpStatusCode, out var statusCode) ? statusCode : Unknown;

    /// <summary>Gets HTTP status codes by category.</summary>
    /// <param name="category">The HTTP status code category.</param>
    /// <returns>An enumerable of status codes in the specified category.</returns>
    public static IEnumerable<HttpStatusCodeInfo> ByCategory(HttpStatusCodeCategory category) => _allCodes.Where(c => c.Category == category);

    /// <summary>Implicit conversion to System.Net.HttpStatusCode for seamless integration.</summary>
    public static implicit operator HttpStatusCode(HttpStatusCodeInfo info) => info.HttpStatusCode;

    /// <summary>Implicit conversion from System.Net.HttpStatusCode for seamless integration.</summary>
    public static implicit operator HttpStatusCodeInfo(HttpStatusCode httpStatusCode) => FromHttpStatusCode(httpStatusCode);
}