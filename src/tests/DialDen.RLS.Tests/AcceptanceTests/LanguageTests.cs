using DialDen.RLS.Tests.Core.Drivers.RLS;
using DialDen.RLS.Tests.Core.Dsl.Scenario;
using DialDen.RLS.Tests.Fixtures;

namespace DialDen.RLS.Tests.AcceptanceTests;

[Collection(RLSTestCollection.Name)]
public class LanguageTests
{
    private const string MissingDescriptionInFrench = "Une description d'offre est requise.";

    private readonly TestEnvironment _testEnvironment;

    public LanguageTests(TestEnvironments testEnvironments)
    {
        _testEnvironment = testEnvironments.For(ExternalSystems.Stubbed);
    }

    [Trait("Category", "Acceptance")]
    [Fact]
    public async Task WorkspaceIsInFrenchByDefault()
    {
        await _testEnvironment.Scenario(Channel.UI)
            .Given().NoLanguageChosen()
            .When().ViewOverview()
            .Then().ShouldSucceed()
                .And().Overview().HasTitle("Aperçu");
    }

    [Trait("Category", "Acceptance")]
    [Fact]
    public async Task CanSwitchTheWorkspaceToEnglish()
    {
        await _testEnvironment.Scenario(Channel.UI)
            .Given().Language(Language.English)
            .When().ViewOverview()
            .Then().ShouldSucceed()
                .And().Overview().HasTitle("Overview");
    }

    [Trait("Category", "Acceptance")]
    [Fact]
    public async Task CanSwitchTheWorkspaceBackToFrench()
    {
        await _testEnvironment.Scenario(Channel.UI)
            .Given().Language(Language.English)
            .And().Language(Language.French)
            .When().ViewOverview()
            .Then().ShouldSucceed()
                .And().Overview().HasTitle("Aperçu");
    }

    [Trait("Category", "Acceptance")]
    [Fact]
    public async Task ValidationMessagesAreInFrenchByDefault()
    {
        await _testEnvironment.Scenario(Channel.API)
            .Given().NoLanguageChosen()
            .When().CreatePosting().WithDescription("")
            .Then().ShouldFail().FieldErrorMessage("description", MissingDescriptionInFrench);
    }

    [Trait("Category", "Acceptance")]
    [Fact]
    public async Task ValidationMessagesFollowTheChosenLanguage()
    {
        await _testEnvironment.Scenario(Channel.API)
            .Given().Language(Language.French)
            .When().CreatePosting().WithDescription("")
            .Then().ShouldFail().FieldErrorMessage("description", MissingDescriptionInFrench);
    }

    [Trait("Category", "Acceptance")]
    [Fact]
    public async Task ValidationMessagesAreShownInFrenchByDefaultThroughUI()
    {
        await _testEnvironment.Scenario(Channel.UI)
            .Given().NoLanguageChosen()
            .When().CreatePosting().WithDescription("")
            .Then().ShouldFail().ErrorMessage(MissingDescriptionInFrench);
    }

    [Trait("Category", "Acceptance")]
    [Fact]
    public async Task ConflictMessagesAreInFrench()
    {
        await _testEnvironment.Scenario(Channel.API)
            .Given().Language(Language.French)
            .And().Posting().WithId("opportunity").PublishedTo()
            .When().PublishPosting().WithId("opportunity")
            .Then().ShouldFail().ErrorMessage("Cette offre ne peut pas être publiée à nouveau.");
    }

    [Trait("Category", "Acceptance")]
    [Fact]
    public async Task JobSiteSelectionMessagesAreInFrench()
    {
        await _testEnvironment.Scenario(Channel.API)
            .Given().Language(Language.French)
            .And().Posting()
            .When().PublishPosting().ToJobSites("unknown")
            .Then().ShouldFail().FieldErrorMessage(
                "jobSiteIds", "Sélectionnez des sites d'emploi distincts et disponibles.");
    }
}
