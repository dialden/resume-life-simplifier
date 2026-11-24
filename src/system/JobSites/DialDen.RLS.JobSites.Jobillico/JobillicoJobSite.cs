using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace DialDen.RLS.JobSites.Jobillico;

internal sealed class JobillicoJobSite(IHttpClientFactory clients, JobillicoOptions options) : IJobSite
{
    private static readonly Regex EducationPattern = new(
        @"^\s*(?<degree>certificat universitaire|baccalauréat|baccalaureat|maîtrise|maitrise|doctorat|postdoctorat|post-doctorat|bachelor's|bachelor’s|bachelor|master's|master’s|master|doctorate|phd|des|aep|dep|asp|aec|dec|cer|bac|dess|dss|mas|pos)(?:\s+(?:en|in|of)\s+(?<subject>[\p{L}\p{M}\p{N}\s&<>'’\-]+))?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
    private static readonly Regex SubjectWords = new(
        @"[\p{L}\p{M}\p{N}]+",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
    private static readonly HashSet<string> AmbiguousWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "ou", "or", "et", "and", "non", "not", "no", "pas", "sans", "without", "aucun", "aucune",
        "équivalent", "équivalente", "equivalent", "equivalente", "expérience", "experience",
        "requis", "requise", "required", "souhaité", "souhaitée", "souhaite", "souhaitee",
        "preferred", "optional", "facultatif", "facultative", "atout", "asset", "préférable", "preferable",
        "peut", "remplacé", "remplacée", "remplace", "remplacee", "cours", "inachevé", "inachevée",
        "des", "aep", "dep", "asp", "aec", "dec", "cer", "bac", "dess", "dss", "mas", "phd", "pos",
        "baccalauréat", "baccalaureat", "bachelor", "maîtrise", "maitrise", "master", "doctorat",
        "doctorate", "postdoctorat", "diplôme", "diplome", "diploma", "degree", "certificat", "certificate"
    };

    public string Id => "jobillico";

    public string Name => "Jobillico";

    public async Task Publish(JobSitePosting posting, CancellationToken cancellationToken)
    {
        var publicListingUrl = posting.PublicListingUrl;
        var education = posting.RequiredEducation;
        var diploma = string.IsNullOrWhiteSpace(education) ? null : InferDiploma(education);
        var request = new XElement("request",
            new XElement("bsandboxoff", options.UseProduction ? "1" : "0"),
            new XElement("oauth",
                new XElement("sparam", options.Username),
                new XElement("sparam", options.Password)),
            new XElement("ocreator", new XElement("sparam", options.Username)),
            new XElement("job",
                new XElement("elang", "en"),
                new XElement("irecruiter", options.RecruiterId),
                new XElement("stitle_en", posting.Title),
                new XElement("srefnumber", posting.PostingId.ToString(CultureInfo.InvariantCulture)),
                publicListingUrl is null ? null : new XElement("surl_fr", publicListingUrl.AbsoluteUri),
                publicListingUrl is null ? null : new XElement("surl_en", publicListingUrl.AbsoluteUri),
                new XElement("olocations", new XElement("ilocation", options.LocationId)),
                new XElement("sdescription_en", posting.Description),
                string.IsNullOrWhiteSpace(education) ? null : new XElement("oeducationcriteria",
                    diploma is null ? null : new XElement("evalue", diploma.Value.Level),
                    diploma is null ? null : new XElement("ediploma", diploma.Value.Diploma),
                    new XElement("bcleared", "1")),
                string.IsNullOrWhiteSpace(education) ? null : new XElement("sskills_en", education)));
        using var content = new StringContent(
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + request.ToString(SaveOptions.DisableFormatting),
            Encoding.UTF8, "application/xml");
        using var client = clients.CreateClient("Jobillico");
        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync(options.Endpoint, content, cancellationToken);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("Jobillico did not respond within the HTTP timeout.", exception);
        }
        using (response)
        {
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                Async = true,
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = 1_000_000
            });
            var results = await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken);
            if (results.Descendants("error").Any())
            {
                throw new InvalidOperationException("Jobillico rejected the posting.");
            }
            var jobs = results.Root?.Name == "results" ? results.Root.Elements("job").ToArray() : [];
            if (jobs.Length != 1 || !long.TryParse(jobs[0].Element("ijob")?.Value,
                    NumberStyles.None, CultureInfo.InvariantCulture, out var jobId) || jobId <= 0)
            {
                throw new InvalidOperationException("Jobillico returned no valid job identifier.");
            }
        }
    }

    private static (string Level, string Diploma)? InferDiploma(string education)
    {
        var match = EducationPattern.Match(education);
        if (!match.Success)
            return null;

        var subject = match.Groups["subject"];
        if (subject.Success)
        {
            var words = SubjectWords.Matches(subject.Value);
            if (words.Count == 0 || words.Any(word => AmbiguousWords.Contains(word.Value)))
                return null;
        }

        return match.Groups["degree"].Value.ToUpperInvariant() switch
        {
            "DES" => ("HIS", "DES"),
            "AEP" => ("PRO", "AEP"),
            "DEP" => ("PRO", "DEP"),
            "ASP" => ("PRO", "ASP"),
            "AEC" => ("COL", "AEC"),
            "DEC" => ("COL", "DEC"),
            "CER" or "CERTIFICAT UNIVERSITAIRE" => ("UNI", "CER"),
            "BAC" or "BACCALAURÉAT" or "BACCALAUREAT" or "BACHELOR" or "BACHELOR'S" or "BACHELOR’S" => ("UNI", "BAC"),
            "DESS" or "DSS" => ("UNI", "DSS"),
            "MAS" or "MAÎTRISE" or "MAITRISE" or "MASTER" or "MASTER'S" or "MASTER’S" => ("UNI", "MAS"),
            "PHD" or "DOCTORAT" or "DOCTORATE" => ("UNI", "PHD"),
            "POS" or "POSTDOCTORAT" or "POST-DOCTORAT" => ("UNI", "POS"),
            _ => null
        };
    }
}
