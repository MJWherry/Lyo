namespace Lyo.Diagnostic.Classification;

/// <summary>High-level taxonomy of .NET exception types.</summary>
public enum ExceptionKind
{
    /// <summary>Null dereference or missing object.</summary>
    NullReference,

    /// <summary>Cast or type conversion failure.</summary>
    TypeMismatch,

    /// <summary>Collection index out of range or key not found.</summary>
    CollectionAccess,

    /// <summary>Race condition, deadlock, or concurrent modification.</summary>
    Concurrency,

    /// <summary>File, stream, network, or database I/O failure.</summary>
    IO,

    /// <summary>JSON, XML, binary, or other serialisation failure.</summary>
    Serialisation,

    /// <summary>Operation or request timeout.</summary>
    Timeout,

    /// <summary>Authentication or authorisation failure.</summary>
    Auth,

    /// <summary>Memory pressure or allocation failure.</summary>
    Memory,

    /// <summary>Stack overflow from unbounded recursion.</summary>
    StackOverflow,

    /// <summary>Argument validation failure.</summary>
    ArgumentValidation,

    /// <summary>Operation was cancelled via a <see cref="System.Threading.CancellationToken" />.</summary>
    Cancellation,

    /// <summary>Database query or connection failure.</summary>
    Database,

    /// <summary>HTTP or network communication failure.</summary>
    Network,

    /// <summary>Exception type did not match any known category.</summary>
    Unknown
}