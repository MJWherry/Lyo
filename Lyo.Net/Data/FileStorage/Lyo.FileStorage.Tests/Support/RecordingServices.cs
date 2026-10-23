using System.Reflection;
using System.Text;
using Lyo.Compression;
using Lyo.Compression.Models;
using Lyo.Encryption.TwoKey;

namespace Lyo.FileStorage.Tests;

internal static class Recording
{
    public static Counter<ITwoKeyEncryptionService> Encryption()
    {
        var proxy = DispatchProxy.Create<ITwoKeyEncryptionService, CounterProxy>();
        var counter = (CounterProxy)proxy;
        counter.Kind = CounterProxy.ProxyKind.Encryption;
        return new(proxy, counter);
    }

    public static Counter<ICompressionService> Compression()
    {
        var resolver = DispatchProxy.Create<ICompressionResolver, CounterProxy>();
        var service = DispatchProxy.Create<ICompressionService, CounterProxy>();
        var counter = (CounterProxy)service;
        counter.Kind = CounterProxy.ProxyKind.Compression;
        counter.Resolver = resolver;
        ((CounterProxy)resolver).Kind = CounterProxy.ProxyKind.Resolver;
        ((CounterProxy)resolver).Owner = counter;
        return new(service, counter);
    }

    internal sealed class Counter<T>(T service, CounterProxy proxy)
        where T : class
    {
        public T Service { get; } = service;

        public int Calls => proxy.Calls;
    }

    internal class CounterProxy : DispatchProxy
    {
        public int Calls;

        public ProxyKind Kind;

        public CounterProxy? Owner;

        public ICompressionResolver? Resolver;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            var name = targetMethod?.Name;
            if (name == "get_Resolver")
                return Resolver;

            if (name == "get_Algorithm")
                return CompressionAlgorithm.GZip;

            if (name == "get_FileExtension")
                return Kind == ProxyKind.Encryption ? ".enc" : ".gz";

            if (name is "get_AlgorithmSelector" or "get_DekAlgorithm" or "get_KekAlgorithm")
                return null;

            if (name is "GetEncryptionEncoding" or "GetDecryptionEncoding")
                return Encoding.UTF8;

            if (name == "ResolveForCompress")
                return new CompressionSelectionResult(true, CompressionAlgorithm.GZip);

            if (name is "EncryptToStreamAsync" or "CompressAsync") {
                (Owner ?? this).Calls++;
                if (args is { Length: >= 2 } && args[0] is Stream input && args[1] is Stream output)
                    input.CopyTo(output);

                return Task.CompletedTask;
            }

            var returnsTask = targetMethod?.ReturnType == typeof(Task) ||
                (targetMethod?.ReturnType.IsGenericType == true && targetMethod.ReturnType.GetGenericTypeDefinition() == typeof(Task<>));

            if (returnsTask)
                return Task.CompletedTask;

            return targetMethod?.ReturnType == typeof(void) ? null : null;
        }

        internal enum ProxyKind
        {
            Encryption,
            Compression,
            Resolver
        }
    }
}