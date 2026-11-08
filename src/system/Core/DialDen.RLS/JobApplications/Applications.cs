using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using DialDen.RLS.Abstractions.Applications;
using DialDen.RLS.Abstractions.Postings;
using DialDen.RLS.Postings;

namespace DialDen.RLS.JobApplications;

internal sealed class Applications(
    IApplicationsArchive archive, IPostingsArchive postings, IJobSitePublications publications) : IApplications
{
    private const int MaxResumeBytes = 5 * 1024 * 1024;
    private const string Source = "Client website";

    public async Task<int?> Submit(SubmitApplicationRequest request, CancellationToken cancellationToken)
    {
        var posting = await postings.FindById(request.PostingId, cancellationToken);
        if (posting is not { Status: PostingStatus.Published })
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200)
            throw new InvalidApplicationException("name");
        if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Trim().Length > 254
            || !new EmailAddressAttribute().IsValid(request.Email.Trim()))
            throw new InvalidApplicationException("email");

        var extension = Path.GetExtension(request.ResumeFileName);
        var pdf = extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase)
            && request.Resume.AsSpan().StartsWith("%PDF-"u8);
        var docx = extension.Equals(".docx", StringComparison.OrdinalIgnoreCase)
            && IsWordDocument(request.Resume);
        if (request.Resume.Length is 0 or > MaxResumeBytes || (!pdf && !docx))
            throw new InvalidApplicationException("resume");

        var referral = request.ReferralJobSiteId is null
            ? null
            : await publications.Find(request.PostingId, request.ReferralJobSiteId, cancellationToken);
        var application = new Application(
            0, request.PostingId, request.Name.Trim(), request.Email.Trim(),
            pdf ? "resume.pdf" : "resume.docx", request.Resume, Source, referral?.JobSiteId);
        return await archive.Add(application, cancellationToken);
    }

    public async Task<IReadOnlyList<ApplicationResponse>?> List(int postingId, CancellationToken cancellationToken)
    {
        if (await postings.FindById(postingId, cancellationToken) is null)
            return null;
        var applications = await archive.List(postingId, cancellationToken);
        return applications.Select(application => new ApplicationResponse(
            application.Id, application.Name, application.Email, application.Source, application.ReferralJobSiteId)).ToArray();
    }

    public async Task<ApplicationResponse?> Find(int postingId, int applicationId, CancellationToken cancellationToken)
    {
        var application = await archive.Find(postingId, applicationId, cancellationToken);
        return application is null ? null : new ApplicationResponse(
            application.Id, application.Name, application.Email, application.Source, application.ReferralJobSiteId);
    }

    public async Task<ResumeResponse?> Resume(int postingId, int applicationId, CancellationToken cancellationToken)
    {
        var application = await archive.Find(postingId, applicationId, cancellationToken);
        return application is null ? null : new ResumeResponse(application.ResumeFileName, application.Resume);
    }

    private static bool IsWordDocument(byte[] content)
    {
        if (content.Length > MaxResumeBytes || !content.AsSpan().StartsWith("PK\x03\x04"u8))
            return false;
        try
        {
            using var stream = new MemoryStream(content, writable: false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            return archive.GetEntry("[Content_Types].xml") is not null
                && archive.GetEntry("word/document.xml") is not null;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }
}
