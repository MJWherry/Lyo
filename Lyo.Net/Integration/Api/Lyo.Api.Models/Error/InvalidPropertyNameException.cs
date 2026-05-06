namespace Lyo.Api.Models.Error;

//todo include typename?
public class InvalidPropertyNameException(string errorCode, IReadOnlyList<string> propertyNames, Exception? innerException = null)
    : LFException(errorCode, $"Invalid property name(s): {string.Join(",", propertyNames)}", innerException)
{
    public IReadOnlyList<string> PropertyNames { get; } = propertyNames;

    public InvalidPropertyNameException(string errorCode, string propertyName, Exception? innerException = null)
        : this(errorCode, [propertyName], innerException) { }
}