#if NETSTANDARD2_0
// ReSharper disable once CheckNamespace
namespace System.Diagnostics.CodeAnalysis;

/// <summary>Specifies that an output is not null even if the corresponding type allows it.</summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue)]
internal sealed class NotNullAttribute : Attribute { }

/// <summary>Specifies that a method or property never returns normally.</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
internal sealed class DoesNotReturnAttribute : Attribute { }
#endif