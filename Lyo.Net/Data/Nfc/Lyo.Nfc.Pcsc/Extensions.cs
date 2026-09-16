using Lyo.Exceptions;
using Lyo.Nfc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Nfc.Pcsc;

/// <summary>Registers <see cref="PcscNfcReader"/> as <see cref="INfcReader"/>.</summary>
public static class Extensions
{
    /// <summary>
    /// Adds a process-wide PC/SC reader. The host needs <c>pcscd</c> on Linux. There is no options object: the caller passes the reader name to <see cref="INfcReader.ConnectAsync"/>.
    /// </summary>
    public static IServiceCollection AddPcscNfc(this IServiceCollection services)
    {
        ArgumentHelpers.ThrowIfNull(services);
        services.TryAddSingleton<INfcReader, PcscNfcReader>();
        return services;
    }
}
