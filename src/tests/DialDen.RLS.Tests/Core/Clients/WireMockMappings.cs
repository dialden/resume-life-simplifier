using System.Collections.Concurrent;
using DialDen.RLS.Tests.Extensions;
using WireMock.Admin.Mappings;
using WireMock.Client;

namespace DialDen.RLS.Tests.Core.Clients;

public sealed class WireMockMappings(IWireMockAdminApi adminApi, string pathPrefix, Uri serverUrl) : IAsyncDisposable
{
    private static readonly ConcurrentDictionary<Uri, SemaphoreSlim> RequestLogLocks = new();
    private readonly SemaphoreSlim _requestLogLock = RequestLogLocks.GetOrAdd(
        new Uri(serverUrl.GetLeftPart(UriPartial.Authority)), static _ => new(1, 1));
    private readonly HashSet<Guid> _mappingIds = [];

    public string PathFor(string path) => pathPrefix + path;

    public async Task PostAsync(MappingModel mapping)
    {
        var id = Guid.NewGuid();
        mapping.Guid = id;
        _mappingIds.Add(id);
        await adminApi.PostMappingAsync(mapping);
    }

    public async Task ResetAsync()
    {
        var cleanup = _mappingIds.Select<Guid, Func<ValueTask>>(id => async () =>
        {
            await adminApi.DeleteMappingAsync(id);
            _mappingIds.Remove(id);
        }).ToList();
        cleanup.Add(async () =>
        {
            // WireMock's shared journal can remove the wrong entry when deletes overlap.
            await _requestLogLock.WaitAsync();
            try
            {
                foreach (var entry in await adminApi.GetRequestsAsync())
                {
                    if (entry.Request?.Path?.StartsWith(pathPrefix + "/", StringComparison.Ordinal) != true)
                        continue;
                    await adminApi.DeleteRequestAsync(entry.Guid);
                }
            }
            finally
            {
                _requestLogLock.Release();
            }
        });
        await TestResourceCleanup.RunAsync(cleanup.ToArray());
    }

    public async ValueTask DisposeAsync() => await ResetAsync();
}
