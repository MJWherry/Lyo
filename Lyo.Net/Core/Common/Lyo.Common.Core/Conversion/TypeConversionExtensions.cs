using System.Collections;
using System.Runtime.CompilerServices;

namespace Lyo.Common.Core.Conversion;

/// <summary>Reflection helpers for conversion: numeric/nullable/collection checks, element types, and readable type names.</summary>
public static class TypeConversionExtensions
{
    private static readonly HashSet<TypeCode> NumericTypeCodes = [
        TypeCode.Byte, TypeCode.SByte, TypeCode.Int16, TypeCode.UInt16, TypeCode.Int32, TypeCode.UInt32, TypeCode.Int64, TypeCode.UInt64, TypeCode.Single, TypeCode.Double,
        TypeCode.Decimal
    ];

    extension(Type type)
    {
        /// <summary>True when the type is numeric (byte, int, float, decimal, and similar).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNumericType() => NumericTypeCodes.Contains(Type.GetTypeCode(type));

        /// <summary>True when the type is <c>Nullable&lt;T&gt;</c>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNullable() => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

        /// <summary>Underlying type when nullable; otherwise the type itself.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Type GetUnderlyingType() => type.IsNullable() ? Nullable.GetUnderlyingType(type)! : type;

        /// <summary>Element type of an array or generic collection.</summary>
        public Type GetCollectionElementType()
        {
            if (type.IsArray)
                return type.GetElementType() ?? typeof(object);

            if (type.IsGenericType && type.GetGenericArguments().Length > 0)
                return type.GetGenericArguments()[0];

            // Fall back to IEnumerable<T> when the type is not an array or open generic
            var enumerableInterface = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            return enumerableInterface?.GetGenericArguments()[0] ?? typeof(object);
        }

        /// <summary>Readable type name (List&lt;T&gt; rather than List`1).</summary>
        public string GetFriendlyTypeName()
        {
            if (!type.IsGenericType)
                return type.Name;

            var baseName = type.Name.Substring(0, type.Name.IndexOf('`'));
            var args = string.Join(", ", type.GetGenericArguments().Select(a => a.GetFriendlyTypeName()));
            return $"{baseName}<{args}>";
        }

        /// <summary>True when the type is a collection, except <see cref="string" /> and <c>byte[]</c>.</summary>
        public bool IsCollectionType()
            => type.IsArray || (type != typeof(string) && type != typeof(byte[]) && type.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)));
    }

    extension(object? obj)
    {
        /// <summary>True when the object is enumerable and is not a string or byte[].</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsObjectEnumerable() => obj is not null and not string and not byte[] and IEnumerable;

        /// <summary>Tries to treat the object as <c>IEnumerable&lt;T&gt;</c>, skipping strings and byte arrays.</summary>
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