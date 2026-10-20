using Lyo.FileStorage.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FileStorage;

/// <summary>
/// Optional companion to <see cref="IFileStorageStoreProvider" />. Implement this when a configuration section must also register keyed services, such as multipart, that close
/// over the same options and client as the store.
/// </summary>
public interface IFileStorageProviderServices
{
    /// <summary>Builds the store delegate and registers keyed services for <paramref name="storeName" />.</summary>
    Func<IServiceProvider, IFileStorageService> Create(IServiceCollection services, string storeName, IConfiguration section);
}
