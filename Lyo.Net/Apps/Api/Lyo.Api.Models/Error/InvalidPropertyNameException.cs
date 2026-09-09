using Lyo.Exceptions.Models;

namespace Lyo.Api.Models.Error;

/// <summary>Thrown when patch keys do not exist on the target type (surfaced as <c>InvalidPatchRequest</c>).</summary>
public class InvalidPropertyNameException : BadRequestException
{
    /// <summary>Property names that were not found.</summary>
    public IReadOnlyList<string> PropertyNames { get; }

    /// <summary>CLR type that was patched, when known.</summary>
    public Type? TargetType { get; }

    /// <summary>Creates the exception for one or more missing property names.</summary>
    public InvalidPropertyNameException(string errorCode, IReadOnlyList<string> propertyNames, Exception? innerException = null)
        : this(errorCode, propertyNames, targetType: null, innerException) { }

    /// <summary>Creates the exception and names the target type in the message.</summary>
    public InvalidPropertyNameException(string errorCode, IReadOnlyList<string> propertyNames, Type? targetType, Exception? innerException = null)
        : base(FormatMessage(propertyNames, targetType), innerException)
    {
        PropertyNames = propertyNames;
        TargetType = targetType;
        ErrorCode = errorCode;
    }

    /// <summary>Creates the exception for a single missing property name.</summary>
    public InvalidPropertyNameException(string errorCode, string propertyName, Exception? innerException = null)
        : this(errorCode, [propertyName], innerException) { }

    /// <summary>Creates the exception for a single missing property name on <paramref name="targetType" />.</summary>
    public InvalidPropertyNameException(string errorCode, string propertyName, Type targetType, Exception? innerException = null)
        : this(errorCode, [propertyName], targetType, innerException) { }

    private static string FormatMessage(IReadOnlyList<string> propertyNames, Type? targetType)
    {
        var names = string.Join(",", propertyNames);
        return targetType is null ? $"Invalid property name(s): {names}" : $"Invalid property name(s) on {targetType.Name}: {names}";
    }
}
