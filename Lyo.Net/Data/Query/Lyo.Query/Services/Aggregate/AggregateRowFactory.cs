using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace Lyo.Query.Services.Aggregate;

/// <summary>Builds a flat class for an aggregate result row. One property per group key and aggregate, so the provider materializes columns instead of a record.</summary>
internal static class AggregateRowFactory
{
    private static readonly ModuleBuilder Module = AssemblyBuilder.DefineDynamicAssembly(new("Lyo.Query.AggregateRows"), AssemblyBuilderAccess.Run).DefineDynamicModule("Rows");
    private static readonly ConcurrentDictionary<string, AggregateRowShape> Cache = new(StringComparer.Ordinal);
    private static int _next;

    public static AggregateRowShape Create(IReadOnlyList<(string Name, Type Type)> properties, bool equality = false)
    {
        var key = (equality ? "eq:" : "row:") + string.Join("|", properties.Select(p => p.Name + ":" + (p.Type.AssemblyQualifiedName ?? p.Type.FullName)));
        return Cache.GetOrAdd(key, _ => Emit(properties, equality));
    }

    private static AggregateRowShape Emit(IReadOnlyList<(string Name, Type Type)> properties, bool equality)
    {
        var id = Interlocked.Increment(ref _next);
        var typeBuilder = Module.DefineType("AggRow" + id, TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class | TypeAttributes.BeforeFieldInit);
        var fields = new FieldBuilder[properties.Count];
        var propertyTypes = new Type[properties.Count];
        for (var i = 0; i < properties.Count; i++) {
            propertyTypes[i] = properties[i].Type;
            fields[i] = typeBuilder.DefineField("_" + properties[i].Name, propertyTypes[i], FieldAttributes.Private);
            var property = typeBuilder.DefineProperty(properties[i].Name, PropertyAttributes.None, propertyTypes[i], null);
            var getter = typeBuilder.DefineMethod(
                "get_" + properties[i].Name, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyTypes[i], Type.EmptyTypes);

            var getterIl = getter.GetILGenerator();
            getterIl.Emit(OpCodes.Ldarg_0);
            getterIl.Emit(OpCodes.Ldfld, fields[i]);
            getterIl.Emit(OpCodes.Ret);
            property.SetGetMethod(getter);
            var setter = typeBuilder.DefineMethod(
                "set_" + properties[i].Name, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null, [propertyTypes[i]]);

            var setterIl = setter.GetILGenerator();
            setterIl.Emit(OpCodes.Ldarg_0);
            setterIl.Emit(OpCodes.Ldarg_1);
            setterIl.Emit(OpCodes.Stfld, fields[i]);
            setterIl.Emit(OpCodes.Ret);
            property.SetSetMethod(setter);
        }

        var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, Type.EmptyTypes);
        var ctorIl = ctor.GetILGenerator();
        ctorIl.Emit(OpCodes.Ldarg_0);
        ctorIl.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);
        ctorIl.Emit(OpCodes.Ret);
        if (equality)
            EmitEquality(typeBuilder, fields, propertyTypes);

        var type = typeBuilder.CreateType()!;
        var bound = type.GetProperties();
        var ordered = new PropertyInfo[properties.Count];
        for (var i = 0; i < properties.Count; i++)
            ordered[i] = bound.Single(p => p.Name == properties[i].Name);

        return new(type, type.GetConstructor(Type.EmptyTypes)!, ordered);
    }

    private static void EmitEquality(TypeBuilder typeBuilder, FieldBuilder[] fields, Type[] propertyTypes)
    {
        var equals = typeBuilder.DefineMethod("Equals", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig, typeof(bool), [typeof(object)]);
        var equalsIl = equals.GetILGenerator();
        var mismatch = equalsIl.DefineLabel();
        equalsIl.Emit(OpCodes.Ldarg_1);
        equalsIl.Emit(OpCodes.Brfalse, mismatch);
        equalsIl.Emit(OpCodes.Ldarg_1);
        equalsIl.Emit(OpCodes.Callvirt, typeof(object).GetMethod(nameof(GetType))!);
        equalsIl.Emit(OpCodes.Ldarg_0);
        equalsIl.Emit(OpCodes.Call, typeof(object).GetMethod(nameof(GetType))!);
        equalsIl.Emit(OpCodes.Bne_Un, mismatch);
        for (var i = 0; i < fields.Length; i++) {
            var comparer = typeof(EqualityComparer<>).MakeGenericType(propertyTypes[i]);
            equalsIl.Emit(OpCodes.Call, comparer.GetProperty(nameof(EqualityComparer<object>.Default))!.GetGetMethod()!);
            equalsIl.Emit(OpCodes.Ldarg_0);
            equalsIl.Emit(OpCodes.Ldfld, fields[i]);
            equalsIl.Emit(OpCodes.Ldarg_1);
            equalsIl.Emit(OpCodes.Castclass, typeBuilder);
            equalsIl.Emit(OpCodes.Ldfld, fields[i]);
            equalsIl.Emit(OpCodes.Callvirt, comparer.GetMethod(nameof(EqualityComparer<object>.Equals), [propertyTypes[i], propertyTypes[i]])!);
            equalsIl.Emit(OpCodes.Brfalse, mismatch);
        }

        equalsIl.Emit(OpCodes.Ldc_I4_1);
        equalsIl.Emit(OpCodes.Ret);
        equalsIl.MarkLabel(mismatch);
        equalsIl.Emit(OpCodes.Ldc_I4_0);
        equalsIl.Emit(OpCodes.Ret);
        typeBuilder.DefineMethodOverride(equals, typeof(object).GetMethod(nameof(object.Equals), [typeof(object)])!);
        var hash = typeBuilder.DefineMethod("GetHashCode", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig, typeof(int), Type.EmptyTypes);
        var hashIl = hash.GetILGenerator();
        hashIl.DeclareLocal(typeof(HashCode));
        hashIl.Emit(OpCodes.Ldloca_S, 0);
        hashIl.Emit(OpCodes.Initobj, typeof(HashCode));
        var add = typeof(HashCode).GetMethods().Single(method => method.Name == nameof(HashCode.Add) && method.IsGenericMethodDefinition && method.GetParameters().Length == 1);
        for (var i = 0; i < fields.Length; i++) {
            hashIl.Emit(OpCodes.Ldloca_S, 0);
            hashIl.Emit(OpCodes.Ldarg_0);
            hashIl.Emit(OpCodes.Ldfld, fields[i]);
            hashIl.Emit(OpCodes.Call, add.MakeGenericMethod(propertyTypes[i]));
        }

        hashIl.Emit(OpCodes.Ldloca_S, 0);
        hashIl.Emit(OpCodes.Call, typeof(HashCode).GetMethod(nameof(HashCode.ToHashCode))!);
        hashIl.Emit(OpCodes.Ret);
        typeBuilder.DefineMethodOverride(hash, typeof(object).GetMethod(nameof(GetHashCode))!);
    }
}

internal sealed class AggregateRowShape(Type type, ConstructorInfo constructor, PropertyInfo[] properties)
{
    public Type Type { get; } = type;

    public ConstructorInfo Constructor { get; } = constructor;

    public PropertyInfo[] Properties { get; } = properties;
}