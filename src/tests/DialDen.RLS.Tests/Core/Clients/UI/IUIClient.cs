using DialDen.RLS.Tests.Core.Clients.UI.Pages;

namespace DialDen.RLS.Tests.Core.Clients.UI;

public interface IUIClient
{
    Task<HomePage> OpenHomePage();
    Task<string?> ReadLoadingLogoPathBeforeBlazorStarts();
    Task<string> ReadLoadingLogoAnimationNameBeforeBlazorStarts();
    Task<string> ReadLoadingScreenBackgroundColorBeforeBlazorStarts();
    Task<string> ReadLoadingLogoFilterBeforeBlazorStarts();
    Task<string> ReadLoadingPercentageBeforeBlazorStarts(int? percentage = null);
    Uri PublicWebsiteBaseUrl { get; }
    Task<string> ReadPublicPageHeadingInLanguage(string path, string language);
    Task<bool> ShowsWorkspaceNavigationAt(string path);
    Task<PublicListingPage> OpenPublicListing(Uri address);
    Task<PostingPage> OpenPosting(int id);
    Result<PostingPage, SystemError> DisplayedPostingPage(int id);
    Result<ApplicantsPage, SystemError> DisplayedApplicantsPage(int id);
    Result<VoidValue, SystemError> CheckSharedLiveUpdates();
    Task<Result<VoidValue, SystemError>> InterruptLiveUpdates();
    Result<PostingsPage, SystemError> DisplayedPostingsPage();
}
