namespace Lyo.Common.Core;

/// <summary>Thrown when a context read fails, a frozen bag is changed, or a factory reads the key it is computing.</summary>
public sealed class ContextBagException(string message) : Exception(message);
