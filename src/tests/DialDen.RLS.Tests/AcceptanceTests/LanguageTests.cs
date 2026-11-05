using DialDen.RLS.Tests.Core.Drivers.RLS;
using DialDen.RLS.Tests.Core.Dsl.Scenario;
using DialDen.RLS.Tests.Fixtures;

namespace DialDen.RLS.Tests.AcceptanceTests;

[Collection(RLSTestCollection.Name)]
public class LanguageTests
{
    private const string MissingDescriptionInFrench = "Une description d'offre est requise.";
    private const string MissingDescriptionInEnglish = "A posting description is required.";

    private readonly TestEnvironment _testEnvironment;

    public LanguageTests(TestEnvironments testEnvironments)
    {
        _testEnvironment = testEnvironments.For(ExternalSystems.Stubbed);
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task WorkspaceUsesFrenchByDefault(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().NoLanguageChosen()
            .When().CreatePosting().WithDescription("")
            .Then().ShouldFail().MessageOrFieldErrorMessage("description", MissingDescriptionInFrench);
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task WorkspaceUsesTheChosenEnglishLanguage(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().Language(Language.English)
            .When().CreatePosting().WithDescription("")
            .Then().ShouldFail().MessageOrFieldErrorMessage("description", MissingDescriptionInEnglish);
    }

    [Trait("Category", "Acceptance")]
    [Theory, CombinatorialData]
    public async Task WorkspaceCanSwitchBackToFrench(Channel channel)
    {
        await _testEnvironment.Scenario(channel)
            .Given().Language(Language.English)
            .And().Language(Language.French)
            .When().CreatePosting().WithDescription("")
            .Then().ShouldFail().MessageOrFieldErrorMessage("description", MissingDescriptionInFrench);
    }

    [Trait("Category", "Acceptance")]
    [Fact]
    public async Task ApiConflictMessagesAreInFrench()
    {
        await _testEnvironment.Scenario(Channel.API)
            .Given().Language(Language.French)
            .And().Posting().WithId("opportunity").PublishedTo()
            .When().PublishPosting().WithId("opportunity")
            .Then().ShouldFail().ErrorMessage("Cette offre ne peut pas être publiée à nouveau.");
    }

    [Trait("Category", "Acceptance")]
    [Fact]
    public async Task ApiJobSiteSelectionMessagesAreInFrench()
    {
        await _testEnvironment.Scenario(Channel.API)
            .Given().Language(Language.French)
            .And().Posting()
            .When().PublishPosting().ToJobSites("unknown")
            .Then().ShouldFail().FieldErrorMessage(
                "jobSiteIds", "Sélectionnez des sites d'emploi distincts et disponibles.");
    }
}
