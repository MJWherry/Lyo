using System.Collections;
using System.Runtime.CompilerServices;

namespace Lyo.Common.Conversion;

/// <summary>Reflection helpers used by type-conversion scenarios: numeric/nullable/collection classification, element types, and friendly type names.</summary>
public static class TypeConversionExtensions
{
    private static readonly HashSet<TypeCode> NumericTypeCodes = [
        TypeCode.Byte,
        TypeCode.SByte,
        TypeCode.Int16,
        TypeCode.UInt16,
        TypeCode.Int32,
        TypeCode.UInt32,
        TypeCode.Int64,
        TypeCode.UInt64,
        TypeCode.Single,
        TypeCode.Double,
        TypeCode.Decimal
    ];

    extension(Type type)
    {
        /// <summary>Determines if the type is a numeric type (byte, int, float, decimal, etc.)</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNumericType() => NumericTypeCodes.Contains(Type.GetTypeCode(type));

        /// <summary>Determines if the type is nullable</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNullable() => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

        /// <summary>Gets the underlying type if nullable, otherwise returns the original type</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Type GetUnderlyingType() => type.IsNullable() ? Nullable.GetUnderlyingType(type)! : type;

        /// <summary> Gets the element type of a collection (array or generic collection)</summary>
        public Type GetCollectionElementType()
        {
            if (type.IsArray)
                return type.GetElementType() ?? typeof(object);

            if (type.IsGenericType && type.GetGenericArguments().Length > 0)
                return type.GetGenericArguments()[0];

            // Check if it implements IEnumerable<T>
            var enumerableInterface = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            return enumerableInterface?.GetGenericArguments()[0] ?? typeof(object);
        }

        /// <summary>Returns a human-readable type name, resolving generic types like List&lt;T&gt; instead of List`1.</summary>
        public string GetFriendlyTypeName()
        {
            if (!type.IsGenericType)
                return type.Name;

            var baseName = type.Name.Substring(0, type.Name.IndexOf('`'));
            var args = string.Join(", ", type.GetGenericArguments().Select(a => a.GetFriendlyTypeName()));
            return $"{baseName}<{args}>";
        }

        /// <summary>Checks if type is a collection type (excluding string and byte[])</summary>
        public bool IsCollectionType()
            => type.IsArray || (type != typeof(string) && type != typeof(byte[]) && type.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)));
    }

    extension(object? obj)
    {
        /// <summary>Determines if an object is enumerable (but not a string or byte[])</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsObjectEnumerable() => obj is not null and not string and not byte[] and IEnumerable;

        /// <summary>Tries to cast an object to IEnumerable{T}, excluding strings and byte arrays</summary>
        public bool TryGetAsEnumerable<T>(out IEnumerable<T> enumerable)
        {
            if (obj is IEnumerable<T> e and not string and not byte[]) {
                enumerable = e;
                return true;
            }

            enumerable = [];
            return false;
        }
    }
}
