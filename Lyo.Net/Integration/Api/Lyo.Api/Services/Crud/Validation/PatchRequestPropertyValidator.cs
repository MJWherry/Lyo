using System.Reflection;
using Lyo.Api.Models;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Error;

namespace Lyo.Api.Services.Crud.Validation;

/// <summary>Validates <see cref="PatchRequest.Properties" /> keys and convertible values against <typeparamref name="TDbModel" />.</summary>
public static class PatchRequestPropertyValidator
{
    public static IReadOnlyList<ApiError> Validate<TDbModel>(PatchRequest request)
        where TDbModel : class
    {
        if (request.Properties.Count == 0)
            return [];

        var errors = new List<ApiError>();
        var map = typeof(TDbModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in request.Properties) {
            if (!map.TryGetValue(kvp.Key, out var property) || !property.CanWrite) {
                errors.Add(new(
                    Constants.ApiErrorCodes.InvalidField,
                    $"Property {ValidationFieldFormatter.Quote(kvp.Key)} is not valid or not writable on type '{typeof(TDbModel).Name}'."));
                continue;
            }

            if (kvp.Value == null) {
                if (property.PropertyType.IsValueType && !property.PropertyType.IsNullable())
                    errors.Add(new(
                        Constants.ApiErrorCodes.InvalidField,
                        $"Cannot set null on non-nullable property {ValidationFieldFormatter.Quote(property.Name)} of type '{property.PropertyType.GetFriendlyTypeName()}' on '{typeof(TDbModel).Name}'."));
                continue;
            }

            try {
                _ = kvp.Value.ConvertToType(property.PropertyType);
            }
            catch (Exception ex) {
                errors.Add(new(
                    Constants.ApiErrorCodes.InvalidField,
                    $"Value for property {ValidationFieldFormatter.Quote(kvp.Key)} cannot convert to type '{property.PropertyType.GetFriendlyTypeName()}': {ex.Message}"));
            }
        }

        return errors;
    }
}
