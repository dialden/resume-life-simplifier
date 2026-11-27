using DialDen.RLS.Tests.Core;
using DialDen.RLS.Tests.Core.Clients.UI;
using DialDen.RLS.Tests.Core.Clients.UI.Pages;

namespace DialDen.RLS.Tests.Fixtures;

public sealed class ScopedUIClient : IUIClient, IAsyncDisposable
{
    private readonly TestScope _scope;
    private bool _disposed;

    internal ScopedUIClient(TestScope scope)
    {
        _scope = scope;
    }

    internal ScenarioDiagnostics Diagnostics => _scope.Environment.Diagnostics;

    private IUIClient Client
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _scope.UIClient;
        }
    }

    public Task<HomePage> OpenHomePage() => Client.OpenHomePage();
    public Task<string?> ReadLoadingLogoPathBeforeBlazorStarts() => Client.ReadLoadingLogoPathBeforeBlazorStarts();
    public Task<string> ReadLoadingLogoAnimationNameBeforeBlazorStarts() =>
        Client.ReadLoadingLogoAnimationNameBeforeBlazorStarts();
    public Task<string> ReadLoadingScreenBackgroundColorBeforeBlazorStarts() =>
        Client.ReadLoadingScreenBackgroundColorBeforeBlazorStarts();
    public Task<string> ReadLoadingLogoFilterBeforeBlazorStarts() => Client.ReadLoadingLogoFilterBeforeBlazorStarts();
    public Task<string> ReadLoadingPercentageBeforeBlazorStarts(int? percentage = null) =>
        Client.ReadLoadingPercentageBeforeBlazorStarts(percentage);
    public Uri PublicWebsiteBaseUrl => Client.PublicWebsiteBaseUrl;
    public Task<string> ReadPublicPageHeadingInLanguage(string path, string language) =>
        Client.ReadPublicPageHeadingInLanguage(path, language);
    public Task<bool> ShowsWorkspaceNavigationAt(string path) => Client.ShowsWorkspaceNavigationAt(path);
    public Task<PublicListingPage> OpenPublicListing(Uri address) => Client.OpenPublicListing(address);
    public Task<PostingPage> OpenPosting(int id) => Client.OpenPosting(id);
    public Result<PostingPage, SystemError> DisplayedPostingPage(int id) => Client.DisplayedPostingPage(id);
    public Result<ApplicantsPage, SystemError> DisplayedApplicantsPage(int id) => Client.DisplayedApplicantsPage(id);
    public Result<VoidValue, SystemError> CheckSharedLiveUpdates() => Client.CheckSharedLiveUpdates();
    public Task<Result<VoidValue, SystemError>> InterruptLiveUpdates() => Client.InterruptLiveUpdates();
    public Result<PostingsPage, SystemError> DisplayedPostingsPage() => Client.DisplayedPostingsPage();

    public ValueTask DisposeAsync()
    {
        _disposed = true;
        return _scope.DisposeAsync();
    }
}
