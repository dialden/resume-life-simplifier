using DialDen.RLS.Abstractions.Postings;

namespace DialDen.RLS.Postings;

public sealed record Posting(int Id, string Title, PostingStatus Status, string? RequiredEducation)
{
    public string Description { get; init; } = "";
    public string? RequiredEducation { get; init; } = NormalizeEducation(RequiredEducation);

    public Posting Publish()
    {
        if (Status == PostingStatus.Published)
        {
            throw new PostingAlreadyPublishedException();
        }

        return this with { Status = PostingStatus.Published };
    }

    private static string? NormalizeEducation(string? requiredEducation) =>
        string.IsNullOrWhiteSpace(requiredEducation) ? null : requiredEducation;
}
