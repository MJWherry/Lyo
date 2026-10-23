using System.Reflection;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace Lyo.KeyStore.Aws.Tests;

/// <summary>In-memory <see cref="IAmazonSecretsManager" /> covering the calls <see cref="AwsKeyStore" /> makes.</summary>
internal static class MemorySecretsManager
{
    public static (IAmazonSecretsManager Client, MemorySecretsState State) Create()
    {
        var client = DispatchProxy.Create<IAmazonSecretsManager, MemorySecretsProxy>();
        var proxy = (MemorySecretsProxy)client;
        return (client, proxy.State);
    }

    private class MemorySecretsProxy : DispatchProxy
    {
        public MemorySecretsState State { get; } = new();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            var name = targetMethod?.Name;
            switch (name) {
                case nameof(IAmazonSecretsManager.GetSecretValueAsync):
                    return State.GetAsync((GetSecretValueRequest)args![0]!);
                case nameof(IAmazonSecretsManager.CreateSecretAsync):
                    return State.CreateAsync((CreateSecretRequest)args![0]!);
                case nameof(IAmazonSecretsManager.UpdateSecretAsync):
                    return State.UpdateAsync((UpdateSecretRequest)args![0]!);
                case nameof(IDisposable.Dispose):
                    return null;
                default:
                    throw new NotSupportedException(name);
            }
        }
    }
}

internal sealed class MemorySecretsState
{
    private readonly Dictionary<string, List<SecretVersion>> _secrets = new(StringComparer.Ordinal);
    private int _nextVersion;

    public IReadOnlyCollection<string> Names => _secrets.Keys;

    public void Put(string name, string secretString)
    {
        if (!_secrets.TryGetValue(name, out var versions)) {
            versions = [];
            _secrets[name] = versions;
        }

        versions.Add(new(NextVersionId(), secretString));
    }

    public Task<GetSecretValueResponse> GetAsync(GetSecretValueRequest request)
    {
        if (!_secrets.TryGetValue(request.SecretId, out var versions) || versions.Count == 0)
            throw new ResourceNotFoundException("missing");

        SecretVersion version;
        if (!string.IsNullOrEmpty(request.VersionId)) {
            version = versions.FirstOrDefault(item => item.VersionId == request.VersionId) ?? throw new ResourceNotFoundException("missing version");
        }
        else
            version = versions[^1];

        return Task.FromResult(new GetSecretValueResponse { SecretString = version.SecretString, VersionId = version.VersionId, Name = request.SecretId });
    }

    public Task<CreateSecretResponse> CreateAsync(CreateSecretRequest request)
    {
        if (_secrets.ContainsKey(request.Name))
            throw new ResourceExistsException("exists");

        var versionId = NextVersionId();
        _secrets[request.Name] = [new(versionId, request.SecretString)];
        return Task.FromResult(new CreateSecretResponse { VersionId = versionId, Name = request.Name });
    }

    public Task<UpdateSecretResponse> UpdateAsync(UpdateSecretRequest request)
    {
        if (!_secrets.ContainsKey(request.SecretId))
            throw new ResourceNotFoundException("missing");

        var versionId = NextVersionId();
        _secrets[request.SecretId].Add(new(versionId, request.SecretString));
        return Task.FromResult(new UpdateSecretResponse { VersionId = versionId, Name = request.SecretId });
    }

    private string NextVersionId()
    {
        _nextVersion++;
        return _nextVersion.ToString("x32");
    }

    private sealed record SecretVersion(string VersionId, string SecretString);
}