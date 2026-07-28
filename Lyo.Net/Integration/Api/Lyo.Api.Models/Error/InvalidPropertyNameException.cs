using Lyo.Exceptions.Models;

namespace Lyo.Api.Models.Error;

//todo include typename?
public class InvalidPropertyNameException : BadRequestException
{
    public IReadOnlyList<string> PropertyNames { get; }

    public InvalidPropertyNameException(string errorCode, IReadOnlyList<string> propertyNames, Exception? innerException = null)
        : base($"Invalid property name(s): {string.Join(",", propertyNames)}", innerException)
    {
        PropertyNames = propertyNames;
        ErrorCode = errorCode;
    }

    public InvalidPropertyNameException(string errorCode, string propertyName, Exception? innerException = null)
        : this(errorCode, [propertyName], innerException) { }
}