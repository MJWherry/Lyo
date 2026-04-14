namespace Lyo.Api.Models.Error;

//todo include typename?
public class InvalidPropertyNameException(string errorCode, IEnumerable<string> propertyNames, Exception? innerException = null)
    : LFException(errorCode, $"Invalid property name(s): {string.Join(",", propertyNames)}", innerException)
{
    public string[] PropertyNames { get; } = propertyNames.ToArray();

    public InvalidPropertyNameException(string errorCode, string propertyName, Exception? innerException = null)
        : this(errorCode, [propertyName], innerException) { }
}
