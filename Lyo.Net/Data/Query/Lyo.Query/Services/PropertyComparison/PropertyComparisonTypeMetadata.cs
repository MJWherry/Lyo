namespace Lyo.Query.Services.PropertyComparison;

internal record PropertyComparisonTypeMetadata(bool IsEnum, bool IsNullableEnum, Type? UnderlyingEnumType, Type UnderlyingType);