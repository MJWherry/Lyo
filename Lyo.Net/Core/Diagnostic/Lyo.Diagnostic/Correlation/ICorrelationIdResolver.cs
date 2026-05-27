namespace Lyo.Diagnostic.Correlation;

/// <summary>
/// Resolves the current ambient correlation id for outbound HTTP calls, structured-log enrichment, and audit recording. Implementations are responsible for deciding where to source
/// the id (inbound headers, <see cref="System.Diagnostics.Activity"/>, ASP.NET <c>HttpContext.TraceIdentifier</c>, freshly-minted GUID for the no-context case, ...). The interface
/// itself carries no parameters so any callsite that holds an <c>IServiceProvider</c> can ask for the id without having to know which host it's running in.
/// </summary>
public interface ICorrelationIdResolver
{
    /// <summary>Returns a non-empty correlation id. Implementations MUST never return <c>null</c> or whitespace; mint a fresh id rather than returning nothing.</summary>
    string Resolve();
}
