using Lyo.Exceptions;

namespace Lyo.Diagnostic.Classification;

/// <summary>Classifies exceptions into a diagnostic taxonomy with severity and remediation hints. Thread-safe singleton.</summary>
public sealed class ExceptionClassifier : IExceptionClassifier
{
    // Each rule: (substring to match in type name, kind, severity, label, hint, isControlFlow)
    private static readonly (string Match, ExceptionKind Kind, ExceptionSeverity Severity, string Label, string Hint, bool IsControlFlow)[] Rules = [
        ("NullReferenceException", ExceptionKind.NullReference, ExceptionSeverity.High, "Null Reference",
            "An object was used before being initialised. Check for null guards or missing dependency injection registrations.", false),
        ("ArgumentNullException", ExceptionKind.ArgumentValidation, ExceptionSeverity.Medium, "Null Argument",
            "A required argument was null. Add null-checks or use nullable reference types to enforce contracts.", false),
        ("ArgumentOutOfRangeException", ExceptionKind.ArgumentValidation, ExceptionSeverity.Medium, "Argument Out of Range",
            "An argument fell outside its valid range. Validate inputs before passing them to this method.", false),
        ("ArgumentException", ExceptionKind.ArgumentValidation, ExceptionSeverity.Medium, "Invalid Argument",
            "An argument was invalid. Review the calling code and add input validation.", false),
        ("InvalidCastException", ExceptionKind.TypeMismatch, ExceptionSeverity.High, "Invalid Cast",
            "A type cast failed at runtime. Use pattern matching ('is' / 'as') instead of direct casts.", false),
        ("InvalidOperationException", ExceptionKind.TypeMismatch, ExceptionSeverity.Medium, "Invalid Operation",
            "An operation was called in an invalid state. Check preconditions and object lifecycle.", false),
        ("IndexOutOfRangeException", ExceptionKind.CollectionAccess, ExceptionSeverity.High, "Index Out of Range",
            "An array or list was accessed with an out-of-bounds index. Add bounds checking.", false),
        ("KeyNotFoundException", ExceptionKind.CollectionAccess, ExceptionSeverity.Medium, "Key Not Found", "A dictionary key was missing. Use TryGetValue instead of the indexer.",
            false),
        ("StackOverflowException", ExceptionKind.StackOverflow, ExceptionSeverity.Critical, "Stack Overflow",
            "Unbounded recursion detected. Add a base case or convert to an iterative algorithm.", false),
        ("OutOfMemoryException", ExceptionKind.Memory, ExceptionSeverity.Critical, "Out of Memory",
            "The process exhausted available memory. Check for large allocations, memory leaks, or infinite buffering.", false),
        ("OperationCanceledException", ExceptionKind.Cancellation, ExceptionSeverity.Low, "Operation Cancelled",
            "The operation was cancelled via a CancellationToken. This is often expected; verify cancellation is handled gracefully.", true),
        ("TaskCanceledException", ExceptionKind.Cancellation, ExceptionSeverity.Low, "Task Cancelled",
            "An async task was cancelled. Ensure cancellation tokens are propagated and awaited tasks are wrapped appropriately.", true),
        ("TimeoutException", ExceptionKind.Timeout, ExceptionSeverity.High, "Timeout",
            "An operation exceeded its time limit. Review timeout configuration and the latency of downstream dependencies.", false),
        ("UnauthorizedAccessException", ExceptionKind.Auth, ExceptionSeverity.High, "Unauthorised Access",
            "Access was denied. Verify that the calling identity has the required permissions.", false),
        ("AuthenticationException", ExceptionKind.Auth, ExceptionSeverity.High, "Authentication Failure",
            "Authentication failed. Check credentials, token expiry, and certificate validity.", false),
        ("SecurityException", ExceptionKind.Auth, ExceptionSeverity.High, "Security Exception",
            "A security policy was violated. Review permission grants and code access security configuration.", false),

        // I/O
        ("FileNotFoundException", ExceptionKind.IO, ExceptionSeverity.High, "File Not Found",
            "A required file was missing. Verify file paths, deployment artifacts, and file system permissions.", false),
        ("DirectoryNotFoundException", ExceptionKind.IO, ExceptionSeverity.High, "Directory Not Found",
            "A required directory was missing. Ensure the directory exists and the process has access.", false),
        ("IOException", ExceptionKind.IO, ExceptionSeverity.High, "I/O Error",
            "An I/O operation failed. Check file system permissions, disk space, and network share availability.", false),

        // Serialisation
        ("JsonException", ExceptionKind.Serialisation, ExceptionSeverity.Medium, "JSON Error",
            "JSON parsing or serialisation failed. Validate the payload schema against the expected contract.", false),
        ("JsonSerializationException", ExceptionKind.Serialisation, ExceptionSeverity.Medium, "JSON Serialisation Error",
            "Newtonsoft.Json could not serialise or deserialise the object. Check for missing constructors or incompatible types.", false),
        ("XmlException", ExceptionKind.Serialisation, ExceptionSeverity.Medium, "XML Error",
            "XML parsing failed. Validate the document is well-formed and matches the expected schema.", false),
        ("SerializationException", ExceptionKind.Serialisation, ExceptionSeverity.Medium, "Serialisation Error",
            "Binary or custom serialisation failed. Check for non-serialisable types or version mismatches.", false),

        // Concurrency
        ("AggregateException", ExceptionKind.Concurrency, ExceptionSeverity.High, "Aggregate Exception",
            "Multiple exceptions occurred, often from parallel or async operations. Inspect InnerExceptions for root causes.", false),
        ("SemaphoreFullException", ExceptionKind.Concurrency, ExceptionSeverity.High, "Semaphore Full",
            "A semaphore was released more times than acquired. Review locking logic for mismatched acquire/release calls.", false),
        ("ObjectDisposedException", ExceptionKind.Concurrency, ExceptionSeverity.High, "Object Disposed",
            "A disposed object was accessed, often due to a race condition or incorrect lifetime management. Check DI scopes and using blocks.", false),

        // Database
        ("DbUpdateException", ExceptionKind.Database, ExceptionSeverity.High, "Database Update Error",
            "EF Core failed to persist changes. Check for constraint violations, missing migrations, or connection issues.", false),
        ("DbUpdateConcurrencyException", ExceptionKind.Database, ExceptionSeverity.Medium, "Concurrency Conflict",
            "A concurrency conflict occurred during database update. Implement optimistic concurrency handling with retry logic.", false),
        ("SqlException", ExceptionKind.Database, ExceptionSeverity.High, "SQL Error",
            "A SQL Server error occurred. Check the SQL error number, connection string, and database permissions.", false),
        ("PostgresException", ExceptionKind.Database, ExceptionSeverity.High, "Postgres Error",
            "A PostgreSQL error occurred. Check the error code, query, and connection configuration.", false),

        // Network
        ("HttpRequestException", ExceptionKind.Network, ExceptionSeverity.High, "HTTP Request Error",
            "An HTTP request failed. Verify the downstream service URL, health, and network connectivity.", false),
        ("SocketException", ExceptionKind.Network, ExceptionSeverity.High, "Socket Error",
            "A low-level socket operation failed. Check network connectivity, firewall rules, and port availability.", false),
        ("WebException", ExceptionKind.Network, ExceptionSeverity.High, "Web Exception",
            "A network-level web request failed. Check endpoint availability, SSL certificates, and proxy configuration.", false)
    ];

    private readonly ExceptionClassifierOptions _options;

    public ExceptionClassifier(ExceptionClassifierOptions? options = null) => _options = options ?? ExceptionClassifierOptions.Default;

    /// <inheritdoc />
    public ClassifiedExceptionResult Classify(Exception exception)
    {
        ArgumentHelpers.ThrowIfNull(exception);
        return ClassifyByTypeName(exception.GetType().Name);
    }

    /// <inheritdoc />
    public ClassifiedExceptionResult ClassifyByTypeName(string exceptionTypeName)
    {
        ArgumentHelpers.ThrowIfNull(exceptionTypeName);
        foreach (var i in _options.CustomMappings) {
            if (exceptionTypeName.Contains(i.Key, StringComparison.OrdinalIgnoreCase))
                return BuildCustomResult(i.Value, exceptionTypeName);
        }

        foreach (var rule in Rules) {
            if (exceptionTypeName.Contains(rule.Match, StringComparison.OrdinalIgnoreCase))
                return new(rule.Kind, rule.Severity, rule.Label, rule.Hint, rule.IsControlFlow, exceptionTypeName);
        }

        return new(ExceptionKind.Unknown, ExceptionSeverity.Medium, "Unknown Exception", "Review the exception message and stack trace for context.", false, exceptionTypeName);
    }

    private static ClassifiedExceptionResult BuildCustomResult(ExceptionKind kind, string typeName)
        => new(kind, ExceptionSeverity.Medium, kind.ToString(), "Review the custom exception for context.", false, typeName);
}