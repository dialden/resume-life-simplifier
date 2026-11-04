using System.Globalization;

namespace DialDen.RLS.UI.Localization;

/// <summary>Lets the API answer in the language the workspace is displayed in.</summary>
public sealed class AcceptLanguageHandler(HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler)
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.AcceptLanguage.Clear();
        request.Headers.AcceptLanguage.Add(new(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName));
        return base.SendAsync(request, cancellationToken);
    }
}
