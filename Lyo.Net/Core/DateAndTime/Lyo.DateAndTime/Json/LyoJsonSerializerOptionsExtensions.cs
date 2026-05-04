using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Exceptions;

namespace Lyo.DateAndTime.Json;

/// <summary>Registers <see cref="DateOnlyModel"/> and <see cref="TimeOnlyModel"/> JSON converters on <see cref="JsonSerializerOptions"/>.</summary>
public static class LyoJsonSerializerOptionsExtensions
{
    /// <summary>Adds <see cref="DateOnlyModelConverter"/> and <see cref="TimeOnlyModelConverter"/>.</summary>
    public static JsonSerializerOptions AddLyoDateOnlyModelConverters(this JsonSerializerOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        options.Converters.Add(new DateOnlyModelConverter());
        options.Converters.Add(new TimeOnlyModelConverter());
        return options;
    }
}
