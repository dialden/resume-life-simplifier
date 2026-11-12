using System.Net.Http.Headers;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using System.Net.Mime;
namespace DialDen.RLS.UI.Notifications;

public sealed class NotificationStreamClient(HttpClient api) : IDisposable
{
    public void Dispose() => api.Dispose();

    public async IAsyncEnumerable<NotificationSignal> ReadAsync(
        string href,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, href);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        request.SetBrowserResponseStreamingEnabled(true);

        using var response = await api.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        if (!string.Equals(
                response.Content.Headers.ContentType?.MediaType,
                MediaTypeNames.Text.EventStream,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The notification endpoint did not return an event stream.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await foreach (var item in SseParser.Create(stream).EnumerateAsync(cancellationToken))
        {
            switch (item.EventType)
            {
                case "ready":
                    yield return NotificationSignal.Ready;
                    break;
                case "notifications-changed":
                    yield return NotificationSignal.Changed;
                    break;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        throw new IOException("The notification stream ended.");
    }
}
