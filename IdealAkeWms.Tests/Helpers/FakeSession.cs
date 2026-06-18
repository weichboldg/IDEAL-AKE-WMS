using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace IdealAkeWms.Tests.Helpers;

/// <summary>Minimaler ISession-Fake mit Dictionary-Backing fuer Middleware-/Service-Tests.</summary>
public sealed class FakeSession : ISession
{
    private readonly Dictionary<string, byte[]> _data = new();

    public bool IsAvailable => true;
    public string Id => "fake-session";
    public IEnumerable<string> Keys => _data.Keys;

    public void Set(string key, byte[] value) => _data[key] = value;
    public bool TryGetValue(string key, out byte[] value) => _data.TryGetValue(key, out value!);
    public void Remove(string key) => _data.Remove(key);
    public void Clear() => _data.Clear();
    public void Load() { }
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Commit() { }
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

/// <summary>ISessionFeature-Stub, der eine <see cref="FakeSession"/> bereitstellt.</summary>
public sealed class SessionFeatureStub : ISessionFeature
{
    public ISession Session { get; set; } = new FakeSession();
}
