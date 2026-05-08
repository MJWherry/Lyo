using System.Text.Json;
using Lyo.Exceptions;

namespace Lyo.DateAndTime.Json;

/// <summary>Extension methods for wiring Lyo date/time JSON converters into <see cref="JsonSerializerOptions" />.</summary>
/// <remarks>Use these on .NET Standard 2.0 where <c>System.Text.Json</c> lacks built-in <c>DateOnly</c>/<c>TimeOnly</c> support.</remarks>
public static class LyoJsonSerializerOptionsExtensions
{
    /// <summary>Registers <see cref="DateOnlyModelConverter" /> and <see cref="TimeOnlyModelConverter" /> on <paramref name="options" />.</summary>
    /// <returns>The same <paramref name="options" /> instance for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options" /> is <see langword="null" />.</exception>
    public static JsonSerializerOptions AddLyoDateOnlyModelConverters(this JsonSerializerOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        options.Converters.Add(new DateOnlyModelConverter());
        options.Converters.Add(new TimeOnlyModelConverter());
        return options;
    }
}