using System.Diagnostics;

namespace Lyo.Api.Services.TypeConversion;

[DebuggerDisplay("{ToString(),nq}")]
internal record TypeConversionMetadata(Type UnderlyingType, bool IsEnum, bool IsNullable, Type? EnumType)
{
    public override string ToString() => $"IsEnum={IsEnum} IsNullable={IsNullable} UnderlyingType={UnderlyingType.Name}";
}