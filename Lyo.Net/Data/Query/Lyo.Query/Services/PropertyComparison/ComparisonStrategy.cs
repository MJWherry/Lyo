namespace Lyo.Query.Services.PropertyComparison;

internal enum ComparisonStrategy
{
    Direct,
    EnumToEnum,
    EntityStringToRequestEnum,
    EntityEnumToRequestString,
    Convert
}